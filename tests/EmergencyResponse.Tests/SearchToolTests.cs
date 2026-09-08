using EmergencyResponse.Core.Capabilities;
using EmergencyResponse.Core.Incidents;
using EmergencyResponse.Core.Responders;
using EmergencyResponse.Core.Search;

namespace EmergencyResponse.Tests;

public class SearchToolTests
{
    private static List<Responder> Responders() =>
    [
        new AnimalCatcher("Bo", 80),
        new DronePilot("Cyd", 10),
        new WildlifeCalmer("Dev", 95)
    ];

    private static List<Incident> Incidents() =>
    [
        new("Three alpacas on the motorway", "E45", SeverityLevel.Critical),
        new("Cat in a tree", "Elm Road", SeverityLevel.Low, typeof(ICanClimb)),
        new("Goat stranded on the library roof", "Central Library", SeverityLevel.High)
    ];

    [Fact]
    public void FindMatchesFiltersResponders()
    {
        List<Responder> energetic =
            SearchTool.FindMatches(Responders(), r => r.Energy > 50).ToList();

        Assert.Equal(["Bo", "Dev"], energetic.Select(r => r.Name));
    }

    [Fact]
    public void FindMatchesFiltersIncidents()
    {
        List<Incident> urgent = SearchTool
            .FindMatches(Incidents(), i => i.Severity >= SeverityLevel.High)
            .ToList();

        Assert.Equal(
            ["Three alpacas on the motorway", "Goat stranded on the library roof"],
            urgent.Select(i => i.Description));
    }

    [Fact]
    public void TheSameMethodServesBothCollectionTypes()
    {
        // Requirement 4 is that one generic method searches responders and
        // incidents alike. Nothing in SearchTool names either type.
        Assert.Single(SearchTool.FindMatches(Responders(), r => !r.IsAvailable || r.Energy < 50));
        Assert.Single(SearchTool.FindMatches(Incidents(), i => i.RequiredCapabilities.Count > 0));
    }

    [Fact]
    public void FindMatchesReturnsNothingWhenNothingMatches()
    {
        Assert.Empty(SearchTool.FindMatches(Responders(), r => r.Energy > Responder.MaxEnergy));
    }

    [Fact]
    public void FindMatchesPreservesSourceOrder()
    {
        Assert.Equal(
            ["Bo", "Cyd", "Dev"],
            SearchTool.FindMatches(Responders(), _ => true).Select(r => r.Name));
    }

    [Fact]
    public void FindFirstMatchReturnsTheFirstInSourceOrder()
    {
        Responder? found = SearchTool.FindFirstMatch(Responders(), r => r.Energy > 50);

        Assert.NotNull(found);
        Assert.Equal("Bo", found.Name);
    }

    [Fact]
    public void FindFirstMatchReturnsNullWhenNothingMatches()
    {
        Assert.Null(SearchTool.FindFirstMatch(Responders(), r => r.Energy < 0));
    }

    [Fact]
    public void FindMatchesDefersEvaluationUntilEnumerated()
    {
        int evaluations = 0;
        IEnumerable<int> matches = SearchTool.FindMatches([1, 2, 3], _ =>
        {
            evaluations++;
            return true;
        });

        Assert.Equal(0, evaluations);

        _ = matches.ToList();

        Assert.Equal(3, evaluations);
    }

    [Fact]
    public void FindMatchesRejectsNullArgumentsOnTheCallNotOnEnumeration()
    {
        // No .ToList() here on purpose. An iterator method's body does not run
        // until the first MoveNext, so validating inside it would let a caller
        // who never enumerates pass null and get no error at all.
        Assert.Throws<ArgumentNullException>(
            () => SearchTool.FindMatches<int>(null!, _ => true));
        Assert.Throws<ArgumentNullException>(
            () => SearchTool.FindMatches<int>([1, 2], null!));
    }

    [Fact]
    public void FindFirstMatchRejectsNullArguments()
    {
        Assert.Throws<ArgumentNullException>(
            () => SearchTool.FindFirstMatch<string>(null!, _ => true));
        Assert.Throws<ArgumentNullException>(
            () => SearchTool.FindFirstMatch<string>(["a"], null!));
    }
}
