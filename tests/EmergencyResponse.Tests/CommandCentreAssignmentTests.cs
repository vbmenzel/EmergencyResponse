using EmergencyResponse.Core.Assignment;
using EmergencyResponse.Core.Capabilities;
using EmergencyResponse.Core.Coordination;
using EmergencyResponse.Core.Exceptions;
using EmergencyResponse.Core.Incidents;
using EmergencyResponse.Core.Responders;

namespace EmergencyResponse.Tests;

[Collection("CommandCentre")]
public class CommandCentreAssignmentTests : IDisposable
{
    private readonly CommandCentre centre;

    public CommandCentreAssignmentTests()
    {
        CommandCentre.ResetForTests();
        centre = CommandCentre.GetInstance(new FirstAvailableStrategy());
    }

    public void Dispose()
    {
        CommandCentre.ResetForTests();
        GC.SuppressFinalize(this);
    }

    private Incident Report(string description, SeverityLevel severity, params Type[] capabilities)
    {
        Incident incident = new(description, "Town centre", severity, capabilities);
        centre.ReportIncident(incident);
        return incident;
    }

    [Fact]
    public void AssignRecordsWhoIsOnItAndTakesThemOutOfCirculation()
    {
        AnimalCatcher bo = new("Bo", 80);
        centre.RegisterResponder(bo);
        Incident alpacas = Report("Three alpacas on the motorway", SeverityLevel.Critical);

        Responder chosen = centre.AssignIncident(alpacas);

        Assert.Same(bo, chosen);
        Assert.Same(bo, centre.GetAssignedResponder(alpacas));
        Assert.Equal(IncidentStatus.Assigned, alpacas.Status);
        Assert.False(centre.IsAvailable(bo));
        Assert.Empty(centre.AvailableResponders);
    }

    [Fact]
    public void AssignUsesWhicheverStrategyIsCurrent()
    {
        centre.RegisterResponder(new AnimalCatcher("Bo", 40));
        centre.RegisterResponder(new WildlifeCalmer("Dev", 95));

        Assert.Equal("Bo", centre.AssignIncident(Report("First", SeverityLevel.Low)).Name);

        centre.ChangeStrategy(new HighestEnergyAvailableStrategy());

        Assert.Equal("Dev", centre.AssignIncident(Report("Second", SeverityLevel.Low)).Name);
    }

    [Fact]
    public void AssignThrowsWhenEverybodyIsAlreadyOut()
    {
        centre.RegisterResponder(new AnimalCatcher("Bo", 80));
        centre.AssignIncident(Report("First", SeverityLevel.Low));

        Assert.Throws<NoSuitableResponderException>(
            () => centre.AssignIncident(Report("Second", SeverityLevel.Low)));
    }

    [Fact]
    public void AssignThrowsWhenNobodyHasTheRequiredCapability()
    {
        centre.RegisterResponder(new AnimalCatcher("Bo", 80));

        Assert.Throws<NoSuitableResponderException>(() => centre.AssignIncident(
            Report("Goat stranded on the library roof", SeverityLevel.High, typeof(ICanClimb))));
    }

    [Fact]
    public void AssignSpecificResponderReportsWhyANamedResponderCannotGo()
    {
        AnimalCatcher bo = new("Bo", 80);
        centre.RegisterResponder(bo);
        centre.AssignIncident(Report("First", SeverityLevel.Low));

        // AssignIncident could not surface this: its strategy filters an
        // unavailable responder out and raises the other exception instead.
        ResponderUnavailableException error = Assert.Throws<ResponderUnavailableException>(
            () => centre.AssignSpecificResponder(Report("Second", SeverityLevel.Low), bo));

        Assert.Contains("Bo", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AssignSpecificResponderRejectsSomebodyLackingTheCapability()
    {
        AnimalCatcher bo = new("Bo", 80);
        centre.RegisterResponder(bo);

        Assert.Throws<ResponderUnavailableException>(() => centre.AssignSpecificResponder(
            Report("Goat stranded on the library roof", SeverityLevel.High, typeof(ICanClimb)), bo));
    }

    [Fact]
    public void AssignSpecificResponderOverridesThePolicysChoice()
    {
        WildlifeCalmer dev = new("Dev", 95);
        centre.RegisterResponder(new AnimalCatcher("Bo", 40));
        centre.RegisterResponder(dev);
        Incident swan = Report("Territorial swan occupying a bus stop", SeverityLevel.Medium);

        // First-available would have picked Bo.
        Assert.Same(dev, centre.AssignSpecificResponder(swan, dev));
        Assert.Same(dev, centre.GetAssignedResponder(swan));
    }

    [Fact]
    public void ResolveRunsEveryCallbackInRegistrationOrder()
    {
        List<string> log = [];
        void Named(Incident i) => log.Add($"named:{i.Description}");

        centre.AddResolutionCallback(Named);
        centre.AddResolutionCallback(i => log.Add($"lambda:{i.ResolutionNote}"));

        centre.RegisterResponder(new AnimalCatcher("Bo", 80));
        Incident swan = Report("Territorial swan occupying a bus stop", SeverityLevel.Medium);
        centre.AssignIncident(swan);

        centre.ResolveIncident(swan, "Swan escorted to the pond");

        Assert.Equal(
            ["named:Territorial swan occupying a bus stop", "lambda:Swan escorted to the pond"],
            log);
        Assert.Equal(IncidentStatus.Resolved, swan.Status);
    }

    [Fact]
    public void ResolvingFreesTheResponderWithNoSeparateReleaseStep()
    {
        AnimalCatcher bo = new("Bo", 80);
        centre.RegisterResponder(bo);
        Incident swan = Report("Territorial swan occupying a bus stop", SeverityLevel.Medium);
        centre.AssignIncident(swan);
        Assert.False(centre.IsAvailable(bo));

        centre.ResolveIncident(swan, "Swan escorted to the pond");

        // Availability is derived from open assignments, so closing the
        // incident is what frees the responder. There is nothing to forget.
        Assert.True(centre.IsAvailable(bo));
        Assert.Same(bo, centre.AvailableResponders.Single());
    }

    [Fact]
    public void AResolvedIncidentStillRemembersWhoHandledIt()
    {
        AnimalCatcher bo = new("Bo", 80);
        centre.RegisterResponder(bo);
        Incident swan = Report("Territorial swan occupying a bus stop", SeverityLevel.Medium);
        centre.AssignIncident(swan);

        centre.ResolveIncident(swan, "Swan escorted to the pond");

        Assert.Same(bo, centre.GetAssignedResponder(swan));
    }

    [Fact]
    public void CallbacksRunOutsideTheLockSoOtherThreadsAreNotBlocked()
    {
        centre.RegisterResponder(new AnimalCatcher("Bo", 80));
        Incident swan = Report("Territorial swan occupying a bus stop", SeverityLevel.Medium);
        centre.AssignIncident(swan);

        using ManualResetEventSlim otherThreadGotIn = new(false);
        centre.AddResolutionCallback(resolved =>
        {
            Thread other = new(() =>
            {
                _ = resolved.Description;
                _ = centre.Responders.Count;   // needs assignmentLock
                otherThreadGotIn.Set();
            });
            other.Start();
            otherThreadGotIn.Wait(TimeSpan.FromSeconds(5));
        });

        centre.ResolveIncident(swan, "Swan escorted to the pond");

        Assert.True(otherThreadGotIn.IsSet);
    }

    [Fact]
    public void ResolveRejectsAnUnassignedIncidentAndABlankNote()
    {
        centre.RegisterResponder(new AnimalCatcher("Bo", 80));
        Incident cat = Report("Cat in a tree", SeverityLevel.Low);

        Assert.Throws<InvalidOperationException>(() => centre.ResolveIncident(cat, "done"));

        centre.AssignIncident(cat);
        Assert.Throws<ArgumentException>(() => centre.ResolveIncident(cat, "  "));
    }

    [Fact]
    public void AssignmentAndResolutionRejectNulls()
    {
        Assert.Throws<ArgumentNullException>(() => centre.AssignIncident(null!));
        Assert.Throws<ArgumentNullException>(() => centre.ResolveIncident(null!, "done"));
        Assert.Throws<ArgumentNullException>(() => centre.IsAvailable(null!));
        Assert.Throws<ArgumentNullException>(() => centre.GetAssignedResponder(null!));
        Assert.Throws<ArgumentNullException>(
            () => centre.AssignSpecificResponder(null!, new AnimalCatcher("Bo", 80)));
    }

    [Fact]
    public void ReassigningAnIncidentThatAlreadyHasSomebodyChangesNothing()
    {
        AnimalCatcher bo = new("Bo", 80);
        WildlifeCalmer dev = new("Dev", 95);
        centre.RegisterResponder(bo);
        centre.RegisterResponder(dev);
        Incident swan = Report("Territorial swan occupying a bus stop", SeverityLevel.Medium);
        centre.AssignIncident(swan);

        Assert.Throws<ResponderUnavailableException>(() => centre.AssignIncident(swan));

        Assert.True(centre.IsAvailable(dev));
        Assert.Same(bo, centre.GetAssignedResponder(swan));
    }

    [Fact]
    public void AssigningAResolvedIncidentChangesNothing()
    {
        AnimalCatcher bo = new("Bo", 80);
        centre.RegisterResponder(bo);
        Incident swan = Report("Territorial swan occupying a bus stop", SeverityLevel.Medium);
        centre.AssignIncident(swan);
        centre.ResolveIncident(swan, "Swan escorted to the pond");

        Assert.Throws<InvalidOperationException>(() => centre.AssignIncident(swan));

        Assert.True(centre.IsAvailable(bo));
    }

    [Fact]
    public void ConcurrentAssignmentsNeverDoubleBookAResponder()
    {
        centre.RegisterResponder(new AnimalCatcher("Bo", 100));
        centre.RegisterResponder(new WildlifeCalmer("Dev", 100));

        List<Incident> incidents = [.. Enumerable.Range(0, 16)
            .Select(i => Report($"Incident {i}", SeverityLevel.Medium))];

        Parallel.ForEach(incidents, incident =>
        {
            try
            {
                centre.AssignIncident(incident);
            }
            catch (NoSuitableResponderException)
            {
                // Expected once both responders are out.
            }
        });

        List<Responder> assigned = [.. incidents
            .Select(centre.GetAssignedResponder)
            .Where(r => r is not null)
            .Select(r => r!)];

        Assert.Equal(2, assigned.Count);
        Assert.Equal(assigned.Count, assigned.Distinct().Count());
    }
}
