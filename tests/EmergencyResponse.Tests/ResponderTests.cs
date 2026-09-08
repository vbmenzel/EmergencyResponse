using EmergencyResponse.Core.Capabilities;
using EmergencyResponse.Core.Exceptions;
using EmergencyResponse.Core.Incidents;
using EmergencyResponse.Core.Responders;

namespace EmergencyResponse.Tests;

public class ResponderTests
{
    private static Incident PlainIncident() =>
        new("Territorial swan occupying a bus stop", "Market Street", SeverityLevel.Medium);

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void ConstructorRejectsEnergyOutsideZeroToOneHundred(int energy)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TestResponder("Ada", energy));
    }

    [Theory]
    [InlineData(Responder.MinEnergy)]
    [InlineData(Responder.MaxEnergy)]
    public void ConstructorAcceptsBothEndsOfTheRange(int energy)
    {
        Assert.Equal(energy, new TestResponder("Ada", energy).Energy);
    }

    [Fact]
    public void ConstructorRejectsABlankName()
    {
        Assert.Throws<ArgumentException>(() => new TestResponder("   ", 50));
    }

    [Fact]
    public void NewResponderIsAvailable()
    {
        Assert.True(new TestResponder("Ada", 50).IsAvailable);
    }

    [Fact]
    public void AdjustEnergyClampsToTheValidRange()
    {
        TestResponder responder = new("Ada", 90);

        responder.AdjustEnergy(50);
        Assert.Equal(Responder.MaxEnergy, responder.Energy);

        responder.AdjustEnergy(-500);
        Assert.Equal(Responder.MinEnergy, responder.Energy);
    }

    [Fact]
    public void ExhaustedResponderIsNotEligible()
    {
        Assert.False(new TestResponder("Ada", 0).IsEligibleFor(PlainIncident()));
    }

    [Fact]
    public void BusyResponderIsNotEligible()
    {
        TestResponder responder = new("Ada", 80);
        responder.AssignTo(PlainIncident());

        Assert.False(responder.IsEligibleFor(PlainIncident()));
    }

    private static Incident RooftopIncident() =>
        new("Goat stranded on the library roof", "Central Library",
            SeverityLevel.High, typeof(ICanClimb));

    [Fact]
    public void ResponderMissingARequiredCapabilityIsNotEligible()
    {
        Assert.False(new TestResponder("Ada", 80).IsEligibleFor(RooftopIncident()));
    }

    [Fact]
    public void ResponderWithTheRequiredCapabilityIsEligible()
    {
        Assert.True(new ClimbingTestResponder("Cyd", 80).IsEligibleFor(RooftopIncident()));
    }

    [Fact]
    public void CapabilityAloneIsNotEnoughWithoutEnergy()
    {
        Assert.False(new ClimbingTestResponder("Cyd", 0).IsEligibleFor(RooftopIncident()));
    }

    [Fact]
    public void AssigningABusyResponderThrows()
    {
        TestResponder responder = new("Ada", 50);
        responder.AssignTo(PlainIncident());

        Assert.Throws<ResponderUnavailableException>(
            () => responder.AssignTo(PlainIncident()));
    }

    [Fact]
    public void AssigningAnExhaustedResponderThrows()
    {
        Assert.Throws<ResponderUnavailableException>(
            () => new TestResponder("Ada", 0).AssignTo(PlainIncident()));
    }

    [Fact]
    public void ReleaseMakesTheResponderAvailableAgain()
    {
        TestResponder responder = new("Ada", 50);
        responder.AssignTo(PlainIncident());

        responder.Release();

        Assert.True(responder.IsAvailable);
    }

    [Fact]
    public void ReleaseIsSafeToCallOnAnAvailableResponder()
    {
        TestResponder responder = new("Ada", 50);

        responder.Release();

        Assert.True(responder.IsAvailable);
    }
}
