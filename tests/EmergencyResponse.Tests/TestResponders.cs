using EmergencyResponse.Core.Capabilities;
using EmergencyResponse.Core.Incidents;
using EmergencyResponse.Core.Responders;

namespace EmergencyResponse.Tests;

/// A minimal concrete responder, so the abstract base can be tested without
/// depending on the three real responder types, which arrive in Task 5.
internal sealed class TestResponder : Responder
{
    public TestResponder(string name, int energy)
        : base(name, energy)
    {
    }

    public override string HandleIncident(Incident incident) =>
        $"{Name} handled {incident.Description}";
}

/// A responder that can climb, for exercising the eligible branch of
/// Responder.IsEligibleFor against a proper capability interface.
internal sealed class ClimbingTestResponder : Responder, ICanClimb
{
    public ClimbingTestResponder(string name, int energy)
        : base(name, energy)
    {
    }

    public override string HandleIncident(Incident incident) =>
        $"{Name} handled {incident.Description}";

    public string ClimbTo(string location) => $"{Name} climbed to {location}";
}
