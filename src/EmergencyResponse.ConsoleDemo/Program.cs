using EmergencyResponse.Core.Assignment;
using EmergencyResponse.Core.Capabilities;
using EmergencyResponse.Core.Coordination;
using EmergencyResponse.Core.Exceptions;
using EmergencyResponse.Core.Incidents;
using EmergencyResponse.Core.Responders;
using EmergencyResponse.Core.Search;

namespace EmergencyResponse.ConsoleDemo;

/// <summary>
/// Composition root and presentation host. Picks the concrete assignment
/// policy, wires the callbacks, and runs the scripted demonstration.
/// </summary>
internal sealed class Program
{
    /// <summary>Runs the demonstration.</summary>
    /// <param name="args">Command line arguments. Unused.</param>
    private static void Main(string[] args)
    {
        CommandCentre centre = CommandCentre.GetInstance(new FirstAvailableStrategy());
        DemoData.RegisterResponders(centre);
        IReadOnlyList<Incident> board = DemoData.ReportIncidents(centre);

        ShowRoster(centre);
        ShowSearches(centre);
        AssignAndResolve(centre, board);
        ShowHandledFailures(centre, board);
        ShowStrategySwap(centre);
        ShowFinalState(centre);
    }

    /// <summary>Section 1: who is on shift and what has been called in.</summary>
    /// <param name="centre">The command centre.</param>
    private static void ShowRoster(CommandCentre centre)
    {
        ConsoleReport.Section("Registering responders and incidents");

        ShowState(centre);
    }

    /// <summary>Section 6: the same listing again, to show what the shift changed.</summary>
    /// <param name="centre">The command centre.</param>
    private static void ShowFinalState(CommandCentre centre)
    {
        ConsoleReport.Section("The board at the end of the shift");
        ShowState(centre);
    }

    /// <summary>Prints the roster and the incident board as they stand.</summary>
    /// <param name="centre">The command centre.</param>
    private static void ShowState(CommandCentre centre)
    {
        ConsoleReport.Line($"Responders on the roster ({centre.Responders.Count}):");
        foreach (Responder responder in centre.Responders)
        {
            ConsoleReport.ResponderRow(responder, centre.IsResponderAvailable(responder));
        }

        ConsoleReport.Line($"Incidents on the board ({centre.Incidents.Count}):");
        foreach (Incident incident in centre.Incidents)
        {
            ConsoleReport.IncidentRow(incident);
            ConsoleReport.IncidentDetail(incident, centre.GetAssignedResponder(incident));
        }
    }

    /// <summary>Section 2: the same generic method answering two questions.</summary>
    /// <param name="centre">The command centre.</param>
    private static void ShowSearches(CommandCentre centre)
    {
        ConsoleReport.Section("Searching with the generic SearchTool.FindMatches<T>");

        ConsoleReport.Line("Available responders with energy above 50:");
        foreach (Responder responder in SearchTool.FindMatches(
                     centre.Responders,
                     r => centre.IsResponderAvailable(r) && r.Energy > 50))
        {
            ConsoleReport.ResponderRow(responder, available: true);
        }

        ConsoleReport.Line("Unresolved incidents of high severity or worse:");
        foreach (Incident incident in SearchTool.FindMatches(
                     centre.Incidents,
                     i => i.Status != IncidentStatus.Resolved && i.Severity >= SeverityLevel.High))
        {
            ConsoleReport.IncidentRow(incident);
        }
    }

    /// <summary>Section 3: one callout end to end, with both callback forms.</summary>
    /// <param name="centre">The command centre.</param>
    /// <param name="board">The reported incidents, in report order.</param>
    private static void AssignAndResolve(CommandCentre centre, IReadOnlyList<Incident> board)
    {
        ConsoleReport.Section("Assigning and resolving, with both callback forms");

        centre.AddResolutionCallback(LogResolution);

        centre.AddResolutionCallback(incident =>
        {
            if (centre.GetAssignedResponder(incident) is Responder responder)
            {
                ConsoleReport.Item($"{responder.Name} is free again.");
            }
        });

        ConsoleReport.Line("Registered a named method and a lambda as resolution callbacks.");

        Incident alpacas = board[0];
        Responder chosen = centre.AssignIncident(alpacas);
        ConsoleReport.Line($"Assigned by the {centre.CurrentStrategyName} policy:");
        ConsoleReport.ResponderRow(chosen, centre.IsResponderAvailable(chosen));
        ConsoleReport.Item(chosen.HandleIncident(alpacas));

        centre.ResolveIncident(alpacas, "Alpacas walked back to the paddock");
    }

    /// <summary>Section 4: both custom exceptions, caught and reported.</summary>
    /// <param name="centre">The command centre.</param>
    /// <param name="board">The reported incidents, in report order.</param>
    private static void ShowHandledFailures(CommandCentre centre, IReadOnlyList<Incident> board)
    {
        ConsoleReport.Section("Handling the two custom exceptions");

        // Send both climbers out, so the next climbing job has nobody left.
        centre.AssignIncident(board[2]);
        centre.AssignIncident(board[4]);
        ConsoleReport.Line("Both drone pilots are now out on climbing jobs.");

        Incident chapel = new("Kitten on the chapel roof", "St Mary's", SeverityLevel.Medium,
            typeof(ICanClimb));
        centre.ReportIncident(chapel);

        try
        {
            centre.AssignIncident(chapel);
        }
        catch (NoSuitableResponderException error)
        {
            ConsoleReport.Problem(error.Message);
            ConsoleReport.Detail("Nobody in the pool fits, so the policy gives up.");
        }

        Responder busyPilot = SearchTool.FindFirstMatch(
            centre.Responders, r => !centre.IsResponderAvailable(r) && r is DronePilot)!;

        try
        {
            centre.AssignIncidentTo(chapel, busyPilot);
        }
        catch (ResponderUnavailableException error)
        {
            ConsoleReport.Problem(error.Message);
            ConsoleReport.Detail(
                "Naming a responder is the only way to reach this one: the policy " +
                "would have filtered them out and raised the other exception.");
        }

        ConsoleReport.Success("Both were handled. The program is still running.");
    }

    /// <summary>Section 5: the same centre, a different policy, a different pick.</summary>
    /// <param name="centre">The command centre.</param>
    private static void ShowStrategySwap(CommandCentre centre)
    {
        ConsoleReport.Section("Changing the assignment policy");

        // Both policies have to choose from the same pool, or the comparison
        // shows nothing but who happened to be busy.
        foreach (Incident open in SearchTool.FindMatches(
                     centre.Incidents, i => i.Status == IncidentStatus.Assigned))
        {
            centre.ResolveIncident(open, "Stood down so both policies see the same roster");
        }

        ConsoleReport.Line($"Everyone is free again ({centre.AvailableResponders.Count} responders).");

        Pick(centre, "Fox raiding the school bins", "Riverside School");

        centre.ChangeStrategy(new HighestEnergyAvailableStrategy());

        Pick(centre, "Heron queueing at the fishmonger", "Market Hall");

        ConsoleReport.Detail(
            "CommandCentre was not edited between those two picks. Only the " +
            "IAssignmentStrategy it was given changed.");
    }

    /// <summary>Reports one incident, assigns it, and shows who the policy chose.</summary>
    /// <param name="centre">The command centre.</param>
    /// <param name="description">What was reported.</param>
    /// <param name="location">Where it is.</param>
    private static void Pick(CommandCentre centre, string description, string location)
    {
        Incident incident = new(description, location, SeverityLevel.Medium);
        centre.ReportIncident(incident);

        ConsoleReport.Line($"Policy \"{centre.CurrentStrategyName}\" picks:");
        Responder chosen = centre.AssignIncident(incident);
        ConsoleReport.ResponderRow(chosen, available: false);

        centre.ResolveIncident(incident, "Dealt with");
    }

    /// <summary>Named resolution callback, one of the two forms required.</summary>
    /// <param name="incident">The incident that was just resolved.</param>
    private static void LogResolution(Incident incident) =>
        ConsoleReport.Success($"Resolved: {incident.Description}. {incident.ResolutionNote}.");
}
