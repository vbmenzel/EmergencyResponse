using EmergencyResponse.Core.Assignment;
using EmergencyResponse.Core.Coordination;
using EmergencyResponse.Core.Incidents;
using EmergencyResponse.Core.Responders;
using EmergencyResponse.Core.Search;

namespace EmergencyResponse.ConsoleHost;

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

        ShowRoster(centre);
        ShowSearches(centre);
    }

    /// <summary>Section 1: who is on shift and what has been called in.</summary>
    /// <param name="centre">The command centre.</param>
    private static void ShowRoster(CommandCentre centre)
    {
        ConsoleReport.Section("Registering responders and incidents");

        DemoData.RegisterResponders(centre);
        DemoData.ReportIncidents(centre);

        ConsoleReport.Line($"Responders on the roster ({centre.Responders.Count}):");
        foreach (Responder responder in centre.Responders)
        {
            ConsoleReport.ResponderRow(responder, centre.IsResponderAvailable(responder));
        }

        ConsoleReport.Line($"Incidents reported ({centre.Incidents.Count}):");
        foreach (Incident incident in centre.Incidents)
        {
            ConsoleReport.IncidentRow(incident);
            ConsoleReport.IncidentDetail(incident);
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
}
