using static SDL2.SDL;

namespace Zelos.Minesweeper;

public class Grid
{

    private Media media;


    private class Cell
    {
        public bool IsVisible;
        public bool ContainsMine;
        public bool Marker;
        public int SurroundingMines;

    }

    private Cell[,]? grid = null;

    private int Rows;
    private int Columns;

    public Grid(Media media)
    {
        this.media = media;
    }

    public void Initalize(int cellsX, int cellsY)
    {
        Rows = cellsX;
        Columns = cellsY;

        // Create an empty grid
        grid = new Cell[Rows, Columns];
        for (int i = 0; i < Rows; i++)
            for (int j = 0; j < Columns; j++)
                grid[i, j] = new Cell();

        // Set some mines up
        var rand = new Random();
        var mines = Rows * Columns / 5;
        while (mines > 0)
        {
            var x = rand.Next(Rows);
            var y = rand.Next(Columns);
            var cell = grid[x, y];
            if (!cell.ContainsMine)
            {
                cell.ContainsMine = true;
                mines--;
            }
        }

        // Precount the number of mines around each cell
        for (int i = 0; i < Rows; i++)
            for (int j = 0; j < Columns; j++)
                grid[i, j].SurroundingMines = CountSurroundingMines(i, j);
    }

    /// <summary>
    /// Draw the grid with all markers and counters
    /// </summary>
    /// <param name="window"></param>
    /// <param name="renderer"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    public void Draw(
        IntPtr window,
        IntPtr renderer,
        int x,
        int y,
        int width,
        int height)
    {

        // Draw background (will show through as border)
        media.Rectangle(renderer, x, y, width, height, Colour.Black);

        // get cell size allowing for a 1 pixel border
        var cellWidth = ((width - 1) / Rows) - 1;
        var cellHeight = ((height - 1) / Columns) - 1;

        // Draw Cells
        for (int i = 0; i < Rows; i++)
            for (int j = 0; j < Columns; j++)
            {
                var cell = grid[i, j];

                var cellX = x + 1 + (i * (cellWidth + 1));
                var cellY = y + 1 + (j * (cellHeight + 1));

                if (cell.IsVisible)
                {
                    if (cell.ContainsMine)
                    {
                        media.Draw(Resources.TEXTURE_MINE, renderer, cellX, cellY, cellWidth, cellHeight);
                    }
                    else
                    {
                        media.Rectangle(renderer, cellX, cellY, cellWidth, cellHeight, Colour.LightGrey);
                        DrawSurroundingMinesCounts(renderer, x, y, i, j, cellWidth, cellHeight, cell.SurroundingMines);
                    }

                }
                else
                {
                    media.Rectangle(renderer, cellX, cellY, cellWidth, cellHeight, Colour.DarkGrey);
                    if (cell.Marker)
                        DrawMarker(renderer, x, y, i, j, cellWidth, cellHeight);
                }
            }

    }

    /// <summary>
    /// Toggle the marker on a cell using coordinates
    /// </summary>
    /// <param name="window"></param>
    /// <param name="xWindowCoord"></param>
    /// <param name="yWindowCoord"></param>
    public void MarkCell(IntPtr window, int xWindowCoord, int yWindowCoord)
    {
        var cell = GetCellFromCoordinates(window, xWindowCoord, yWindowCoord);
        if (cell.Marker)
            cell.Marker = false;
        else
            cell.Marker = true;
    }

    /// <summary>
    /// Open a cell and its surrounding (as long as its not a mine)
    /// </summary>
    /// <param name="window"></param>
    /// <param name="xWindowCoord"></param>
    /// <param name="yWindowCoord"></param>
    /// <returns></returns>
    public bool OpenCell(IntPtr window, int xWindowCoord, int yWindowCoord)
    {
        var (x, y) = GetCellIndexFromCoordinates(window, xWindowCoord, yWindowCoord);

        var cell = grid[x, y];
        cell.IsVisible = true;

        if (cell.ContainsMine)
            return false;

        OpenSurroundingCells(x, y);

        return true;
    }

    /// <summary>
    /// Returns true when all cells have been opened except those containing a mine
    /// </summary>
    /// <returns></returns>
    public bool IsComplete()
    {
        for (int i = 0; i < Rows; i++)
            for (int j = 0; j < Columns; j++)
            {
                var cell = grid[i, j];
                if (!cell.ContainsMine && !cell.IsVisible)
                    return false;
            }
        return true;
    }

    /// <summary>
    /// Make the whole grid visible
    /// </summary>
    public void Uncover()
    {
        for (int i = 0; i < Rows; i++)
            for (int j = 0; j < Columns; j++)
                grid[i, j].IsVisible = true;
    }

    private void DrawSurroundingMinesCounts(IntPtr renderer, int x, int y, int i, int j, int cellWidth, int cellHeight, int mines)
    {
        if (mines == 0)
            return;

        Colour colour;
        switch (mines)
        {
            case 1:
                colour = Colour.Black;
                break;
            case 2:
                colour = Colour.Blue;
                break;
            case 3:
                colour = Colour.Green;
                break;
            default:
                colour = Colour.Red;
                break;
        }

        var cellX = x + 1 + (i * (cellWidth + 1)) + cellWidth / 2;
        var cellY = y + 1 + (j * (cellHeight + 1)) + cellHeight / 2;
        media.DrawTextCentered(mines.ToString(), renderer, cellX, cellY, colour);
    }

    private void DrawMarker(IntPtr renderer, int x, int y, int i, int j, int cellWidth, int cellHeight)
    {
        var cellX = x + 1 + (i * (cellWidth + 1)) + cellWidth / 2;
        var cellY = y + 1 + (j * (cellHeight + 1)) + cellHeight / 2;
        media.DrawTextCentered("*", renderer, cellX, cellY, Colour.Red);
    }

    private (int x, int y) GetCellIndexFromCoordinates(IntPtr window, int xWindowCoord, int yWindowCoord)
    {
        SDL_GetWindowSize(window, out int width, out int height);

        // Cell size includes border
        var cellWidth = (width / Rows) - 1;
        var cellHeight = (height / Columns) - 1;

        var cellX = xWindowCoord / cellWidth;
        var cellY = yWindowCoord / cellHeight;

        return (cellX, cellY);
    }

    private Cell GetCellFromCoordinates(IntPtr window, int xWindowCoord, int yWindowCoord)
    {
        var (x, y) = GetCellIndexFromCoordinates(window, xWindowCoord, yWindowCoord);
        return grid[x, y];
    }

    private void OpenSurroundingCells(int cellX, int cellY)
    {

        var (lowerX, lowerY, upperX, upperY) = GetSurroundingCellBounds(cellX, cellY);

        for (int x = lowerX; x <= upperX; x++)
        {
            for (int y = lowerY; y <= upperY; y++)
            {
                var cell = grid[x, y];
                if (cell.IsVisible || cell.ContainsMine)
                    continue;
                cell.IsVisible = true;
                if (cell.SurroundingMines == 0)
                    OpenSurroundingCells(x, y);
            }
        }
    }

    private int CountSurroundingMines(int cellX, int cellY)
    {
        var (lowerX, lowerY, upperX, upperY) = GetSurroundingCellBounds(cellX, cellY);

        int count = 0;
        for (int x = lowerX; x <= upperX; x++)
        {
            for (int y = lowerY; y <= upperY; y++)
            {
                if (grid[x, y].ContainsMine)
                    count++;
            }
        }

        return count;
    }

    private (int, int, int, int) GetSurroundingCellBounds(int cellX, int cellY)
    {
        int lowerX = cellX > 0 ? cellX - 1 : cellX;
        int upperX = cellX < (Rows - 1) ? cellX + 1 : cellX;

        int lowerY = cellY > 0 ? cellY - 1 : cellY;
        int upperY = cellY < (Columns - 1) ? cellY + 1 : cellY;

        return (lowerX, lowerY, upperX, upperY);
    }
    
}
