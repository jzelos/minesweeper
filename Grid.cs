using System.Collections;
using static SDL2.SDL;

namespace Zelos.Minesweeper;

public class Grid {

    private Media media;

    private class Cell {
        public bool IsVisible;
        public bool ContainsMine;    
        public int Marker;    
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
                    DrawMarker(renderer, i, j, cellWidth, cellHeight, cell.Marker);
                }
            }
 
    }

    public void MarkCell(IntPtr window, int xWindowCoord, int yWindowCoord) {
            SDL_GetWindowSize(window, out int width, out int height);

            // Cell size includes border
            var cellWidth = (width / CellsX) - 1;
            var cellHeight = (height / CellsY) - 1;   
            
            var cellX = xWindowCoord / cellWidth;
            var cellY = yWindowCoord / cellHeight;

            var cell = grid[cellX, cellY];
            if (cell.Marker == 2)
                cell.Marker = 0;
            else
                cell.Marker++;            
    }

    public bool OpenCell(IntPtr window, int xWindowCoord, int yWindowCoord) {
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

    private void CheckSurroundingCells(int cellX, int cellY) {

        int lowerX = cellX > 0 ? cellX -1 : cellX;
        int upperX = cellX < (CellsX-1) ? cellX + 1 : cellX;

        int lowerY = cellY > 0 ? cellY -1 : cellY;
        int upperY = cellY < (CellsY-1) ? cellY + 1 : cellY;

        for(int x = lowerX;x<=upperX;x++) {
            for(int y = lowerY;y<=upperY;y++) {
                var cell = grid[x, y];
                if (cell.ContainsMine || cell.IsVisible || DoesCellHaveAMineAroundIt(x, y)) 
                    continue;
                cell.IsVisible = true;  
                CheckSurroundingCells(x, y);                                                
            }
        }     
    }

    private bool DoesCellHaveAMineAroundIt(int cellX, int cellY) {

        int lowerX = cellX > 0 ? cellX -1 : cellX;
        int upperX = cellX < (CellsX-1) ? cellX + 1 : cellX;

        int lowerY = cellY > 0 ? cellY -1 : cellY;
        int upperY = cellY < (CellsY-1) ? cellY + 1 : cellY;

        for(int x = lowerX;x<=upperX;x++) {
            for(int y = lowerY;y<=upperY;y++) {
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

     private void DrawMarker(IntPtr renderer, int i, int j, int cellWidth, int cellHeight, int marker) {

        if (marker == 0)
            return;

        var cellX = 1 + (i * (cellWidth+1)) + cellWidth /2;
        var cellY = 1 + (j * (cellHeight+1)) + cellHeight /2;

        switch (marker) {
            case 1:   
                media.DrawTextCentered("?", renderer, cellX, cellY, 0, 0, 0);             
                break;
            case 2:
                media.DrawTextCentered("*", renderer, cellX, cellY, 0, 0, 0);
                break;                                 
        }        
    }
}
