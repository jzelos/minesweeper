using static SDL2.SDL;
using static SDL2.SDL_image;
using System;
using System.Runtime.InteropServices;

namespace Zelos.Minesweeper;

class Program
{
    static bool running = true; 
    static IntPtr window;
    static IntPtr renderer;    

    public enum GameMode {
        Menu,
        Game
    }

    static GameMode mode;

    static Grid grid;

    static Media media;

    public static void Main (string[] args) 
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
    private static void Setup() {

        SDL_SetHint(SDL_HINT_WINDOWS_DISABLE_THREAD_NAMING, "1");

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
        media = new Media();
        media.LoadMedia(renderer);

        // TODO move to menu
        grid = new Grid(media);
        grid.Initalize();
        mode = GameMode.Game;
    }

    /// <summary>
    /// Checks to see if there are any events to be processed.
    /// </summary>
    private static void PollEvents()
    {
        // Check to see if there are any events and continue to do so until the queue is empty.
        while (SDL_PollEvent(out SDL_Event e) == 1)
        {
            switch (e.type)
            {
                case SDL_EventType.SDL_QUIT:
                    running = false;
                    break;
            }
        }
    }

    /// <summary>
    /// Renders to the window.
    /// </summary>
    private static void Render()
    {
         // Clears the current render surface.
        SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255);
        SDL_RenderClear(renderer);

        // Draw        
        switch (mode) {
            case GameMode.Menu:
            case GameMode.Game:
                grid.Draw(window, renderer);
                break;
        }
        
        // Switches out the currently presented render surface with the one we just did work on.
        SDL_RenderPresent(renderer);
    }

    /// <summary>
    /// Clean up the resources that were created.
    /// </summary>
    private static void CleanUp() {        
        // Clean up the resources that were created.
        SDL_DestroyRenderer(renderer);
        SDL_DestroyWindow(window);
        media.FreeMedia();
        SDL_Quit();
    }
}
