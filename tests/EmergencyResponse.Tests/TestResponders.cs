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
