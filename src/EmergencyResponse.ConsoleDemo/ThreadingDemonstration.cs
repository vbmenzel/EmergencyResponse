using EmergencyResponse.Core.Coordination;
using EmergencyResponse.Core.Exceptions;
using EmergencyResponse.Core.Incidents;
using EmergencyResponse.Core.Responders;
using EmergencyResponse.Core.Search;

namespace EmergencyResponse.ConsoleDemo;

/// <summary>
/// Several incidents arriving at once, first without synchronisation and then
/// with it.
/// </summary>
internal static class ThreadingDemonstration
{
    /// <summary>
    /// How long every task waits after deciding and before recording in order
    /// for us to observe reliably.
    /// </summary>
    private const int RaceWindowMilliseconds = 40;

    /// <summary>How many incidents arrive simultaneously in each run.</summary>
    private const int SimultaneousCallouts = 7;

    /// <summary>
    /// Splits deciding from recording, which is the bug. Every task asks who is
    /// free, they all get the same answer, and only then do they record.
    /// </summary>
    /// <param name="centre">The command centre.</param>
    /// <returns>A task that completes when the run is finished.</returns>
    internal static async Task RunUnsafeAsync(CommandCentre centre)
    {
        ConsoleReport.Line(
            $"{SimultaneousCallouts} callouts arrive together. Each task decides who is " +
            "free, waits, then records. No lock spans both steps.");

        List<Incident> incidents = ReportCallouts(centre, "Unsynchronised callout");

        Responder?[] picked = await Task.WhenAll(
            incidents.Select(incident => DecideThenRecordAsync(centre, incident)));

        ReportOutcome(picked, unsafeRun: true);
        StandDown(centre, incidents);
    }

    /// <summary>
    /// The same callouts through <see cref="CommandCentre.AssignIncident"/>,
    /// where deciding and recording happen inside one lock.
    /// </summary>
    /// <param name="centre">The command centre.</param>
    /// <returns>A task that completes when the run is finished.</returns>
    internal static async Task RunSafeAsync(CommandCentre centre)
    {
        ConsoleReport.Line(
            $"The same {SimultaneousCallouts} callouts through AssignIncident, which holds " +
            "one lock across choosing and recording.");

        List<Incident> incidents = ReportCallouts(centre, "Synchronised callout");

        Responder?[] picked = await Task.WhenAll(
            incidents.Select(incident => Task.Run(() => AssignOrRefuse(centre, incident))));

        ReportOutcome(picked, unsafeRun: false);
        StandDown(centre, incidents);
    }

    /// <summary>Check, pause, act.</summary>
    /// <param name="centre">The command centre.</param>
    /// <param name="incident">The incident this task is handling.</param>
    /// <returns>The responder recorded, or null if none was free.</returns>
    private static async Task<Responder?> DecideThenRecordAsync(
        CommandCentre centre, Incident incident)
    {
        Responder? chosen = SearchTool.FindFirstMatch(
            centre.AvailableResponders, r => r.CanHandle(incident));

        if (chosen is null)
        {
            return null;
        }

        // Every task has now decided. None has recorded. This is the window.
        await Task.Delay(RaceWindowMilliseconds).ConfigureAwait(false);

        centre.AssignIncidentUnsafeForDemo(incident, chosen);
        return chosen;
    }

    /// <summary>Assigns through the guarded path, or reports that nobody was free.</summary>
    /// <param name="centre">The command centre.</param>
    /// <param name="incident">The incident this task is handling.</param>
    /// <returns>The responder assigned, or null if nobody was eligible.</returns>
    private static Responder? AssignOrRefuse(CommandCentre centre, Incident incident)
    {
        try
        {
            return centre.AssignIncident(incident);
        }
        catch (NoSuitableResponderException)
        {
            // Correct behaviour once everybody is out, not a failure.
            return null;
        }
    }

    /// <summary>Reports a batch of simultaneous callouts.</summary>
    /// <param name="centre">The command centre.</param>
    /// <param name="label">How to name them on the board.</param>
    /// <returns>The incidents, in report order.</returns>
    private static List<Incident> ReportCallouts(CommandCentre centre, string label)
    {
        List<Incident> incidents = [.. Enumerable.Range(1, SimultaneousCallouts)
            .Select(n => new Incident($"{label} {n}", "Town centre", SeverityLevel.Medium))];

        foreach (Incident incident in incidents)
        {
            centre.ReportIncident(incident);
        }

        return incidents;
    }

    /// <summary>States whether any responder ended up on more than one incident.</summary>
    /// <param name="picked">What each task recorded, in task order.</param>
    /// <param name="unsafeRun">Whether this was the unsynchronised run.</param>
    private static void ReportOutcome(Responder?[] picked, bool unsafeRun)
    {
        List<IGrouping<Responder, Responder>> doubleBooked = [.. picked
            .Where(r => r is not null)
            .Select(r => r!)
            .GroupBy(r => r)
            .Where(group => group.Count() > 1)];

        int assigned = picked.Count(r => r is not null);
        ConsoleReport.Item($"{assigned} of {picked.Length} callouts were recorded.");

        if (doubleBooked.Count > 0)
        {
            foreach (IGrouping<Responder, Responder> group in doubleBooked)
            {
                ConsoleReport.Problem(
                    $"{group.Key.Name} was sent to {group.Count()} incidents at once.");
            }

            ConsoleReport.Detail(
                "Every task read the roster before any task wrote to it, so they all " +
                "saw the same person as free.");
        }
        else if (unsafeRun)
        {
            ConsoleReport.Item(
                "No double booking this time, go buy a lottery ticket. The race depends on timing; run again.");
        }
        else
        {
            ConsoleReport.Success("No responder appears twice. The lock held.");
            ConsoleReport.Detail(
                $"The other {picked.Length - assigned} were refused with " +
                "NoSuitableResponderException, which is correct once everybody is out.");
        }

    }

    /// <summary>Closes what this run opened, so the next section starts clean.</summary>
    /// <param name="centre">The command centre.</param>
    /// <param name="ours">The incidents this run reported.</param>
    private static void StandDown(CommandCentre centre, IEnumerable<Incident> ours)
    {
        foreach (Incident open in SearchTool.FindMatches(
                     ours, i => i.Status == IncidentStatus.Assigned))
        {
            centre.ResolveIncident(open, "Stood down after the concurrency run");
        }
    }
}
