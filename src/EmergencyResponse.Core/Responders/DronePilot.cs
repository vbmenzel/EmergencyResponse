using EmergencyResponse.Core.Capabilities;
using EmergencyResponse.Core.Incidents;

namespace EmergencyResponse.Core.Responders;

/// <summary>
/// Scouts rooftops, trees and other places nobody can simply walk to. Works the
/// scene from the air first and only climbs when the incident calls for it.
/// </summary>
public sealed class DronePilot : Responder, ICanOperateDrone, ICanClimb
{
    /// <summary>The effort a pilot spends on the least demanding callout.</summary>
    private const int BaseCallOutCost = 4;

    /// <summary>Creates a drone pilot.</summary>
    /// <param name="name">The pilot's name.</param>
    /// <param name="energy">Starting energy, 0 to 100 inclusive.</param>
    public DronePilot(string name, int energy)
        : base(name, energy)
    {
    }

    /// <inheritdoc />
    /// <remarks>
    /// The survey always happens; the climb only when the incident requires
    /// <see cref="ICanClimb"/>. Staying on the ground for everything else is
    /// why a pilot is the cheapest of the three to send.
    /// </remarks>
    public override string HandleIncident(Incident incident)
    {
        ArgumentNullException.ThrowIfNull(incident);

        AdjustEnergy(-EnergyCostFor(incident.Severity, BaseCallOutCost));

        string survey = OperateDrone(incident);

        return incident.RequiredCapabilities.Contains(typeof(ICanClimb))
            ? $"{survey} {ClimbTo(incident.Location)}"
            : $"{survey} {Name} talks the ground team in from the launch point.";
    }

    /// <inheritdoc />
    public string OperateDrone(Incident incident)
    {
        ArgumentNullException.ThrowIfNull(incident);
        return $"{Name} puts the drone up over {incident.Location} and finds the animal.";
    }

    /// <inheritdoc />
    public string ClimbTo(string location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(location);
        return $"{Name} climbs up to {location} to bring it down by hand.";
    }
}
