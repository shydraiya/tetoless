using System;
using UnityEngine;

public sealed class TetrisBoard
{
    private readonly int width;
    private readonly int depth;
    private readonly float cellSize;
    private readonly float blockHeight;
    private readonly Transform plane;
    private readonly TetrisBoardCell[,] cells;
    private readonly Transform settledRoot;

    public TetrisBoard(int width, int depth, float cellSize, float blockHeight, Transform plane)
    {
        this.width = width;
        this.depth = depth;
        this.cellSize = cellSize;
        this.blockHeight = blockHeight;
        this.plane = plane;
        cells = new TetrisBoardCell[width, depth];
        settledRoot = new GameObject("Settled Blocks").transform;
    }

    public int Width => width;
    public int Depth => depth;

    public bool IsValid(TetrominoData data, Vector2Int position, int rotation)
    {
        foreach (Vector2Int source in data.Cells)
        {
            Vector2Int cell = position + TetrominoData.RotateCell(source, rotation);
            if (cell.x < 0 || cell.x >= width || cell.y < 0 || cell.y >= depth)
            {
                return false;
            }

            if (cells[cell.x, cell.y].IsOccupied)
            {
                return false;
            }
        }

        return true;
    }

    public bool Lock(TetrominoData data, ActiveTetrisPiece piece)
    {
        Transform[] cubes = TetrominoData.GetCubes(piece.Root.transform);
        if (cubes.Length != data.Cells.Length)
        {
            Debug.LogError($"{piece.Root.name} prefab must contain exactly four rendered block children.", piece.Root);
            return false;
        }

        for (int i = 0; i < data.Cells.Length; i++)
        {
            Vector2Int cell = piece.Position + TetrominoData.RotateCell(data.Cells[i], piece.Rotation);
            Transform cube = cubes[i];
            cube.SetParent(settledRoot, true);
            cube.position = CellToWorld(cell);
            cube.rotation = plane.rotation;
            cube.localScale = Vector3.one * cellSize;
            cells[cell.x, cell.y] = new TetrisBoardCell(cube, piece.LockedBlockId, data.OrderNumbers[i]);
        }

        UnityEngine.Object.Destroy(piece.Root);
        return true;
    }

    public int ClearFullLines(Action<Transform> beforeDestroyBlock = null)
    {
        int cleared = 0;
        int row = 0;
        while (row < depth)
        {
            if (!IsRowFull(row))
            {
                row++;
                continue;
            }

            DeleteRow(row, beforeDestroyBlock);
            MoveLaterRowsBackward(row);
            cleared++;
        }

        return cleared;
    }

    public bool AddGarbageLines(int lineCount, int[] holeColumns, Func<Vector2Int, Transform> createBlock, int blockId, int order)
    {
        lineCount = Mathf.Clamp(lineCount, 0, depth);
        if (lineCount == 0 || createBlock == null || holeColumns == null || holeColumns.Length == 0)
        {
            return true;
        }

        for (int y = depth - lineCount; y < depth; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (cells[x, y].IsOccupied)
                {
                    return false;
                }
            }
        }

        for (int y = depth - lineCount - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                TetrisBoardCell cell = cells[x, y];
                cells[x, y + lineCount] = cell;
                cells[x, y] = default;
                if (cell.IsOccupied)
                {
                    cell.View.position = CellToWorld(new Vector2Int(x, y + lineCount));
                }
            }
        }

        for (int y = 0; y < lineCount; y++)
        {
            int holeColumn = Mathf.Clamp(holeColumns[Mathf.Min(y, holeColumns.Length - 1)], 0, width - 1);
            for (int x = 0; x < width; x++)
            {
                if (x == holeColumn)
                {
                    continue;
                }

                Vector2Int position = new Vector2Int(x, y);
                Transform block = createBlock(position);
                if (block == null)
                {
                    continue;
                }

                block.SetParent(settledRoot, true);
                block.position = CellToWorld(position);
                block.rotation = plane.rotation;
                block.localScale = Vector3.one * cellSize;
                cells[x, y] = new TetrisBoardCell(block, blockId, order);
            }
        }

        return true;
    }

    public Vector3 CellToWorld(Vector2Int cell)
    {
        return GridPointToWorld(cell);
    }

    public Vector3 GridPointToWorld(Vector2Int point)
    {
        float x = (point.x - (width - 1) * 0.5f) * cellSize;
        float z = (point.y - (depth - 1) * 0.5f) * cellSize;
        return plane.position + plane.right * x + plane.up * blockHeight + plane.forward * z;
    }

    private bool IsRowFull(int row)
    {
        for (int x = 0; x < width; x++)
        {
            if (!cells[x, row].IsOccupied)
            {
                return false;
            }
        }

        return true;
    }

    private void DeleteRow(int row, Action<Transform> beforeDestroyBlock)
    {
        for (int x = 0; x < width; x++)
        {
            Transform block = cells[x, row].View;
            beforeDestroyBlock?.Invoke(block);
            UnityEngine.Object.Destroy(block.gameObject);
            cells[x, row] = default;
        }
    }

    private void MoveLaterRowsBackward(int clearedRow)
    {
        for (int z = clearedRow + 1; z < depth; z++)
        {
            for (int x = 0; x < width; x++)
            {
                TetrisBoardCell cell = cells[x, z];
                cells[x, z - 1] = cell;
                cells[x, z] = default;
                if (cell.IsOccupied)
                {
                    cell.View.position = CellToWorld(new Vector2Int(x, z - 1));
                }
            }
        }
    }

    public bool TryGetCell(Vector2Int position, out TetrisBoardCell cell)
    {
        cell = default;
        if (!IsInside(position))
        {
            return false;
        }

        cell = cells[position.x, position.y];
        return cell.IsOccupied;
    }

    public bool IsInside(Vector2Int position)
    {
        return position.x >= 0 && position.x < width && position.y >= 0 && position.y < depth;
    }
}

public readonly struct TetrisBoardCell
{
    public readonly Transform View;
    public readonly int BlockId;
    public readonly int Order;

    public TetrisBoardCell(Transform view, int blockId, int order)
    {
        View = view;
        BlockId = blockId;
        Order = order;
    }

    public bool IsOccupied => View != null;
}
