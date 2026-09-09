using EmergencyResponse.Core.Capabilities;
using EmergencyResponse.Core.Incidents;
using EmergencyResponse.Core.Responders;

namespace EmergencyResponse.Tests;

public class ConcreteResponderTests
{
    private static Incident PlainIncident() =>
        new("Seagull has stolen a hot-dog stand", "Harbour promenade", SeverityLevel.Medium);

    private static Incident RooftopIncident() =>
        new("Goat stranded on the library roof", "Central Library",
            SeverityLevel.High, typeof(ICanClimb));

    [Fact]
    public void EachResponderTypeHandlesAnIncidentDifferently()
    {
        Incident incident = PlainIncident();

        string catcher = new AnimalCatcher("Bo", 80).HandleIncident(incident);
        string pilot = new DronePilot("Cyd", 80).HandleIncident(incident);
        string calmer = new WildlifeCalmer("Dev", 80).HandleIncident(incident);

        Assert.Equal(3, new HashSet<string> { catcher, pilot, calmer }.Count);
        Assert.Contains("net", catcher, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("drone", pilot, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("calm", calmer, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EachResponderReportsItsOwnName()
    {
        Incident incident = PlainIncident();

        Assert.Contains("Bo", new AnimalCatcher("Bo", 80).HandleIncident(incident), StringComparison.Ordinal);
        Assert.Contains("Cyd", new DronePilot("Cyd", 80).HandleIncident(incident), StringComparison.Ordinal);
        Assert.Contains("Dev", new WildlifeCalmer("Dev", 80).HandleIncident(incident), StringComparison.Ordinal);
    }

    [Fact]
    public void AnimalCatcherDrivesToTheIncidentLocation()
    {
        AnimalCatcher catcher = new("Bo", 80);

        Assert.Contains("Harbour promenade", catcher.HandleIncident(PlainIncident()), StringComparison.Ordinal);
        Assert.Contains("Elm Road", catcher.DriveTo("Elm Road"), StringComparison.Ordinal);
    }

    [Fact]
    public void DronePilotClimbsOnlyWhenTheIncidentRequiresIt()
    {
        Assert.DoesNotContain("climb", new DronePilot("Cyd", 80).HandleIncident(PlainIncident()),
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains("climb", new DronePilot("Cyd", 80).HandleIncident(RooftopIncident()),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DronePilotSatisfiesBothOfItsCapabilities()
    {
        Incident needsBoth = new("Goat stranded on the library roof", "Central Library",
            SeverityLevel.High, typeof(ICanClimb), typeof(ICanOperateDrone));

        Assert.True(new DronePilot("Cyd", 80).CanHandle(needsBoth));
        Assert.False(new AnimalCatcher("Bo", 80).CanHandle(needsBoth));
        Assert.False(new WildlifeCalmer("Dev", 80).CanHandle(needsBoth));
    }

    [Fact]
    public void EachResponderTypeSatisfiesOnlyItsOwnCapabilities()
    {
        Assert.IsAssignableFrom<ICanDriveRescueVehicle>(new AnimalCatcher("Bo", 80));
        Assert.IsAssignableFrom<ICanCalmAnimals>(new WildlifeCalmer("Dev", 80));
        // Asserted on the types, not on instances: the responder classes are
        // sealed, so `instance is ICanCalmAnimals` is a compile-time constant
        // and the compiler rejects it as a pointless check (CS0184).
        Assert.False(typeof(ICanCalmAnimals).IsAssignableFrom(typeof(AnimalCatcher)));
        Assert.False(typeof(ICanClimb).IsAssignableFrom(typeof(WildlifeCalmer)));
        Assert.False(typeof(ICanOperateDrone).IsAssignableFrom(typeof(AnimalCatcher)));
    }

    [Fact]
    public void HandlingAnIncidentCostsEnergy()
    {
        AnimalCatcher catcher = new("Bo", 80);

        catcher.HandleIncident(PlainIncident());

        Assert.True(catcher.Energy < 80);
    }

    [Fact]
    public void ACriticalIncidentCostsMoreThanALowOne()
    {
        AnimalCatcher hardJob = new("Bo", Responder.MaxEnergy);
        AnimalCatcher easyJob = new("Eli", Responder.MaxEnergy);

        hardJob.HandleIncident(new("Three alpacas on the motorway", "E45", SeverityLevel.Critical));
        easyJob.HandleIncident(new("Cat in a tree", "Elm Road", SeverityLevel.Low));

        Assert.True(hardJob.Energy < easyJob.Energy);
    }

    [Fact]
    public void TheDronePilotSpendsLeastEnergyStayingOnTheGround()
    {
        Incident incident = PlainIncident();

        AnimalCatcher catcher = new("Bo", Responder.MaxEnergy);
        DronePilot pilot = new("Cyd", Responder.MaxEnergy);
        catcher.HandleIncident(incident);
        pilot.HandleIncident(incident);

        Assert.True(pilot.Energy > catcher.Energy);
    }

    [Fact]
    public void RepeatedCalloutsNeverPushEnergyBelowZero()
    {
        WildlifeCalmer calmer = new("Dev", 10);

        for (int i = 0; i < 20; i++)
        {
            calmer.HandleIncident(new("Swan again", "Market Street", SeverityLevel.Critical));
        }

        Assert.Equal(Responder.MinEnergy, calmer.Energy);
    }

    [Fact]
    public void HandleIncidentRejectsANullIncident()
    {
        Assert.Throws<ArgumentNullException>(() => new AnimalCatcher("Bo", 80).HandleIncident(null!));
        Assert.Throws<ArgumentNullException>(() => new DronePilot("Cyd", 80).HandleIncident(null!));
        Assert.Throws<ArgumentNullException>(() => new WildlifeCalmer("Dev", 80).HandleIncident(null!));
    }
}
