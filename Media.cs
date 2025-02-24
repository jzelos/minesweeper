using static SDL2.SDL;
using static SDL2.SDL_image;

namespace Zelos.Minesweeper;

public class Media
{

    private readonly Dictionary<string , IntPtr> textures = [];

    public void LoadMedia(IntPtr renderer) {
        var files = Directory.GetFiles("media", "*.png");
        foreach(var file in files)
            textures.Add(Path.GetFileNameWithoutExtension(file), GetTexure(file, renderer));
    }

    public void FreeMedia() {
        FreeTextures();
    }

    public void Draw(string resourceName, IntPtr renderer, int x, int y, int width, int height) {

        var source_rect = new SDL_Rect {
            x = 0,
            y = 0,
            w = 64,
            h = 64
        };

        // TODO preserve aspect ratio of source image
         var dest_rect = new SDL_Rect {
            x = x,
            y = y,
            w = width,
            h = height
        };
                    
        if (SDL_RenderCopy(renderer, textures[resourceName], ref source_rect, ref dest_rect)<0) {
                Console.WriteLine($"There was an issue rendering a texture. {SDL_GetError()}");
        };                
    }


    private static IntPtr GetTexure(string path, IntPtr renderer) {
        var surface = IMG_Load(path);
        if (surface < 0) {            
            throw new Exception($"There was an issue loading an image. {SDL_GetError()}");
        }

        var texture = SDL_CreateTextureFromSurface(renderer, surface);        
        
        if (texture < 0) {     
            var error = SDL_GetError();
            SDL_FreeSurface(surface);     
            throw new Exception($"There was an issue creating a texture. {error}");
        }
        
        SDL_FreeSurface(surface);
        return texture;
    }

    private void FreeTextures() {
          foreach(var value in textures.Values)
            SDL_DestroyTexture(value);
    }
}