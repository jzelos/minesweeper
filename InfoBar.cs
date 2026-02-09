using static SDL2.SDL;

namespace Zelos.Minesweeper;

public class InfoBar
{
    private Media media;
    private const int INFO_BAR_HEIGHT = 40;
    private const int PANEL_WIDTH = 80;
    private const int PANEL_HEIGHT = 30;
    
    private int totalMines;
    private int flagsPlaced;
    private int elapsedSeconds;
    private bool timerRunning;
    private uint timerStartTicks;
    private GameMode currentMode;
    private Action? onRestartClick;
    
    private const int BUTTON_SIZE = 35;

    public InfoBar(Media media)
    {
        this.media = media;
    }

    public void StartGame(int mineCount)
    {
        totalMines = mineCount;
        flagsPlaced = 0;
        elapsedSeconds = 0;
        timerRunning = true;
        timerStartTicks = SDL_GetTicks();
        currentMode = GameMode.Game;
    }

    public void SetRestartCallback(Action callback)
    {
        onRestartClick = callback;
    }

    public void SetGameMode(GameMode mode)
    {
        currentMode = mode;
    }

    public void StopTimer()
    {
        timerRunning = false;
    }

    public void SetFlagsPlaced(int count)
    {
        flagsPlaced = count;
    }

    public void UpdateTimer()
    {
        if (timerRunning)
        {
            uint currentTicks = SDL_GetTicks();
            elapsedSeconds = (int)((currentTicks - timerStartTicks) / 1000);
            
            // Cap at 999
            if (elapsedSeconds > 999)
                elapsedSeconds = 999;
        }
    }

    public void Draw(IntPtr renderer, int windowWidth, int yOffset)
    {
        int y = yOffset;
        
        // Draw background
        media.Rectangle(renderer, 0, y, windowWidth, INFO_BAR_HEIGHT, Colour.MenuBar);
        
        // Draw border
        media.Rectangle(renderer, 0, y + INFO_BAR_HEIGHT - 1, windowWidth, 1, Colour.Gray);
        
        // Calculate positions
        int leftPanelX = 10;
        int rightPanelX = windowWidth - PANEL_WIDTH - 10;
        int panelY = y + (INFO_BAR_HEIGHT - PANEL_HEIGHT) / 2;
        
        // Draw mine counter panel
        DrawPanel(renderer, leftPanelX, panelY, totalMines - flagsPlaced, Colour.Red);
        
        // Draw timer panel
        DrawPanel(renderer, rightPanelX, panelY, elapsedSeconds, Colour.Blue);
        
        // Draw smiley button in center
        int buttonX = (windowWidth - BUTTON_SIZE) / 2;
        int buttonY = y + (INFO_BAR_HEIGHT - BUTTON_SIZE) / 2;
        DrawSmileyButton(renderer, buttonX, buttonY);
        
        // Draw labels
        media.DrawText("Mines", renderer, leftPanelX, y + 5, Colour.Black);
        media.DrawText("Time", renderer, rightPanelX, y + 5, Colour.Black);
    }

    private void DrawPanel(IntPtr renderer, int x, int y, int value, Colour textColor)
    {
        // Draw sunken panel background (dark background like LED display)
        media.Rectangle(renderer, x, y, PANEL_WIDTH, PANEL_HEIGHT, Colour.Black);
        
        // Draw 3D border (inverted for sunken look)
        media.Rectangle(renderer, x, y, PANEL_WIDTH, 1, Colour.BorderDark);
        media.Rectangle(renderer, x, y, 1, PANEL_HEIGHT, Colour.BorderDark);
        media.Rectangle(renderer, x + PANEL_WIDTH - 1, y, 1, PANEL_HEIGHT, Colour.BorderLight);
        media.Rectangle(renderer, x, y + PANEL_HEIGHT - 1, PANEL_WIDTH, 1, Colour.BorderLight);
        
        // Format value with leading zeros (3 digits)
        string displayValue = value.ToString().PadLeft(3, '0');
        
        // Clamp to 3 digits
        if (value > 999) displayValue = "999";
        if (value < 0) displayValue = "000";
        
        // Draw text centered in panel
        int textX = x + PANEL_WIDTH / 2;
        int textY = y + PANEL_HEIGHT / 2;
        media.DrawTextCentered(displayValue, renderer, textX, textY, textColor);
    }

    private void DrawSmileyButton(IntPtr renderer, int x, int y)
    {
        // Draw button background (raised)
        media.Rectangle(renderer, x, y, BUTTON_SIZE, BUTTON_SIZE, Colour.CellUnopened);
        
        // Draw 3D border
        media.Rectangle(renderer, x, y, BUTTON_SIZE, 2, Colour.BorderLight);
        media.Rectangle(renderer, x, y, 2, BUTTON_SIZE, Colour.BorderLight);
        media.Rectangle(renderer, x, y + BUTTON_SIZE - 2, BUTTON_SIZE, 2, Colour.BorderDark);
        media.Rectangle(renderer, x + BUTTON_SIZE - 2, y, 2, BUTTON_SIZE, Colour.BorderDark);
        
        // Choose smiley based on game state
        string smiley = currentMode switch
        {
            GameMode.GameOverWin => "B",  // B for "Winner" (sunglasses style)
            GameMode.GameOverLose => "X", // X for dead
            _ => "O"  // O for normal playing
        };
        
        Colour faceColor = currentMode switch
        {
            GameMode.GameOverWin => Colour.Green,
            GameMode.GameOverLose => Colour.Red,
            _ => Colour.Black
        };
        
        // Draw face in center
        media.DrawTextCentered(smiley, renderer, x + BUTTON_SIZE / 2, y + BUTTON_SIZE / 2, faceColor);
    }

    public bool HandleClick(int mouseX, int mouseY, int windowWidth, int yOffset)
    {
        int buttonX = (windowWidth - BUTTON_SIZE) / 2;
        int buttonY = yOffset + (INFO_BAR_HEIGHT - BUTTON_SIZE) / 2;
        
        if (mouseX >= buttonX && mouseX < buttonX + BUTTON_SIZE &&
            mouseY >= buttonY && mouseY < buttonY + BUTTON_SIZE)
        {
            onRestartClick?.Invoke();
            return true;
        }
        
        return false;
    }

    public int GetHeight()
    {
        return INFO_BAR_HEIGHT;
    }
}
