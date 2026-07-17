using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class test : MonoBehaviour
{
    [Serializable]
    public struct BlockData
    {
        public string name;
        public Color color;
        public Vector2Int[] cells;

        public BlockData(string name, Color color, params Vector2Int[] cells)
        {
            this.name = name;
            this.color = color;
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

    [Header("Board")]
    [SerializeField] private int width = 10;
    [SerializeField] private int height = 20;
    [SerializeField, Min(0.05f)] private float fallInterval = 0.7f;
    [SerializeField, Min(0.01f)] private float fastFallInterval = 0.05f;

    private BlockData[] blockData;
    private GameObject[,] board;
    private ActiveBlock activeBlock;
    private Transform boardRoot;
    private Sprite blockSprite;
    private float nextFallTime;
    private int score;
    private int clearedLines;
    private bool gameOver;

    private void Start()
    {
        CreateBlockData();
        CreateBlockSprite();
        SetupBoard();
        SetupCamera();
        SpawnBlock();
    }

    private void Update()
    {
        if (gameOver)
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

        if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
        {
            TryRotate();
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
            new BlockData("I", Color.cyan,
                new Vector2Int(-1, 0), new Vector2Int(0, 0),
                new Vector2Int(1, 0), new Vector2Int(2, 0)),
            new BlockData("O", Color.yellow,
                new Vector2Int(0, 0), new Vector2Int(1, 0),
                new Vector2Int(0, 1), new Vector2Int(1, 1)),
            new BlockData("T", new Color(0.65f, 0.2f, 0.9f),
                new Vector2Int(-1, 0), new Vector2Int(0, 0),
                new Vector2Int(1, 0), new Vector2Int(0, 1)),
            new BlockData("S", Color.green,
                new Vector2Int(-1, 0), new Vector2Int(0, 0),
                new Vector2Int(0, 1), new Vector2Int(1, 1)),
            new BlockData("Z", Color.red,
                new Vector2Int(-1, 1), new Vector2Int(0, 1),
                new Vector2Int(0, 0), new Vector2Int(1, 0)),
            new BlockData("J", Color.blue,
                new Vector2Int(-1, 1), new Vector2Int(-1, 0),
                new Vector2Int(0, 0), new Vector2Int(1, 0)),
            new BlockData("L", new Color(1f, 0.5f, 0f),
                new Vector2Int(1, 1), new Vector2Int(-1, 0),
                new Vector2Int(0, 0), new Vector2Int(1, 0))
        };
    }

    private void SetupBoard()
    {
        board = new GameObject[width, height];
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

    private void SpawnBlock()
    {
        int index = UnityEngine.Random.Range(0, blockData.Length);
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
        }

        activeBlock = next;
        UpdateActiveViews();
        nextFallTime = Time.time + fallInterval;

        if (!IsValid(activeBlock.position, activeBlock.rotation))
        {
            gameOver = true;
        }
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

    private void TryRotate()
    {
        if (blockData[activeBlock.dataIndex].name == "O")
        {
            return;
        }

        int targetRotation = (activeBlock.rotation + 1) % 4;
        int[] wallKicks = { 0, -1, 1, -2, 2 };

        foreach (int kick in wallKicks)
        {
            Vector2Int target = activeBlock.position + new Vector2Int(kick, 0);
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
        for (int i = 0; i < cells.Length; i++)
        {
            Vector2Int cell = activeBlock.position + RotateCell(cells[i], activeBlock.rotation);
            board[cell.x, cell.y] = activeBlock.views[i];
            activeBlock.views[i].name = "Locked Block";
        }

        int lineCount = ClearCompletedLines();
        score += GetLineScore(lineCount);
        clearedLines += lineCount;
        SpawnBlock();
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
            Destroy(board[x, y]);
            board[x, y] = null;
        }
    }

    private void MoveLinesDown(int startY)
    {
        for (int y = startY; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject block = board[x, y];
                if (block == null)
                {
                    continue;
                }

                board[x, y - 1] = block;
                board[x, y] = null;
                block.transform.localPosition += Vector3.down;
            }
        }
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
        SetupBoard();
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
        GUI.Label(new Rect(20, 52, 600, 35), "← → 이동  |  ↑ 회전  |  ↓ 빠르게  |  Space 즉시 낙하", style);

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
