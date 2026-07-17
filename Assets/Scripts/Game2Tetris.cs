using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Game2Tetris : MonoBehaviour
{
    [Serializable]
    private struct TetrominoData
    {
        public string name;
        public GameObject prefab;
        public Vector2Int[] cells;

        public TetrominoData(string name, GameObject prefab, params Vector2Int[] cells)
        {
            this.name = name;
            this.prefab = prefab;
            this.cells = cells;
        }
    }

    private struct ActivePiece
    {
        public int type;
        public Vector2Int position;
        public int rotation;
        public GameObject root;
    }

    [Header("Board")]
    [Tooltip("Game2 씬의 Plane을 연결하세요.")]
    [SerializeField] private Transform plane;
    [SerializeField, Min(4)] private int boardWidth = 10;
    [SerializeField, Min(4)] private int boardDepth = 20;
    [SerializeField, Min(0.1f)] private float cellSize = 1f;
    [SerializeField] private float blockHeight = 0.5f;

    [Header("Block Prefabs")]
    [SerializeField] private GameObject blockI;
    [SerializeField] private GameObject blockO;
    [SerializeField] private GameObject blockT;
    [SerializeField] private GameObject blockS;
    [SerializeField] private GameObject blockZ;
    [SerializeField] private GameObject blockJ;
    [SerializeField] private GameObject blockL;

    [Header("Speed")]
    [SerializeField, Min(0.05f)] private float fallInterval = 0.7f;
    [SerializeField, Min(0.01f)] private float softDropInterval = 0.05f;

    [Header("Controls")]
    [SerializeField] private Key moveLeftKey = Key.LeftArrow;
    [SerializeField] private Key moveRightKey = Key.RightArrow;
    [SerializeField] private Key softDropKey = Key.DownArrow;
    [SerializeField] private Key hardDropKey = Key.Space;
    [SerializeField] private Key clockwiseKey = Key.X;
    [SerializeField] private Key counterClockwiseKey = Key.Z;

    private TetrominoData[] tetrominoes;
    private Transform[,] settledCells;
    private ActivePiece activePiece;
    private Transform settledRoot;
    private int[] bag;
    private int bagIndex;
    private float nextFallTime;
    private int score;
    private int clearedLines;
    private bool gameOver;

    private void Start()
    {
        if (!ValidateSetup())
        {
            enabled = false;
            return;
        }

        CreateTetrominoData();
        settledCells = new Transform[boardWidth, boardDepth];
        settledRoot = new GameObject("Settled Blocks").transform;
        SpawnPiece();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || gameOver)
        {
            return;
        }

        if (keyboard[moveLeftKey].wasPressedThisFrame)
        {
            TryMove(Vector2Int.right);
        }

        if (keyboard[moveRightKey].wasPressedThisFrame)
        {
            TryMove(Vector2Int.left);
        }

        if (keyboard[clockwiseKey].wasPressedThisFrame)
        {
            TryRotate(1);
        }

        if (keyboard[counterClockwiseKey].wasPressedThisFrame)
        {
            TryRotate(-1);
        }

        if (keyboard[hardDropKey].wasPressedThisFrame)
        {
            HardDrop();
            return;
        }

        float interval = keyboard[softDropKey].isPressed ? softDropInterval : fallInterval;
        if (Time.time < nextFallTime)
        {
            return;
        }

        // 보드 좌표의 +Y가 Plane의 +Z 방향이다.
        if (!TryMove(Vector2Int.up))
        {
            LockPiece();
        }

        nextFallTime = Time.time + interval;
    }

    private bool ValidateSetup()
    {
        if (plane == null)
        {
            Debug.LogError("Game2Tetris: Game2 씬의 Plane을 Inspector에 연결하세요.", this);
            return false;
        }

        GameObject[] prefabs = { blockI, blockO, blockT, blockS, blockZ, blockJ, blockL };
        for (int i = 0; i < prefabs.Length; i++)
        {
            if (prefabs[i] != null)
            {
                continue;
            }

            Debug.LogError("Game2Tetris: Block Prefabs의 7개 슬롯을 모두 연결하세요.", this);
            return false;
        }

        return true;
    }

    private void CreateTetrominoData()
    {
        tetrominoes = new[]
        {
            CreateDataFromPrefab("I", blockI),
            CreateDataFromPrefab("O", blockO),
            CreateDataFromPrefab("T", blockT),
            CreateDataFromPrefab("S", blockS),
            CreateDataFromPrefab("Z", blockZ),
            CreateDataFromPrefab("J", blockJ),
            CreateDataFromPrefab("L", blockL)
        };
    }

    private static TetrominoData CreateDataFromPrefab(string name, GameObject prefab)
    {
        Transform[] cubes = GetPieceCubes(prefab.transform);
        Vector2Int[] cells = new Vector2Int[cubes.Length];
        for (int i = 0; i < cubes.Length; i++)
        {
            Vector3 localPosition = prefab.transform.InverseTransformPoint(cubes[i].position);
            cells[i] = new Vector2Int(
                Mathf.RoundToInt(localPosition.x),
                Mathf.RoundToInt(localPosition.z));
        }

        return new TetrominoData(name, prefab, cells);
    }

    private void SpawnPiece()
    {
        int type = DrawFromBag();
        int spawnRow = -GetMinimumCellY(tetrominoes[type].cells);
        activePiece = new ActivePiece
        {
            type = type,
            position = new Vector2Int(boardWidth / 2 - 1, spawnRow),
            rotation = 0,
            root = Instantiate(tetrominoes[type].prefab)
        };

        activePiece.root.name = $"Active {tetrominoes[type].name}";
        activePiece.root.transform.localScale = Vector3.one * cellSize;
        UpdatePieceTransform();
        nextFallTime = Time.time + fallInterval;

        if (!IsValid(activePiece.position, activePiece.rotation))
        {
            gameOver = true;
            Debug.Log("Game Over");
        }
    }

    private int DrawFromBag()
    {
        if (bag == null || bagIndex >= bag.Length)
        {
            bag = new int[7];
            for (int i = 0; i < bag.Length; i++)
            {
                bag[i] = i;
            }

            for (int i = bag.Length - 1; i > 0; i--)
            {
                int randomIndex = UnityEngine.Random.Range(0, i + 1);
                (bag[i], bag[randomIndex]) = (bag[randomIndex], bag[i]);
            }

            bagIndex = 0;
        }

        return bag[bagIndex++];
    }

    private bool TryMove(Vector2Int direction)
    {
        Vector2Int target = activePiece.position + direction;
        if (!IsValid(target, activePiece.rotation))
        {
            return false;
        }

        activePiece.position = target;
        UpdatePieceTransform();
        return true;
    }

    private void TryRotate(int direction)
    {
        if (tetrominoes[activePiece.type].name == "O")
        {
            return;
        }

        int rotation = (activePiece.rotation + direction + 4) % 4;
        int[] kicks = direction > 0 ? new[] { 0, -1, 1, -2, 2 } : new[] { 0, 1, -1, 2, -2 };
        foreach (int kick in kicks)
        {
            Vector2Int target = activePiece.position + new Vector2Int(kick, 0);
            if (!IsValid(target, rotation))
            {
                continue;
            }

            activePiece.position = target;
            activePiece.rotation = rotation;
            UpdatePieceTransform();
            return;
        }
    }

    private void HardDrop()
    {
        int distance = 0;
        while (TryMove(Vector2Int.up))
        {
            distance++;
        }

        score += distance * 2;
        LockPiece();
    }

    private bool IsValid(Vector2Int position, int rotation)
    {
        foreach (Vector2Int sourceCell in tetrominoes[activePiece.type].cells)
        {
            Vector2Int cell = position + RotateCell(sourceCell, rotation);
            if (cell.x < 0 || cell.x >= boardWidth || cell.y < 0 || cell.y >= boardDepth)
            {
                return false;
            }

            if (settledCells[cell.x, cell.y] != null)
            {
                return false;
            }
        }

        return true;
    }

    private void LockPiece()
    {
        Transform[] cubes = GetPieceCubes(activePiece.root.transform);
        Vector2Int[] shape = tetrominoes[activePiece.type].cells;
        if (cubes.Length != shape.Length)
        {
            Debug.LogError($"{activePiece.root.name} 프리팹에는 자식 블록이 정확히 4개 있어야 합니다.", activePiece.root);
            gameOver = true;
            return;
        }

        for (int i = 0; i < shape.Length; i++)
        {
            Vector2Int cell = activePiece.position + RotateCell(shape[i], activePiece.rotation);
            Transform cube = cubes[i];
            cube.SetParent(settledRoot, true);
            cube.position = CellToWorld(cell);
            cube.rotation = plane.rotation;
            cube.localScale = Vector3.one * cellSize;
            settledCells[cell.x, cell.y] = cube;
        }

        Destroy(activePiece.root);
        int lines = ClearFullLines();
        clearedLines += lines;
        score += lines switch { 1 => 100, 2 => 300, 3 => 500, 4 => 800, _ => 0 };
        SpawnPiece();
    }

    private int ClearFullLines()
    {
        int cleared = 0;
        int row = boardDepth - 1;
        while (row >= 0)
        {
            if (!IsRowFull(row))
            {
                row--;
                continue;
            }

            for (int x = 0; x < boardWidth; x++)
            {
                Destroy(settledCells[x, row].gameObject);
                settledCells[x, row] = null;
            }

            // +Z 쪽 줄이 없어지면 -Z 쪽의 블록들이 한 칸씩 +Z로 이동한다.
            for (int z = row - 1; z >= 0; z--)
            {
                for (int x = 0; x < boardWidth; x++)
                {
                    Transform cube = settledCells[x, z];
                    settledCells[x, z + 1] = cube;
                    settledCells[x, z] = null;
                    if (cube != null)
                    {
                        cube.position = CellToWorld(new Vector2Int(x, z + 1));
                    }
                }
            }

            cleared++;
        }

        return cleared;
    }

    private bool IsRowFull(int row)
    {
        for (int x = 0; x < boardWidth; x++)
        {
            if (settledCells[x, row] == null)
            {
                return false;
            }
        }

        return true;
    }

    private void UpdatePieceTransform()
    {
        activePiece.root.transform.position = CellToWorld(activePiece.position);
        activePiece.root.transform.rotation = plane.rotation * Quaternion.Euler(0f, activePiece.rotation * 90f, 0f);
    }

    private Vector3 CellToWorld(Vector2Int cell)
    {
        float x = (cell.x - (boardWidth - 1) * 0.5f) * cellSize;
        float z = (cell.y - (boardDepth - 1) * 0.5f) * cellSize;
        return plane.position + plane.right * x + plane.up * blockHeight + plane.forward * z;
    }

    private static int GetMinimumCellY(Vector2Int[] cells)
    {
        int minimum = int.MaxValue;
        foreach (Vector2Int cell in cells)
        {
            minimum = Mathf.Min(minimum, cell.y);
        }

        return minimum;
    }

    private static Vector2Int RotateCell(Vector2Int cell, int rotation)
    {
        return rotation switch
        {
            1 => new Vector2Int(-cell.y, cell.x),
            2 => new Vector2Int(-cell.x, -cell.y),
            3 => new Vector2Int(cell.y, -cell.x),
            _ => cell
        };
    }

    private static Transform[] GetPieceCubes(Transform root)
    {
        Transform[] allChildren = root.GetComponentsInChildren<Transform>();
        Transform[] cubes = new Transform[allChildren.Length - 1];
        int index = 0;
        foreach (Transform child in allChildren)
        {
            if (child == root || child.GetComponent<Renderer>() == null)
            {
                continue;
            }

            if (index < cubes.Length)
            {
                cubes[index++] = child;
            }
        }

        if (index == cubes.Length)
        {
            return cubes;
        }

        Array.Resize(ref cubes, index);
        return cubes;
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(20, 20, 400, 30), $"Score: {score}  Lines: {clearedLines}");
        if (gameOver)
        {
            GUI.Label(new Rect(20, 50, 400, 30), "GAME OVER");
        }
    }
}
