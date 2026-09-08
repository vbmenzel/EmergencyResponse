namespace EmergencyResponse.Core.Exceptions;

/// <summary>
/// Thrown when no registered responder satisfies an incident's requirements,
/// because every candidate is busy, exhausted, or missing a required
/// capability.
/// </summary>
/// <remarks>
/// This is the "nobody at all" failure, raised by an assignment strategy after
/// it has filtered the whole pool. When a specific named responder is rejected,
/// the failure is <see cref="ResponderUnavailableException"/> instead.
/// </remarks>
public class NoSuitableResponderException : Exception
{
    /// <summary>Creates the exception with an explanatory message.</summary>
    /// <param name="message">Which incident could not be staffed, and why.</param>
    public NoSuitableResponderException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and an underlying cause.</summary>
    /// <param name="message">Which incident could not be staffed, and why.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    public NoSuitableResponderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
