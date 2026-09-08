using EmergencyResponse.Core.Exceptions;
using EmergencyResponse.Core.Incidents;

namespace EmergencyResponse.Core.Responders;

/// <summary>
/// A field responder who can be sent to an incident.
/// </summary>
/// <remarks>
/// A responder owns its own invariants. <see cref="Energy"/> and
/// <see cref="IsAvailable"/> have private setters, and the methods that change
/// them are <see langword="internal"/>, so unrelated code cannot mark somebody
/// free while they are still out on a call. That double booking is the failure
/// the paper-based system suffered from.
/// </remarks>
public abstract class Responder
{
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
        IsAvailable = true;
    }

    /// <summary>The responder's name, fixed for the lifetime of the object.</summary>
    public string Name { get; }

    /// <summary>
    /// How much energy the responder has left, from <see cref="MinEnergy"/> to
    /// <see cref="MaxEnergy"/> inclusive.
    /// </summary>
    /// <remarks>
    /// A responder at <see cref="MinEnergy"/> is not eligible for new work.
    /// Only <see cref="AdjustEnergy"/> changes this, and it clamps into range
    /// rather than throwing, so handling a hard job can never push a responder
    /// below zero.
    /// </remarks>
    public int Energy { get; private set; }

    /// <summary>
    /// Whether the responder is free to take an incident. A newly created
    /// responder is available.
    /// </summary>
    public bool IsAvailable { get; private set; }

    /// <summary>
    /// Deals with the incident in whatever way this kind of responder works.
    /// </summary>
    /// <param name="incident">The incident to handle.</param>
    /// <returns>
    /// A line describing what the responder did, for a host to present.
    /// The domain returns text rather than printing it, so this assembly stays
    /// free of any dependency on the console.
    /// </returns>
    public abstract string HandleIncident(Incident incident);

    /// <summary>
    /// Whether this responder may be sent to the given incident: available,
    /// with energy left, and implementing every capability the incident needs.
    /// </summary>
    /// <param name="incident">The incident to check against.</param>
    /// <returns><see langword="true"/> if the responder can take the incident.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="incident"/> is null.</exception>
    public bool IsEligibleFor(Incident incident)
    {
        ArgumentNullException.ThrowIfNull(incident);

        if (!IsAvailable || Energy <= MinEnergy)
        {
            return false;
        }

        foreach (Type capability in incident.RequiredCapabilities)
        {
            if (!capability.IsInstanceOfType(this))
            {
                return false;
            }
        }

        return true;
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
    /// The scale is shared so that severity means the same thing across the
    /// unit. Responder types differ only in their base cost, which is what
    /// makes a drone pilot cheaper to send than an animal catcher.
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
    /// Marks the responder as busy on the given incident.
    /// </summary>
    /// <param name="incident">The incident being taken on.</param>
    /// <remarks>
    /// The eligibility check here is defence in depth. The command centre
    /// already holds a lock across selection and reservation; this catches any
    /// caller that does not.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="incident"/> is null.</exception>
    /// <exception cref="ResponderUnavailableException">
    /// The responder is busy, out of energy, or lacks a required capability.
    /// </exception>
    internal void AssignTo(Incident incident)
    {
        ArgumentNullException.ThrowIfNull(incident);

        if (!IsEligibleFor(incident))
        {
            throw new ResponderUnavailableException(
                $"{Name} cannot take '{incident.Description}'. " +
                $"Available: {IsAvailable}, energy: {Energy}, " +
                $"required capabilities: {incident.RequiredCapabilities.Count}.");
        }

        IsAvailable = false;
    }

    /// <summary>
    /// Frees the responder for new work. Safe to call on a responder who is
    /// already available.
    /// </summary>
    internal void Release() => IsAvailable = true;

    /// <summary>
    /// Changes the responder's energy, clamped into the valid range.
    /// </summary>
    /// <param name="amount">
    /// How much to add. Negative for the cost of handling an incident.
    /// </param>
    internal void AdjustEnergy(int amount) =>
        Energy = Math.Clamp(Energy + amount, MinEnergy, MaxEnergy);
}
