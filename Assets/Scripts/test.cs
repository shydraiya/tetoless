using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class test : MonoBehaviour
{
    [Serializable]
    public struct BlockData
    {
        public string name;
        public Color color;
        public int[] orderNumbers;
        public Vector2Int[] cells;

        public BlockData(string name, Color color, int[] orderNumbers, params Vector2Int[] cells)
        {
            this.name = name;
            this.color = color;
            this.orderNumbers = orderNumbers;
            this.cells = cells;
        }
    }

    private struct ActiveBlock
    {
        public int dataIndex;
        public Vector2Int position;
        public int rotation;
        public GameObject[] views;
    }

    private class BoardCell
    {
        public GameObject view;
        public int blockId;
        public int order;
        public Vector2Int exitDirection;
    }

    [Header("Board")]
    [SerializeField] private int width = 10;
    [SerializeField] private int height = 20;
    [SerializeField, Min(0.05f)] private float fallInterval = 0.7f;
    [SerializeField, Min(0.01f)] private float fastFallInterval = 0.05f;

    [Header("Rules")]
    [SerializeField] private bool enableTetrisCondition = true;

    [Header("Path")]
    [SerializeField, Range(0.05f, 1f)] private float pathLineThickness = 0.3f;
    [SerializeField] private Color pathLineColor = Color.white;
    [SerializeField, Tooltip("-1 keeps the clear target at the top center.")]
    private int clearTargetX = -1;

    [Header("Danger Zones")]
    [SerializeField]
    private Vector2Int[] dangerZonePositions =
    {
        new Vector2Int(4, 10),
        new Vector2Int(5, 10),
        new Vector2Int(6, 10)
    };
    [SerializeField] private Color dangerZoneColor = new Color(1f, 0f, 0f, 0.45f);

    [Header("Debug")]
    [SerializeField] private bool showBlockOrderNumbers = true;

    [Header("Controls")]
    [SerializeField] private Key clockwiseRotationKey = Key.X;
    [SerializeField] private Key counterClockwiseRotationKey = Key.Z;

    private BlockData[] blockData;
    private BoardCell[,] board;
    private ActiveBlock activeBlock;
    private Transform boardRoot;
    private Sprite blockSprite;
    private float nextFallTime;
    private int score;
    private int clearedLines;
    private bool gameOver;
    private int[] blockBag;
    private int blockBagIndex;
    private int nextLockedBlockId = 1;
    private bool gameClear;
    private Transform pathRoot;
    private GameObject clearTargetView;
    private GameObject[] dangerZoneViews;
    private Vector2Int startPosition;

    private void Start()
    {
        CreateBlockData();
        CreateBlockSprite();
        SetupBoard();
        SetupPlayerStart();
        UpdateClearTargetView();
        UpdateDangerZoneViews();
        SetupCamera();
        RefreshPathLines();
        SpawnBlock();
    }

    private void Update()
    {
        if (gameOver || gameClear)
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                RestartGame();
            }

            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
        {
            TryMove(Vector2Int.left);
        }

        if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
        {
            TryMove(Vector2Int.right);
        }

        if (keyboard[clockwiseRotationKey].wasPressedThisFrame)
        {
            TryRotate(1);
        }

        if (keyboard[counterClockwiseRotationKey].wasPressedThisFrame)
        {
            TryRotate(-1);
        }

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            HardDrop();
            return;
        }

        float interval = keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed
            ? fastFallInterval
            : fallInterval;

        if (Time.time >= nextFallTime)
        {
            if (!TryMove(Vector2Int.down))
            {
                LockBlock();
            }

            nextFallTime = Time.time + interval;
        }
    }

    private void CreateBlockData()
    {
        blockData = new[]
        {
            new BlockData("I", Color.cyan, new[] { 1, 2, 3, 4 },
                new Vector2Int(-1, 0), new Vector2Int(0, 0),
                new Vector2Int(1, 0), new Vector2Int(2, 0)),
            new BlockData("O", Color.yellow, new[] { 2, 3, 4, 3 },
                new Vector2Int(0, 1), new Vector2Int(1, 1),
                new Vector2Int(1, 0), new Vector2Int(0, 0)),
            new BlockData("T", new Color(0.65f, 0.2f, 0.9f), new[] { 2, 3, 2, 4 },
                new Vector2Int(-1, 0), new Vector2Int(0, 0),
                new Vector2Int(1, 0), new Vector2Int(0, 1)),
            new BlockData("S", Color.green, new[] { 3, 4, 1, 2 },
                new Vector2Int(0, 1), new Vector2Int(1, 1),
                new Vector2Int(-1, 0), new Vector2Int(0, 0)),
            new BlockData("Z", Color.red, new[] { 1, 2, 3, 4 },
                new Vector2Int(-1, 1), new Vector2Int(0, 1),
                new Vector2Int(0, 0), new Vector2Int(1, 0)),
            new BlockData("J", new Color(1f, 0.25f, 0.65f), new[] { 1, 2, 3, 4 },
                new Vector2Int(-1, 1), new Vector2Int(-1, 0),
                new Vector2Int(0, 0), new Vector2Int(1, 0)),
            new BlockData("L", new Color(1f, 0.5f, 0f), new[] { 1, 2, 3, 4 },
                new Vector2Int(1, 1), new Vector2Int(1, 0),
                new Vector2Int(0, 0), new Vector2Int(-1, 0))
        };
    }

    private void SetupBoard()
    {
        board = new BoardCell[width, height];
        boardRoot = new GameObject("Tetris Board").transform;
        boardRoot.SetParent(transform, false);

        for (int y = -1; y <= height; y++)
        {
            CreateBorderBlock(-1, y);
            CreateBorderBlock(width, y);
        }

        for (int x = 0; x < width; x++)
        {
            CreateBorderBlock(x, -1);
        }
    }

    private void SetupCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        mainCamera.orthographic = true;
        mainCamera.orthographicSize = 12f;
        mainCamera.transform.position = new Vector3((width - 1) * 0.5f, (height - 1) * 0.5f, -10f);
        mainCamera.transform.rotation = Quaternion.identity;
        mainCamera.backgroundColor = new Color(0.04f, 0.04f, 0.08f);
    }

    private void CreateBlockSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.name = "Runtime Block Texture";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        blockSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
    }

    private void CreateBorderBlock(int x, int y)
    {
        GameObject border = CreateView(new Color(0.2f, 0.2f, 0.25f), "Border");
        border.transform.SetParent(boardRoot, false);
        border.transform.localPosition = new Vector3(x, y, 0f);
    }

    private GameObject CreateView(Color color, string objectName)
    {
        GameObject view = new GameObject(objectName);
        SpriteRenderer renderer = view.AddComponent<SpriteRenderer>();
        renderer.sprite = blockSprite;
        renderer.color = color;
        renderer.sortingOrder = 1;
        view.transform.localScale = Vector3.one * 0.92f;
        return view;
    }

    private void SetupPlayerStart()
    {
        startPosition = new Vector2Int(width / 2, 0);

        GameObject startBlock = CreateView(new Color(0.55f, 0.55f, 0.6f), "Player Start Block");
        startBlock.transform.SetParent(boardRoot, false);
        startBlock.transform.localPosition = new Vector3(startPosition.x, startPosition.y, 0f);
        AddOrderLabel(startBlock, 1);

        board[startPosition.x, startPosition.y] = new BoardCell
        {
            view = startBlock,
            blockId = 0,
            order = 1,
            exitDirection = Vector2Int.up
        };
    }

    private void UpdateClearTargetView()
    {
        Vector2Int clearTarget = GetClearTargetPosition();
        if (clearTargetView == null)
        {
            clearTargetView = CreateView(new Color(0f, 1f, 0f, 0.45f), "Clear Target");
            clearTargetView.transform.SetParent(boardRoot, false);
            clearTargetView.transform.localScale = Vector3.one * 0.98f;

            SpriteRenderer renderer = clearTargetView.GetComponent<SpriteRenderer>();
            renderer.sortingOrder = 2;
        }

        clearTargetView.transform.localPosition = new Vector3(clearTarget.x, clearTarget.y, -0.4f);
    }

    private void UpdateDangerZoneViews()
    {
        if (dangerZoneViews != null)
        {
            for (int i = 0; i < dangerZoneViews.Length; i++)
            {
                if (dangerZoneViews[i] != null)
                {
                    Destroy(dangerZoneViews[i]);
                }
            }
        }

        dangerZoneViews = new GameObject[dangerZonePositions.Length];
        for (int i = 0; i < dangerZonePositions.Length; i++)
        {
            Vector2Int dangerZone = ClampToBoard(dangerZonePositions[i]);
            dangerZonePositions[i] = dangerZone;

            GameObject view = CreateView(dangerZoneColor, "Danger Zone");
            view.transform.SetParent(boardRoot, false);
            view.transform.localScale = Vector3.one * 0.98f;
            view.transform.localPosition = new Vector3(dangerZone.x, dangerZone.y, -0.45f);

            SpriteRenderer renderer = view.GetComponent<SpriteRenderer>();
            renderer.sortingOrder = 2;
            dangerZoneViews[i] = view;
        }
    }

    private void SpawnBlock()
    {
        int index = DrawBlockFromBag();
        ActiveBlock next = new ActiveBlock
        {
            dataIndex = index,
            position = new Vector2Int(width / 2 - 1, height - 2),
            rotation = 0,
            views = new GameObject[4]
        };

        for (int i = 0; i < next.views.Length; i++)
        {
            next.views[i] = CreateView(blockData[index].color, blockData[index].name);
            next.views[i].transform.SetParent(boardRoot, false);
            AddOrderLabel(next.views[i], blockData[index].orderNumbers[i]);
        }

        activeBlock = next;
        UpdateActiveViews();
        nextFallTime = Time.time + fallInterval;

        if (!IsValid(activeBlock.position, activeBlock.rotation))
        {
            gameOver = true;
            return;
        }

    }

    private int DrawBlockFromBag()
    {
        if (blockBag == null || blockBagIndex >= blockBag.Length)
        {
            FillAndShuffleBlockBag();
        }

        return blockBag[blockBagIndex++];
    }

    private void FillAndShuffleBlockBag()
    {
        blockBag = new int[blockData.Length];
        for (int i = 0; i < blockBag.Length; i++)
        {
            blockBag[i] = i;
        }

        // Fisher-Yates 셔플: 한 세트 안에서 7종 블록이 정확히 한 번씩 나온다.
        for (int i = blockBag.Length - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            (blockBag[i], blockBag[randomIndex]) = (blockBag[randomIndex], blockBag[i]);
        }

        blockBagIndex = 0;
    }

    private bool TryMove(Vector2Int direction)
    {
        Vector2Int target = activeBlock.position + direction;
        if (!IsValid(target, activeBlock.rotation))
        {
            return false;
        }

        activeBlock.position = target;
        UpdateActiveViews();
        return true;
    }

    private void TryRotate(int direction)
    {
        int targetRotation = (activeBlock.rotation + direction + 4) % 4;
        Vector2Int[] wallKicks = direction > 0
            ? new[]
            {
                Vector2Int.zero,
                Vector2Int.left,
                Vector2Int.right,
                Vector2Int.up,
                new Vector2Int(-1, 1),
                new Vector2Int(1, 1),
                Vector2Int.left * 2,
                Vector2Int.right * 2
            }
            : new[]
            {
                Vector2Int.zero,
                Vector2Int.right,
                Vector2Int.left,
                Vector2Int.up,
                new Vector2Int(1, 1),
                new Vector2Int(-1, 1),
                Vector2Int.right * 2,
                Vector2Int.left * 2
            };

        foreach (Vector2Int kick in wallKicks)
        {
            Vector2Int target = activeBlock.position + kick;
            if (!IsValid(target, targetRotation))
            {
                continue;
            }

            activeBlock.position = target;
            activeBlock.rotation = targetRotation;
            UpdateActiveViews();
            return;
        }
    }

    private void HardDrop()
    {
        int distance = 0;
        while (TryMove(Vector2Int.down))
        {
            distance++;
        }

        score += distance * 2;
        LockBlock();
    }

    private bool IsValid(Vector2Int position, int rotation)
    {
        Vector2Int[] cells = blockData[activeBlock.dataIndex].cells;
        for (int i = 0; i < cells.Length; i++)
        {
            Vector2Int cell = position + RotateCell(cells[i], rotation);
            if (cell.x < 0 || cell.x >= width || cell.y < 0 || cell.y >= height)
            {
                return false;
            }

            if (board[cell.x, cell.y] != null)
            {
                return false;
            }
        }

        return true;
    }

    private void LockBlock()
    {
        Vector2Int[] cells = blockData[activeBlock.dataIndex].cells;
        Vector2Int exitDirection = GetExitDirection(cells, activeBlock.position, activeBlock.rotation);
        int blockId = nextLockedBlockId++;

        for (int i = 0; i < cells.Length; i++)
        {
            Vector2Int cell = activeBlock.position + RotateCell(cells[i], activeBlock.rotation);
            board[cell.x, cell.y] = new BoardCell
            {
                view = activeBlock.views[i],
                blockId = blockId,
                order = blockData[activeBlock.dataIndex].orderNumbers[i],
                exitDirection = exitDirection
            };
            activeBlock.views[i].name = "Locked Block";
        }

        int lineCount = enableTetrisCondition ? ClearCompletedLines() : 0;
        score += GetLineScore(lineCount);
        clearedLines += lineCount;
        RefreshPathLines();

        if (!gameOver && !gameClear)
        {
            SpawnBlock();
        }
    }

    private int ClearCompletedLines()
    {
        int count = 0;
        int y = 0;

        while (y < height)
        {
            // 블록이 사라지는 조건: 이 줄의 모든 칸이 채워져 있어야 한다.
            if (!IsLineFull(y))
            {
                y++;
                continue;
            }

            DeleteLine(y);
            MoveLinesDown(y + 1);
            count++;
        }

        return count;
    }

    private bool IsLineFull(int y)
    {
        for (int x = 0; x < width; x++)
        {
            if (board[x, y] == null)
            {
                return false;
            }
        }

        return true;
    }

    private void DeleteLine(int y)
    {
        for (int x = 0; x < width; x++)
        {
            Destroy(board[x, y].view);
            board[x, y] = null;
        }

    }

    private void MoveLinesDown(int startY)
    {
        for (int y = startY; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                BoardCell block = board[x, y];
                if (block == null)
                {
                    continue;
                }

                board[x, y - 1] = block;
                board[x, y] = null;
                block.view.transform.localPosition += Vector3.down;

            }
        }
    }

    private void RefreshPathLines()
    {
        if (pathRoot != null)
        {
            Destroy(pathRoot.gameObject);
        }

        pathRoot = new GameObject("Reachable Path Lines").transform;
        pathRoot.SetParent(boardRoot, false);
        gameClear = false;

        if (!IsInsideBoard(startPosition) || board[startPosition.x, startPosition.y] == null)
        {
            return;
        }

        bool[,] visited = new bool[width, height];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<string> drawnEdges = new HashSet<string>();

        visited[startPosition.x, startPosition.y] = true;
        queue.Enqueue(startPosition);

        Vector2Int clearTarget = GetClearTargetPosition();
        if (startPosition == clearTarget)
        {
            gameClear = true;
        }

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            List<Vector2Int> nextPositions = GetReachableNeighbors(current);

            for (int i = 0; i < nextPositions.Count; i++)
            {
                Vector2Int next = nextPositions[i];
                string edgeKey = GetEdgeKey(current, next);
                if (drawnEdges.Add(edgeKey))
                {
                    CreatePathLine(current, next);
                }

                if (IsDangerZone(next))
                {
                    gameOver = true;
                }

                if (visited[next.x, next.y])
                {
                    continue;
                }

                visited[next.x, next.y] = true;
                queue.Enqueue(next);

                if (next == clearTarget)
                {
                    gameClear = true;
                }
            }
        }
    }

    private List<Vector2Int> GetReachableNeighbors(Vector2Int position)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        if (!IsInsideBoard(position))
        {
            return neighbors;
        }

        BoardCell current = board[position.x, position.y];
        if (current == null)
        {
            return neighbors;
        }

        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.right,
            Vector2Int.left,
            Vector2Int.down
        };

        for (int i = 0; i < directions.Length; i++)
        {
            Vector2Int candidatePosition = position + directions[i];
            if (!IsInsideBoard(candidatePosition))
            {
                continue;
            }

            BoardCell candidate = board[candidatePosition.x, candidatePosition.y];
            if (candidate == null)
            {
                continue;
            }

            if (current.blockId == 0)
            {
                neighbors.Add(candidatePosition);
                continue;
            }

            if (current.order == 4)
            {
                if (candidate.blockId == current.blockId && candidate.order == 3)
                {
                    continue;
                }

                neighbors.Add(candidatePosition);
                continue;
            }

            if (candidate.blockId == current.blockId && candidate.order == current.order + 1)
            {
                neighbors.Add(candidatePosition);
            }
        }

        return neighbors;
    }

    private void CreatePathLine(Vector2Int from, Vector2Int to)
    {
        Vector3 start = new Vector3(from.x, from.y, -0.6f);
        Vector3 end = new Vector3(to.x, to.y, -0.6f);
        Vector3 delta = end - start;

        GameObject line = new GameObject("Path Line");
        line.transform.SetParent(pathRoot, false);
        line.transform.localPosition = (start + end) * 0.5f;
        line.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        line.transform.localScale = new Vector3(delta.magnitude, pathLineThickness, 1f);

        SpriteRenderer renderer = line.AddComponent<SpriteRenderer>();
        renderer.sprite = blockSprite;
        renderer.color = pathLineColor;
        renderer.sortingOrder = 3;
    }

    private Vector2Int GetClearTargetPosition()
    {
        int targetX = clearTargetX < 0 ? width / 2 : Mathf.Clamp(clearTargetX, 0, width - 1);
        return new Vector2Int(targetX, height - 3);
    }

    private bool IsDangerZone(Vector2Int position)
    {
        for (int i = 0; i < dangerZonePositions.Length; i++)
        {
            if (ClampToBoard(dangerZonePositions[i]) == position)
            {
                return true;
            }
        }

        return false;
    }

    private Vector2Int ClampToBoard(Vector2Int position)
    {
        return new Vector2Int(
            Mathf.Clamp(position.x, 0, width - 1),
            Mathf.Clamp(position.y, 0, height - 1));
    }

    private static string GetEdgeKey(Vector2Int a, Vector2Int b)
    {
        int aKey = a.y * 1000 + a.x;
        int bKey = b.y * 1000 + b.x;
        return aKey < bKey ? $"{aKey}:{bKey}" : $"{bKey}:{aKey}";
    }

    private bool IsInsideBoard(Vector2Int position)
    {
        return position.x >= 0 && position.x < width && position.y >= 0 && position.y < height;
    }

    private Vector2Int GetExitDirection(Vector2Int[] cells, Vector2Int position, int rotation)
    {
        int[] orderNumbers = blockData[activeBlock.dataIndex].orderNumbers;
        int thirdIndex = Array.IndexOf(orderNumbers, 3);
        int fourthIndex = Array.IndexOf(orderNumbers, 4);

        if (thirdIndex < 0 || fourthIndex < 0)
        {
            return Vector2Int.up;
        }

        Vector2Int third = position + RotateCell(cells[thirdIndex], rotation);
        Vector2Int fourth = position + RotateCell(cells[fourthIndex], rotation);
        Vector2Int direction = fourth - third;
        return new Vector2Int(Math.Sign(direction.x), Math.Sign(direction.y));
    }

    private void UpdateActiveViews()
    {
        Vector2Int[] cells = blockData[activeBlock.dataIndex].cells;
        for (int i = 0; i < cells.Length; i++)
        {
            Vector2Int cell = activeBlock.position + RotateCell(cells[i], activeBlock.rotation);
            activeBlock.views[i].transform.localPosition = new Vector3(cell.x, cell.y, 0f);
        }
    }

    private void AddOrderLabel(GameObject parent, int order)
    {
        if (!showBlockOrderNumbers)
        {
            return;
        }

        GameObject label = new GameObject($"Order {order}");
        label.transform.SetParent(parent.transform, false);
        label.transform.localPosition = new Vector3(0f, 0f, -0.2f);

        TextMesh text = label.AddComponent<TextMesh>();
        text.text = order.ToString();
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.characterSize = 0.35f;
        text.fontSize = 48;
        text.color = Color.black;

        MeshRenderer renderer = label.GetComponent<MeshRenderer>();
        renderer.sortingOrder = 4;
    }

    private static Vector2Int RotateCell(Vector2Int cell, int rotation)
    {
        return rotation switch
        {
            1 => new Vector2Int(cell.y, -cell.x),
            2 => new Vector2Int(-cell.x, -cell.y),
            3 => new Vector2Int(-cell.y, cell.x),
            _ => cell
        };
    }

    private static int GetLineScore(int lineCount)
    {
        return lineCount switch
        {
            1 => 100,
            2 => 300,
            3 => 500,
            4 => 800,
            _ => 0
        };
    }

    private void RestartGame()
    {
        if (boardRoot != null)
        {
            Destroy(boardRoot.gameObject);
        }

        score = 0;
        clearedLines = 0;
        gameOver = false;
        gameClear = false;
        blockBag = null;
        blockBagIndex = 0;
        nextLockedBlockId = 1;
        SetupBoard();
        SetupPlayerStart();
        UpdateClearTargetView();
        UpdateDangerZoneViews();
        RefreshPathLines();
        SpawnBlock();
    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            normal = { textColor = Color.white }
        };

        GUI.Label(new Rect(20, 20, 350, 35), $"Score: {score}   Lines: {clearedLines}", style);
        GUI.Label(
            new Rect(20, 52, 850, 35),
            $"← → 이동 | {clockwiseRotationKey} 시계 회전 | {counterClockwiseRotationKey} 반시계 회전 | ↓ 빠르게 | Space 즉시 낙하",
            style);

        if (!gameOver && gameClear)
        {
            GUIStyle clearStyle = new GUIStyle(style)
            {
                fontSize = 42,
                alignment = TextAnchor.MiddleCenter
            };
            GUI.Label(new Rect(0, Screen.height / 2f - 60f, Screen.width, 120f), "GAME CLEAR\nR 키로 다시 시작", clearStyle);
            return;
        }

        if (!gameOver)
        {
            return;
        }

        GUIStyle gameOverStyle = new GUIStyle(style)
        {
            fontSize = 42,
            alignment = TextAnchor.MiddleCenter
        };
        GUI.Label(new Rect(0, Screen.height / 2f - 60f, Screen.width, 120f), "GAME OVER\nR 키로 다시 시작", gameOverStyle);
    }
}
