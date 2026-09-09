using EmergencyResponse.Core.Assignment;
using EmergencyResponse.Core.Coordination;
using EmergencyResponse.Core.Exceptions;
using EmergencyResponse.Core.Incidents;
using EmergencyResponse.Core.Responders;
using EmergencyResponse.Core.Search;
using Raylib_cs;

namespace EmergencyResponse.RaylibUi;

/// <summary>
/// The whole screen: roster on the left, incident board on the right, controls
/// along the bottom.
/// </summary>
/// <remarks>
/// Talks to <see cref="CommandCentre"/> and nothing else. Every rule it obeys,
/// including who may be sent where, comes from the domain; this class only
/// draws and forwards clicks.
/// </remarks>
internal sealed class DispatchBoard
{
    private const int Width = 1180;
    private const int Height = 720;

    private readonly CommandCentre centre;
    private string message = "Pick a responder to send them by name, or dispatch by policy.";
    private Color messageTint = Theme.TextDim;
    private int calloutCount;
    private Responder? selected;
    private float scroll;
    private bool followNewest;

    /// <summary>Creates the board over a command centre.</summary>
    /// <param name="centre">The centre to drive.</param>
    internal DispatchBoard(CommandCentre centre)
    {
        this.centre = centre;

        // A resolution callback, same contract the console host uses.
        centre.AddResolutionCallback(incident =>
            Say($"Closed: {incident.Description}", Theme.Good));
    }

    /// <summary>The window size this board expects.</summary>
    internal static (int Width, int Height) WindowSize => (Width, Height);

    /// <summary>Draws one frame and handles whatever the mouse did.</summary>
    internal void Draw()
    {
        Raylib.ClearBackground(Theme.Background);

        Ui.Text("EMERGENCY RESPONSE", 24, 20, 24, Theme.Text);
        Ui.Text("dispatch board", 268, 30, 14, Theme.TextDim);

        DrawRoster();
        DrawIncidents();
        DrawFooter();
    }

    /// <summary>The left column: who is on shift and what they can do.</summary>
    private void DrawRoster()
    {
        Ui.Text($"ROSTER  ({centre.AvailableResponders.Count} free)", 24, 70, 14, Theme.Accent);

        int y = 96;
        foreach (Responder responder in centre.Responders)
        {
            bool free = centre.IsResponderAvailable(responder);
            bool isSelected = ReferenceEquals(responder, selected);
            Rectangle card = new(24, y, 320, 74);

            Ui.Panel(card, isSelected ? Theme.PanelRaised : Theme.Panel);
            if (isSelected)
            {
                Raylib.DrawRectangleLinesEx(card, 2, Theme.Accent);
                Raylib.DrawRectangleRec(new Rectangle(24, y, 4, 74), Theme.Accent);
            }

            Ui.Text(responder.Name, 38, y + 12, 16, free ? Theme.Text : Theme.TextDim);
            Ui.Text(responder.GetType().Name, 38, y + 34, 12, Theme.TextDim);
            Ui.Text(free ? "free" : "out", 292, y + 12, 12, free ? Theme.Good : Theme.Warn);

            Ui.Bar(new Rectangle(38, y + 54, 246, 8), responder.Energy, EnergyTint(responder.Energy));
            Ui.Text($"{responder.Energy}", 292, y + 50, 12, Theme.TextDim);

            if (Ui.Hovered(card) && Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                selected = isSelected ? null : responder;
                Say(selected is null
                    ? "Dispatching by policy again."
                    : $"{selected.Name} selected. Dispatch will send them by name.", Theme.Accent);
            }

            y += 84;
        }
    }

    /// <summary>The right column: the board, with what you can do to each incident.</summary>
    private void DrawIncidents()
    {
        // Closed callouts sink to the bottom. OrderBy is stable, so within each
        // group the board keeps the order things were called in.
        List<Incident> board = [.. centre.Incidents
            .OrderBy(i => i.Status == IncidentStatus.Resolved ? 1 : 0)];

        Rectangle viewport = new(372, 88, 784, Height - 176);

        int open = board.Count(i => i.Status != IncidentStatus.Resolved);
        Ui.Text($"INCIDENTS  ({open} open of {board.Count})", 372, 70, 14, Theme.Accent);

        if (followNewest)
        {
            // Show the newest open one, which is the last before the closed block.
            scroll = Math.Min(0, viewport.Height - (open * 84) - 16);
            followNewest = false;
        }

        // The list outgrows the window as soon as you call in a few, so it
        // scrolls rather than silently hiding the newest ones.
        float contentHeight = board.Count * 84;
        if (Ui.Hovered(viewport))
        {
            scroll += Raylib.GetMouseWheelMove() * 42;
        }

        scroll = Math.Clamp(scroll, Math.Min(0, viewport.Height - contentHeight), 0);

        Raylib.BeginScissorMode(
            (int)viewport.X, (int)viewport.Y, (int)viewport.Width, (int)viewport.Height);

        int index = 0;
        foreach (Incident incident in board)
        {
            int y = (int)(viewport.Y + 8 + (index * 84) + scroll);
            index++;

            if (y + 74 < viewport.Y || y > viewport.Y + viewport.Height)
            {
                continue;
            }

            DrawIncidentCard(incident, y);
        }

        Raylib.EndScissorMode();

        if (contentHeight > viewport.Height)
        {
            float fraction = viewport.Height / contentHeight;
            float barHeight = viewport.Height * fraction;
            float barY = viewport.Y + (-scroll / contentHeight * viewport.Height);
            Raylib.DrawRectangleRec(new Rectangle(1162, barY, 4, barHeight), Theme.Line);
        }
    }

    /// <summary>One incident card.</summary>
    /// <param name="incident">The incident to show.</param>
    /// <param name="y">Top of the card.</param>
    private void DrawIncidentCard(Incident incident, int y)
    {
        Rectangle card = new(372, y, 776, 74);
        Ui.Panel(card, Theme.Panel);
        Raylib.DrawRectangleRec(new Rectangle(372, y, 4, 74), SeverityTint(incident.Severity));

        Ui.Text(incident.Description, 388, y + 12, 16, Theme.Text);

        string where = $"{incident.Severity}  ·  {incident.Location}  ·  ";
        Ui.Text(where, 388, y + 34, 12, Theme.TextDim);

        // The requirement is drawn separately so it can go red when the
        // responder you have picked cannot meet it.
        bool blocked = selected is not null && !selected.CanHandle(incident);
        Ui.Text(
            Capabilities(incident),
            388 + Ui.Measure(where, 12),
            y + 34,
            12,
            blocked ? Theme.Bad : Theme.TextDim);

        Responder? assigned = centre.GetAssignedResponder(incident);
        string state = incident.Status switch
        {
            IncidentStatus.Reported => "waiting for a responder",
            IncidentStatus.Assigned => $"dispatched: {assigned?.Name} is on it",
            _ => $"closed by {assigned?.Name}"
        };
        Ui.Text(state, 388, y + 52, 12, StatusTint(incident.Status));

        DrawIncidentActions(incident, y);
    }

    /// <summary>
    /// The action button on a card. Which one, and what it does, depends on
    /// whether a responder is currently selected in the roster.
    /// </summary>
    /// <param name="incident">The incident the card belongs to.</param>
    /// <param name="y">Top of the card.</param>
    private void DrawIncidentActions(Incident incident, int y)
    {
        Rectangle slot = new(984, y + 20, 152, 32);

        // Read the status once. Both buttons share this rectangle, and a click
        // is reported for the whole frame, so acting on the live status would
        // let one press dispatch the incident and then immediately resolve it.
        IncidentStatus status = incident.Status;

        if (status == IncidentStatus.Reported)
        {
            string label = selected is null ? "Dispatch" : $"Send {FirstName(selected)}";

            if (Ui.Button(slot, label, true, Theme.Accent))
            {
                Dispatch(incident);
            }
        }
        else if (status == IncidentStatus.Assigned &&
                 Ui.Button(slot, "Mark resolved", true, Theme.Good))
        {
            centre.ResolveIncident(incident, "Handled from the dispatch board");
        }
    }

    /// <summary>Strategy toggle, new callout, and the last message.</summary>
    private void DrawFooter()
    {
        int y = Height - 62;
        Raylib.DrawLine(24, y - 14, Width - 24, y - 14, Theme.Line);

        if (Ui.Button(new Rectangle(24, y, 260, 34), $"Policy: {centre.CurrentStrategyName}", true, Theme.Accent))
        {
            SwapStrategy();
        }

        if (Ui.Button(new Rectangle(296, y, 160, 34), "New callout", true, Theme.Warn))
        {
            Incident called = SeedData.NextCallout(calloutCount++);
            centre.ReportIncident(called);
            followNewest = true;
            Say($"Called in: {called.Description}", Theme.Warn);
        }

        Ui.TextClipped(message, 24, y - 30, 14, messageTint, Width - 48);
    }

    /// <summary>Sends whoever the current policy picks, and reports either way.</summary>
    /// <param name="incident">The incident to staff.</param>
    private void Dispatch(Incident incident)
    {
        try
        {
            Responder chosen = selected is null
                ? centre.AssignIncident(incident)
                : centre.AssignIncidentTo(incident, selected);

            // Assigning records who is going; handling is the work, and the
            // work is what costs energy. The domain writes the line, we draw it.
            Say(chosen.HandleIncident(incident), Theme.Good);
        }
        catch (NoSuitableResponderException error)
        {
            Say(error.Message, Theme.Bad);
        }
        catch (ResponderUnavailableException error)
        {
            Say(error.Message, Theme.Bad);
        }
        catch (InvalidOperationException error)
        {
            Say(error.Message, Theme.Warn);
        }
    }

    /// <summary>Swaps between the two policies without touching the centre's code.</summary>
    private void SwapStrategy()
    {
        IAssignmentStrategy next = centre.CurrentStrategyName == "First available"
            ? new HighestEnergyAvailableStrategy()
            : new FirstAvailableStrategy();

        centre.ChangeStrategy(next);
        Say($"Policy is now {next.Name}. CommandCentre was not modified.", Theme.Accent);
    }

    /// <summary>Puts a line in the footer.</summary>
    /// <param name="text">What to say.</param>
    /// <param name="tint">How to colour it.</param>
    private void Say(string text, Color tint)
    {
        message = text;
        messageTint = tint;
    }

    /// <summary>What an incident needs from whoever takes it.</summary>
    /// <param name="incident">The incident.</param>
    /// <returns>The capability interface names, or that anyone will do.</returns>
    private static string Capabilities(Incident incident) =>
        incident.RequiredCapabilities.Count == 0
            ? "anyone available"
            : $"needs {string.Join(" + ", incident.RequiredCapabilities.Select(c => c.Name))}";

    /// <summary>Just the given name, so buttons stay narrow.</summary>
    /// <param name="responder">The responder.</param>
    /// <returns>The first word of their name.</returns>
    private static string FirstName(Responder responder) => responder.Name.Split(' ')[0];

    /// <summary>Green when rested, red when spent.</summary>
    /// <param name="energy">The responder's energy.</param>
    /// <returns>The bar colour.</returns>
    private static Color EnergyTint(int energy) => energy switch
    {
        > 60 => Theme.Good,
        > 25 => Theme.Warn,
        _ => Theme.Bad
    };

    /// <summary>Only the incidents that cannot wait get a loud colour.</summary>
    /// <param name="severity">The incident's severity.</param>
    /// <returns>The stripe colour.</returns>
    private static Color SeverityTint(SeverityLevel severity) => severity switch
    {
        SeverityLevel.Critical => Theme.Bad,
        SeverityLevel.High => Theme.Warn,
        _ => Theme.Line
    };

    /// <summary>Colour for the status line on a card.</summary>
    /// <param name="status">The incident's status.</param>
    /// <returns>The text colour.</returns>
    private static Color StatusTint(IncidentStatus status) => status switch
    {
        IncidentStatus.Reported => Theme.Warn,
        IncidentStatus.Assigned => Theme.Accent,
        _ => Theme.TextDim
    };

    /// <summary>Unused, but proves the generic search works from this host too.</summary>
    /// <returns>The unresolved incidents, most urgent first.</returns>
    internal IEnumerable<Incident> Outstanding() =>
        SearchTool.FindMatches(centre.Incidents, i => i.Status != IncidentStatus.Resolved);
}
