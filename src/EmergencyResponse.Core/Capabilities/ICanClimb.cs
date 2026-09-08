namespace EmergencyResponse.Core.Capabilities;

/// <summary>
/// The optional skill of reaching a roof, tree, or other height safely.
/// </summary>
public interface ICanClimb
{
    /// <summary>Climbs to somewhere a responder cannot simply walk.</summary>
    /// <param name="location">Where to climb to.</param>
    /// <returns>A line describing the climb, for a host to present.</returns>
    string ClimbTo(string location);
}
