using EmergencyResponse.Core.Capabilities;
using EmergencyResponse.Core.Incidents;
using EmergencyResponse.Core.Responders;

namespace EmergencyResponse.Tests;

public class ResponderTests
{
    private static Incident PlainIncident() =>
        new("Territorial swan occupying a bus stop", "Market Street", SeverityLevel.Medium);

    private static Incident RooftopIncident() =>
        new("Goat stranded on the library roof", "Central Library",
            SeverityLevel.High, typeof(ICanClimb));

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
    public void AdjustEnergyClampsToTheValidRange()
    {
        TestResponder responder = new("Ada", 90);

        responder.AdjustEnergy(50);
        Assert.Equal(Responder.MaxEnergy, responder.Energy);

        responder.AdjustEnergy(-500);
        Assert.Equal(Responder.MinEnergy, responder.Energy);
    }

    [Fact]
    public void AnyResponderCanHandleAnIncidentThatDemandsNothingSpecial()
    {
        Assert.True(new TestResponder("Ada", 50).CanHandle(PlainIncident()));
    }

    [Fact]
    public void AnExhaustedResponderCanHandleNothing()
    {
        Assert.False(new TestResponder("Ada", Responder.MinEnergy).CanHandle(PlainIncident()));
    }

    [Fact]
    public void AResponderMissingARequiredCapabilityCannotHandleIt()
    {
        Assert.False(new TestResponder("Ada", 80).CanHandle(RooftopIncident()));
    }

    [Fact]
    public void AResponderWithTheRequiredCapabilityCanHandleIt()
    {
        Assert.True(new ClimbingTestResponder("Cyd", 80).CanHandle(RooftopIncident()));
    }

    [Fact]
    public void CapabilityAloneIsNotEnoughWithoutEnergy()
    {
        Assert.False(new ClimbingTestResponder("Cyd", 0).CanHandle(RooftopIncident()));
    }

    [Fact]
    public void CanHandleRejectsANullIncident()
    {
        Assert.Throws<ArgumentNullException>(() => new TestResponder("Ada", 50).CanHandle(null!));
    }
}
