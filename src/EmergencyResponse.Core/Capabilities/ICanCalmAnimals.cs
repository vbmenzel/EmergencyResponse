using EmergencyResponse.Core.Incidents;

namespace EmergencyResponse.Core.Capabilities;

/// <summary>
/// The optional skill of de-escalating a stressed, loud, or territorial animal.
/// </summary>
public interface ICanCalmAnimals
{
    /// <summary>Talks the animal down rather than confronting it.</summary>
    /// <param name="incident">The incident whose animal needs calming.</param>
    /// <returns>A line describing the de-escalation, for a host to present.</returns>
    string CalmAnimal(Incident incident);
}
