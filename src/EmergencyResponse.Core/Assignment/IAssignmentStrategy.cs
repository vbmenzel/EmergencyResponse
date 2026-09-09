using EmergencyResponse.Core.Exceptions;
using EmergencyResponse.Core.Incidents;
using EmergencyResponse.Core.Responders;

namespace EmergencyResponse.Core.Assignment;

/// <summary>
/// Decides which responder should take an incident.
/// </summary>
/// <remarks>
/// The command centre depends on this abstraction rather than on any one
/// algorithm, so a new policy can be introduced or swapped in without changing
/// the centre. Implementations answer a question; they do not reserve anybody.
/// Reserving is the command centre's job, which is what lets it do selection
/// and reservation together under a single lock.
/// </remarks>
public interface IAssignmentStrategy
{
    /// <summary>
    /// A short label for this policy, so a host can report which one is active
    /// without knowing the concrete type.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Chooses a responder for the incident from the supplied pool.
    /// </summary>
    /// <param name="responders">The responders to choose from.</param>
    /// <param name="incident">The incident that needs someone.</param>
    /// <returns>The chosen responder.</returns>
    /// <exception cref="ArgumentNullException">Either argument is null.</exception>
    /// <exception cref="NoSuitableResponderException">
    /// Nobody in the pool is eligible: all busy, exhausted, or missing a
    /// capability the incident requires.
    /// </exception>
    Responder SelectResponder(IEnumerable<Responder> responders, Incident incident);
}
