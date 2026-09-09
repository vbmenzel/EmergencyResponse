using EmergencyResponse.Core.Incidents;
using EmergencyResponse.Core.Responders;

namespace EmergencyResponse.ConsoleHost;

/// <summary>
/// Formatting for the scripted demonstration, so <see cref="Program"/> reads as
/// a sequence of steps rather than a wall of write calls.
/// </summary>
internal static class ConsoleReport
{
    /// <summary>How many sections have been started. Sections number themselves.</summary>
    private static int sectionCount;

    /// <summary>
    /// Colour is for a human at a terminal. Piping to a file or a grader's log
    /// should produce clean text, and NO_COLOR is the usual way to ask for that.
    /// </summary>
    private static readonly bool useColour =
        !Console.IsOutputRedirected &&
        Environment.GetEnvironmentVariable("NO_COLOR") is null;

    /// <summary>
    /// Starts the next section. The number is tracked here rather than typed
    /// at each call site, so inserting or reordering a section cannot leave the
    /// output numbered 1, 2, 2, 4.
    /// </summary>
    /// <param name="title">The heading, without a number.</param>
    internal static void Section(string title)
    {
        string heading = $"{++sectionCount}. {title}";

        Console.WriteLine();
        Write(heading, ConsoleColor.Cyan);
        Write(
            $"{new string('─', heading.Length)}",
            ConsoleColor.DarkCyan
        );
    }

    /// <summary>Writes a line of ordinary output.</summary>
    /// <param name="text">The line to write.</param>
    internal static void Line(string text) => Write(text, null);

    /// <summary>Writes indented supporting detail.</summary>
    /// <param name="text">The detail.</param>
    internal static void Detail(string text) => Write($"     {text}", ConsoleColor.DarkGray);

    /// <summary>Writes something that worked.</summary>
    /// <param name="text">The line to write.</param>
    internal static void Success(string text) => Write($"  {text}", ConsoleColor.Green);

    /// <summary>Writes something that went wrong and was handled.</summary>
    /// <param name="text">The line to write.</param>
    internal static void Problem(string text) => Write($"  ! {text}", ConsoleColor.Yellow);

    /// <summary>Writes one responder as a roster row.</summary>
    /// <param name="responder">The responder to show.</param>
    /// <param name="available">Whether they are free right now.</param>
    internal static void ResponderRow(Responder responder, bool available)
    {
        Write($"  {responder.Name,-14} {responder.GetType().Name,-15} " +
              $"energy {responder.Energy,3}   {(available ? "free" : "out")}",
            available ? null : ConsoleColor.DarkGray);
    }

    /// <summary>Writes one incident, coloured by how urgent it is.</summary>
    /// <param name="incident">The incident to show.</param>
    internal static void IncidentRow(Incident incident) =>
        Write($"  [{incident.Severity,-8}] {incident.Description}", ColourFor(incident.Severity));

    /// <summary>Writes an incident's supporting detail under its row.</summary>
    /// <param name="incident">The incident to describe.</param>
    internal static void IncidentDetail(Incident incident) =>
        Detail($"{incident.Location}, needs {Capabilities(incident)}");

    /// <summary>Names an incident's required capabilities for display.</summary>
    /// <param name="incident">The incident.</param>
    /// <returns>The interface names, or "anyone available".</returns>
    private static string Capabilities(Incident incident) =>
        incident.RequiredCapabilities.Count == 0
            ? "anyone available"
            : string.Join(", ", incident.RequiredCapabilities.Select(c => c.Name));

    /// <summary>
    /// Only the incidents that cannot wait are coloured.
    /// </summary>
    /// <param name="severity">The severity to colour.</param>
    /// <returns>The colour, or null for the default.</returns>
    private static ConsoleColor? ColourFor(SeverityLevel severity) => severity switch
    {
        SeverityLevel.Critical => ConsoleColor.Red,
        SeverityLevel.High => ConsoleColor.Yellow,
        _ => null
    };

    /// <summary>
    /// The place that touches the console's colour, restoring whatever was
    /// there before rather than resetting to the default.
    /// </summary>
    /// <param name="text">The line to write.</param>
    /// <param name="colour">The colour, or null to leave it alone.</param>
    private static void Write(string text, ConsoleColor? colour)
    {
        if (colour is null || !useColour)
        {
            Console.WriteLine(text);
            return;
        }

        ConsoleColor previous = Console.ForegroundColor;
        Console.ForegroundColor = colour.Value;
        Console.WriteLine(text);
        Console.ForegroundColor = previous;
    }
}
