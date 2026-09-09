using EmergencyResponse.Core.Exceptions;
using EmergencyResponse.Core.Incidents;

namespace EmergencyResponse.Core.Responders;

/// <summary>
/// A field responder who can be sent to an incident.
/// </summary>
public abstract class Responder
{
    /// <summary>
    /// How much energy the responder has left, from <see cref="MinEnergy"/> to
    /// <see cref="MaxEnergy"/> inclusive.
    /// </summary>
    /// <remarks>
    /// A responder at <see cref="MinEnergy"/> takes no new work.
    /// <see cref="AdjustEnergy"/> clamps rather than throwing, so a hard callout
    /// exhausts a responder instead of crashing the dispatch.
    /// </remarks>
    public int Energy { get; private set; }

    /// <summary>The lowest valid energy level. A responder at this level takes no new work.</summary>
    public const int MinEnergy = 0;

    /// <summary>The highest valid energy level, a fully rested responder.</summary>
    public const int MaxEnergy = 100;

    /// <summary>
    /// Initialises the shared responder state.
    /// </summary>
    /// <param name="name">The responder's name. Must not be blank.</param>
    /// <param name="energy">
    /// Starting energy, from <see cref="MinEnergy"/> to <see cref="MaxEnergy"/> inclusive.
    /// </param>
    /// <exception cref="ArgumentException"><paramref name="name"/> is blank.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="energy"/> is outside the valid range.
    /// </exception>
    protected Responder(string name, int energy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(energy, MinEnergy);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(energy, MaxEnergy);

        Name = name;
        Energy = energy;
    }

    /// <summary>The responder's name, fixed for the lifetime of the object.</summary>
    public string Name { get; }

    /// <summary>
    /// Deals with the incident in whatever way this kind of responder works.
    /// </summary>
    /// <param name="incident">The incident to handle.</param>
    /// <returns>
    /// A line describing what the responder did. The domain returns text rather
    /// than printing it, so this assembly never depends on a console.
    /// </returns>
    public abstract string HandleIncident(Incident incident);

    /// <summary>
    /// Whether this responder is capable of the incident: enough energy left,
    /// and implementing every capability it requires.
    /// </summary>
    /// <param name="incident">The incident to check against.</param>
    /// <returns><see langword="true"/> if the responder is capable of it.</returns>
    /// <remarks>
    /// Capability only. Whether the responder is free is a question about the
    /// roster, not about this object, so the command centre answers it.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="incident"/> is null.</exception>
    public bool CanHandle(Incident incident)
    {
        ArgumentNullException.ThrowIfNull(incident);

        return Energy > MinEnergy &&
               incident.RequiredCapabilities.All(
                   capability => capability.IsInstanceOfType(this));
    }

    /// <summary>
    /// Scales a responder's base effort by how demanding the incident is.
    /// </summary>
    /// <param name="severity">How urgent the incident is.</param>
    /// <param name="baseCost">
    /// What this kind of responder spends on the least demanding callout.
    /// </param>
    /// <returns>The energy this callout costs.</returns>
    /// <remarks>
    /// Shared so severity means the same thing across the unit; responder types
    /// differ only in their base cost.
    /// </remarks>
    protected static int CostFor(SeverityLevel severity, int baseCost) => severity switch
    {
        SeverityLevel.Low => baseCost,
        SeverityLevel.Medium => baseCost * 2,
        SeverityLevel.High => baseCost * 3,
        SeverityLevel.Critical => baseCost * 4,
        _ => baseCost
    };

    /// <summary>
    /// Changes the responder's energy, clamped into the valid range.
    /// </summary>
    /// <param name="amount">
    /// How much to add. Negative for the cost of handling an incident.
    /// </param>
    internal void AdjustEnergy(int amount) =>
        Energy = Math.Clamp(Energy + amount, MinEnergy, MaxEnergy);
}
