using EmergencyResponse.Core.Assignment;
using EmergencyResponse.Core.Capabilities;
using EmergencyResponse.Core.Coordination;
using EmergencyResponse.Core.Exceptions;
using EmergencyResponse.Core.Incidents;
using EmergencyResponse.Core.Responders;
using EmergencyResponse.Core.Search;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace EmergencyResponse.Tui;

/// <summary>
/// The menu-driven control room. A menu bar and a row of clickable buttons
/// both drive the same actions; two tables show the roster and the incident
/// board, a status bar summarises the shift, and every action answers through
/// a dialog so the mouse and the keyboard do the same work.
/// </summary>
internal sealed class ControlRoom : IDisposable
{
    private const string AboutTitle = "Emergency Response Command Centre";
    private const string CancelLabel = "_Cancel";
    private const string OkLabel = "_OK";

    private static readonly (Type Capability, string Label, string ShortLabel)[] capabilityChoices =
    [
        (typeof(ICanClimb), "Climb (roofs, trees, high shelves)", "Climb"),
        (typeof(ICanOperateDrone), "Operate a drone (aerial survey)", "Drone"),
        (typeof(ICanCalmAnimals), "Calm animals (de-escalation)", "Calm"),
        (typeof(ICanDriveRescueVehicle), "Drive a rescue vehicle", "Drive")
    ];

    private static readonly string[] strategyNames =
    [
        new FirstAvailableStrategy().Name,
        new HighestEnergyAvailableStrategy().Name
    ];

    private readonly IApplication app;
    private readonly CommandCentre centre;
    private readonly Window window = new() { Title = AboutTitle };
    private readonly TableView responderTable = new();
    private readonly TableView incidentTable = new();
    private readonly Shortcut statusShortcut = new();
    private string? lastCallbackLog;

    /// <summary>
    /// Opens the control room and subscribes the status line to resolved
    /// incidents, so the callback shown on screen is the real one.
    /// </summary>
    /// <param name="app">The application that owns the terminal.</param>
    /// <param name="centre">The centre to run.</param>
    internal ControlRoom(IApplication app, CommandCentre centre)
    {
        this.app = app;
        this.centre = centre;
        centre.AddResolutionCallback(CaptureResolution);
    }

    /// <summary>
    /// Builds the screen, seeds the sample data, and runs until the operator
    /// quits, then draws the closing summary.
    /// </summary>
    internal void Run()
    {
        BuildScreen();
        LoadSampleData(silent: true);
        Refresh();

        app.Run(window);

        Greet("Shift over",
            $"Incidents received: {centre.Incidents.Count}\n" +
            $"Resolved this shift: {centre.Incidents.Count(i => i.Status == IncidentStatus.Resolved)}\n" +
            $"Still on the board: {centre.Incidents.Count(i => i.Status != IncidentStatus.Resolved)}\n" +
            $"Final policy: {centre.CurrentStrategyName}");
    }

    /// <summary>
    /// Lays out the menu bar, the quick-action buttons, the two tables and the
    /// status bar on the main window.
    /// </summary>
    private void BuildScreen()
    {
        MenuBar menuBar = BuildMenuBar();

        string[] labels = ["_Report…", "_Dispatch", "_Assign…", "_Resolve…", "_Strategy", "_Register", "_Search"];
        Action[] actions = [ReportIncident, DispatchNextOpen, AssignByName, ResolveIncident, ChangePolicy, RegisterResponder, Search];
        int x = 0;
        for (int i = 0; i < labels.Length; i++)
        {
            Button button = QuickAction(labels[i], actions[i]);
            button.X = x;
            window.Add(button);
            x += VisibleLength(labels[i]) + 9;
        }

        FrameView respondersFrame = new()
        {
            Title = "Responders",
            X = 0,
            Y = 3,
            Width = Dim.Percent(42),
            Height = Dim.Fill(4)
        };
        responderTable.X = 0;
        responderTable.Y = 0;
        responderTable.Width = Dim.Fill();
        responderTable.Height = Dim.Fill();
        respondersFrame.Add(responderTable);

        FrameView incidentsFrame = new()
        {
            Title = "Incidents",
            X = Pos.Right(respondersFrame) + 1,
            Y = 3,
            Width = Dim.Fill(1),
            Height = Dim.Fill(4)
        };
        incidentTable.X = 0;
        incidentTable.Y = 0;
        incidentTable.Width = Dim.Fill();
        incidentTable.Height = Dim.Fill();
        incidentsFrame.Add(incidentTable);

        StatusBar statusBar = new();
        statusBar.Add(statusShortcut);
        statusBar.Add(new Shortcut
        {
            Title = "_Help (F1)",
            Key = Key.F1,
            Action = HowTo
        });
        statusBar.Add(new Shortcut
        {
            Title = "_Quit (Ctrl/Q)",
            Key = Key.Q.WithCtrl,
            Action = Quit
        });
        statusBar.Y = Pos.AnchorEnd();

        window.Add(menuBar,
            respondersFrame, incidentsFrame, statusBar);
    }

    /// <summary>The visible length of a button label, ignoring its hotkey marker.</summary>
    /// <param name="text">The label, possibly with an underscore hotkey.</param>
    private static int VisibleLength(string text) => text.Replace("_", string.Empty, StringComparison.Ordinal).Length;

    /// <summary>
    /// Builds the top menu bar. Every entry calls the same handler as the
    /// matching quick-action button.
    /// </summary>
    private MenuBar BuildMenuBar() =>
        new(
        [
            new MenuBarItem("_File",
            [
                new MenuItem { Title = "_Load sample data", Action = () => LoadSampleData(silent: false) },
                new MenuItem { Title = "_Quit", Key = Key.Q.WithCtrl, Action = Quit }
            ]),
            new MenuBarItem("_Dispatch",
            [
                new MenuItem { Title = "_Report incident…", Key = Key.R.WithCtrl, Action = ReportIncident },
                new MenuItem { Title = "Dispatch _next open", Key = Key.N.WithCtrl, Action = DispatchNextOpen },
                new MenuItem { Title = "_Assign by name…", Key = Key.A.WithCtrl, Action = AssignByName },
                new MenuItem { Title = "_Resolve incident…", Key = Key.E.WithCtrl, Action = ResolveIncident },
                new MenuItem { Title = "Change _strategy…", Key = Key.S.WithCtrl, Action = ChangePolicy },
                new MenuItem { Title = "_Register responder…", Key = Key.U.WithCtrl, Action = RegisterResponder }
            ]),
            new MenuBarItem("_Search",
            [
                new MenuItem { Title = "_Search the board…", Key = Key.F.WithCtrl, Action = Search }
            ]),
            new MenuBarItem("_Help",
            [
                new MenuItem { Title = "_About…", Action = About },
                new MenuItem { Title = "_How to use…", Action = HowTo }
            ])
        ]);

    /// <summary>Creates a quick-action button wired to the given handler.</summary>
    /// <param name="text">The button label, with an underscore hotkey.</param>
    /// <param name="action">What the button does.</param>
    private static Button QuickAction(string text, Action action)
    {
        Button button = new()
        {
            Text = text,
            IsDefault = true,
            Width = VisibleLength(text) + 6,
            Y = 1
        };
        button.Accepting += (_, e) =>
        {
            action();
            e.Handled = true;
        };
        return button;
    }

    /// <summary>
    /// Rebuilds both tables and the status bar from the centre's current state.
    /// </summary>
    private void Refresh()
    {
        List<Responder> roster = [.. centre.Responders];
        responderTable.Table = new EnumerableTableSource<Responder>(
            roster,
            new Dictionary<string, Func<Responder, object>>
            {
                ["Name"] = responder => responder.Name,
                ["Role"] = responder => responder.GetType().Name,
                ["Energy"] = responder => EnergyLabel(responder.Energy),
                ["Status"] = responder => centre.IsResponderAvailable(responder) ? "Free" : "Out"
            });

        List<Incident> board = [.. centre.Incidents];
        incidentTable.Table = new EnumerableTableSource<Incident>(
            board,
            new Dictionary<string, Func<Incident, object>>
            {
                ["Status"] = incident => incident.Status.ToString(),
                ["Severity"] = incident => incident.Severity.ToString(),
                ["Description"] = incident => incident.Description,
                ["Location"] = incident => incident.Location,
                ["Needs"] = NeedsLabel,
                ["Responder"] = incident => centre.GetAssignedResponder(incident)?.Name ?? "—"
            });

        int awaiting = board.Count(incident => incident.Status == IncidentStatus.Reported);
        int assigned = board.Count(incident => incident.Status == IncidentStatus.Assigned);
        int resolved = board.Count(incident => incident.Status == IncidentStatus.Resolved);

        statusShortcut.Title =
            $"  Policy: {centre.CurrentStrategyName}   ·   Received: {board.Count}   ·   Awaiting: ● {awaiting}   ·   " +
            $"Assigned: ◉ {assigned}   ·   Resolved: ✓ {resolved}";
    }

    /// <summary>Takes the call from a citizen and puts it on the board.</summary>
    private void ReportIncident()
    {
        using Dialog dialog = new()
        {
            Title = "Report an incident",
            Buttons = [new Button { Text = CancelLabel }, new Button { Text = OkLabel, IsDefault = true }],
            ButtonAlignment = Alignment.Center,
            Width = 76
        };

        Label descriptionLabel = new() { X = 0, Y = 0, Text = "What was reported?" };
        TextField description = new() { X = 0, Y = Pos.Bottom(descriptionLabel), Width = Dim.Fill() };
        Label locationLabel = new() { X = 0, Y = Pos.Bottom(description), Text = "Where is it?" };
        TextField location = new() { X = 0, Y = Pos.Bottom(locationLabel), Width = Dim.Fill() };
        Label severityLabel = new() { X = 0, Y = Pos.Bottom(location), Text = "Severity:" };
        OptionSelector<SeverityLevel> severity = new()
        {
            X = 0,
            Y = Pos.Bottom(severityLabel),
            Value = SeverityLevel.Medium
        };
        Label skillsLabel = new() { X = 0, Y = Pos.Bottom(severity), Text = "Required skills (select none for any responder):" };

        List<CheckBox> skillBoxes = [];
        View previous = skillsLabel;
        foreach ((_, string label, _) in capabilityChoices)
        {
            CheckBox box = new() { X = 0, Y = Pos.Bottom(previous), Text = label };
            skillBoxes.Add(box);
            previous = box;
        }

        dialog.Add(descriptionLabel, description, locationLabel, location, severityLabel, severity, skillsLabel);
        dialog.Add(skillBoxes.ToArray());

        if (!Confirm(dialog))
        {
            return;
        }

        string summary = description.Text.Trim();
        string place = location.Text.Trim();
        if (summary.Length == 0 || place.Length == 0)
        {
            Error("A description and a location are both required.");
            return;
        }

        List<Type> capabilities =
        [
            .. capabilityChoices
                .Where((choice, index) => skillBoxes[index].Value == CheckState.Checked)
                .Select(choice => choice.Capability)
        ];

        Incident incident = new(summary, place, severity.Value ?? SeverityLevel.Medium, [.. capabilities]);
        centre.ReportIncident(incident);
        Refresh();

        Greet("Reported", $"{incident.Severity}: {incident.Description} at {incident.Location}.");
    }

    /// <summary>Sends the next awaiting incident out under the current policy.</summary>
    private void DispatchNextOpen()
    {
        Incident? open = SearchTool.FindFirstMatch(
            centre.Incidents, incident => incident.Status == IncidentStatus.Reported);

        if (open is null)
        {
            Info("Nothing on the board is awaiting dispatch.");
            return;
        }

        try
        {
            Responder chosen = centre.AssignIncident(open);
            Refresh();
            Greet("Dispatched",
                $"{chosen.Name} to {open.Description}.\n\n{chosen.HandleIncident(open)}");
        }
        catch (NoSuitableResponderException error)
        {
            Error(error.Message);
        }
    }

    /// <summary>Lets the operator name both the incident and the responder, bypassing the policy.</summary>
    private void AssignByName()
    {
        List<Incident> open =
        [
            .. SearchTool.FindMatches(
                centre.Incidents, incident => incident.Status == IncidentStatus.Reported)
        ];
        if (open.Count == 0)
        {
            Info("Nothing on the board is awaiting assignment.");
            return;
        }

        Incident picked = open[PickFromDialog(
            "Assign a responder by name",
            "Which incident?",
            open.Select(IncidentLabel).ToArray()) ?? 0];

        List<Responder> candidates =
        [
            .. SearchTool.FindMatches(
                centre.Responders,
                responder => centre.IsResponderAvailable(responder) && responder.CanHandle(picked))
        ];
        if (candidates.Count == 0)
        {
            Error($"Nobody free can take: {picked.Description}");
            return;
        }

        int? responderIndex = PickFromDialog(
            "Assign a responder by name",
            $"Who should take \"{picked.Description}\"?",
            candidates.Select(ResponderLabel).ToArray());

        if (responderIndex is null)
        {
            return;
        }

        Responder chosen = candidates[responderIndex.Value];
        try
        {
            centre.AssignIncidentTo(picked, chosen);
            Refresh();
            Greet("Dispatched by name",
                $"{chosen.Name} (no policy involved).\n\n{chosen.HandleIncident(picked)}");
        }
        catch (ResponderUnavailableException error)
        {
            Error(error.Message);
        }
    }

    /// <summary>Closes an assigned incident with a note and lets the callbacks fire.</summary>
    private void ResolveIncident()
    {
        List<Incident> assigned =
        [
            .. SearchTool.FindMatches(
                centre.Incidents, incident => incident.Status == IncidentStatus.Assigned)
        ];
        if (assigned.Count == 0)
        {
            Info("Nothing is out on a call right now.");
            return;
        }

        using Dialog dialog = new()
        {
            Title = "Resolve an incident",
            Buttons = [new Button { Text = CancelLabel }, new Button { Text = OkLabel, IsDefault = true }],
            ButtonAlignment = Alignment.Center,
            Width = 70
        };

        Label pickLabel = new() { X = 0, Y = 0, Text = "Which incident is finished?" };
        OptionSelector picker = new()
        {
            X = 0,
            Y = Pos.Bottom(pickLabel),
            Labels = assigned.Select(IncidentLabel).ToArray(),
            Value = 0,
            Orientation = Orientation.Vertical
        };
        Label noteLabel = new() { X = 0, Y = Pos.Bottom(picker), Text = "Resolution note:" };
        TextField note = new() { X = 0, Y = Pos.Bottom(noteLabel), Width = Dim.Fill() };

        dialog.Add(pickLabel, picker, noteLabel, note);

        if (!Confirm(dialog))
        {
            return;
        }

        if (note.Text.Trim().Length == 0)
        {
            Error("A resolution note is required.");
            return;
        }

        Incident incident = assigned[picker.Value ?? 0];
        try
        {
            centre.ResolveIncident(incident, note.Text.Trim());
            Refresh();
            Greet("Closed", $"{incident.Description}.\n\n{lastCallbackLog}");
        }
        catch (InvalidOperationException error)
        {
            Error(error.Message);
        }
    }

    /// <summary>Swaps the selection policy on the same centre.</summary>
    private void ChangePolicy()
    {
        int currentIndex = Array.IndexOf(strategyNames, centre.CurrentStrategyName);
        int? picked = PickFromDialog(
            "Change dispatch strategy",
            "Dispatch strategy:",
            strategyNames,
            currentIndex < 0 ? 0 : currentIndex);

        if (picked is null)
        {
            return;
        }

        IAssignmentStrategy strategy = picked.Value == 0
            ? new FirstAvailableStrategy()
            : new HighestEnergyAvailableStrategy();

        centre.ChangeStrategy(strategy);
        Refresh();
        Greet("Policy changed", $"Dispatch policy is now: {strategy.Name}");
    }

    /// <summary>Adds a responder to the roster.</summary>
    private void RegisterResponder()
    {
        using Dialog dialog = new()
        {
            Title = "Register a responder",
            Buttons = [new Button { Text = CancelLabel }, new Button { Text = OkLabel, IsDefault = true }],
            ButtonAlignment = Alignment.Center,
            Width = 64
        };

        Label nameLabel = new() { X = 0, Y = 0, Text = "Responder's name:" };
        TextField name = new() { X = 0, Y = Pos.Bottom(nameLabel), Width = Dim.Fill() };
        Label roleLabel = new() { X = 0, Y = Pos.Bottom(name), Text = "Role:" };
        string[] roles = ["Animal catcher", "Drone pilot", "Wildlife calmer"];
        OptionSelector role = new()
        {
            X = 0,
            Y = Pos.Bottom(roleLabel),
            Labels = roles,
            Value = 0,
            Orientation = Orientation.Vertical
        };
        Label energyLabel = new() { X = 0, Y = Pos.Bottom(role), Text = $"Starting energy ({Responder.MinEnergy} - {Responder.MaxEnergy}):" };
        TextField energy = new() { X = 0, Y = Pos.Bottom(energyLabel), Width = 6, Text = "60" };

        dialog.Add(nameLabel, name, roleLabel, role, energyLabel, energy);

        if (!Confirm(dialog))
        {
            return;
        }

        string responderName = name.Text.Trim();
        if (responderName.Length == 0 || !int.TryParse(energy.Text.Trim(), out int responderEnergy) ||
            responderEnergy < Responder.MinEnergy || responderEnergy > Responder.MaxEnergy)
        {
            Error("A name is required and the energy must be a whole number from 0 to 100.");
            return;
        }

        Responder responder = roles[role.Value ?? 0] switch
        {
            "Animal catcher" => new AnimalCatcher(responderName, responderEnergy),
            "Drone pilot" => new DronePilot(responderName, responderEnergy),
            _ => new WildlifeCalmer(responderName, responderEnergy)
        };

        centre.RegisterResponder(responder);
        Refresh();
        Greet("On the roster", $"{responder.Name} ({responder.GetType().Name}, {responder.Energy}% energy).");
    }

    /// <summary>Runs the generic search over responders or incidents and shows the hits.</summary>
    private void Search()
    {
        string[] queries =
        [
            "Available responders",
            "Responders with energy above a level",
            "Incidents of a severity or worse",
            "Critical incidents still on the board"
        ];

        int? picked = PickFromDialog("Search the board", "What would you like to search?", queries, 0);
        if (picked is null)
        {
            return;
        }

        switch (picked.Value)
        {
            case 0:
                ShowResults(
                    "Responders with nothing assigned",
                    centre.AvailableResponders.Select(ResponderLabel).ToList());
                break;

            case 1:
                int threshold = AskInt("Energy must be above (0 - 100)", 50);
                if (threshold < 0)
                {
                    return;
                }

                ShowResults(
                    $"Responders with energy above {threshold}",
                    SearchTool.FindMatches(centre.Responders, responder => responder.Energy > threshold)
                        .Select(ResponderLabel)
                        .ToList());
                break;

            case 2:
                SeverityLevel? severity = PickSeverity();
                if (severity is null)
                {
                    return;
                }

                ShowResults(
                    $"Unresolved incidents of severity {severity.Value} or worse",
                    SearchTool.FindMatches(
                            centre.Incidents,
                            incident => incident.Status != IncidentStatus.Resolved &&
                                        incident.Severity >= severity.Value)
                        .Select(IncidentLabel)
                        .ToList());
                break;

            default:
                ShowResults(
                    "Critical incidents still on the board",
                    SearchTool.FindMatches(
                            centre.Incidents,
                            incident => incident.Status != IncidentStatus.Resolved &&
                                        incident.Severity == SeverityLevel.Critical)
                        .Select(IncidentLabel)
                        .ToList());
                break;
        }
    }

    /// <summary>Shows a list of matches in a small modal table.</summary>
    /// <param name="title">What the search looked for.</param>
    /// <param name="matches">The lines found.</param>
    private void ShowResults(string title, List<string> matches)
    {
        if (matches.Count == 0)
        {
            Info("Nothing matched that search.");
            return;
        }

        using Dialog dialog = new()
        {
            Title = title,
            Buttons = [new Button { Text = "_Close" }],
            ButtonAlignment = Alignment.Center,
            Width = 78,
            Height = Math.Min(matches.Count + 6, 24)
        };

        TableView table = new()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        table.Table = new EnumerableTableSource<int>(
            Enumerable.Range(0, matches.Count).ToArray(),
            new Dictionary<string, Func<int, object>>
            {
                ["Matches"] = index => matches[index]
            });
        dialog.Add(table);

        app.Run(dialog);
    }

    /// <summary>
    /// Seeds the opening shift from the sample data unless the roster already
    /// has responders.
    /// </summary>
    /// <param name="silent">
    /// When <see langword="true"/>, nothing is announced; used at startup.
    /// </param>
    private void LoadSampleData(bool silent)
    {
        if (centre.Responders.Count > 0)
        {
            if (!silent)
            {
                Info("Sample data is already on the board.");
            }

            return;
        }

        TuiData.RegisterResponders(centre);
        TuiData.ReportIncidents(centre);
        Refresh();

        if (!silent)
        {
            Info("Sample data loaded: five responders and five incidents.");
        }
    }

    /// <summary>Stops the main window so <see cref="Run"/> can return.</summary>
    private void Quit() => app.RequestStop(window);

    /// <summary>Releases the views owned by this control room.</summary>
    public void Dispose()
    {
        window.Dispose();
        responderTable.Dispose();
        incidentTable.Dispose();
        statusShortcut.Dispose();
    }

    /// <summary>Shows the about box.</summary>
    private void About()
    {
        Greet(AboutTitle,
            "A Terminal.Gui control room for EmergencyResponse.Core.\n" +
            "Menu bar, quick-action buttons, dialogs and tables — " +
            "everything responds to the mouse and the keyboard.");
    }

    /// <summary>Shows the controls summary.</summary>
    private void HowTo()
    {
        Greet("How to use",
            "File → Load sample data brings up the opening shift.\n" +
            "Dispatch and the buttons below the menu run the same actions.\n" +
            "Select text or options inside dialogs with arrows, Tab or the mouse.\n" +
            "F1 help, Ctrl+Q quits.");
    }

    /// <summary>The resolution callback registered with the centre, feeding the status line.</summary>
    /// <param name="incident">The incident that was just resolved.</param>
    private void CaptureResolution(Incident incident) =>
        lastCallbackLog = $"Resolution callback: {incident.Description} — {incident.ResolutionNote!}.";

    /// <summary>
    /// Runs a dialog whose buttons are <see cref="CancelLabel"/> and
    /// <see cref="OkLabel"/>, and reports whether the OK button was accepted.
    /// </summary>
    /// <param name="dialog">The open dialog.</param>
    /// <returns><see langword="true"/> when OK was pressed.</returns>
    private bool Confirm(Dialog dialog) =>
        app.Run(dialog) is int result && result == dialog.Buttons.Length - 1;

    /// <summary>
    /// Shows a modal dialog that picks one of the given options.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="prompt">The question above the list.</param>
    /// <param name="options">The labels to choose from.</param>
    /// <param name="selected">Which option starts selected.</param>
    /// <returns>The picked index, or <see langword="null"/> when cancelled.</returns>
    private int? PickFromDialog(string title, string prompt, IReadOnlyList<string> options, int selected = 0)
    {
        using Dialog dialog = new()
        {
            Title = title,
            Buttons = [new Button { Text = CancelLabel }, new Button { Text = OkLabel, IsDefault = true }],
            ButtonAlignment = Alignment.Center,
            Width = 70
        };

        Label promptLabel = new() { X = 0, Y = 0, Text = prompt };
        OptionSelector picker = new()
        {
            X = 0,
            Y = Pos.Bottom(promptLabel),
            Labels = options,
            Value = selected,
            Orientation = Orientation.Vertical
        };
        dialog.Add(promptLabel, picker);

        if (!Confirm(dialog))
        {
            return null;
        }

        return picker.Value ?? 0;
    }

    /// <summary>Asks for a whole number in a range via a small dialog.</summary>
    /// <param name="prompt">The question to ask.</param>
    /// <param name="defaultValue">The pre-filled answer.</param>
    /// <returns>The entered value, or -1 when cancelled or invalid.</returns>
    private int AskInt(string prompt, int defaultValue)
    {
        using Dialog dialog = new()
        {
            Title = "Pick a number",
            Buttons = [new Button { Text = CancelLabel }, new Button { Text = OkLabel, IsDefault = true }],
            ButtonAlignment = Alignment.Center,
            Width = 50
        };

        Label promptLabel = new() { X = 0, Y = 0, Text = prompt };
        TextField value = new() { X = 0, Y = Pos.Bottom(promptLabel), Width = 6, Text = defaultValue.ToString(System.Globalization.CultureInfo.InvariantCulture) };
        dialog.Add(promptLabel, value);

        if (!Confirm(dialog))
        {
            return -1;
        }

        return int.TryParse(value.Text.Trim(), out int parsed)
            ? Math.Clamp(parsed, 0, Responder.MaxEnergy)
            : -1;
    }

    /// <summary>Asks for a severity.</summary>
    /// <returns>The chosen severity, or <see langword="null"/> when cancelled.</returns>
    private SeverityLevel? PickSeverity()
    {
        int? picked = PickFromDialog(
            "Incidents of a severity or worse",
            "Severity:",
            Enum.GetNames<SeverityLevel>(),
            1);

        return picked is null ? null : (SeverityLevel)picked.Value;
    }

    /// <summary>Runs an informational message box.</summary>
    /// <param name="message">The message text.</param>
    private void Info(string message) => MessageBox.Query(app, AboutTitle, message, "_OK");

    /// <summary>Runs an informational message box with a specific title.</summary>
    /// <param name="title">The box title.</param>
    /// <param name="message">The message text.</param>
    private void Greet(string title, string message) => MessageBox.Query(app, title, message, "_OK");

    /// <summary>Runs an error message box.</summary>
    /// <param name="message">The message text.</param>
    private void Error(string message) => MessageBox.ErrorQuery(app, AboutTitle, message, "_OK");

    /// <summary>Turns an energy value into a bar with a percentage.</summary>
    /// <param name="energy">The energy, 0 to 100.</param>
    private static string EnergyLabel(int energy) =>
        $"{new string('█', energy / 10)}{new string('░', 10 - energy / 10)} {energy,3}%";

    /// <summary>Short labels for an incident's required capabilities.</summary>
    /// <param name="incident">The incident to describe.</param>
    /// <returns>Comma-joined capability names, or "any" when none are required.</returns>
    private static string NeedsLabel(Incident incident) =>
        incident.RequiredCapabilities.Count == 0
            ? "any"
            : string.Join(", ",
                incident.RequiredCapabilities.Select(
                    capability => capabilityChoices
                        .FirstOrDefault(choice => choice.Capability == capability)
                        .ShortLabel));

    /// <summary>A one-line label for a responder.</summary>
    /// <param name="responder">The responder to describe.</param>
    private static string ResponderLabel(Responder responder) =>
        $"{responder.Name}: {responder.GetType().Name}, {responder.Energy}% energy";

    /// <summary>A one-line label for an incident.</summary>
    /// <param name="incident">The incident to describe.</param>
    private static string IncidentLabel(Incident incident) =>
        $"[{incident.Severity}] {incident.Description} ({incident.Location})";
}