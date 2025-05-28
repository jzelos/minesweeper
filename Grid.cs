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

    public void Draw(
        IntPtr window, 
        IntPtr renderer,
        int x,
        int y, 
        int width, 
        int height) {

        // Draw background (will show through as border)
        media.Rectangle(renderer, x, y, width, height, Colour.Black);

        // get cell size allowing for a 1 pixel border
        var cellWidth = ((width - 1) / CellsX) - 1;
        var cellHeight = ((height - 1) / CellsY) - 1;   

         // Draw Cells
        for (int i=0;i<CellsX;i++)
            for (int j=0;j<CellsY;j++)
            {
                var cell = grid[i,j];   
                
                var cellX = x + 1 + (i * (cellWidth+1));
                var cellY = y + 1 + (j * (cellHeight+1));

                if (cell.IsVisible) {                    
                    if (cell.ContainsMine) {                        
                        media.Draw(Resources.TEXTURE_MINE, renderer, cellX, cellY, cellWidth, cellHeight);
                    } else {
                        media.Rectangle(renderer, cellX, cellY, cellWidth, cellHeight, Colour.LightGrey);                        
                    }
                     
                } else {                    
                    media.Rectangle(renderer, cellX, cellY, cellWidth, cellHeight, Colour.DarkGrey);                     
                    DrawMarker(renderer, x, y, i, j, cellWidth, cellHeight, cell.Marker);
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

    private void DrawMarker(IntPtr renderer, int x, int y, int i, int j, int cellWidth, int cellHeight, int marker) {

        if (marker == 0)
            return;

        var cellX = x+1 + (i * (cellWidth+1)) + cellWidth /2;
        var cellY = y+1 + (j * (cellHeight+1)) + cellHeight /2;

        switch (marker) {
            case 1:   
                media.DrawTextCentered("?", renderer, cellX, cellY, Colour.Black);             
                break;
            case 2:
                media.DrawTextCentered("*", renderer, cellX, cellY, Colour.Black);
                break;                                 
        }        
    }
}
