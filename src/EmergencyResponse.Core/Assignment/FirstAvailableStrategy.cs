using EmergencyResponse.Core.Exceptions;
using EmergencyResponse.Core.Incidents;
using EmergencyResponse.Core.Responders;
using EmergencyResponse.Core.Search;

namespace EmergencyResponse.Core.Assignment;

/// <summary>
/// Sends whoever comes first in the pool and is eligible.
/// </summary>
public sealed class FirstAvailableStrategy : IAssignmentStrategy
{
    /// <inheritdoc />
    public string Name => "First available";

    /// <inheritdoc />
    public Responder SelectResponder(IEnumerable<Responder> responders, Incident incident)
    {
        ArgumentNullException.ThrowIfNull(responders);
        ArgumentNullException.ThrowIfNull(incident);

        // FindFirstMatch stops at the first hit rather than filtering the whole
        // pool, which is the whole point of picking the first one.
        return SearchTool.FindFirstMatch(responders, r => r.CanHandle(incident))
            ?? throw new NoSuitableResponderException(
                $"No eligible responder for '{incident.Description}' under the {Name} policy.");
    }
}
