using EmergencyResponse.Core.Capabilities;
using EmergencyResponse.Core.Incidents;

namespace EmergencyResponse.Core.Responders;

/// <summary>
/// Handles escaped or cornered animals on the ground, with nets, treats and a
/// crate. Drives the unit's rescue vehicle, which is what makes road-blocking
/// incidents reachable.
/// </summary>
public sealed class AnimalCatcher : Responder, ICanDriveRescueVehicle
{
    /// <summary>The effort a catcher spends on the least demanding callout.</summary>
    private const int BaseCallOutCost = 8;

    /// <summary>Creates an animal catcher.</summary>
    /// <param name="name">The catcher's name.</param>
    /// <param name="energy">Starting energy, 0 to 100 inclusive.</param>
    public AnimalCatcher(string name, int energy)
        : base(name, energy)
    {
    }

    /// <inheritdoc />
    /// <remarks>
    /// A catcher always travels to the scene first, then works hands-on. That
    /// journey plus the physical handling is why this is the most tiring of the
    /// three responder types.
    /// </remarks>
    public override string HandleIncident(Incident incident)
    {
        ArgumentNullException.ThrowIfNull(incident);

        AdjustEnergy(-EnergyCostFor(incident.Severity, BaseCallOutCost));

        string handling = incident.Severity >= SeverityLevel.High
            ? "casts the heavy net over it and lifts it clear"
            : "works a light net and a handful of treats to walk it into the crate";

        return $"{DriveTo(incident.Location)} {Name} {handling}.";
    }

    /// <inheritdoc />
    public string DriveTo(string location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(location);
        return $"{Name} takes the rescue vehicle to {location}.";
    }
}
