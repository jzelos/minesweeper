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
    private bool running = true; 
    private IntPtr window;
    private IntPtr renderer;    
    private GameMode mode;

    private readonly Media media;
    private readonly Grid grid;
    private readonly Mouse mouse;
    private readonly MenuBar menuBar;

    public Game() {
        media = new Media();
        mouse = new Mouse();
        grid = new Grid(media);
        menuBar = new MenuBar(media);
    }

    public void Start () 
    {
        Setup();        

        while (running)
        {
            PollEvents();
            Render();
        }

        CleanUp();
    }

    /// <summary>
    /// Setup all of the SDL resources we'll need to display a window.
    /// </summary>
    private void Setup() {

        SDL_SetHint(SDL_HINT_WINDOWS_DISABLE_THREAD_NAMING, "1");
        SDL_SetHint(SDL_HINT_RENDER_SCALE_QUALITY, "1"); // Enable linear filtering

        // Initilizes SDL.
        if (SDL_Init(SDL_INIT_VIDEO) < 0)
        {
            Console.WriteLine($"There was an issue initilizing SDL. {SDL_GetError()}");
        }

        // Initilizes SDL_image for use with png files.
        if (IMG_Init(IMG_InitFlags.IMG_INIT_PNG) == 0)
        {
            Console.WriteLine($"There was an issue initilizing SDL2_Image {IMG_GetError()}");
        }

        // Initialise true type fonts
        if (TTF_Init() < 0)
        {
            Console.WriteLine($"There was an issue initilizing SDL2_Image {IMG_GetError()}");        
        }
        
        // Create a new window given a title, size, and passes it a flag indicating it should be shown.
        window = SDL_CreateWindow(Resources.WINDOW_TITLE, SDL_WINDOWPOS_UNDEFINED, SDL_WINDOWPOS_UNDEFINED, 640, 480, SDL_WindowFlags.SDL_WINDOW_SHOWN | SDL_WindowFlags.SDL_WINDOW_RESIZABLE );        

        if (window == IntPtr.Zero)
        {
            Console.WriteLine($"There was an issue creating the window. {SDL_GetError()}");
        }

        // Creates a new SDL hardware renderer using the default graphics device with VSYNC enabled.
        renderer = SDL_CreateRenderer(window, -1, SDL_RendererFlags.SDL_RENDERER_ACCELERATED | SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);

        if (renderer == IntPtr.Zero)
        {
            Console.WriteLine($"There was an issue creating the renderer. {SDL_GetError()}");
        }        

        media.LoadMedia(renderer);

        InitializeMenuBar();

        // TODO move to menu        
        grid.Initalize(10,10);
        mode = GameMode.Game;
    }

    private void InitializeMenuBar() {
        // Game menu
        var gameMenu = new MenuItem("Game");
        gameMenu.SubItems.Add(new MenuItem("New Game - Easy", () => {
            grid.Initalize(9, 9);
            mode = GameMode.Game;
        }));
        gameMenu.SubItems.Add(new MenuItem("New Game - Medium", () => {
            grid.Initalize(16, 16);
            mode = GameMode.Game;
        }));
        gameMenu.SubItems.Add(new MenuItem("New Game - Hard", () => {
            grid.Initalize(30, 16);
            mode = GameMode.Game;
        }));
        gameMenu.SubItems.Add(MenuItem.Separator());
        gameMenu.SubItems.Add(new MenuItem("Exit", () => {
            running = false;
        }));
        menuBar.AddMenu(gameMenu);

        // Options menu
        var optionsMenu = new MenuItem("Options");
        optionsMenu.SubItems.Add(new MenuItem("Custom Grid...", () => {
            // TODO: Implement custom grid dialog
        }));
        menuBar.AddMenu(optionsMenu);

        // Help menu
        var helpMenu = new MenuItem("Help");
        helpMenu.SubItems.Add(new MenuItem("About", () => {
            // TODO: Show about dialog
        }));
        menuBar.AddMenu(helpMenu);
    }

    /// <summary>
    /// Checks to see if there are any events to be processed.
    /// </summary>
    private void PollEvents()
    {
        // Check to see if there are any events and continue to do so until the queue is empty.
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

    private void ProcessMouseMoveEvent(SDL_MouseMotionEvent e) {
        menuBar.HandleMouseMove(e.x, e.y);
        
        // Update grid hover if menu is not open
        if (!menuBar.IsMenuOpen()) {
            grid.UpdateHover(window, e.x, e.y);
        }
    }


    private void ProcessMouseEvent(SDL_MouseButtonEvent e) {        
        mouse.ProcessButtonEvent(e);      

        // Handle menu bar clicks first
        if (e.state == SDL_PRESSED) {
            if (menuBar.HandleMouseClick(e.x, e.y)) {
                return; // Menu handled the click
            }
        }

        // Only process game clicks if menu is not open
        if (!menuBar.IsMenuOpen()) {
            if (mouse.ButtonState[3] == 1) {
                grid.MarkCell(window, mouse.X, mouse.Y);
            }

            if (mouse.ButtonState[1] == 1) {
                if (!grid.OpenCell(window, mouse.X, mouse.Y))
                {
                    grid.Uncover();
                    mode = GameMode.GameOverLose;
                }
            }

            if (grid.IsComplete()) {
                mode = GameMode.GameOverWin;
            }
        }
    }

    /// <summary>
    /// Renders to the window.
    /// </summary>
    private void Render()
    {
        SDL_GetWindowSize(window, out int width, out int height);

         // Clears the current render surface.
        SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255);
        SDL_RenderClear(renderer);

        // Draw menu bar (without submenus)
        menuBar.DrawBar(window, renderer, width);

        // Draw game content below menu bar
        int menuHeight = menuBar.GetHeight();
        switch (mode) {
            case GameMode.Menu:
            case GameMode.GameOverWin:                                
            case GameMode.GameOverLose:                                
            case GameMode.Game:
                grid.Draw(window, renderer, 0, menuHeight, width, height - menuHeight);
                break;
        }

        // Draw submenus on top of everything
        menuBar.DrawSubmenus(renderer);
        
        // Switches out the currently presented render surface with the one we just did work on.
        SDL_RenderPresent(renderer);
    }

    /// <summary>
    /// Clean up the resources that were created.
    /// </summary>
    private void CleanUp() {        
        // Clean up the resources that were created.
        TTF_Quit();
        SDL_DestroyRenderer(renderer);
        SDL_DestroyWindow(window);
        media.FreeMedia();
        SDL_Quit();
    }
}