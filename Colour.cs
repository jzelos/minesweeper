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
    public static Colour Green = new Colour(0, 255, 0, 255);
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
    public static Colour Number8 = new Colour(128, 128, 128, 255);  // Gray
    
    // Cell colors
    public static Colour CellUnopened = new Colour(192, 192, 192, 255);  // Silver
    public static Colour CellOpened = new Colour(189, 189, 189, 255);    // Light Grey
    public static Colour CellHover = new Colour(220, 220, 220, 255);     // Lighter on hover
    
    // 3D Border colors
    public static Colour BorderLight = new Colour(255, 255, 255, 255);   // White for raised edge
    public static Colour BorderDark = new Colour(128, 128, 128, 255);    // Dark grey for shadow
    
    // Menu colors
    public static Colour MenuBar = new Colour(240, 240, 240, 255);
    public static Colour MenuHighlight = new Colour(200, 200, 200, 255);
    public static Colour MenuShadow = new Colour(60, 60, 60, 128);       // Semi-transparent
}
