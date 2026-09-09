using EmergencyResponse.Core.Assignment;
using EmergencyResponse.Core.Coordination;
using EmergencyResponse.Core.Incidents;
using EmergencyResponse.Core.Responders;

namespace EmergencyResponse.Tests;

[Collection("CommandCentre")]
public class CommandCentreSingletonTests : IDisposable
{
    public CommandCentreSingletonTests() => CommandCentre.ResetForTests();

    public void Dispose()
    {
        CommandCentre.ResetForTests();
        GC.SuppressFinalize(this);
    }

    private static CommandCentre NewCentre() =>
        CommandCentre.GetInstance(new FirstAvailableStrategy());

    [Fact]
    public void TheConstructorIsNotPubliclyReachable()
    {
        Assert.Empty(typeof(CommandCentre).GetConstructors());
    }

    [Fact]
    public void GetInstanceAlwaysReturnsTheSameCentre()
    {
        CommandCentre first = NewCentre();

        Assert.Same(first, CommandCentre.GetInstance());
        Assert.Same(first, CommandCentre.GetInstance());
    }

    [Fact]
    public void GetInstanceBeforeInitialisationThrows()
    {
        Assert.Throws<InvalidOperationException>(CommandCentre.GetInstance);
    }

    [Fact]
    public void ASecondGetInstanceWithAStrategyThrowsRatherThanSilentlyIgnoringIt()
    {
        NewCentre();

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => CommandCentre.GetInstance(new HighestEnergyAvailableStrategy()));

        Assert.Contains(nameof(CommandCentre.ChangeStrategy), error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GetInstanceRejectsANullStrategy()
    {
        Assert.Throws<ArgumentNullException>(() => CommandCentre.GetInstance(null!));
    }

    [Fact]
    public void ConcurrentCallersAllEndUpWithOneCentre()
    {
        CommandCentre[] centres = new CommandCentre[32];

        Parallel.For(0, centres.Length, i =>
        {
            try
            {
                centres[i] = CommandCentre.GetInstance(new FirstAvailableStrategy());
            }
            catch (InvalidOperationException)
            {
                // Another thread got there first, which is the contract.
                centres[i] = CommandCentre.GetInstance();
            }
        });

        Assert.Single(centres.Distinct());
    }

    [Fact]
    public void TheCentreStartsWithTheStrategyItWasGiven()
    {
        Assert.Equal("First available", NewCentre().CurrentStrategyName);
    }

    [Fact]
    public void ChangeStrategyReplacesThePolicy()
    {
        CommandCentre centre = NewCentre();

        centre.ChangeStrategy(new HighestEnergyAvailableStrategy());

        Assert.Equal("Highest energy available", centre.CurrentStrategyName);
    }

    [Fact]
    public void ChangeStrategyRejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => NewCentre().ChangeStrategy(null!));
    }

    [Fact]
    public void RegisteredRespondersAndIncidentsAreReadBack()
    {
        CommandCentre centre = NewCentre();
        AnimalCatcher bo = new("Bo", 80);
        Incident swan = new("Territorial swan occupying a bus stop", "Market Street", SeverityLevel.Medium);

        centre.RegisterResponder(bo);
        centre.ReportIncident(swan);

        Assert.Same(bo, Assert.Single(centre.Responders));
        Assert.Same(swan, Assert.Single(centre.Incidents));
    }

    [Fact]
    public void RegistrationOrderIsPreserved()
    {
        CommandCentre centre = NewCentre();
        centre.RegisterResponder(new AnimalCatcher("Bo", 80));
        centre.RegisterResponder(new WildlifeCalmer("Dev", 95));
        centre.RegisterResponder(new DronePilot("Cyd", 70));

        Assert.Equal(["Bo", "Dev", "Cyd"], centre.Responders.Select(r => r.Name));
    }

    [Fact]
    public void RegisteringTheSameResponderTwiceThrows()
    {
        CommandCentre centre = NewCentre();
        AnimalCatcher bo = new("Bo", 80);
        centre.RegisterResponder(bo);

        Assert.Throws<InvalidOperationException>(() => centre.RegisterResponder(bo));
    }

    [Fact]
    public void TwoDifferentRespondersMayShareAName()
    {
        // Identity is the object, not the name. Two people called Bo is a
        // staffing question, not a modelling error.
        CommandCentre centre = NewCentre();
        centre.RegisterResponder(new AnimalCatcher("Bo", 80));
        centre.RegisterResponder(new WildlifeCalmer("Bo", 60));

        Assert.Equal(2, centre.Responders.Count);
    }

    [Fact]
    public void ReportingTheSameIncidentTwiceThrows()
    {
        CommandCentre centre = NewCentre();
        Incident swan = new("Territorial swan occupying a bus stop", "Market Street", SeverityLevel.Medium);
        centre.ReportIncident(swan);

        Assert.Throws<InvalidOperationException>(() => centre.ReportIncident(swan));
    }

    [Fact]
    public void RegistrationRejectsNulls()
    {
        CommandCentre centre = NewCentre();

        Assert.Throws<ArgumentNullException>(() => centre.RegisterResponder(null!));
        Assert.Throws<ArgumentNullException>(() => centre.ReportIncident(null!));
        Assert.Throws<ArgumentNullException>(() => centre.AddResolutionCallback(null!));
    }

    [Fact]
    public void RespondersHandsOutASnapshotNotTheLiveList()
    {
        CommandCentre centre = NewCentre();
        centre.RegisterResponder(new AnimalCatcher("Bo", 80));

        IReadOnlyList<Responder> snapshot = centre.Responders;
        centre.RegisterResponder(new WildlifeCalmer("Dev", 95));

        // Had this been the live list, the snapshot would now show two, and a
        // caller enumerating it while another thread registered would throw.
        Assert.Single(snapshot);
        Assert.Equal(2, centre.Responders.Count);
    }

    [Fact]
    public void IncidentsHandsOutASnapshotNotTheLiveList()
    {
        CommandCentre centre = NewCentre();
        centre.ReportIncident(new("Cat in a tree", "Elm Road", SeverityLevel.Low));

        IReadOnlyList<Incident> snapshot = centre.Incidents;
        centre.ReportIncident(new("Goat on the roof", "Library", SeverityLevel.High));

        Assert.Single(snapshot);
        Assert.Equal(2, centre.Incidents.Count);
    }

    [Fact]
    public void ConcurrentRegistrationLosesNobody()
    {
        CommandCentre centre = NewCentre();

        Parallel.For(0, 100, i => centre.RegisterResponder(new AnimalCatcher($"Bo {i}", 80)));

        Assert.Equal(100, centre.Responders.Count);
        Assert.Equal(100, centre.Responders.Select(r => r.Name).Distinct().Count());
    }
}
