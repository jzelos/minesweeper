namespace Zelos.Minesweeper;

public class Resources {
    public const string WINDOW_TITLE = "Minesweeper";

    public const string TEXTURE_MINE = "mine";

    public const int FONT_SIZE = 14;

    /// <summary>Default cell size in pixels when the window is auto-sized to the grid.</summary>
    public const int CELL_SIZE = 32;

    /// <summary>Minimum window width so the info bar always has room for its controls.</summary>
    public const int MIN_WINDOW_WIDTH = 380;
}

/// <summary>Standard mine counts matching official Minesweeper rules.</summary>
public static class StandardMines {
    public const int EASY   = 10;   // 9 × 9
    public const int MEDIUM = 40;   // 16 × 16
    public const int HARD   = 99;   // 30 × 16
}