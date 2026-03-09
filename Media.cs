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
    
    private IntPtr glyphsLarge;
    private SDL_Rect[] glyphMapLarge = [];

    public void Rectangle(IntPtr renderer, int x, int y, int width, int height, Colour colour) {
        SDL_SetRenderDrawColor(renderer, colour.R, colour.G, colour.B, colour.A);
        var r = new SDL_Rect { x = x, y = y, w = width, h = height };
        SDL_RenderFillRect(renderer, ref r);
    }

    /// <summary>Alpha-blended rectangle (A channel of colour controls opacity).</summary>
    public void RectangleAlpha(IntPtr renderer, int x, int y, int width, int height, Colour colour)
    {
        SDL_SetRenderDrawBlendMode(renderer, SDL_BlendMode.SDL_BLENDMODE_BLEND);
        SDL_SetRenderDrawColor(renderer, colour.R, colour.G, colour.B, colour.A);
        var r = new SDL_Rect { x = x, y = y, w = width, h = height };
        SDL_RenderFillRect(renderer, ref r);
        SDL_SetRenderDrawBlendMode(renderer, SDL_BlendMode.SDL_BLENDMODE_NONE);
    }

    /// <summary>Draw a thick line between two points.</summary>
    public void DrawLine(IntPtr renderer, int x1, int y1, int x2, int y2, Colour colour, int thickness = 1)
    {
        SDL_SetRenderDrawColor(renderer, colour.R, colour.G, colour.B, colour.A);
        int half = thickness / 2;
        int dx = Math.Abs(x2 - x1), dy = Math.Abs(y2 - y1);
        for (int t = -half; t <= half; t++)
        {
            if (dx >= dy)
                SDL_RenderDrawLine(renderer, x1, y1 + t, x2, y2 + t);
            else
                SDL_RenderDrawLine(renderer, x1 + t, y1, x2 + t, y2);
        }
    }

    /// <summary>Fill a circle using horizontal scan lines.</summary>
    public void FillCircle(IntPtr renderer, int cx, int cy, int radius, Colour colour)
    {
        SDL_SetRenderDrawColor(renderer, colour.R, colour.G, colour.B, colour.A);
        for (int dy = -radius; dy <= radius; dy++)
        {
            int w = (int)Math.Sqrt(Math.Max(0.0, (double)(radius * radius - dy * dy)));
            SDL_RenderDrawLine(renderer, cx - w, cy + dy, cx + w, cy + dy);
        }
    }

    /// <summary>Draw a circle outline (1–2 px wide).</summary>
    public void CircleOutline(IntPtr renderer, int cx, int cy, int radius, Colour colour, int thickness = 1)
    {
        for (int t = 0; t < thickness; t++)
        {
            int r = radius - t;
            if (r <= 0) break;
            int x = r, y = 0, p = 1 - r;
            SDL_SetRenderDrawColor(renderer, colour.R, colour.G, colour.B, colour.A);
            while (x >= y)
            {
                SDL_RenderDrawPoint(renderer, cx + x, cy + y);
                SDL_RenderDrawPoint(renderer, cx + y, cy + x);
                SDL_RenderDrawPoint(renderer, cx - x, cy + y);
                SDL_RenderDrawPoint(renderer, cx - y, cy + x);
                SDL_RenderDrawPoint(renderer, cx - x, cy - y);
                SDL_RenderDrawPoint(renderer, cx - y, cy - x);
                SDL_RenderDrawPoint(renderer, cx + x, cy - y);
                SDL_RenderDrawPoint(renderer, cx + y, cy - x);
                y++;
                if (p <= 0) p += 2 * y + 1;
                else { x--; p += 2 * (y - x) + 1; }
            }
        }
    }

    // ── 3D borders ──────────────────────────────────────────────────────────

    public void DrawRaisedBorder(IntPtr renderer, int x, int y, int width, int height, int thickness = 2)
    {
        for (int i = 0; i < thickness; i++)
        {
            Rectangle(renderer, x + i,         y + i,          width  - i * 2, 1,      Colour.BorderLight);
            Rectangle(renderer, x + i,         y + i,          1,              height - i * 2, Colour.BorderLight);
            Rectangle(renderer, x + i,         y + height - 1 - i, width  - i * 2, 1,  Colour.BorderDark);
            Rectangle(renderer, x + width - 1 - i, y + i,      1,              height - i * 2, Colour.BorderDark);
        }
    }

    public void DrawSunkenBorder(IntPtr renderer, int x, int y, int width, int height, int thickness = 2)
    {
        for (int i = 0; i < thickness; i++)
        {
            Rectangle(renderer, x + i,         y + i,          width  - i * 2, 1,      Colour.BorderDark);
            Rectangle(renderer, x + i,         y + i,          1,              height - i * 2, Colour.BorderDark);
            Rectangle(renderer, x + i,         y + height - 1 - i, width  - i * 2, 1,  Colour.BorderLight);
            Rectangle(renderer, x + width - 1 - i, y + i,      1,              height - i * 2, Colour.BorderLight);
        }
    }

    // ── Flag graphic ─────────────────────────────────────────────────────────

    /// <summary>Draw a classic flag (triangular red flag on a dark pole with base).</summary>
    public void DrawFlag(IntPtr renderer, int cellX, int cellY, int cellW, int cellH)
    {
        int poleX      = cellX + cellW * 2 / 5;
        int poleTop    = cellY + cellH / 8;
        int poleBottom = cellY + cellH * 7 / 8;
        int poleWidth  = Math.Max(2, cellW / 14);

        // Pole
        SDL_SetRenderDrawColor(renderer, Colour.PoleDark.R, Colour.PoleDark.G, Colour.PoleDark.B, 255);
        var pRect = new SDL_Rect { x = poleX, y = poleTop, w = poleWidth, h = poleBottom - poleTop };
        SDL_RenderFillRect(renderer, ref pRect);

        // Flag triangle to the right of the pole
        int flagTop   = poleTop;
        int flagBot   = poleTop + (poleBottom - poleTop) * 4 / 10;
        int flagRight = poleX + cellW * 3 / 8;
        SDL_SetRenderDrawColor(renderer, Colour.FlagRed.R, Colour.FlagRed.G, Colour.FlagRed.B, 255);
        FillTriangle(renderer,
            poleX + poleWidth, flagTop,
            poleX + poleWidth, flagBot,
            flagRight, (flagTop + flagBot) / 2);

        // Base
        SDL_SetRenderDrawColor(renderer, Colour.PoleDark.R, Colour.PoleDark.G, Colour.PoleDark.B, 255);
        int basePad   = cellW / 5;
        int baseH     = Math.Max(2, cellH / 14);
        var bRect = new SDL_Rect { x = cellX + basePad, y = poleBottom - baseH, w = cellW - basePad * 2, h = baseH + 1 };
        SDL_RenderFillRect(renderer, ref bRect);
    }

    /// <summary>Draw a red X across the cell (used for incorrect flags on game-over).</summary>
    public void DrawX(IntPtr renderer, int x, int y, int w, int h, Colour colour)
    {
        int inset = Math.Max(4, w / 5);
        DrawLine(renderer, x + inset, y + inset, x + w - inset, y + h - inset, colour, 2);
        DrawLine(renderer, x + w - inset, y + inset, x + inset, y + h - inset, colour, 2);
    }

    // ── Seven-segment LED display ─────────────────────────────────────────────

    // Segment bit masks: bit0=a(top) b c d e f bit6=g(middle)
    private static readonly int[] SegDigits = { 63, 6, 91, 79, 102, 109, 125, 7, 127, 111 };
    private const int SegMinus = 64; // g only

    private void DrawSevenSegmentDigit(IntPtr renderer, int x, int y, int dw, int dh, int mask,
        Colour onCol, Colour offCol)
    {
        int t  = Math.Max(2, dw / 5);
        int hs = dh / 2;

        // Precompute all 7 segment rects: [a, b, c, d, e, f, g]
        var segs = new SDL_Rect[]
        {
            new() { x = x + t,       y = y,         w = dw - t * 2, h = t      }, // a top
            new() { x = x + dw - t,  y = y + t,     w = t,  h = hs - t         }, // b upper-right
            new() { x = x + dw - t,  y = y + hs,    w = t,  h = hs - t         }, // c lower-right
            new() { x = x + t,       y = y + dh - t,w = dw - t * 2, h = t      }, // d bottom
            new() { x = x,           y = y + hs,    w = t,  h = hs - t         }, // e lower-left
            new() { x = x,           y = y + t,     w = t,  h = hs - t         }, // f upper-left
            new() { x = x + t,       y = y + hs - t / 2, w = dw - t * 2, h = t }, // g middle
        };

        for (int s = 0; s < 7; s++)
        {
            bool on = (mask & (1 << s)) != 0;
            Colour c = on ? onCol : offCol;
            SDL_SetRenderDrawColor(renderer, c.R, c.G, c.B, 255);
            SDL_RenderFillRect(renderer, ref segs[s]);
        }
    }

    /// <summary>
    /// Draw a <paramref name="numDigits"/>-digit 7-segment display. Handles 0 … 999 and negative values
    /// (shown as '-' + up to 2 digits). Background is drawn automatically.
    /// </summary>
    public void DrawSevenSegmentDisplay(IntPtr renderer, int x, int y, int totalW, int totalH,
        int value, int numDigits, Colour onCol, Colour offCol, Colour bgCol)
    {
        // Background
        Rectangle(renderer, x, y, totalW, totalH, bgCol);

        bool negative = value < 0;
        int  absVal   = Math.Abs(value);
        if (absVal > 999) absVal = 999;

        int padX    = 4;
        int padY    = 3;
        int gap     = 3;
        int dh      = totalH - padY * 2;
        int dw      = (totalW - padX * 2 - (numDigits - 1) * gap) / numDigits;

        // Build digit array right-to-left
        int[] digits = new int[numDigits];
        int v = absVal;
        for (int i = numDigits - 1; i >= 0; i--)
        {
            digits[i] = v % 10;
            v /= 10;
        }

        int dx = x + padX;
        for (int i = 0; i < numDigits; i++)
        {
            int mask;
            if (negative && i == 0)
                mask = SegMinus;
            else
                mask = SegDigits[digits[i]];

            DrawSevenSegmentDigit(renderer, dx, y + padY, dw, dh, mask, onCol, offCol);
            dx += dw + gap;
        }
    }

    // ── Smiley face ───────────────────────────────────────────────────────────

    /// <summary>Draw a smiley face centred at (cx, cy) with the given radius.</summary>
    public void DrawSmileyFace(IntPtr renderer, int cx, int cy, int radius, GameMode mode)
    {
        // Face fill
        FillCircle(renderer, cx, cy, radius, Colour.SmileyYellow);
        CircleOutline(renderer, cx, cy, radius, Colour.SmileyBorder, 2);

        int eyeOffX = radius * 2 / 5;
        int eyeOffY = radius / 4;

        if (mode == GameMode.GameOverLose)
        {
            // X eyes
            int eyeH = Math.Max(2, radius / 5);
            SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255);
            SDL_RenderDrawLine(renderer, cx - eyeOffX - eyeH, cy - eyeOffY - eyeH,
                                         cx - eyeOffX + eyeH, cy - eyeOffY + eyeH);
            SDL_RenderDrawLine(renderer, cx - eyeOffX + eyeH, cy - eyeOffY - eyeH,
                                         cx - eyeOffX - eyeH, cy - eyeOffY + eyeH);
            SDL_RenderDrawLine(renderer, cx + eyeOffX - eyeH, cy - eyeOffY - eyeH,
                                         cx + eyeOffX + eyeH, cy - eyeOffY + eyeH);
            SDL_RenderDrawLine(renderer, cx + eyeOffX + eyeH, cy - eyeOffY - eyeH,
                                         cx + eyeOffX - eyeH, cy - eyeOffY + eyeH);
            // Frown
            DrawMouthArc(renderer, cx, cy + radius / 5, radius / 2, false);
        }
        else if (mode == GameMode.GameOverWin)
        {
            // Sunglasses over eyes (cool-win face)
            int shadeW = radius * 2 / 3;
            int shadeH = Math.Max(3, radius / 4);
            SDL_SetRenderDrawColor(renderer, 20, 20, 20, 255);
            var leftShade  = new SDL_Rect { x = cx - eyeOffX - shadeW / 2, y = cy - eyeOffY - shadeH / 2, w = shadeW, h = shadeH };
            var rightShade = new SDL_Rect { x = cx + eyeOffX - shadeW / 2, y = cy - eyeOffY - shadeH / 2, w = shadeW, h = shadeH };
            SDL_RenderFillRect(renderer, ref leftShade);
            SDL_RenderFillRect(renderer, ref rightShade);
            // Bridge between shades
            var bridge = new SDL_Rect { x = cx - 2, y = cy - eyeOffY, w = 4, h = 2 };
            SDL_RenderFillRect(renderer, ref bridge);
            // Big smile
            DrawMouthArc(renderer, cx, cy + radius / 6, radius * 6 / 10, true);
        }
        else
        {
            // Normal eyes (small filled circles)
            FillCircle(renderer, cx - eyeOffX, cy - eyeOffY, Math.Max(1, radius / 7), Colour.Black);
            FillCircle(renderer, cx + eyeOffX, cy - eyeOffY, Math.Max(1, radius / 7), Colour.Black);
            // Slight smile
            DrawMouthArc(renderer, cx, cy + radius / 5, radius / 2, true);
        }
    }

    // ── Text helpers ─────────────────────────────────────────────────────────

    /// <summary>Draw small-font text centred at (cx, cy).</summary>
    public void DrawTextSmallCentered(string text, IntPtr renderer, int cx, int cy, Colour colour)
    {
        int width = 0, height = 0;
        foreach (char c in text)
        {
            if (c < 0 || c >= glyphMap.Length || glyphMap[c].w == 0) continue;
            var g = glyphMap[c];
            width += g.w;
            if (g.h > height) height = g.h;
        }
        DrawText(text, renderer, cx - width / 2, cy - height / 2, colour);
    }

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

    public void DrawText(string text, IntPtr renderer, int x, int y, Colour colour)
    {            
        if (SDL_SetTextureColorMod(glyphs, colour.R, colour.G, colour.B)<0) {
            Console.WriteLine($"There was an issue setting the font atlas colour for drawing. {SDL_GetError()}"); 
        }   

        foreach(var c in text.ToCharArray())
        {
            if (c < 0 || c >= glyphMap.Length || glyphMap[c].w == 0) continue;
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

    public void DrawTextCentered(string text, IntPtr renderer, int x, int y, Colour colour)
    {            
        int width = 0;
        int height = 0;
        foreach(var c in text.ToCharArray())
        {
            if (c < 0 || c >= glyphMapLarge.Length || glyphMapLarge[c].w == 0) continue;
            var glyph = glyphMapLarge[c];            
            width += glyph.w;
            height = glyph.h > height ? glyph.h : height;
        }

        x-=width/2;
        y-=height/2;
        DrawTextLarge(text, renderer, x, y, colour);
    }

    public void DrawTextLarge(string text, IntPtr renderer, int x, int y, Colour colour)
    {            
        if (SDL_SetTextureColorMod(glyphsLarge, colour.R, colour.G, colour.B)<0) {
            Console.WriteLine($"There was an issue setting the font atlas colour for drawing. {SDL_GetError()}"); 
        }   

        foreach(var c in text.ToCharArray())
        {
            if (c < 0 || c >= glyphMapLarge.Length || glyphMapLarge[c].w == 0) continue;
            var glyph = glyphMapLarge[c];

            var dest = new SDL_Rect {
                x = x,
                y = y,
                w = glyph.w,
                h = glyph.h
            };

            if (SDL_RenderCopy(renderer, glyphsLarge, ref glyph, ref dest) < 0) {
                Console.WriteLine($"There was an issue rendering a glyph. {SDL_GetError()}");
            }

            x += glyph.w;        
        }
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
            SDL_DestroyTexture(glyphsLarge);
    }

    private void LoadFonts(IntPtr renderer) {

        // Load small font for menus
        glyphMap = new SDL_Rect[128];
        glyphs = LoadFontAtlas(renderer, 14, glyphMap);
        
        // Load large font for grid numbers / centred text
        glyphMapLarge = new SDL_Rect[128];
        glyphsLarge = LoadFontAtlas(renderer, 16, glyphMapLarge);
    }

    private IntPtr LoadFontAtlas(IntPtr renderer, int fontSize, SDL_Rect[] targetGlyphMap) {

        var white = new SDL_Color {
            r = 255,
            g = 255,
            b = 255,
            a = 255
        };
        
        var dest_rect = new SDL_Rect();

        var font = TTF_OpenFont(@"media\roboto-regular.ttf", fontSize);
        if (font == IntPtr.Zero) {
            Console.WriteLine($"TTF_OpenFont failed (size {fontSize}): {TTF_GetError()}");
            return IntPtr.Zero;
        }

        var surfacePtr = SDL_CreateRGBSurface(0, FONT_TEXTURE_SIZE, FONT_TEXTURE_SIZE, 32,
            0x00FF0000, 0x0000FF00, 0x000000FF, 0xFF000000); // ARGB8888 — matches TTF_RenderUTF8_Blended output
        if (surfacePtr == IntPtr.Zero) {
            Console.WriteLine($"SDL_CreateRGBSurface failed: {SDL_GetError()}");
            return IntPtr.Zero;
        }
        // SDL zero-initialises new surfaces; all pixels start as (0,0,0,0) = fully transparent.
        // No SDL_SetColorKey needed — proper per-pixel alpha handles transparency.

        for (var i = ' ' ; i <= 'z' ; i++) {

            var glyph = TTF_RenderUTF8_Blended(font, i.ToString(), white);
            if (glyph == IntPtr.Zero) continue;

            if (TTF_SizeText(font, i.ToString(), out dest_rect.w, out dest_rect.h) < 0) {
                Console.WriteLine($"TTF_SizeText failed: {SDL_GetError()}");
                continue;
            }

            if (dest_rect.x + dest_rect.w >= FONT_TEXTURE_SIZE) {
                dest_rect.x = 0;
                dest_rect.y += dest_rect.h + 1;
            }

            // KEY QUALITY FIX: BLENDMODE_NONE on the glyph surface copies the raw
            // alpha channel into the atlas instead of compositing it into RGB.
            // This preserves the full anti-aliasing gradient.
            SDL_SetSurfaceBlendMode(glyph, SDL_BlendMode.SDL_BLENDMODE_NONE);

            if (SDL_BlitSurface(glyph, IntPtr.Zero, surfacePtr, ref dest_rect) < 0) {
                Console.WriteLine($"SDL_BlitSurface failed: {SDL_GetError()}");
            }

            targetGlyphMap[i] = new SDL_Rect {
                x = dest_rect.x,
                y = dest_rect.y,
                w = dest_rect.w,
                h = dest_rect.h                
            };

            dest_rect.x += dest_rect.w;
        }

        var glyphTexture = SDL_CreateTextureFromSurface(renderer, surfacePtr);
        SDL_SetTextureBlendMode(glyphTexture, SDL_BlendMode.SDL_BLENDMODE_BLEND);
        return glyphTexture;
    }

    // ── Private drawing helpers ───────────────────────────────────────────────

    /// <summary>Scanline-rasterised filled triangle. Colour must be set before calling.</summary>
    private void FillTriangle(IntPtr renderer, int x0, int y0, int x1, int y1, int x2, int y2)
    {
        // Sort vertices by y (bubble sort, 3 elements)
        if (y0 > y1) { (x0, y0, x1, y1) = (x1, y1, x0, y0); }
        if (y0 > y2) { (x0, y0, x2, y2) = (x2, y2, x0, y0); }
        if (y1 > y2) { (x1, y1, x2, y2) = (x2, y2, x1, y1); }

        int totalH = y2 - y0;
        if (totalH == 0) return;

        for (int y = y0; y <= y2; y++)
        {
            bool second   = y > y1 || y1 == y0;
            int  segH     = second ? (y2 - y1) : (y1 - y0);
            if (segH == 0) continue;

            float alpha = (float)(y - y0) / totalH;
            float beta  = second
                ? (float)(y - y1) / segH
                : (float)(y - y0) / segH;

            int ax = x0 + (int)((x2 - x0) * alpha);
            int bx = second
                ? x1 + (int)((x2 - x1) * beta)
                : x0 + (int)((x1 - x0) * beta);

            if (ax > bx) (ax, bx) = (bx, ax);
            SDL_RenderDrawLine(renderer, ax, y, bx, y);
        }
    }

    /// <summary>Draw a smile (isSmile=true) or frown arc.</summary>
    private void DrawMouthArc(IntPtr renderer, int cx, int cy, int r, bool isSmile)
    {
        SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255);
        int sqr = r * r;
        for (int skip = 0; skip < 2; skip++)   // 2 px thick
        {
            int rad = r + skip;
            int radSq = rad * rad;
            for (int px = -rad; px <= rad; px++)
            {
                int sq = radSq - px * px;
                if (sq < 0) continue;
                int arcY = (int)Math.Sqrt(sq);
                if (isSmile)
                    SDL_RenderDrawPoint(renderer, cx + px, cy + arcY);
                else
                    SDL_RenderDrawPoint(renderer, cx + px, cy - arcY + r / 2);
            }
        }
    }
}