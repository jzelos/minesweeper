using static SDL2.SDL;
using static SDL2.SDL_image;
using static SDL2.SDL_ttf;

namespace Zelos.Minesweeper;

public enum GameMode {
    Menu,
    Game,
    GameOverWin,
    GameOverLose
}

class Game
{
    private bool    running  = true;
    private IntPtr  window;
    private IntPtr  renderer;
    private GameMode mode;

    // Current difficulty
    private int    currentRows       = 16;
    private int    currentColumns    = 16;
    private int    currentMines      = StandardMines.MEDIUM;
    private string currentDifficulty = "Medium";

    // Mouse chord-click tracking
    private bool leftButtonDown;
    private bool rightButtonDown;

    private readonly Media            media;
    private readonly Grid             grid;
    private readonly Mouse            mouse;
    private readonly MenuBar          menuBar;
    private readonly InfoBar          infoBar;
    private readonly HighScoreManager highScore;
    private readonly GameOverlay      overlay;

    public Game()
    {
        media     = new Media();
        mouse     = new Mouse();
        grid      = new Grid(media);
        menuBar   = new MenuBar(media);
        infoBar   = new InfoBar(media);
        highScore = new HighScoreManager();
        overlay   = new GameOverlay(media);
    }

    public void Start()
    {
        Setup();
        while (running)
        {
            PollEvents();
            Render();
        }
        CleanUp();
    }

    // ── Initialisation ───────────────────────────────────────────────────────

    private void Setup()
    {
        SDL_SetHint(SDL_HINT_WINDOWS_DISABLE_THREAD_NAMING, "1");
        SDL_SetHint(SDL_HINT_RENDER_SCALE_QUALITY, "1");

        if (SDL_Init(SDL_INIT_VIDEO) < 0)
            Console.WriteLine($"SDL init error: {SDL_GetError()}");

        if (IMG_Init(IMG_InitFlags.IMG_INIT_PNG) == 0)
            Console.WriteLine($"SDL_image init error: {IMG_GetError()}");

        if (TTF_Init() < 0)
            Console.WriteLine($"SDL_ttf init error: {TTF_GetError()}");

        window = SDL_CreateWindow(Resources.WINDOW_TITLE,
            SDL_WINDOWPOS_CENTERED, SDL_WINDOWPOS_CENTERED,
            640, 480,
            SDL_WindowFlags.SDL_WINDOW_SHOWN | SDL_WindowFlags.SDL_WINDOW_RESIZABLE);

        if (window == IntPtr.Zero)
            Console.WriteLine($"Window creation error: {SDL_GetError()}");

        renderer = SDL_CreateRenderer(window, -1,
            SDL_RendererFlags.SDL_RENDERER_ACCELERATED |
            SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);

        if (renderer == IntPtr.Zero)
            Console.WriteLine($"Renderer creation error: {SDL_GetError()}");

        media.LoadMedia(renderer);
        InitializeMenuBar();
        infoBar.SetRestartCallback(RestartGame);

        StartNewGame(currentRows, currentColumns, currentMines, currentDifficulty);
    }

    private void StartNewGame(int rows, int cols, int mines, string difficulty)
    {
        currentRows       = rows;
        currentColumns    = cols;
        currentMines      = mines;
        currentDifficulty = difficulty;

        grid.Initalize(rows, cols, mines);
        infoBar.StartGame(grid.GetTotalMines());
        mode = GameMode.Game;
        overlay.Hide();
        leftButtonDown  = false;
        rightButtonDown = false;

        ResizeWindowToFit();
    }

    private void RestartGame()
    {
        StartNewGame(currentRows, currentColumns, currentMines, currentDifficulty);
    }

    private void ResizeWindowToFit()
    {
        var (gridW, gridH)  = grid.GetPreferredSize();
        int windowW = Math.Max(Resources.MIN_WINDOW_WIDTH, gridW);
        int windowH = menuBar.GetHeight() + infoBar.GetHeight() + gridH;
        SDL_SetWindowSize(window, windowW, windowH);
        SDL_SetWindowMinimumSize(window,
            Math.Min(windowW, Resources.MIN_WINDOW_WIDTH), windowH);
    }

    private void InitializeMenuBar()
    {
        // Game menu
        var gameMenu = new MenuItem("Game");
        gameMenu.SubItems.Add(new MenuItem("New Game - Easy", () =>
            StartNewGame(9, 9, StandardMines.EASY, "Easy")));
        gameMenu.SubItems.Add(new MenuItem("New Game - Medium", () =>
            StartNewGame(16, 16, StandardMines.MEDIUM, "Medium")));
        gameMenu.SubItems.Add(new MenuItem("New Game - Hard", () =>
            StartNewGame(30, 16, StandardMines.HARD, "Hard")));
        gameMenu.SubItems.Add(MenuItem.Separator());
        gameMenu.SubItems.Add(new MenuItem("Exit", () => running = false));
        menuBar.AddMenu(gameMenu);

        // Options menu
        var optionsMenu = new MenuItem("Options");
        optionsMenu.SubItems.Add(new MenuItem("Custom - Small  (12x10, 20 mines)", () =>
            StartNewGame(12, 10, 20, "Custom")));
        optionsMenu.SubItems.Add(new MenuItem("Custom - Large  (24x20, 80 mines)", () =>
            StartNewGame(24, 20, 80, "Custom")));
        optionsMenu.SubItems.Add(new MenuItem("Custom - XL     (30x24, 130 mines)", () =>
            StartNewGame(30, 24, 130, "Custom")));
        optionsMenu.SubItems.Add(MenuItem.Separator());
        optionsMenu.SubItems.Add(new MenuItem("View Best Times", () =>
        {
            var easy   = highScore.GetBest("Easy");
            var medium = highScore.GetBest("Medium");
            var hard   = highScore.GetBest("Hard");
            overlay.ShowMessage("Best Times", new[]
            {
                $"Easy:   {(easy.HasValue   ? easy.Value   + "s" : "--")}",
                $"Medium: {(medium.HasValue ? medium.Value + "s" : "--")}",
                $"Hard:   {(hard.HasValue   ? hard.Value   + "s" : "--")}",
            }, () => { });
        }));
        menuBar.AddMenu(optionsMenu);

        // Help menu
        var helpMenu = new MenuItem("Help");
        helpMenu.SubItems.Add(new MenuItem("How to Play", () =>
            overlay.ShowMessage("How to Play", new[]
            {
                "Left click  - open a cell",
                "Right click - place / remove a flag",
                "Middle click or L+R - chord open",
                "Click smiley to restart",
            }, () => { })));
        helpMenu.SubItems.Add(MenuItem.Separator());
        helpMenu.SubItems.Add(new MenuItem("About", () =>
            overlay.ShowMessage("About Minesweeper", new[]
            {
                "Built with SDL2 + C#",
                "Version 1.0",
            }, () => { })));
        menuBar.AddMenu(helpMenu);
    }

    // ── Event processing ─────────────────────────────────────────────────────

    private void PollEvents()
    {
        while (SDL_PollEvent(out SDL_Event e) == 1)
        {
            switch (e.type)
            {
                case SDL_EventType.SDL_QUIT:
                    running = false;
                    break;
                case SDL_EventType.SDL_MOUSEMOTION:
                    ProcessMouseMoveEvent(e.motion);
                    break;
                case SDL_EventType.SDL_MOUSEBUTTONDOWN:
                case SDL_EventType.SDL_MOUSEBUTTONUP:
                    ProcessMouseEvent(e.button);
                    break;
            }
        }
    }

    private void ProcessMouseMoveEvent(SDL_MouseMotionEvent e)
    {
        overlay.UpdateMouse(e.x, e.y);
        menuBar.HandleMouseMove(e.x, e.y);

        if (!menuBar.IsMenuOpen() && !overlay.IsVisible)
            grid.UpdateHover(window, e.x, e.y);
    }

    private void ProcessMouseEvent(SDL_MouseButtonEvent e)
    {
        mouse.ProcessButtonEvent(e);

        if (e.state == SDL_PRESSED)
        {
            // Overlay gets first priority
            if (overlay.IsVisible)
            {
                SDL_GetWindowSize(window, out int ww, out int wh);
                overlay.HandleClick(e.x, e.y, ww, wh, menuBar.GetHeight() + infoBar.GetHeight());
                return;
            }

            // Menu bar
            if (menuBar.HandleMouseClick(e.x, e.y)) return;

            // Info bar (smiley button)
            SDL_GetWindowSize(window, out int width, out int height);
            if (infoBar.HandleClick(e.x, e.y, width, menuBar.GetHeight())) return;
        }

        if (menuBar.IsMenuOpen()) return;
        if (mode != GameMode.Game) return;

        // Reset chord-tracking state on button release
        if (e.state != SDL_PRESSED)
        {
            if (e.button == 1) leftButtonDown  = false;
            if (e.button == 3) rightButtonDown = false;
            return;
        }

        // Track individual button downs for chord detection
        if (e.button == 1) leftButtonDown  = true;
        if (e.button == 3) rightButtonDown = true;

        // Chord click: both buttons held simultaneously (or middle click)
        bool isChord = (leftButtonDown && rightButtonDown) || e.button == 2;
        if (isChord)
        {
            if (!grid.ChordOpen(window, e.x, e.y))
                TriggerGameOver();
            else if (grid.IsComplete())
                TriggerWin();

            leftButtonDown  = false;
            rightButtonDown = false;
            return;
        }

        // Right click – flag / unflag
        if (e.button == 3)
        {
            grid.MarkCell(window, e.x, e.y);
            infoBar.SetFlagsPlaced(grid.GetFlagCount());
        }

        // Left click – open cell
        if (e.button == 1)
        {
            if (!grid.OpenCell(window, e.x, e.y))
                TriggerGameOver();
            else if (grid.IsComplete())
                TriggerWin();
        }
    }

    // ── Game-state transitions ────────────────────────────────────────────────

    private void TriggerGameOver()
    {
        grid.UncoverWithClickedMine();
        infoBar.StopTimer();
        mode = GameMode.GameOverLose;
        overlay.ShowLose(RestartGame);
    }

    private void TriggerWin()
    {
        grid.FlagRemainingMines();
        infoBar.SetFlagsPlaced(grid.GetFlagCount());
        infoBar.StopTimer();
        mode = GameMode.GameOverWin;

        int elapsed  = infoBar.GetElapsedSeconds();
        bool isNew   = highScore.CheckAndUpdate(currentDifficulty, elapsed);
        int? prevBest = isNew ? (int?)null : highScore.GetBest(currentDifficulty);
        overlay.ShowWin(elapsed, isNew, prevBest, RestartGame);
    }

    // ── Rendering ────────────────────────────────────────────────────────────

    private void Render()
    {
        SDL_GetWindowSize(window, out int width, out int height);

        SDL_SetRenderDrawColor(renderer, 30, 30, 30, 255);
        SDL_RenderClear(renderer);

        menuBar.DrawBar(window, renderer, width);

        int menuH = menuBar.GetHeight();
        infoBar.UpdateTimer();
        infoBar.SetFlagsPlaced(grid.GetFlagCount());
        infoBar.SetGameMode(mode);
        infoBar.Draw(renderer, width, menuH);

        int topOffset = menuH + infoBar.GetHeight();
        grid.Draw(window, renderer, 0, topOffset, width, height - topOffset);

        // Overlay (win / lose / message) sits on top of the grid
        overlay.Draw(renderer, width, height, topOffset);

        // Submenus render on top of everything
        menuBar.DrawSubmenus(renderer);

        SDL_RenderPresent(renderer);
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────

    private void CleanUp()
    {
        TTF_Quit();
        SDL_DestroyRenderer(renderer);
        SDL_DestroyWindow(window);
        media.FreeMedia();
        SDL_Quit();
    }
}
