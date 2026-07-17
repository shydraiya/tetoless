using System;
using UnityEngine;

public sealed class TetrisBoard
{
    private readonly int width;
    private readonly int depth;
    private readonly float cellSize;
    private readonly float blockHeight;
    private readonly Transform plane;
    private readonly Transform[,] cells;
    private readonly Transform settledRoot;

    public TetrisBoard(int width, int depth, float cellSize, float blockHeight, Transform plane)
    {
        this.width = width;
        this.depth = depth;
        this.cellSize = cellSize;
        this.blockHeight = blockHeight;
        this.plane = plane;
        cells = new Transform[width, depth];
        settledRoot = new GameObject("Settled Blocks").transform;
    }

    public bool IsValid(TetrominoData data, Vector2Int position, int rotation)
    {
        foreach (Vector2Int source in data.Cells)
        {
            Vector2Int cell = position + TetrominoData.RotateCell(source, rotation);
            if (cell.x < 0 || cell.x >= width || cell.y < 0 || cell.y >= depth)
            {
                return false;
            }

            if (cells[cell.x, cell.y] != null)
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
            cells[cell.x, cell.y] = cube;
        }

        UnityEngine.Object.Destroy(piece.Root);
        return true;
    }

    public int ClearFullLines(Action<Transform> beforeDestroyBlock = null)
    {
        int cleared = 0;
        int row = depth - 1;
        while (row >= 0)
        {
            if (!IsRowFull(row))
            {
                row--;
                continue;
            }

            DeleteRow(row, beforeDestroyBlock);
            MoveEarlierRowsForward(row);
            cleared++;
        }

        return cleared;
    }

    public Vector3 CellToWorld(Vector2Int cell)
    {
        float x = (cell.x - (width - 1) * 0.5f) * cellSize;
        float z = (cell.y - (depth - 1) * 0.5f) * cellSize;
        return plane.position + plane.right * x + plane.up * blockHeight + plane.forward * z;
    }

    private bool IsRowFull(int row)
    {
        for (int x = 0; x < width; x++)
        {
            if (cells[x, row] == null)
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
            Transform block = cells[x, row];
            beforeDestroyBlock?.Invoke(block);
            UnityEngine.Object.Destroy(block.gameObject);
            cells[x, row] = null;
        }
    }

    private void MoveEarlierRowsForward(int clearedRow)
    {
        for (int z = clearedRow - 1; z >= 0; z--)
        {
            for (int x = 0; x < width; x++)
            {
                Transform cube = cells[x, z];
                cells[x, z + 1] = cube;
                cells[x, z] = null;
                if (cube != null)
                {
                    cube.position = CellToWorld(new Vector2Int(x, z + 1));
                }
            }
        }
    }
}
