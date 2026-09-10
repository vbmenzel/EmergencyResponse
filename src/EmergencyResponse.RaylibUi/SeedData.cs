using EmergencyResponse.Core.Capabilities;
using EmergencyResponse.Core.Coordination;
using EmergencyResponse.Core.Incidents;
using EmergencyResponse.Core.Responders;

namespace EmergencyResponse.RaylibUi;

/// <summary>The roster and the opening call log for the board.</summary>
internal static class SeedData
{
    /// <summary>Registers the unit's responders.</summary>
    /// <param name="centre">The centre to register with.</param>
    internal static void RegisterResponders(CommandCentre centre)
    {
        centre.RegisterResponder(new AnimalCatcher("Bo Nielsen", 60));
        centre.RegisterResponder(new DronePilot("Cyd Rahman", 85));
        centre.RegisterResponder(new WildlifeCalmer("Dev Okonkwo", 95));
        centre.RegisterResponder(new AnimalCatcher("Eli Sorensen", 40));
        centre.RegisterResponder(new DronePilot("Fay Lindqvist", 15));
    }

    /// <summary>Reports the incidents already on the board at start of shift.</summary>
    /// <param name="centre">The centre to report to.</param>
    internal static void ReportIncidents(CommandCentre centre)
    {
        foreach (Incident incident in Opening())
        {
            centre.ReportIncident(incident);
        }
    }

    /// <summary>Builds a fresh incident for the "new callout" button.</summary>
    /// <param name="number">How many have been called in so far.</param>
    /// <returns>The next incident.</returns>
    internal static Incident NextCallout(int number)
    {
        (string Description, string Location, SeverityLevel Severity, Type[] Needs)[] pool =
        [
            ("Fox raiding the school bins", "Riverside School", SeverityLevel.Medium, []),
            ("Heron queueing at the fishmonger", "Market Hall", SeverityLevel.Low, []),
            ("Peacock on the bypass", "Ring Road", SeverityLevel.Critical, [typeof(ICanDriveRescueVehicle)]),
            ("Owl asleep in the bell tower", "St Mary's", SeverityLevel.High, [typeof(ICanClimb)]),
            ("Badger holding the allotments", "Allotment Way", SeverityLevel.Medium, [typeof(ICanCalmAnimals)])
        ];

        (string description, string location, SeverityLevel severity, Type[] needs) =
            pool[number % pool.Length];

        return new Incident(description, location, severity, needs);
    }

    /// <summary>The five incidents the shift starts with.</summary>
    /// <returns>The opening board.</returns>
    private static IEnumerable<Incident> Opening()
    {
        yield return new Incident("Three alpacas on the motorway", "E45 northbound",
            SeverityLevel.Critical, typeof(ICanDriveRescueVehicle));
        yield return new Incident("Territorial swan occupying a bus stop", "Market Street stop C",
            SeverityLevel.Medium, typeof(ICanCalmAnimals));
        yield return new Incident("Goat stranded on the library roof", "Central Library",
            SeverityLevel.High, typeof(ICanClimb));
        yield return new Incident("Seagull has stolen a hot-dog stand", "Harbour promenade",
            SeverityLevel.Low);
        yield return new Incident("Cat in a tree that insists it is not an emergency", "Elm Road 14",
            SeverityLevel.Low, typeof(ICanClimb));
    }
}
