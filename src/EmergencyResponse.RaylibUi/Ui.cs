using System.Numerics;
using Raylib_cs;

namespace EmergencyResponse.RaylibUi;

/// <summary>
/// Immediate-mode widgets. Each one draws itself and reports what the mouse did
/// in the same call, which is the whole idea behind raylib's style.
/// </summary>
internal static class Ui
{
    /// <summary>Spacing between glyphs, in pixels at the drawn size.</summary>
    private const float Tracking = 0.5f;

    /// <summary>The atlas is built once at this size and scaled down when drawn.</summary>
    private const int AtlasSize = 48;

    private static Font font;
    private static bool bundledFontLoaded;

    /// <summary>
    /// Loads the bundled font. Must run after the window exists, because the
    /// atlas is uploaded to the GPU.
    /// </summary>
    /// <param name="path">Path to the .ttf.</param>
    /// <remarks>
    /// If the file is missing the app carries on with raylib's built-in font
    /// rather than refusing to start; a missing typeface is a cosmetic problem.
    /// </remarks>
    internal static void LoadBundledFont(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        // Printable ASCII plus the few punctuation marks the board actually uses.
        int[] codepoints = [.. Enumerable.Range(32, 95), 0x00B7, 0x2022, 0x2192];

        font = Raylib.LoadFontEx(path, AtlasSize, codepoints, codepoints.Length);

        if (font.BaseSize > 0)
        {
            Raylib.SetTextureFilter(font.Texture, TextureFilter.Bilinear);
            bundledFontLoaded = true;
        }
    }

    /// <summary>Releases the font atlas.</summary>
    internal static void UnloadBundledFont()
    {
        if (bundledFontLoaded)
        {
            Raylib.UnloadFont(font);
            bundledFontLoaded = false;
        }
    }

    /// <summary>Width of a string as it will be drawn.</summary>
    /// <param name="text">The string.</param>
    /// <param name="size">Font size.</param>
    /// <returns>Width in pixels.</returns>
    internal static int Measure(string text, int size) =>
        bundledFontLoaded
            ? (int)Raylib.MeasureTextEx(font, text, size, Tracking).X
            : Raylib.MeasureText(text, size);

    /// <summary>Draws a filled panel with a border.</summary>
    /// <param name="bounds">Where to draw it.</param>
    /// <param name="fill">Fill colour.</param>
    internal static void Panel(Rectangle bounds, Color fill)
    {
        Raylib.DrawRectangleRec(bounds, fill);
        Raylib.DrawRectangleLinesEx(bounds, 1, Theme.Line);
    }

    /// <summary>Draws text.</summary>
    /// <param name="text">What to write.</param>
    /// <param name="x">Left edge.</param>
    /// <param name="y">Top edge.</param>
    /// <param name="size">Font size.</param>
    /// <param name="colour">Text colour.</param>
    internal static void Text(string text, int x, int y, int size, Color colour)
    {
        if (bundledFontLoaded)
        {
            Raylib.DrawTextEx(font, text, new Vector2(x, y), size, Tracking, colour);
            return;
        }

        Raylib.DrawText(text, x, y, size, colour);
    }

    /// <summary>Draws text, trimming it with an ellipsis if it will not fit.</summary>
    /// <param name="text">What to write.</param>
    /// <param name="x">Left edge.</param>
    /// <param name="y">Top edge.</param>
    /// <param name="size">Font size.</param>
    /// <param name="colour">Text colour.</param>
    /// <param name="maxWidth">The space available.</param>
    internal static void TextClipped(string text, int x, int y, int size, Color colour, int maxWidth)
    {
        string shown = text;

        while (shown.Length > 1 && Measure(shown + "...", size) > maxWidth)
        {
            shown = shown[..^1];
        }

        Text(shown.Length == text.Length ? text : shown + "...", x, y, size, colour);
    }

    /// <summary>Draws a button and reports whether it was clicked this frame.</summary>
    /// <param name="bounds">Where to draw it.</param>
    /// <param name="label">The caption.</param>
    /// <param name="enabled">Whether it can be clicked.</param>
    /// <param name="tint">Accent colour for the border and label.</param>
    /// <returns><see langword="true"/> on the frame the button is clicked.</returns>
    internal static bool Button(Rectangle bounds, string label, bool enabled, Color tint)
    {
        bool hovered = enabled && Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), bounds);

        Raylib.DrawRectangleRec(bounds, hovered ? Theme.PanelRaised : Theme.Panel);
        Raylib.DrawRectangleLinesEx(bounds, 1, enabled ? tint : Theme.Line);

        int width = Measure(label, 14);
        Text(
            label,
            (int)(bounds.X + ((bounds.Width - width) / 2)),
            (int)(bounds.Y + ((bounds.Height - 14) / 2)),
            14,
            enabled ? tint : Theme.TextDim);

        return hovered && Raylib.IsMouseButtonPressed(MouseButton.Left);
    }

    /// <summary>Draws a horizontal bar for a 0 to 100 value.</summary>
    /// <param name="bounds">Where to draw it.</param>
    /// <param name="value">The value, 0 to 100.</param>
    /// <param name="fill">Bar colour.</param>
    internal static void Bar(Rectangle bounds, int value, Color fill)
    {
        Raylib.DrawRectangleRec(bounds, Theme.Panel);
        Raylib.DrawRectangleRec(
            bounds with { Width = bounds.Width * Math.Clamp(value, 0, 100) / 100f },
            fill);
        Raylib.DrawRectangleLinesEx(bounds, 1, Theme.Line);
    }

    /// <summary>Whether the pointer is inside a rectangle.</summary>
    /// <param name="bounds">The rectangle.</param>
    /// <returns><see langword="true"/> if the mouse is over it.</returns>
    internal static bool Hovered(Rectangle bounds) =>
        Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), bounds);

    /// <summary>The current mouse position.</summary>
    /// <returns>Pointer position in window coordinates.</returns>
    internal static Vector2 Pointer() => Raylib.GetMousePosition();
}
