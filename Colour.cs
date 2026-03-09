namespace Zelos.Minesweeper;

public struct Colour {
    public byte R { get; init; }
    public byte G { get; init; }
    public byte B { get; init; }
    public byte A { get; init; }

    public Colour(byte r, byte g, byte b, byte a) {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public static Colour White = new Colour(255, 255, 255, 255);
    public static Colour Black = new Colour(0, 0, 0, 255);
    public static Colour Green = new Colour(0, 200, 0, 255);
    public static Colour Blue = new Colour(0, 0, 255, 255);
    public static Colour Red = new Colour(255, 0, 0, 255);
    public static Colour LightGrey = new Colour(220, 220, 220, 255);
    public static Colour Gray = new Colour(128, 128, 128, 255);
    public static Colour DarkGrey = new Colour(180, 180, 180, 255);

    // Classic Minesweeper number colors
    public static Colour Number1 = new Colour(0, 0, 255, 255);      // Blue
    public static Colour Number2 = new Colour(0, 128, 0, 255);      // Green
    public static Colour Number3 = new Colour(255, 0, 0, 255);      // Red
    public static Colour Number4 = new Colour(0, 0, 128, 255);      // Dark Blue
    public static Colour Number5 = new Colour(128, 0, 0, 255);      // Dark Red
    public static Colour Number6 = new Colour(0, 128, 128, 255);    // Cyan
    public static Colour Number7 = new Colour(0, 0, 0, 255);        // Black
    public static Colour Number8 = new Colour(75, 75, 75, 255);     // Dark gray — readable on opened cell (184,184,184)

    // Cell colors
    public static Colour CellUnopened = new Colour(196, 196, 196, 255);
    public static Colour CellOpened   = new Colour(184, 184, 184, 255);
    public static Colour CellHover    = new Colour(218, 218, 218, 255);

    // 3D border colors
    public static Colour BorderLight = new Colour(255, 255, 255, 255);
    public static Colour BorderDark  = new Colour(120, 120, 120, 255);

    // Menu colors
    public static Colour MenuBar        = new Colour(240, 240, 240, 255);
    public static Colour MenuHighlight  = new Colour(198, 210, 240, 255);
    public static Colour MenuShadow     = new Colour(60, 60, 60, 128);
    public static Colour DialogTitleBg  = new Colour(47, 85, 151, 255);  // dark blue — pair with White text

    // Smiley face
    public static Colour SmileyYellow = new Colour(255, 210, 0, 255);
    public static Colour SmileyBorder = new Colour(170, 140, 0, 255);

    // Seven-segment LED display
    public static Colour SevenSegOn  = new Colour(240, 30, 30, 255);   // lit segment
    public static Colour SevenSegOff = new Colour(55, 8, 8, 255);      // dark (unlit) segment
    public static Colour SevenSegBg  = new Colour(6, 0, 0, 255);       // display background

    // Overlay / dialog
    public static Colour OverlayBg    = new Colour(0, 0, 0, 160);
    public static Colour OverlayPanel = new Colour(240, 240, 240, 255);

    // Win / reward accent
    public static Colour WinGold     = new Colour(212, 175, 55, 255);   // gold bg — pair with Black text
    public static Colour WinGoldText = new Colour(150, 100, 0, 255);    // dark amber — readable on light bg

    // Flag graphics
    public static Colour FlagRed  = new Colour(210, 30, 30, 255);
    public static Colour PoleDark = new Colour(40, 30, 20, 255);
}
