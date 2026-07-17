using System.Collections.Generic;
using UnityEngine;

public sealed class TetrisPathRules
{
    private readonly TetrisBoard board;
    private readonly int width;
    private readonly int depth;
    private readonly Vector2Int startPoint;
    private readonly Vector2Int endPoint;
    private readonly HashSet<Vector2Int> dangerZones;

    public TetrisPathRules(
        TetrisBoard board,
        int width,
        int depth,
        Vector2Int startPoint,
        Vector2Int endPoint,
        IEnumerable<Vector2Int> dangerZones)
    {
        this.board = board;
        this.width = width;
        this.depth = depth;
        this.startPoint = ClampToPointArea(startPoint);
        this.endPoint = ClampToPointArea(endPoint);
        this.dangerZones = new HashSet<Vector2Int>();

        foreach (Vector2Int dangerZone in dangerZones)
        {
            this.dangerZones.Add(ClampToBoard(dangerZone));
        }
    }

    public TetrisPathResult Evaluate()
    {
        TetrisPathResult result = new TetrisPathResult();
        bool[,] visited = new bool[width, depth];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<string> edges = new HashSet<string>();

        foreach (Vector2Int startCell in GetConnectedCells(startPoint))
        {
            visited[startCell.x, startCell.y] = true;
            queue.Enqueue(startCell);
            result.ReachableCells.Add(startCell);
            result.Edges.Add(new TetrisPathEdge(startPoint, startCell));
            result.ReachedEndPoint |= IsConnectedToPoint(startCell, endPoint);
        }

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            foreach (Vector2Int next in GetReachableNeighbors(current))
            {
                if (edges.Add(GetEdgeKey(current, next)))
                {
                    result.Edges.Add(new TetrisPathEdge(current, next));
                }

                if (visited[next.x, next.y])
                {
                    continue;
                }

                visited[next.x, next.y] = true;
                queue.Enqueue(next);
                result.ReachableCells.Add(next);

                if (IsConnectedToPoint(next, endPoint))
                {
                    result.ReachedEndPoint = true;
                    result.Edges.Add(new TetrisPathEdge(next, endPoint));
                }
            }
        }

        return result;
    }

    private List<Vector2Int> GetConnectedCells(Vector2Int point)
    {
        List<Vector2Int> cells = new List<Vector2Int>();
        foreach (Vector2Int direction in Directions)
        {
            Vector2Int candidate = point + direction;
            if (!IsDangerZone(candidate) && board.TryGetCell(candidate, out _))
            {
                cells.Add(candidate);
            }
        }

        return cells;
    }

    private List<Vector2Int> GetReachableNeighbors(Vector2Int position)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        if (!board.TryGetCell(position, out TetrisBoardCell current))
        {
            return neighbors;
        }

        foreach (Vector2Int direction in Directions)
        {
            Vector2Int candidatePosition = position + direction;
            if (IsDangerZone(candidatePosition))
            {
                continue;
            }

            if (!board.TryGetCell(candidatePosition, out TetrisBoardCell candidate))
            {
                continue;
            }

            if (current.Order == 4)
            {
                if (candidate.BlockId == current.BlockId && candidate.Order == 3)
                {
                    continue;
                }

                neighbors.Add(candidatePosition);
                continue;
            }

            if (candidate.BlockId == current.BlockId && candidate.Order == current.Order + 1)
            {
                neighbors.Add(candidatePosition);
            }
        }

        return neighbors;
    }

    private bool IsConnectedToPoint(Vector2Int cell, Vector2Int point)
    {
        return Mathf.Abs(cell.x - point.x) + Mathf.Abs(cell.y - point.y) == 1;
    }

    private Vector2Int ClampToPointArea(Vector2Int position)
    {
        return new Vector2Int(
            Mathf.Clamp(position.x, -1, width),
            Mathf.Clamp(position.y, -1, depth));
    }

    private Vector2Int ClampToBoard(Vector2Int position)
    {
        return new Vector2Int(
            Mathf.Clamp(position.x, 0, width - 1),
            Mathf.Clamp(position.y, 0, depth - 1));
    }

    private bool IsDangerZone(Vector2Int position)
    {
        return dangerZones.Contains(position);
    }

    private static string GetEdgeKey(Vector2Int a, Vector2Int b)
    {
        int aKey = a.y * 1000 + a.x;
        int bKey = b.y * 1000 + b.x;
        return aKey < bKey ? $"{aKey}:{bKey}" : $"{bKey}:{aKey}";
    }

    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.left,
        Vector2Int.down
    };
}

public sealed class TetrisPathResult
{
    public readonly List<Vector2Int> ReachableCells = new List<Vector2Int>();
    public readonly List<TetrisPathEdge> Edges = new List<TetrisPathEdge>();
    public bool ReachedEndPoint;
}

public readonly struct TetrisPathEdge
{
    public readonly Vector2Int From;
    public readonly Vector2Int To;

    public TetrisPathEdge(Vector2Int from, Vector2Int to)
    {
        From = from;
        To = to;
    }
}
