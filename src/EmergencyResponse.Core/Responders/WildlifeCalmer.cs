using EmergencyResponse.Core.Capabilities;
using EmergencyResponse.Core.Incidents;

namespace EmergencyResponse.Core.Responders;

/// <summary>
/// Handles stressed, loud or territorial animals by de-escalating rather than
/// confronting. Slower than catching, but it is the only thing that works on an
/// animal that is holding ground.
/// </summary>
public sealed class WildlifeCalmer : Responder, ICanCalmAnimals
{
    /// <summary>The effort a calmer spends on the least demanding callout.</summary>
    private const int BaseCallOutCost = 6;

    /// <summary>Creates a wildlife calmer.</summary>
    /// <param name="name">The calmer's name.</param>
    /// <param name="energy">Starting energy, 0 to 100 inclusive.</param>
    public WildlifeCalmer(string name, int energy)
        : base(name, energy)
    {
    }

    /// <inheritdoc />
    /// <remarks>
    /// No vehicle and no equipment, but the waiting is what costs. A calmer
    /// sits between the catcher and the pilot on effort.
    /// </remarks>
    public override string HandleIncident(Incident incident)
    {
        ArgumentNullException.ThrowIfNull(incident);

        AdjustEnergy(-EnergyCostFor(incident.Severity, BaseCallOutCost));

        return $"{CalmAnimal(incident)} The space at {incident.Location} is held " +
               "until the animal chooses to leave.";
    }

    /// <inheritdoc />
    public string CalmAnimal(Incident incident)
    {
        ArgumentNullException.ThrowIfNull(incident);
        return $"{Name} keeps low and quiet and calms the animal.";
    }
}
