using EmergencyResponse.Core.Capabilities;
using EmergencyResponse.Core.Coordination;
using EmergencyResponse.Core.Incidents;
using EmergencyResponse.Core.Responders;

namespace EmergencyResponse.Tui;

/// <summary>
/// The unit's opening roster and the incidents the shift is handed, kept out
/// of <see cref="ControlRoom"/> so the screen stays readable.
/// </summary>
internal static class TuiData
{
    /// <summary>
    /// Registers five responders of the three kinds.
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

    /// <summary>
    /// Reports the incidents already on the board when the shift starts.
    /// </summary>
    /// <param name="centre">The centre to report to.</param>
    /// <returns>The incidents, in report order.</returns>
    internal static IReadOnlyList<Incident> ReportIncidents(CommandCentre centre)
    {
        List<Incident> board =
        [
            new("Three alpacas on the motorway", "E45 northbound",
                SeverityLevel.Critical, typeof(ICanDriveRescueVehicle)),
            new("Territorial swan occupying a bus stop", "Market Street stop C",
                SeverityLevel.Medium, typeof(ICanCalmAnimals)),
            new("Goat stranded on the library roof", "Central Library",
                SeverityLevel.High, typeof(ICanClimb)),
            new("Seagull has stolen a hot-dog stand", "Harbour promenade",
                SeverityLevel.Low),
            new("Cat in a tree that insists it is not an emergency", "Elm Road 14",
                SeverityLevel.Low, typeof(ICanClimb))
        ];

        foreach (Incident incident in board)
        {
            centre.ReportIncident(incident);
        }

        return board;
    }
}
