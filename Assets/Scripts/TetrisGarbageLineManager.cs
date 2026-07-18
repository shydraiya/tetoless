using UnityEngine;

public sealed class TetrisGarbageLineManager
{
    private const int GarbageBlockId = -100;
    private const int GarbageOrder = 0;

    private readonly TetrisBoard board;
    private readonly GameObject blockPrefab;
    private readonly Color blockColor;
    private readonly int linesPerRise;
    private readonly float riseInterval;
    private readonly int rowsPerHoleColumn;

    private float nextRiseTime;

    public TetrisGarbageLineManager(
        TetrisBoard board,
        GameObject blockPrefab,
        Color blockColor,
        int linesPerRise,
        float riseInterval,
        int rowsPerHoleColumn)
    {
        this.board = board;
        this.blockPrefab = blockPrefab;
        this.blockColor = blockColor;
        this.linesPerRise = Mathf.Max(1, linesPerRise);
        this.riseInterval = Mathf.Max(0.1f, riseInterval);
        this.rowsPerHoleColumn = Mathf.Max(1, rowsPerHoleColumn);
        nextRiseTime = Time.time + this.riseInterval;
    }

    public bool AddInitialLines(int lineCount)
    {
        return AddLines(lineCount);
    }

    public bool Tick(out bool addedLines)
    {
        addedLines = false;
        if (Time.time < nextRiseTime)
        {
            return true;
        }

        nextRiseTime = Time.time + riseInterval;
        addedLines = true;
        return AddLines(linesPerRise);
    }

    private bool AddLines(int lineCount)
    {
        int[] holeColumns = CreateHoleColumns(lineCount);
        return board.AddGarbageLines(lineCount, holeColumns, CreateBlock, GarbageBlockId, GarbageOrder);
    }

    private int[] CreateHoleColumns(int lineCount)
    {
        int[] holeColumns = new int[lineCount];
        int currentHole = Random.Range(0, board.Width);

        for (int row = 0; row < lineCount; row++)
        {
            if (row > 0 && row % rowsPerHoleColumn == 0)
            {
                currentHole = GetDifferentHoleColumn(currentHole);
            }

            holeColumns[row] = currentHole;
        }

        return holeColumns;
    }

    private int GetDifferentHoleColumn(int previousHole)
    {
        if (board.Width <= 1)
        {
            return previousHole;
        }

        int nextHole = Random.Range(0, board.Width - 1);
        return nextHole >= previousHole ? nextHole + 1 : nextHole;
    }

    private Transform CreateBlock(Vector2Int position)
    {
        GameObject block = blockPrefab != null
            ? Object.Instantiate(blockPrefab)
            : GameObject.CreatePrimitive(PrimitiveType.Cube);

        block.name = $"Garbage Block {position.x},{position.y}";
        ApplyColor(block);
        return block.transform;
    }

    private void ApplyColor(GameObject block)
    {
        foreach (Renderer renderer in block.GetComponentsInChildren<Renderer>())
        {
            renderer.material.color = blockColor;
        }
    }
}
