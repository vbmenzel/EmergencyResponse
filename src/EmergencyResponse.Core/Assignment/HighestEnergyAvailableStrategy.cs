using EmergencyResponse.Core.Exceptions;
using EmergencyResponse.Core.Incidents;
using EmergencyResponse.Core.Responders;
using EmergencyResponse.Core.Search;

namespace EmergencyResponse.Core.Assignment;

/// <summary>
/// Sends the most rested eligible responder, spreading the load across the unit
/// instead of wearing out whoever happens to be registered first.
/// </summary>
public sealed class HighestEnergyAvailableStrategy : IAssignmentStrategy
{
    /// <inheritdoc />
    public string Name => "Highest energy available";

    /// <inheritdoc />
    public Responder SelectResponder(IEnumerable<Responder> responders, Incident incident)
    {
        ArgumentNullException.ThrowIfNull(responders);
        ArgumentNullException.ThrowIfNull(incident);

        // MaxBy keeps the first of any tie, so an equal-energy pool still
        // resolves in registration order rather than arbitrarily.
        return SearchTool.FindMatches(responders, r => r.CanHandle(incident))
                .MaxBy(r => r.Energy)
            ?? throw new NoSuitableResponderException(
                $"No eligible responder for '{incident.Description}' under the {Name} policy.");
    }
}
