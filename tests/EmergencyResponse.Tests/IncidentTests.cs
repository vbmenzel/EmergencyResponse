using EmergencyResponse.Core.Exceptions;
using EmergencyResponse.Core.Incidents;

namespace EmergencyResponse.Tests;

public class IncidentTests
{
    private static Incident NewIncident() =>
        new("Goat stranded on the library roof", "Central Library", SeverityLevel.High);

    [Fact]
    public void NewIncidentIsReportedAndUnassigned()
    {
        Incident incident = NewIncident();

        Assert.Equal(IncidentStatus.Reported, incident.Status);
        Assert.Null(incident.AssignedResponder);
        Assert.Null(incident.ResolutionNote);
        Assert.NotEqual(Guid.Empty, incident.Id);
        Assert.Empty(incident.RequiredCapabilities);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ConstructorRejectsBlankDescription(string description)
    {
        Assert.Throws<ArgumentException>(
            () => new Incident(description, "Central Library", SeverityLevel.Low));
    }

    [Fact]
    public void ConstructorRejectsBlankLocation()
    {
        Assert.Throws<ArgumentException>(
            () => new Incident("Cat in a tree", "  ", SeverityLevel.Low));
    }

    [Fact]
    public void ConstructorRejectsACapabilityThatIsNotAnInterface()
    {
        Assert.Throws<ArgumentException>(
            () => new Incident("Cat in a tree", "Elm Road", SeverityLevel.Low, typeof(string)));
    }

    [Fact]
    public void ResolvingAnUnassignedIncidentThrows()
    {
        Incident incident = NewIncident();

        Assert.Throws<InvalidOperationException>(() => incident.Resolve("Goat lifted down"));
    }

    [Fact]
    public void ResolvingWithABlankNoteThrows()
    {
        Incident incident = NewIncident();
        incident.AssignResponder(new TestResponder("Ada", 50));

        Assert.Throws<ArgumentException>(() => incident.Resolve("   "));
    }

    [Fact]
    public void AssigningASecondResponderThrows()
    {
        Incident incident = NewIncident();
        incident.AssignResponder(new TestResponder("Ada", 50));

        Assert.Throws<ResponderUnavailableException>(
            () => incident.AssignResponder(new TestResponder("Bo", 50)));
    }

    [Fact]
    public void AResolvedIncidentCannotBeAssignedAgain()
    {
        Incident incident = NewIncident();
        incident.AssignResponder(new TestResponder("Ada", 50));
        incident.Resolve("Goat lifted down with a ladder");

        Assert.Equal(IncidentStatus.Resolved, incident.Status);
        Assert.Equal("Goat lifted down with a ladder", incident.ResolutionNote);
        Assert.Throws<InvalidOperationException>(
            () => incident.AssignResponder(new TestResponder("Bo", 50)));
    }

    [Fact]
    public void RequiredCapabilitiesIsADefensiveCopy()
    {
        Type[] required = [typeof(IDisposable)];
        Incident incident = new("Cat in a tree", "Elm Road", SeverityLevel.Low, required);

        required[0] = typeof(IFormattable);

        Assert.Single(incident.RequiredCapabilities);
        Assert.Contains(typeof(IDisposable), incident.RequiredCapabilities);
    }
}
