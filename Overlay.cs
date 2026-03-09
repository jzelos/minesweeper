using static SDL2.SDL;

namespace Zelos.Minesweeper;

public enum OverlayMode { None, Win, Lose, Message }

/// <summary>
/// Renders a centered modal overlay on top of the game.
/// Handles Win, Lose, and generic Message dialogs.
/// </summary>
public class GameOverlay
{
    private readonly Media media;

    private OverlayMode mode = OverlayMode.None;
    private string title = "";
    private string[] lines = Array.Empty<string>();
    private Action? onConfirm;

    // Win-specific
    private int winTimeSeconds;
    private bool winIsNewRecord;
    private int? winPreviousBest;

    // Mouse tracking for button hover
    private int mouseX, mouseY;

    private const int PANEL_W      = 300;
    private const int BTN_W        = 160;
    private const int BTN_H        = 34;
    private const int BTN_MARGIN   = 16;

    public GameOverlay(Media media)
    {
        this.media = media;
    }

    public bool IsVisible => mode != OverlayMode.None;

    public void UpdateMouse(int x, int y)
    {
        mouseX = x;
        mouseY = y;
    }

    public void ShowWin(int timeSeconds, bool isNewRecord, int? previousBest, Action onRestart)
    {
        mode             = OverlayMode.Win;
        title            = "You Win!";
        winTimeSeconds   = timeSeconds;
        winIsNewRecord   = isNewRecord;
        winPreviousBest  = previousBest;
        onConfirm        = onRestart;
    }

    public void ShowLose(Action onRestart)
    {
        mode      = OverlayMode.Lose;
        title     = "Game Over";
        onConfirm = onRestart;
    }

    public void ShowMessage(string heading, string[] messageLines, Action onOk)
    {
        mode      = OverlayMode.Message;
        title     = heading;
        lines     = messageLines;
        onConfirm = onOk;
    }

    public void Hide()
    {
        mode = OverlayMode.None;
    }

    public void Draw(IntPtr renderer, int windowW, int windowH, int topOffset)
    {
        if (mode == OverlayMode.None) return;

        int gameAreaH = windowH - topOffset;
        int panelH    = ComputePanelHeight();
        int panelX    = (windowW - PANEL_W) / 2;
        int panelY    = topOffset + (gameAreaH - panelH) / 2;

        // Dark translucent backdrop
        media.RectangleAlpha(renderer, 0, topOffset, windowW, gameAreaH, Colour.OverlayBg);

        // Drop shadow
        media.RectangleAlpha(renderer, panelX + 4, panelY + 4, PANEL_W, panelH,
            new Colour(0, 0, 0, 90));

        // Panel body
        media.Rectangle(renderer, panelX, panelY, PANEL_W, panelH, Colour.OverlayPanel);
        media.DrawRaisedBorder(renderer, panelX, panelY, PANEL_W, panelH, 2);

        // Title bar
        Colour titleBg = mode switch
        {
            OverlayMode.Win     => Colour.WinGold,
            OverlayMode.Lose    => new Colour(180, 40, 40, 255),
            _                   => Colour.MenuHighlight
        };
        media.Rectangle(renderer, panelX + 2, panelY + 2, PANEL_W - 4, 32, titleBg);
        media.DrawTextCentered(title, renderer, panelX + PANEL_W / 2, panelY + 18,
            mode == OverlayMode.Win ? Colour.Black : Colour.White);

        int contentY = panelY + 42;

        switch (mode)
        {
            case OverlayMode.Win:
                string timeStr = $"Time: {winTimeSeconds}s";
                media.DrawTextSmallCentered(timeStr, renderer,
                    panelX + PANEL_W / 2, contentY + 6, Colour.Black);

                if (winIsNewRecord)
                {
                    media.DrawTextSmallCentered("*** New Best Time! ***", renderer,
                        panelX + PANEL_W / 2, contentY + 24, Colour.WinGold);
                }
                else if (winPreviousBest.HasValue)
                {
                    media.DrawTextSmallCentered($"Best: {winPreviousBest.Value}s", renderer,
                        panelX + PANEL_W / 2, contentY + 24, Colour.Gray);
                }
                break;

            case OverlayMode.Lose:
                media.DrawTextSmallCentered("Better luck next time!", renderer,
                    panelX + PANEL_W / 2, contentY + 6, Colour.Black);
                break;

            case OverlayMode.Message:
                int ly = contentY + 4;
                foreach (var line in lines)
                {
                    media.DrawTextSmallCentered(line, renderer,
                        panelX + PANEL_W / 2, ly, Colour.Black);
                    ly += 18;
                }
                break;
        }

        // "Play Again" / "OK" button
        int btnX = panelX + (PANEL_W - BTN_W) / 2;
        int btnY = panelY + panelH - BTN_H - BTN_MARGIN;

        bool hovered = mouseX >= btnX && mouseX < btnX + BTN_W &&
                       mouseY >= btnY && mouseY < btnY + BTN_H;

        Colour btnFill = hovered ? Colour.CellHover : Colour.CellUnopened;
        media.Rectangle(renderer, btnX, btnY, BTN_W, BTN_H, btnFill);
        media.DrawRaisedBorder(renderer, btnX, btnY, BTN_W, BTN_H, hovered ? 1 : 2);

        string btnLabel = mode == OverlayMode.Message ? "OK" : "Play Again";
        media.DrawTextSmallCentered(btnLabel, renderer,
            btnX + BTN_W / 2, btnY + BTN_H / 2, Colour.Black);
    }

    /// <summary>
    /// Handles a mouse-press event. Returns true if the overlay consumed the click.
    /// </summary>
    public bool HandleClick(int x, int y, int windowW, int windowH, int topOffset)
    {
        if (mode == OverlayMode.None) return false;

        int gameAreaH = windowH - topOffset;
        int panelH    = ComputePanelHeight();
        int panelX    = (windowW - PANEL_W) / 2;
        int panelY    = topOffset + (gameAreaH - panelH) / 2;

        int btnX = panelX + (PANEL_W - BTN_W) / 2;
        int btnY = panelY + panelH - BTN_H - BTN_MARGIN;

        if (x >= btnX && x < btnX + BTN_W && y >= btnY && y < btnY + BTN_H)
        {
            onConfirm?.Invoke();
            Hide();
        }

        // Consume all clicks while overlay is visible
        return true;
    }

    private int ComputePanelHeight()
    {
        return mode switch
        {
            OverlayMode.Win     => 160,
            OverlayMode.Lose    => 130,
            OverlayMode.Message => 110 + lines.Length * 18 + BTN_H + BTN_MARGIN,
            _                   => 130
        };
    }
}
