using EmergencyResponse.Core.Assignment;
using EmergencyResponse.Core.Capabilities;
using EmergencyResponse.Core.Exceptions;
using EmergencyResponse.Core.Incidents;
using EmergencyResponse.Core.Responders;

namespace EmergencyResponse.Tests;

public class AssignmentStrategyTests
{
    private static List<Responder> Pool() =>
    [
        new AnimalCatcher("Bo", 40),
        new WildlifeCalmer("Dev", 95),
        new DronePilot("Cyd", 70)
    ];

    private static Incident PlainIncident() =>
        new("Seagull has stolen a hot-dog stand", "Harbour promenade", SeverityLevel.Medium);

    private static Incident RooftopIncident() =>
        new("Goat stranded on the library roof", "Central Library",
            SeverityLevel.High, typeof(ICanClimb));

    public static TheoryData<IAssignmentStrategy> BothStrategies() =>
    [
        new FirstAvailableStrategy(),
        new HighestEnergyAvailableStrategy()
    ];

    [Fact]
    public void FirstAvailablePicksRegistrationOrder()
    {
        Assert.Equal("Bo",
            new FirstAvailableStrategy().SelectResponder(Pool(), PlainIncident()).Name);
    }

    [Fact]
    public void HighestEnergyPicksTheMostRestedResponder()
    {
        Assert.Equal("Dev",
            new HighestEnergyAvailableStrategy().SelectResponder(Pool(), PlainIncident()).Name);
    }

    [Theory]
    [MemberData(nameof(BothStrategies))]
    public void EveryStrategyRespectsRequiredCapabilities(IAssignmentStrategy strategy)
    {
        // Neither strategy mentions DronePilot. They only ask IsEligibleFor,
        // and the drone pilot is the only responder who can climb.
        Assert.Equal("Cyd", strategy.SelectResponder(Pool(), RooftopIncident()).Name);
    }

    [Theory]
    [MemberData(nameof(BothStrategies))]
    public void EveryStrategySkipsExhaustedResponders(IAssignmentStrategy strategy)
    {
        List<Responder> pool = [new AnimalCatcher("Bo", 0), new WildlifeCalmer("Dev", 30)];

        Assert.Equal("Dev", strategy.SelectResponder(pool, PlainIncident()).Name);
    }

    [Theory]
    [MemberData(nameof(BothStrategies))]
    public void EveryStrategyThrowsWhenNobodyIsEligible(IAssignmentStrategy strategy)
    {
        List<Responder> exhausted = [new AnimalCatcher("Bo", 0)];

        NoSuitableResponderException error = Assert.Throws<NoSuitableResponderException>(
            () => strategy.SelectResponder(exhausted, PlainIncident()));

        // The console prints this message, so it has to say which incident and
        // which policy could not be satisfied.
        Assert.Contains("Seagull has stolen a hot-dog stand", error.Message, StringComparison.Ordinal);
        Assert.Contains(strategy.Name, error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(BothStrategies))]
    public void EveryStrategyThrowsOnAnEmptyPool(IAssignmentStrategy strategy)
    {
        Assert.Throws<NoSuitableResponderException>(
            () => strategy.SelectResponder([], PlainIncident()));
    }

    [Theory]
    [MemberData(nameof(BothStrategies))]
    public void EveryStrategyRejectsNullArguments(IAssignmentStrategy strategy)
    {
        Assert.Throws<ArgumentNullException>(
            () => strategy.SelectResponder(null!, PlainIncident()));
        Assert.Throws<ArgumentNullException>(
            () => strategy.SelectResponder(Pool(), null!));
    }

    [Fact]
    public void HighestEnergyBreaksATieBySourceOrder()
    {
        List<Responder> tied =
        [
            new AnimalCatcher("Bo", 70),
            new WildlifeCalmer("Dev", 70)
        ];

        Assert.Equal("Bo",
            new HighestEnergyAvailableStrategy().SelectResponder(tied, PlainIncident()).Name);
    }

    [Fact]
    public void EachStrategyReportsItsName()
    {
        Assert.Equal("First available", new FirstAvailableStrategy().Name);
        Assert.Equal("Highest energy available", new HighestEnergyAvailableStrategy().Name);
    }

    [Fact]
    public void SelectingChangesNothing()
    {
        // Selection is a pure query over a pool the centre has already filtered
        // down to free responders. Recording the assignment is the centre's job.
        List<Responder> pool = Pool();
        int[] before = [.. pool.Select(r => r.Energy)];

        new FirstAvailableStrategy().SelectResponder(pool, PlainIncident());

        Assert.Equal(before, pool.Select(r => r.Energy));
        Assert.Equal(3, pool.Count);
    }
}
