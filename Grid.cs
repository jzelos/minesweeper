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
    private readonly Media media;

    private class Cell
    {
        public bool       IsVisible;
        public bool       ContainsMine;
        public MarkerType Marker;
        public int        SurroundingMines;
        public bool       WasClickedMine;  // mine the player stepped on
        public bool       WasWrongFlag;    // flagged non-mine revealed on loss
    }

    private Cell[,] grid;

    private int  Rows;       // horizontal cell count
    private int  Columns;    // vertical   cell count
    private int  mineCount;
    private bool minesPlaced;
    private bool gameOverLost;

    // Last draw rect (used for coordinate mapping)
    private int lastDrawX, lastDrawY, lastDrawWidth, lastDrawHeight;

    // Hover tracking
    private int? hoveredCellX, hoveredCellY;

    public Grid(Media media)
    {
        this.media = media;
        grid = new Cell[1, 1];
        grid[0, 0] = new Cell();
    }

    /// <summary>
    /// (Re)initialise the grid.  Pass <paramref name="mines"/> = -1 to use the
    /// default 20 % density rule, or a positive value for an exact count.
    /// </summary>
    public void Initalize(int cellsX, int cellsY, int mines = -1)
    {
        Rows      = cellsX;
        Columns   = cellsY;
        mineCount = mines > 0 ? mines : Rows * Columns / 5;
        minesPlaced  = false;
        gameOverLost = false;

        grid = new Cell[Rows, Columns];
        for (int i = 0; i < Rows; i++)
            for (int j = 0; j < Columns; j++)
                grid[i, j] = new Cell();
    }

    /// <summary>Preferred window-content size that gives exactly CELL_SIZE pixels per cell.</summary>
    public (int width, int height) GetPreferredSize()
    {
        int cs = Resources.CELL_SIZE;
        return (Rows * (cs + 1) + 1, Columns * (cs + 1) + 1);
    }

    // ── Public game actions ────────────────────────────────────────

    public bool OpenCell(IntPtr window, int xWin, int yWin)
    {
        var (x, y) = GetCellIndexFromCoordinates(xWin, yWin);

        if (!minesPlaced)
        {
            PlaceMines(x, y);
            minesPlaced = true;
        }

        var cell = grid[x, y];
        if (cell.IsVisible || cell.Marker == MarkerType.Flag)
            return true;

        cell.IsVisible = true;

        if (cell.ContainsMine)
        {
            cell.WasClickedMine = true;
            return false;
        }

        if (cell.SurroundingMines == 0)
            OpenSurroundingCells(x, y);

        return true;
    }

    /// <summary>
    /// Chord open: if the numbered visible cell at (xWin,yWin) has enough
    /// neighbouring flags, open all remaining neighbours.
    /// Returns false if a mine was detonated.
    /// </summary>
    public bool ChordOpen(IntPtr window, int xWin, int yWin)
    {
        var (x, y) = GetCellIndexFromCoordinates(xWin, yWin);
        var cell = grid[x, y];

        if (!cell.IsVisible || cell.ContainsMine || cell.SurroundingMines == 0)
            return true;

        var (lx, ly, ux, uy) = GetSurroundingCellBounds(x, y);

        int flags = 0;
        for (int cx = lx; cx <= ux; cx++)
            for (int cy = ly; cy <= uy; cy++)
                if (grid[cx, cy].Marker == MarkerType.Flag)
                    flags++;

        if (flags != cell.SurroundingMines)
            return true;

        bool safe = true;
        for (int cx = lx; cx <= ux; cx++)
        {
            for (int cy = ly; cy <= uy; cy++)
            {
                var n = grid[cx, cy];
                if (n.IsVisible || n.Marker == MarkerType.Flag) continue;

                n.IsVisible = true;
                if (n.ContainsMine)
                {
                    n.WasClickedMine = true;
                    safe = false;
                }
                else if (n.SurroundingMines == 0)
                {
                    OpenSurroundingCells(cx, cy);
                }
            }
        }
        return safe;
    }

    public void MarkCell(IntPtr window, int xWin, int yWin)
    {
        var cell = GetCellFromCoordinates(xWin, yWin);
        if (cell == null || cell.IsVisible) return;

        cell.Marker = cell.Marker switch
        {
            MarkerType.None     => MarkerType.Flag,
            MarkerType.Flag     => MarkerType.Question,
            MarkerType.Question => MarkerType.None,
            _                   => MarkerType.None
        };
    }

    /// <summary>Reveal all cells on loss, marking incorrectly placed flags.</summary>
    public void UncoverWithClickedMine()
    {
        gameOverLost = true;
        for (int i = 0; i < Rows; i++)
            for (int j = 0; j < Columns; j++)
            {
                var cell = grid[i, j];
                if (cell.Marker == MarkerType.Flag && !cell.ContainsMine)
                    cell.WasWrongFlag = true;
                cell.IsVisible = true;
            }
    }

    /// <summary>Auto-flag remaining unflagged mines on a win.</summary>
    public void FlagRemainingMines()
    {
        for (int i = 0; i < Rows; i++)
            for (int j = 0; j < Columns; j++)
            {
                var cell = grid[i, j];
                if (cell.ContainsMine && !cell.IsVisible)
                    cell.Marker = MarkerType.Flag;
            }
    }

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

    public int  GetTotalMines() => mineCount;
    public bool HasStarted()    => minesPlaced;

    public int GetFlagCount()
    {
        int count = 0;
        for (int i = 0; i < Rows; i++)
            for (int j = 0; j < Columns; j++)
                if (grid[i, j].Marker == MarkerType.Flag)
                    count++;
        return count;
    }

    public void UpdateHover(IntPtr window, int xWin, int yWin)
    {
        if (xWin >= lastDrawX && xWin < lastDrawX + lastDrawWidth &&
            yWin >= lastDrawY && yWin < lastDrawY + lastDrawHeight)
        {
            var (x, y) = GetCellIndexFromCoordinates(xWin, yWin);
            hoveredCellX = x;
            hoveredCellY = y;
        }
        else
        {
            hoveredCellX = null;
            hoveredCellY = null;
        }
    }

    // ── Rendering ───────────────────────────────────────────

    public void Draw(IntPtr window, IntPtr renderer, int x, int y, int width, int height)
    {
        lastDrawX      = x;
        lastDrawY      = y;
        lastDrawWidth  = width;
        lastDrawHeight = height;

        // Background (1-px grid lines show through)
        media.Rectangle(renderer, x, y, width, height, Colour.BorderDark);

        int cellWidth  = ((width  - 1) / Rows)    - 1;
        int cellHeight = ((height - 1) / Columns) - 1;

        for (int i = 0; i < Rows; i++)
        {
            for (int j = 0; j < Columns; j++)
            {
                var cell  = grid[i, j];
                int cellX = x + 1 + i * (cellWidth  + 1);
                int cellY = y + 1 + j * (cellHeight + 1);

                DrawCell(renderer, cell, cellX, cellY, cellWidth, cellHeight, i, j);
            }
        }
    }

    private void DrawCell(IntPtr renderer, Cell cell,
        int cellX, int cellY, int cellW, int cellH,
        int gridI, int gridJ)
    {
        if (!cell.IsVisible)
        {
            bool hovered = hoveredCellX == gridI && hoveredCellY == gridJ;
            media.Rectangle(renderer, cellX, cellY, cellW, cellH,
                hovered ? Colour.CellHover : Colour.CellUnopened);
            media.DrawRaisedBorder(renderer, cellX, cellY, cellW, cellH, 2);

            switch (cell.Marker)
            {
                case MarkerType.Flag:
                    media.DrawFlag(renderer, cellX, cellY, cellW, cellH);
                    break;
                case MarkerType.Question:
                    media.DrawTextCentered("?", renderer,
                        cellX + cellW / 2, cellY + cellH / 2, Colour.Number4);
                    break;
            }
        }
        else
        {
            // Wrong flag on loss (flagged but not a mine)
            if (cell.WasWrongFlag)
            {
                media.Rectangle(renderer, cellX, cellY, cellW, cellH, Colour.CellOpened);
                media.DrawSunkenBorder(renderer, cellX, cellY, cellW, cellH, 1);
                media.DrawFlag(renderer, cellX, cellY, cellW, cellH);
                media.DrawX(renderer, cellX, cellY, cellW, cellH, Colour.Red);
                return;
            }

            // Correctly flagged mine on loss: keep the flag shown
            if (gameOverLost && cell.ContainsMine && cell.Marker == MarkerType.Flag)
            {
                media.Rectangle(renderer, cellX, cellY, cellW, cellH, Colour.CellUnopened);
                media.DrawRaisedBorder(renderer, cellX, cellY, cellW, cellH, 2);
                media.DrawFlag(renderer, cellX, cellY, cellW, cellH);
                return;
            }

            if (cell.ContainsMine)
            {
                Colour bg = cell.WasClickedMine ? Colour.Red : Colour.CellOpened;
                media.Rectangle(renderer, cellX, cellY, cellW, cellH, bg);
                media.DrawSunkenBorder(renderer, cellX, cellY, cellW, cellH, 1);
                media.Draw(Resources.TEXTURE_MINE, renderer,
                    cellX + 2, cellY + 2, cellW - 4, cellH - 4);
            }
            else
            {
                media.Rectangle(renderer, cellX, cellY, cellW, cellH, Colour.CellOpened);
                media.DrawSunkenBorder(renderer, cellX, cellY, cellW, cellH, 1);

                if (cell.SurroundingMines > 0)
                {
                    Colour col = cell.SurroundingMines switch
                    {
                        1 => Colour.Number1,
                        2 => Colour.Number2,
                        3 => Colour.Number3,
                        4 => Colour.Number4,
                        5 => Colour.Number5,
                        6 => Colour.Number6,
                        7 => Colour.Number7,
                        _ => Colour.Number8
                    };
                    media.DrawTextCentered(cell.SurroundingMines.ToString(), renderer,
                        cellX + cellW / 2, cellY + cellH / 2, col);
                }
            }
        }
    }

    // ── Private helpers ─────────────────────────────────────────

    private (int x, int y) GetCellIndexFromCoordinates(int xWin, int yWin)
    {
        int cellWidth  = lastDrawWidth  <= 0 ? 1 : ((lastDrawWidth  - 1) / Rows)    - 1;
        int cellHeight = lastDrawHeight <= 0 ? 1 : ((lastDrawHeight - 1) / Columns) - 1;
        if (cellWidth  < 1) cellWidth  = 1;
        if (cellHeight < 1) cellHeight = 1;

        int relX = Math.Max(0, xWin - lastDrawX - 1);
        int relY = Math.Max(0, yWin - lastDrawY - 1);

        int cellX = Math.Max(0, Math.Min(relX / (cellWidth  + 1), Rows    - 1));
        int cellY = Math.Max(0, Math.Min(relY / (cellHeight + 1), Columns - 1));

        return (cellX, cellY);
    }

    private Cell? GetCellFromCoordinates(int xWin, int yWin)
    {
        var (x, y) = GetCellIndexFromCoordinates(xWin, yWin);
        if (x < 0 || x >= Rows || y < 0 || y >= Columns) return null;
        return grid[x, y];
    }

    private void PlaceMines(int safeCellX, int safeCellY)
    {
        var rand  = new Random();
        int mines = mineCount;
        var (lx, ly, ux, uy) = GetSurroundingCellBounds(safeCellX, safeCellY);

        while (mines > 0)
        {
            int x = rand.Next(Rows);
            int y = rand.Next(Columns);
            var cell = grid[x, y];

            if (cell.ContainsMine) continue;
            if (x >= lx && x <= ux && y >= ly && y <= uy) continue;

            cell.ContainsMine = true;
            mines--;
        }

        for (int i = 0; i < Rows; i++)
            for (int j = 0; j < Columns; j++)
                grid[i, j].SurroundingMines = CountSurroundingMines(i, j);
    }

    private void OpenSurroundingCells(int cellX, int cellY)
    {
        var (lx, ly, ux, uy) = GetSurroundingCellBounds(cellX, cellY);
        for (int x = lx; x <= ux; x++)
        {
            for (int y = ly; y <= uy; y++)
            {
                var cell = grid[x, y];
                if (cell.IsVisible || cell.ContainsMine) continue;
                cell.IsVisible = true;
                if (cell.SurroundingMines == 0)
                    OpenSurroundingCells(x, y);
            }
        }
    }

    private int CountSurroundingMines(int cellX, int cellY)
    {
        var (lx, ly, ux, uy) = GetSurroundingCellBounds(cellX, cellY);
        int count = 0;
        for (int x = lx; x <= ux; x++)
            for (int y = ly; y <= uy; y++)
                if (grid[x, y].ContainsMine) count++;
        return count;
    }

    private (int, int, int, int) GetSurroundingCellBounds(int cellX, int cellY)
    {
        return (
            cellX > 0           ? cellX - 1 : cellX,
            cellY > 0           ? cellY - 1 : cellY,
            cellX < Rows    - 1 ? cellX + 1 : cellX,
            cellY < Columns - 1 ? cellY + 1 : cellY
        );
    }
}

