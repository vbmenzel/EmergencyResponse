namespace EmergencyResponse.Core.Exceptions;

/// <summary>
/// Thrown when code attempts to assign a responder who cannot take the work:
/// they are already out on a call, they have no energy left, or they lack a
/// capability the incident requires.
/// </summary>
/// <remarks>
/// This is the "wrong person" failure. When no registered responder at all
/// fits an incident, the command centre raises
/// <see cref="NoSuitableResponderException"/> instead.
/// </remarks>
public class ResponderUnavailableException : Exception
{
    /// <summary>Creates the exception with an explanatory message.</summary>
    /// <param name="message">Why this responder could not be assigned.</param>
    public ResponderUnavailableException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and an underlying cause.</summary>
    /// <param name="message">Why this responder could not be assigned.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    public ResponderUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
