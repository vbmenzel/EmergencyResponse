using EmergencyResponse.Core.Assignment;
using EmergencyResponse.Core.Coordination;
using Raylib_cs;

namespace EmergencyResponse.RaylibUi;

/// <summary>
/// A second host for the same domain. It references
/// <c>EmergencyResponse.Core</c> and nothing else, which is the point: the
/// console demonstration and this window share every rule and no presentation
/// code.
/// </summary>
internal sealed class Program
{
    /// <summary>Opens the window and runs the frame loop.</summary>
    /// <param name="args">Command line arguments. Unused.</param>
    private static void Main(string[] args)
    {
        CommandCentre centre = CommandCentre.GetInstance(new FirstAvailableStrategy());
        SeedData.RegisterResponders(centre);
        SeedData.ReportIncidents(centre);

        DispatchBoard board = new(centre);

        (int width, int height) = DispatchBoard.WindowSize;
        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint);
        Raylib.InitWindow(width, height, "Emergency Response  ·  dispatch board");
        Raylib.SetTargetFPS(60);
        Ui.LoadBundledFont(Path.Combine(AppContext.BaseDirectory, "assets", "MapleMono-NF-Medium.ttf"));

        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();
            board.Draw();
            Raylib.EndDrawing();
        }

        Ui.UnloadBundledFont();
        Raylib.CloseWindow();
    }
}
