using static SDL2.SDL;

namespace Zelos.Minesweeper;

public class Grid {

    private Media media;

    private class Cell {
        public bool IsVisible;
        public bool ContainsMine;    
        public int Number;    
    }

    private Cell[,]? grid = null;

    private const int CellsX = 10;
    private const int CellsY = 10;

    public Grid(Media media) {
        this.media = media;
    }

    public void Initalize() {
        grid = new Cell[CellsX, CellsY];
          for (int i=0;i<CellsX;i++)
            for (int j=0;j<CellsY;j++)
             {
                grid[i,j] = new Cell();                
             }

        var rand = new Random();
        var mines = 10;
        while (mines > 0) {
            var x = rand.Next(CellsX);
            var y = rand.Next(CellsY);
            var cell = grid[x,y]; 
            if (!cell.ContainsMine) {
                cell.ContainsMine = true;
                mines--;
            }
        }
    }

    public void Draw(IntPtr window, IntPtr renderer) {

        SDL_GetWindowSize(window, out int width, out int height);

        // Draw background (will show through as border)
        SDL_SetRenderDrawColor(renderer, 255, 255, 255, 255);        
        var background_rect = new SDL_Rect {
            x = 0,
            y = 0,
            w = width,
            h = height
            };
        SDL_RenderFillRect(renderer, ref background_rect);

        // get cell size allowing for a 1 pixel border
        var cellWidth = ((width - 1) / CellsX) - 1;
        var cellHeight = ((height - 1) / CellsY) - 1;   

         // Draw Cells
        for (int i=0;i<CellsX;i++)
            for (int j=0;j<CellsY;j++)
            {
                var cell = grid[i,j];                
                if (cell.IsVisible) {                    
                    if (cell.ContainsMine) {
                        DrawImage(renderer, i, j, cellWidth, cellHeight, Resources.TEXTURE_MINE);
                    } else {
                        FillCell(renderer, i, j, cellWidth, cellHeight, 188, 188, 188, 255);
                    }
                     
                } else {                    
                    FillCell(renderer, i, j, cellWidth, cellHeight, 100, 100, 100, 255);   
                    // TODO draw number
                }
            }
 
    }

    public bool OpenCell(IntPtr window, nint xWindowCoord, nint yWindowCoord) {
            SDL_GetWindowSize(window, out int width, out int height);

            // Cell size includes border
            var cellWidth = (width / CellsX) - 1;
            var cellHeight = (height / CellsY) - 1;   
            
            var cellX = xWindowCoord / cellWidth;
            var cellY = yWindowCoord / cellHeight;

            grid[cellX, cellY].IsVisible = true;

            if (grid[cellX, cellY].ContainsMine)
                return false;

            CheckSurroundingCells(cellX, cellY);

            return true;
    }

    private void CheckSurroundingCells(nint cellX, nint cellY) {

        nint lowerX = cellX > 0 ? cellX -1 : cellX;
        nint upperX = cellX < (CellsX-1) ? cellX + 1 : cellX;

        nint lowerY = cellY > 0 ? cellY -1 : cellY;
        nint upperY = cellY < (CellsY-1) ? cellY + 1 : cellY;

        for(nint x = lowerX;x<=upperX;x++) {
            for(nint y = lowerY;y<=upperY;y++) {
                if (grid[x, y].ContainsMine || grid[x, y].IsVisible || DoesCellHaveAMineAroundIt(cellX, cellY)) 
                    continue;
                grid[x, y].IsVisible = true;  
                CheckSurroundingCells(x, y);                                                
            }
        }     
    }

    private bool DoesCellHaveAMineAroundIt(nint cellX, nint cellY) {

        nint lowerX = cellX > 0 ? cellX -1 : cellX;
        nint upperX = cellX < (CellsX-1) ? cellX + 1 : cellX;

        nint lowerY = cellY > 0 ? cellY -1 : cellY;
        nint upperY = cellY < (CellsY-1) ? cellY + 1 : cellY;

        for(nint x = lowerX;x<=upperX;x++) {
            for(nint y = lowerY;y<=upperY;y++) {
                if (grid[x, y].ContainsMine)
                    return true;
            }
        }

        return false;      
    }
    
    public bool IsComplete() {
         for (int i=0;i<CellsX;i++)
            for (int j=0;j<CellsY;j++)
            { 
                var cell = grid[i,j]; 
                if (cell.ContainsMine && !cell.IsVisible)
                    return false;
            }
        return true;
    }

    public void UncoverGrid() {       
        for (int i=0;i<CellsX;i++)
            for (int j=0;j<CellsY;j++)
                grid[i,j].IsVisible = true;
    }

    private static void FillCell(IntPtr renderer, int i, int j, int cellWidth, int cellHeight, byte r, byte g, byte b, byte a) {
        var cellX = 1 + (i * (cellWidth+1));
        var cellY = 1 + (j * (cellHeight+1));
        var rect = new SDL_Rect {
            x = cellX,
            y = cellY,
            w = cellWidth,
            h = cellHeight
        };
        SDL_SetRenderDrawColor(renderer, r, g, b, a); 
        SDL_RenderFillRect(renderer, ref rect);
    }

    private void DrawImage(IntPtr renderer, int i, int j, int cellWidth, int cellHeight, string resourceName) {
        var cellX = 1 + (i * (cellWidth+1));
        var cellY = 1 + (j * (cellHeight+1));
        media.Draw(resourceName, renderer, cellX, cellY, cellWidth, cellHeight);
    }
}
