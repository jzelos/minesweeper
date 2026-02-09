using static SDL2.SDL;

namespace Zelos.Minesweeper;

public enum MarkerType
{
    None,
    Flag,
    Question
}

public class Grid
{

    private Media media;


    private class Cell
    {
        public bool IsVisible;
        public bool ContainsMine;
        public MarkerType Marker;
        public int SurroundingMines;
        public bool WasClickedMine; // Track which mine was clicked

    }

    private Cell[,]? grid = null;

    private int Rows;
    private int Columns;
    private int mineCount;
    private bool minesPlaced = false;
    
    // Store last drawing dimensions for coordinate conversion
    private int lastDrawX;
    private int lastDrawY;
    private int lastDrawWidth;
    private int lastDrawHeight;
    
    // Hover tracking
    private int? hoveredCellX;
    private int? hoveredCellY;

    public Grid(Media media)
    {
        this.media = media;
    }

    public void Initalize(int cellsX, int cellsY)
    {
        Rows = cellsX;
        Columns = cellsY;
        mineCount = Rows * Columns / 5;
        minesPlaced = false;

        // Create an empty grid (mines will be placed on first click)
        grid = new Cell[Rows, Columns];
        for (int i = 0; i < Rows; i++)
            for (int j = 0; j < Columns; j++)
                grid[i, j] = new Cell();
    }

    public void UncoverWithClickedMine(int clickedX, int clickedY)
    {
        for (int i = 0; i < Rows; i++)
            for (int j = 0; j < Columns; j++)
            {
                grid[i, j].IsVisible = true;
                if (i == clickedX && j == clickedY && grid[i, j].ContainsMine)
                    grid[i, j].WasClickedMine = true;
            }
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
        // Store drawing dimensions for coordinate conversion
        lastDrawX = x;
        lastDrawY = y;
        lastDrawWidth = width;
        lastDrawHeight = height;

        // Draw background (will show through as border)
        media.Rectangle(renderer, x, y, width, height, Colour.Gray);

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
                        // Draw red background for clicked mine, normal for others
                        Colour bgColor = cell.WasClickedMine ? Colour.Red : Colour.CellOpened;
                        media.Rectangle(renderer, cellX, cellY, cellWidth, cellHeight, bgColor);
                        media.Draw(Resources.TEXTURE_MINE, renderer, cellX, cellY, cellWidth, cellHeight);
                    }
                    else
                    {
                        media.Rectangle(renderer, cellX, cellY, cellWidth, cellHeight, Colour.CellOpened);
                        DrawSurroundingMinesCounts(renderer, x, y, i, j, cellWidth, cellHeight, cell.SurroundingMines);
                    }

                }
                else
                {
                    // Check if this cell is hovered
                    bool isHovered = hoveredCellX == i && hoveredCellY == j;
                    Colour cellColor = isHovered ? Colour.CellHover : Colour.CellUnopened;
                    
                    media.Rectangle(renderer, cellX, cellY, cellWidth, cellHeight, cellColor);
                    Draw3DBorder(renderer, cellX, cellY, cellWidth, cellHeight, true);
                    
                    if (cell.Marker == MarkerType.Flag)
                        DrawMarker(renderer, x, y, i, j, cellWidth, cellHeight, "F", Colour.Red);
                    else if (cell.Marker == MarkerType.Question)
                        DrawMarker(renderer, x, y, i, j, cellWidth, cellHeight, "?", Colour.Blue);
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
        
        // Don't allow marking opened cells
        if (cell.IsVisible)
            return;
            
        // Cycle through marker states: None -> Flag -> Question -> None
        cell.Marker = cell.Marker switch
        {
            MarkerType.None => MarkerType.Flag,
            MarkerType.Flag => MarkerType.Question,
            MarkerType.Question => MarkerType.None,
            _ => MarkerType.None
        };
    }

    public void UpdateHover(IntPtr window, int xWindowCoord, int yWindowCoord)
    {
        // Only update hover for valid coordinates within grid bounds
        if (xWindowCoord >= lastDrawX && xWindowCoord < lastDrawX + lastDrawWidth &&
            yWindowCoord >= lastDrawY && yWindowCoord < lastDrawY + lastDrawHeight)
        {
            var (x, y) = GetCellIndexFromCoordinates(window, xWindowCoord, yWindowCoord);
            hoveredCellX = x;
            hoveredCellY = y;
        }
        else
        {
            hoveredCellX = null;
            hoveredCellY = null;
        }
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

        // Place mines on first click, ensuring clicked cell and neighbors are safe
        if (!minesPlaced)
        {
            PlaceMines(x, y);
            minesPlaced = true;
        }

        var cell = grid[x, y];
        
        // Don't open if already visible or flagged (question marks can be opened)
        if (cell.IsVisible || cell.Marker == MarkerType.Flag)
            return true;
            
        cell.IsVisible = true;

        if (cell.ContainsMine)
        {
            cell.WasClickedMine = true;
            return false;
        }

        // Only open surrounding cells if the clicked cell is empty
        if (cell.SurroundingMines == 0)
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

    public int GetTotalMines()
    {
        return mineCount;
    }

    public int GetFlagCount()
    {
        int count = 0;
        if (grid == null) return 0;
        
        for (int i = 0; i < Rows; i++)
            for (int j = 0; j < Columns; j++)
                if (grid[i, j].Marker == MarkerType.Flag)
                    count++;
        return count;
    }

    public bool HasStarted()
    {
        return minesPlaced;
    }

    private void DrawSurroundingMinesCounts(IntPtr renderer, int x, int y, int i, int j, int cellWidth, int cellHeight, int mines)
    {
        if (mines == 0)
            return;

        Colour colour = mines switch
        {
            1 => Colour.Number1,
            2 => Colour.Number2,
            3 => Colour.Number3,
            4 => Colour.Number4,
            5 => Colour.Number5,
            6 => Colour.Number6,
            7 => Colour.Number7,
            8 => Colour.Number8,
            _ => Colour.Black
        };

        var cellX = x + 1 + (i * (cellWidth + 1)) + cellWidth / 2;
        var cellY = y + 1 + (j * (cellHeight + 1)) + cellHeight / 2;
        media.DrawTextCentered(mines.ToString(), renderer, cellX, cellY, colour);
    }

    private void DrawMarker(IntPtr renderer, int x, int y, int i, int j, int cellWidth, int cellHeight, string symbol, Colour color)
    {
        var cellX = x + 1 + (i * (cellWidth + 1)) + cellWidth / 2;
        var cellY = y + 1 + (j * (cellHeight + 1)) + cellHeight / 2;
        media.DrawTextCentered(symbol, renderer, cellX, cellY, color);
    }

    private void Draw3DBorder(IntPtr renderer, int x, int y, int width, int height, bool raised)
    {
        if (raised)
        {
            // Top and left borders (light)
            media.Rectangle(renderer, x, y, width, 2, Colour.BorderLight);
            media.Rectangle(renderer, x, y, 2, height, Colour.BorderLight);
            
            // Bottom and right borders (dark)
            media.Rectangle(renderer, x, y + height - 2, width, 2, Colour.BorderDark);
            media.Rectangle(renderer, x + width - 2, y, 2, height, Colour.BorderDark);
        }
        else
        {
            // Inverted for sunken look
            media.Rectangle(renderer, x, y, width, 1, Colour.BorderDark);
            media.Rectangle(renderer, x, y, 1, height, Colour.BorderDark);
        }
    }

    private (int x, int y) GetCellIndexFromCoordinates(IntPtr window, int xWindowCoord, int yWindowCoord)
    {
        // Use the same calculation as Draw method
        var cellWidth = ((lastDrawWidth - 1) / Rows) - 1;
        var cellHeight = ((lastDrawHeight - 1) / Columns) - 1;

        // Account for grid offset and 1-pixel border
        var relativeX = xWindowCoord - lastDrawX - 1;
        var relativeY = yWindowCoord - lastDrawY - 1;

        // If click is in border or outside grid, clamp to nearest valid position
        if (relativeX < 0) relativeX = 0;
        if (relativeY < 0) relativeY = 0;

        // Calculate cell index accounting for border between cells
        var cellX = relativeX / (cellWidth + 1);
        var cellY = relativeY / (cellHeight + 1);

        // Clamp to valid range
        cellX = Math.Max(0, Math.Min(cellX, Rows - 1));
        cellY = Math.Max(0, Math.Min(cellY, Columns - 1));

        return (cellX, cellY);
    }

    private Cell GetCellFromCoordinates(IntPtr window, int xWindowCoord, int yWindowCoord)
    {
        var (x, y) = GetCellIndexFromCoordinates(window, xWindowCoord, yWindowCoord);
        return grid[x, y];
    }

    private void PlaceMines(int safeCellX, int safeCellY)
    {
        var rand = new Random();
        var mines = mineCount;
        
        // Get bounds of safe zone (clicked cell and its neighbors)
        var (lowerX, lowerY, upperX, upperY) = GetSurroundingCellBounds(safeCellX, safeCellY);

        while (mines > 0)
        {
            var x = rand.Next(Rows);
            var y = rand.Next(Columns);
            var cell = grid[x, y];
            
            // Skip if already has mine or is in safe zone
            if (cell.ContainsMine)
                continue;
                
            if (x >= lowerX && x <= upperX && y >= lowerY && y <= upperY)
                continue;
            
            cell.ContainsMine = true;
            mines--;
        }

        // Calculate surrounding mines for all cells
        for (int i = 0; i < Rows; i++)
            for (int j = 0; j < Columns; j++)
                grid[i, j].SurroundingMines = CountSurroundingMines(i, j);
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
