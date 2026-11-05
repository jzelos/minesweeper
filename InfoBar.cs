using static SDL2.SDL;

namespace Zelos.Minesweeper;

public class InfoBar
{
    private readonly Media media;
    private const int INFO_BAR_HEIGHT = 50;
    private const int DISPLAY_W       = 82;
    private const int DISPLAY_H       = 34;
    private const int BUTTON_SIZE     = 40;

    private int  totalMines;
    private int  flagsPlaced;
    private int  elapsedSeconds;
    private bool timerRunning;
    private bool timerStarted;
    private uint timerStartTicks;
    private GameMode currentMode;
    private Action? onRestartClick;

    public InfoBar(Media media)
    {
        this.media = media;
    }

    public void StartGame(int mineCount)
    {
        totalMines      = mineCount;
        flagsPlaced     = 0;
        elapsedSeconds  = 0;
        timerRunning    = false;
        timerStarted    = false;
        timerStartTicks = 0;
        currentMode     = GameMode.Game;
    }

    /// <summary>Starts the clock on the first actual reveal. Safe to call repeatedly;
    /// only takes effect once per game, and never re-arms after StopTimer() ends a game.</summary>
    public void StartTimer()
    {
        if (timerStarted) return;
        timerStarted    = true;
        timerRunning    = true;
        timerStartTicks = SDL_GetTicks();
    }

    public void SetRestartCallback(Action callback) => onRestartClick = callback;
    public void SetGameMode(GameMode mode)          => currentMode = mode;
    public void StopTimer()                         => timerRunning = false;
    public void SetFlagsPlaced(int count)           => flagsPlaced = count;
    public int  GetElapsedSeconds()                 => elapsedSeconds;
    public int  GetHeight()                         => INFO_BAR_HEIGHT;

    public void UpdateTimer()
    {
        if (!timerRunning) return;
        elapsedSeconds = (int)((SDL_GetTicks() - timerStartTicks) / 1000u);
        if (elapsedSeconds > 999) elapsedSeconds = 999;
    }

    public void Draw(IntPtr renderer, int windowWidth, int yOffset)
    {
        int y = yOffset;

        // Background
        media.Rectangle(renderer, 0, y, windowWidth, INFO_BAR_HEIGHT, Colour.MenuBar);

        // Sunken inner border
        media.DrawSunkenBorder(renderer, 2, y + 2, windowWidth - 4, INFO_BAR_HEIGHT - 4, 2);

        // Bottom border
        media.Rectangle(renderer, 0, y + INFO_BAR_HEIGHT - 1, windowWidth, 1, Colour.Gray);

        int panelY  = y + (INFO_BAR_HEIGHT - DISPLAY_H) / 2;

        // Mine counter (left)
        int leftX = 10;
        int minesRemaining = totalMines - flagsPlaced;
        media.DrawSunkenBorder(renderer, leftX - 2, panelY - 2, DISPLAY_W + 4, DISPLAY_H + 4, 2);
        media.DrawSevenSegmentDisplay(renderer, leftX, panelY, DISPLAY_W, DISPLAY_H,
            minesRemaining, 3, Colour.SevenSegOn, Colour.SevenSegOff, Colour.SevenSegBg);

        // Timer (right)
        int rightX = windowWidth - DISPLAY_W - 10;
        media.DrawSunkenBorder(renderer, rightX - 2, panelY - 2, DISPLAY_W + 4, DISPLAY_H + 4, 2);
        media.DrawSevenSegmentDisplay(renderer, rightX, panelY, DISPLAY_W, DISPLAY_H,
            elapsedSeconds, 3, Colour.SevenSegOn, Colour.SevenSegOff, Colour.SevenSegBg);

        // Smiley button (centre)
        int btnX = (windowWidth - BUTTON_SIZE) / 2;
        int btnY = y + (INFO_BAR_HEIGHT - BUTTON_SIZE) / 2;
        DrawSmileyButton(renderer, btnX, btnY);
    }

    private void DrawSmileyButton(IntPtr renderer, int x, int y)
    {
        // Raised button background
        media.Rectangle(renderer, x, y, BUTTON_SIZE, BUTTON_SIZE, Colour.CellUnopened);
        media.DrawRaisedBorder(renderer, x, y, BUTTON_SIZE, BUTTON_SIZE, 2);

        // Smiley face
        int cx     = x + BUTTON_SIZE / 2;
        int cy     = y + BUTTON_SIZE / 2;
        int radius = BUTTON_SIZE / 2 - 4;
        media.DrawSmileyFace(renderer, cx, cy, radius, currentMode);
    }

    public bool HandleClick(int mouseX, int mouseY, int windowWidth, int yOffset)
    {
        int btnX = (windowWidth - BUTTON_SIZE) / 2;
        int btnY = yOffset + (INFO_BAR_HEIGHT - BUTTON_SIZE) / 2;

        if (mouseX >= btnX && mouseX < btnX + BUTTON_SIZE &&
            mouseY >= btnY && mouseY < btnY + BUTTON_SIZE)
        {
            onRestartClick?.Invoke();
            return true;
        }
        return false;
    }
}
