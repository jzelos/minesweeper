namespace Zelos.Minesweeper;

public struct Colour {
    public byte R { get; init; }
    public byte B { get; init; }
    public byte G { get; init; }
    public byte A { get; init; }

    public Colour(byte r, byte b, byte g, byte a) {
        R = r;
        B = b;
        G = g;
        A = a;
    }

    public static Colour White = new Colour(255, 255, 255, 255);
    public static Colour Black = new Colour(0, 0, 0, 255);

    public static Colour LightGrey = new Colour(100, 100, 100, 255);
    public static Colour DarkGrey = new Colour(150, 150, 150, 255);
}