using EmergencyResponse.Core.Assignment;
using EmergencyResponse.Core.Coordination;
using Terminal.Gui.App;

namespace EmergencyResponse.Tui;

/// <summary>
/// Composition root for the control room: creates the command centre, hands
/// control to the interactive screen, and restores the terminal afterwards.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Runs the TUI.
    /// </summary>
    /// <returns>Exit code 0 for a normal finish.</returns>
    private static int Main()
    {
        if (Console.IsInputRedirected)
        {
            Console.Error.WriteLine("EmergencyResponse.Tui needs an interactive terminal.");
            return 1;
        }

        CommandCentre centre = CommandCentre.GetInstance(new FirstAvailableStrategy());

        using IApplication app = Application.Create();
        app.Init();

        using ControlRoom controlRoom = new(app, centre);
        controlRoom.Run();

        return 0;
    }
}