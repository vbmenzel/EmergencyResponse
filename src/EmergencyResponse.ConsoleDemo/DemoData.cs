using EmergencyResponse.Core.Coordination;
using EmergencyResponse.Core.Incidents;
using EmergencyResponse.Core.Responders;

namespace EmergencyResponse.ConsoleDemo;

/// <summary>
/// The unit's roster and the day's call log, kept out of
/// <see cref="Program"/> so the demonstration script stays readable.
/// </summary>
internal static class DemoData
{
    /// <summary>
    /// Registers five responders.
    /// </summary>
    /// <param name="centre">The centre to register with.</param>
    /// <returns>The responders, in registration order.</returns>
    internal static IReadOnlyList<Responder> RegisterResponders(CommandCentre centre)
    {
        List<Responder> roster =
        [
            new AnimalCatcher("Bo Nielsen", 60),
            new DronePilot("Cyd Rahman", 85),
            new WildlifeCalmer("Dev Okonkwo", 95),
            new AnimalCatcher("Eli Sorensen", 40),
            new DronePilot("Fay Lindqvist", 15)
        ];

        foreach (Responder responder in roster)
        {
            centre.RegisterResponder(responder);
        }

        return roster;
    }

    /// <summary>Reports the five incidents from the brief.</summary>
    /// <param name="centre">The centre to report to.</param>
    /// <returns>The incidents, in report order.</returns>
    internal static IReadOnlyList<Incident> ReportIncidents(CommandCentre centre)
    {
        List<Incident> board =
        [
            new("Three alpacas on the motorway", "E45 northbound",
                SeverityLevel.Critical, typeof(Core.Capabilities.ICanDriveRescueVehicle)),
            new("Territorial swan occupying a bus stop", "Market Street stop C",
                SeverityLevel.Medium, typeof(Core.Capabilities.ICanCalmAnimals)),
            new("Goat stranded on the library roof", "Central Library",
                SeverityLevel.High, typeof(Core.Capabilities.ICanClimb)),
            new("Seagull has stolen a hot-dog stand", "Harbour promenade",
                SeverityLevel.Low),
            new("Cat in a tree that insists it is not an emergency", "Elm Road 14",
                SeverityLevel.Low, typeof(Core.Capabilities.ICanClimb))
        ];

        foreach (Incident incident in board)
        {
            centre.ReportIncident(incident);
        }

        return board;
    }
}
