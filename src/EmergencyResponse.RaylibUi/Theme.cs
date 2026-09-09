using Raylib_cs;

namespace EmergencyResponse.RaylibUi;

/// <summary>Colours and metrics for the dispatch board.</summary>
internal static class Theme
{
    internal static Color Background { get; } = Rgb(0x0d, 0x11, 0x17);
    internal static Color Panel { get; } = Rgb(0x16, 0x1b, 0x22);
    internal static Color PanelRaised { get; } = Rgb(0x1f, 0x26, 0x30);
    internal static Color Line { get; } = Rgb(0x30, 0x39, 0x45);
    internal static Color Text { get; } = Rgb(0xc9, 0xd1, 0xd9);
    internal static Color TextDim { get; } = Rgb(0x7d, 0x88, 0x96);
    internal static Color Accent { get; } = Rgb(0x58, 0xa6, 0xff);
    internal static Color Good { get; } = Rgb(0x3f, 0xb9, 0x50);
    internal static Color Warn { get; } = Rgb(0xd2, 0x99, 0x22);
    internal static Color Bad { get; } = Rgb(0xf8, 0x51, 0x49);

    /// <summary>Builds an opaque colour from byte components.</summary>
    /// <param name="r">Red.</param>
    /// <param name="g">Green.</param>
    /// <param name="b">Blue.</param>
    /// <returns>The colour.</returns>
    private static Color Rgb(int r, int g, int b) =>
        new((byte)r, (byte)g, (byte)b, (byte)255);
}
