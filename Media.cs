using static SDL2.SDL;
using static SDL2.SDL_image;
using static SDL2.SDL_ttf;

namespace Zelos.Minesweeper;

public class Media
{

    private const int FONT_TEXTURE_SIZE = 500;

    private readonly Dictionary<string , IntPtr> textures = [];

    private IntPtr glyphs;
    private SDL_Rect[] glyphMap = [];

    public void LoadMedia(IntPtr renderer) {
        LoadTexures(renderer);
        LoadFonts(renderer);
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

    public void DrawText(string text, IntPtr renderer, int x, int y, byte r, byte g, byte b)
    {            
        if (SDL_SetTextureColorMod(glyphs, r, g, b)<0) {
            Console.WriteLine($"There was an issue setting the font atlas colour for drawing. {SDL_GetError()}"); 
        }   

        foreach(var c in text.ToCharArray())
        {
            var glyph = glyphMap[c];

            var dest = new SDL_Rect {
                x = x,
                y = y,
                w = glyph.w,
                h = glyph.h
            };

            if (SDL_RenderCopy(renderer, glyphs, ref glyph, ref dest) < 0) {
                Console.WriteLine($"There was an issue rendering a glyph. {SDL_GetError()}");
            }

            x += glyph.w;        
        }
    }

    public void DrawTextCentered(string text, IntPtr renderer, int x, int y, byte r, byte g, byte b)
    {            
        int width = 0;
        int height = 0;
        foreach(var c in text.ToCharArray())
        {
            var glyph = glyphMap[c];            
            width += glyph.w;
            height = glyph.h > height ? glyph.h : height;
        }

        x-=width/2;
        y-=height/2;
        DrawText(text, renderer, x, y, r, g, b);
    }

    private static IntPtr LoadTexure(string path, IntPtr renderer) {
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

    private void LoadTexures(IntPtr renderer) {
        var images = Directory.GetFiles("media", "*.png");
        foreach(var image in images)
            textures.Add(Path.GetFileNameWithoutExtension(image), LoadTexure(image, renderer));
    }
    private void FreeTextures() {        
          foreach(var texture in textures.Values)
            SDL_DestroyTexture(texture);

            SDL_DestroyTexture(glyphs);
    }

    private void LoadFonts(IntPtr renderer) {

        glyphMap = new SDL_Rect[128];

        var white = new SDL_Color {
            r = 255,
            g = 255,
            b = 255,
            a = 255
        };
        
        var dest_rect = new SDL_Rect();

        var font = TTF_OpenFont(@"media\swansea.ttf", 24);

        // Surface to hold all glyphAtlas with transparent background
        var surfacePtr = SDL_CreateRGBSurface(0, FONT_TEXTURE_SIZE, FONT_TEXTURE_SIZE, 32, 0, 0, 0, 0xff);
        var surface = System.Runtime.InteropServices.Marshal.PtrToStructure<SDL_Surface>(surfacePtr);

        if (SDL_SetColorKey(surfacePtr, 1, SDL_MapRGBA(surface.format, 0, 0, 0, 0)) < 0) {
            Console.WriteLine($"There was an issue setting the colour key for the glyph map. {SDL_GetError()}");
        }        

        for (var i = ' ' ; i <= 'z' ; i++) {

            // Render the glyph to a surface            
            var glyph = TTF_RenderUTF8_Blended(font, i.ToString(), white);

            // Get the size of the glyph            
            if (TTF_SizeText(font, i.ToString(), out dest_rect.w, out dest_rect.h) < 0) {
                Console.WriteLine($"There was an issue aqquiring the glyph size. {SDL_GetError()}");
            }

            // check to see if we need to start a new row because we have hit the max width of the map
            if (dest_rect.x + dest_rect.w >= FONT_TEXTURE_SIZE)
            {
                dest_rect.x = 0;
                dest_rect.y += dest_rect.h + 1;

                if (dest_rect.y + dest_rect.h >= FONT_TEXTURE_SIZE)
                {                   
                    throw new Exception($"The Font texture size is insufficant to hold all the glyphs");
                }
            }

            // render the glyph to the surface holding the glyph surface and store the coordinates of the glyph
            // in the atlas            
            if (SDL_BlitSurface(glyph, IntPtr.Zero, surfacePtr, ref dest_rect) < 0) {
                Console.WriteLine($"There was an issue blitting the glyph to the glyph map. {SDL_GetError()}");
            }               
            glyphMap[i] = new SDL_Rect {
                x = dest_rect.x,
                y = dest_rect.y,
                w = dest_rect.w,
                h = dest_rect.h                
            };

            dest_rect.x += dest_rect.w;
        }      

        glyphs = SDL_CreateTextureFromSurface(renderer, surfacePtr);             
    }
}