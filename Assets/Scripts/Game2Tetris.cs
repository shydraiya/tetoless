using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Game2Tetris : MonoBehaviour
{
    [Header("Board")]
    [Tooltip("Connect the Plane from the Game2 scene.")]
    [SerializeField] private Transform plane;
    [SerializeField, Min(4)] private int boardWidth = 10;
    [SerializeField, Min(4)] private int boardDepth = 20;
    [SerializeField, Min(0.1f)] private float cellSize = 1f;
    [SerializeField] private float blockHeight = 0.5f;

    [Header("Camera")]
    [SerializeField] private bool alignCameraToBoard = true;
    [SerializeField, Min(0.1f)] private float cameraDistance = 20f;
    [SerializeField, Min(0f)] private float cameraPadding = 1.5f;

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
    [SerializeField] private Key debugStageClearKey = Key.Backquote;
    [SerializeField] private Key restartKey = Key.R;

    [Header("Ghost Piece")]
    [SerializeField] private Color ghostColor = new Color(1f, 1f, 1f, 0.25f);
    [SerializeField, Min(0f)] private float ghostBehindOffset = 0.05f;

    [Header("Order Labels")]
    [SerializeField] private bool showOrderLabels = true;
    [SerializeField] private Color orderLabelColor = Color.black;
    [SerializeField, Min(0.01f)] private float orderLabelSize = 0.35f;
    [SerializeField] private float orderLabelHeightOffset = 0.55f;

    [Header("Path Points")]
    [SerializeField] private Vector2Int pathStartPoint = new Vector2Int(5, -1);
    [SerializeField] private Vector2Int[] pathEndPoints =
    {
        new Vector2Int(5, 20)
    };
    [SerializeField] private Color pathStartColor = Color.blue;
    [SerializeField] private Color pathEndColor = Color.green;
    [SerializeField] private Color dangerZoneColor = new Color(1f, 0f, 0f, 0.45f);
    [SerializeField] private GameObject dangerZonePrefab;
    [SerializeField, Min(0.01f)] private float dangerZonePrefabScale = 1f;
    [SerializeField] private Vector3 dangerZonePrefabRotation;
    [SerializeField, Min(0.05f)] private float pathPointSize = 0.8f;
    [SerializeField] private float pathPointHeightOffset = 0.08f;
    [SerializeField] private Color pathLineColor = Color.white;
    [SerializeField, Min(0.01f)] private float pathLineThickness = 0.08f;
    [SerializeField] private float pathLineHeightOffset = 0.16f;
    [SerializeField] private GameObject pathLineEffectPrefab;
    [SerializeField] private Vector2Int[] dangerZonePositions =
    {
        new Vector2Int(4, 10),
        new Vector2Int(5, 10),
        new Vector2Int(6, 10)
    };

    [Header("Stage Presentation")]
    [SerializeField] private bool playMapIntro = true;
    [SerializeField, Min(0.05f)] private float mapIntroInterval = 1f;
    [SerializeField] private GameObject mapRevealEffectPrefab;
    [SerializeField, Min(0.1f)] private float mapRevealEffectLifetime = 3f;
    [SerializeField] private GameObject stageBackgroundPrefab;
    [SerializeField] private bool fitBackgroundToCamera = true;
    [SerializeField] private bool fitBackgroundToBoardFrame = true;
    [SerializeField] private Vector2 backgroundFrameCenter = new Vector2(0.497f, 0.508f);
    [SerializeField] private Vector2 backgroundFrameSize = new Vector2(0.285f, 0.723f);
    [SerializeField] private GameObject stageFramePrefab;
    [SerializeField] private Color stageFrameColor = new Color(0.18f, 0.18f, 0.22f, 1f);
    [SerializeField] private float stageFrameHeightOffset = 0.04f;
    [SerializeField, Min(0.01f)] private float stageFrameScale = 1f;
    [SerializeField, Min(0.001f)] private float stageFrameThickness = 0.03f;

    [Header("Stage Clear")]
    [SerializeField] private string nextStageSceneName;
    [SerializeField, Min(0f)] private float nextStageDelay = 1f;

    private TetrominoData[] tetrominoes;
    private ActiveTetrisPiece activePiece;
    private TetrisBoard board;
    private TetrisGhost ghost;
    private TetrisOrderLabelRenderer orderLabelRenderer;
    private TetrisPathPointRenderer pathPointRenderer;
    private TetrisPathLineRenderer pathLineRenderer;
    private TetrisPathRules pathRules;
    private TetrisPathResult pathResult;
    private Vector2Int[] dangerZones;
    private Game2StagePresentation stagePresentation;
    private SevenBag sevenBag;
    private float nextFallTime;
    private int score;
    private int clearedLines;
    private int nextLockedBlockId = 1;
    private bool gameOver;
    private bool gameClear;
    private bool mapIntroPlaying;
    private bool loadingNextStage;

    private void Start()
    {
        StartCoroutine(BeginGame());
    }

    private IEnumerator BeginGame()
    {
        if (!ValidateSetup())
        {
            enabled = false;
            yield break;
        }

        CreateTetrominoes();
        board = new TetrisBoard(boardWidth, boardDepth, cellSize, blockHeight, plane);
        AlignCameraToBoard();
        ghost = new TetrisGhost(ghostColor, ghostBehindOffset);
        orderLabelRenderer = new TetrisOrderLabelRenderer(orderLabelColor, orderLabelSize, orderLabelHeightOffset);
        SetupPath();
        stagePresentation = new Game2StagePresentation(
            transform,
            board,
            plane,
            boardWidth,
            boardDepth,
            stageBackgroundPrefab,
            fitBackgroundToCamera,
            fitBackgroundToBoardFrame,
            backgroundFrameCenter,
            backgroundFrameSize,
            stageFramePrefab,
            stageFrameColor,
            Mathf.Max(0.02f, Mathf.Min(stageFrameHeightOffset, pathPointHeightOffset - 0.01f)),
            cellSize * stageFrameScale,
            stageFrameThickness,
            mapRevealEffectPrefab,
            mapRevealEffectLifetime);
        stagePresentation.SetupFrame();
        stagePresentation.SetupBackground();
        sevenBag = new SevenBag();
        mapIntroPlaying = true;
        yield return stagePresentation.PlayMapIntro(
            playMapIntro,
            mapIntroInterval,
            stagePresentation.LeftWallViews,
            stagePresentation.RightWallViews,
            stagePresentation.FloorViews,
            pathPointRenderer.StartMarkers,
            pathPointRenderer.EndMarkers,
            pathPointRenderer.DangerMarkers);
        mapIntroPlaying = false;
        SpawnPiece();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || mapIntroPlaying)
        {
            return;
        }

        if (gameOver)
        {
            if (keyboard[restartKey].wasPressedThisFrame)
            {
                RestartScene();
            }

            return;
        }

        if (gameClear)
        {
            return;
        }

        HandleInput(keyboard);
        HandleAutomaticFall(keyboard);
    }

    private void HandleInput(Keyboard keyboard)
    {
        if (keyboard[debugStageClearKey].wasPressedThisFrame)
        {
            TriggerGameClear();
            return;
        }

        if (keyboard[moveLeftKey].wasPressedThisFrame) TryMove(Vector2Int.left);
        if (keyboard[moveRightKey].wasPressedThisFrame) TryMove(Vector2Int.right);
        if (keyboard[clockwiseKey].wasPressedThisFrame) TryRotate(1);
        if (keyboard[counterClockwiseKey].wasPressedThisFrame) TryRotate(-1);

        if (keyboard[hardDropKey].wasPressedThisFrame)
        {
            HardDrop();
        }
    }

    private void HandleAutomaticFall(Keyboard keyboard)
    {
        if (activePiece == null || keyboard[hardDropKey].wasPressedThisFrame)
        {
            return;
        }

        float interval = keyboard[softDropKey].isPressed ? softDropInterval : fallInterval;
        if (Time.time < nextFallTime)
        {
            return;
        }

        if (!TryMove(Vector2Int.down))
        {
            LockPiece();
        }

        nextFallTime = Time.time + interval;
    }

    private bool ValidateSetup()
    {
        if (plane == null)
        {
            Debug.LogError("Game2Tetris: Connect the Game2 Plane in the Inspector.", this);
            return false;
        }

        GameObject[] prefabs = { blockI, blockO, blockT, blockS, blockZ, blockJ, blockL };
        foreach (GameObject prefab in prefabs)
        {
            if (prefab == null)
            {
                Debug.LogError("Game2Tetris: Connect all seven block prefabs in the Inspector.", this);
                return false;
            }
        }

        return true;
    }

    private void AlignCameraToBoard()
    {
        if (!alignCameraToBoard)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("Game2Tetris: Main Camera was not found. Camera alignment skipped.", this);
            return;
        }

        Vector3 boardCenter = GetBoardCenterWorld();
        mainCamera.transform.position = boardCenter + plane.up * cameraDistance;
        mainCamera.transform.rotation = Quaternion.LookRotation(-plane.up, plane.forward);

        mainCamera.orthographic = true;
        float boardWorldWidth = boardWidth * cellSize;
        float boardWorldDepth = boardDepth * cellSize;
        float sizeByDepth = boardWorldDepth * 0.5f + cameraPadding;
        float sizeByWidth = (boardWorldWidth * 0.5f + cameraPadding) / mainCamera.aspect;
        mainCamera.orthographicSize = Mathf.Max(sizeByDepth, sizeByWidth);
    }

    private Vector3 GetBoardCenterWorld()
    {
        Vector3 min = board.GridPointToWorld(new Vector2Int(0, 0));
        Vector3 max = board.GridPointToWorld(new Vector2Int(boardWidth - 1, boardDepth - 1));
        return (min + max) * 0.5f;
    }

    private void CreateTetrominoes()
    {
        tetrominoes = new[]
        {
            TetrominoData.FromPrefab("I", blockI), TetrominoData.FromPrefab("O", blockO),
            TetrominoData.FromPrefab("T", blockT), TetrominoData.FromPrefab("S", blockS),
            TetrominoData.FromPrefab("Z", blockZ), TetrominoData.FromPrefab("J", blockJ),
            TetrominoData.FromPrefab("L", blockL)
        };
    }

    private void SpawnPiece()
    {
        int type = sevenBag.Draw();
        TetrominoData data = tetrominoes[type];
        activePiece = new ActiveTetrisPiece
        {
            Type = type,
            LockedBlockId = nextLockedBlockId++,
            Position = new Vector2Int(boardWidth / 2 - 1, data.GetSpawnRowFromTop(boardDepth)),
            Rotation = 0,
            Root = Instantiate(data.Prefab)
        };

        activePiece.Root.name = $"Active {data.Name}";
        activePiece.Root.transform.localScale = Vector3.one * cellSize;
        if (showOrderLabels)
        {
            orderLabelRenderer.AddLabels(data, activePiece.Root.transform, plane);
        }

        UpdatePieceView();
        nextFallTime = Time.time + fallInterval;

        if (!board.IsValid(data, activePiece.Position, activePiece.Rotation))
        {
            gameOver = true;
            ghost.Hide();
            return;
        }

        ghost.Create(data, cellSize);
        UpdateGhost();
    }

    private bool TryMove(Vector2Int direction)
    {
        Vector2Int target = activePiece.Position + direction;
        if (!board.IsValid(CurrentData, target, activePiece.Rotation))
        {
            return false;
        }

        activePiece.Position = target;
        UpdatePieceView();
        return true;
    }

    private void TryRotate(int direction)
    {
        // if (CurrentData.Name == "O") return;

        int rotation = (activePiece.Rotation + direction + 4) % 4;
        int[] kicks = direction > 0 ? new[] { 0, -1, 1, -2, 2 } : new[] { 0, 1, -1, 2, -2 };
        foreach (int kick in kicks)
        {
            Vector2Int target = activePiece.Position + new Vector2Int(kick, 0);
            if (!board.IsValid(CurrentData, target, rotation)) continue;

            activePiece.Position = target;
            activePiece.Rotation = rotation;
            UpdatePieceView();
            return;
        }
    }

    private void HardDrop()
    {
        int distance = 0;
        while (TryMove(Vector2Int.down)) distance++;
        score += distance * 2;
        LockPiece();
    }

    private void LockPiece()
    {
        if (!board.Lock(CurrentData, activePiece))
        {
            gameOver = true;
            ghost.Hide();
            return;
        }

        orderLabelRenderer?.Refresh();
        ghost.Destroy();
        activePiece = null;
        System.Action<Transform> removeLabel = orderLabelRenderer == null ? null : orderLabelRenderer.RemoveLabelForTarget;
        int lines = board.ClearFullLines(removeLabel);
        orderLabelRenderer?.Refresh();
        EvaluatePath();
        clearedLines += lines;
        score += GetLineScore(lines);

        if (gameOver || gameClear)
        {
            return;
        }

        SpawnPiece();
    }

    private void SetupPath()
    {
        Vector2Int startPoint = ClampToPathPointArea(pathStartPoint);
        Vector2Int[] endPoints = ClampPathPoints(pathEndPoints);
        pathPointRenderer = new TetrisPathPointRenderer(
            board,
            plane,
            pathStartColor,
            pathEndColor,
            dangerZoneColor,
            pathPointSize,
            pathPointHeightOffset,
            dangerZonePrefab,
            dangerZonePrefabScale,
            dangerZonePrefabRotation);
        dangerZones = ClampDangerZones(dangerZonePositions);
        pathPointRenderer.Draw(startPoint, endPoints, dangerZones);
        pathLineRenderer = new TetrisPathLineRenderer(
            board,
            plane,
            pathLineColor,
            pathLineThickness,
            pathLineHeightOffset,
            pathLineEffectPrefab);
        pathRules = new TetrisPathRules(board, boardWidth, boardDepth, startPoint, endPoints, dangerZones);
        EvaluatePath();
    }

    private Vector2Int[] ClampPathPoints(Vector2Int[] positions)
    {
        if (positions == null || positions.Length == 0)
        {
            return new[] { ClampToPathPointArea(pathStartPoint + Vector2Int.up) };
        }

        Vector2Int[] clamped = new Vector2Int[positions.Length];
        for (int i = 0; i < positions.Length; i++)
        {
            clamped[i] = ClampToPathPointArea(positions[i]);
        }

        return clamped;
    }

    private void EvaluatePath()
    {
        pathResult = pathRules?.Evaluate();
        pathLineRenderer?.Draw(pathResult);
        if (pathResult == null)
        {
            return;
        }

        if (pathResult.ReachedDangerZone)
        {
            gameOver = true;
            ghost?.Hide();
            return;
        }

        if (pathResult.ReachedEndPoint)
        {
            TriggerGameClear();
        }
    }

    private void TriggerGameClear()
    {
        if (gameOver || gameClear)
        {
            return;
        }

        gameClear = true;
        if (!loadingNextStage && stagePresentation != null)
        {
            loadingNextStage = true;
            StartCoroutine(stagePresentation.LoadNextStageAfterDelay(nextStageSceneName, nextStageDelay));
        }
    }

    private Vector2Int ClampToPathPointArea(Vector2Int position)
    {
        return new Vector2Int(
            Mathf.Clamp(position.x, -1, boardWidth),
            Mathf.Clamp(position.y, -1, boardDepth));
    }

    private Vector2Int[] ClampDangerZones(Vector2Int[] positions)
    {
        Vector2Int[] clamped = new Vector2Int[positions.Length];
        for (int i = 0; i < positions.Length; i++)
        {
            clamped[i] = new Vector2Int(
                Mathf.Clamp(positions[i].x, 0, boardWidth - 1),
                Mathf.Clamp(positions[i].y, 0, boardDepth - 1));
        }

        return clamped;
    }

    private void UpdatePieceView()
    {
        activePiece.Root.transform.position = board.CellToWorld(activePiece.Position);
        activePiece.Root.transform.rotation = plane.rotation * Quaternion.Euler(0f, activePiece.Rotation * 90f, 0f);
        orderLabelRenderer?.Refresh();
        UpdateGhost();
    }

    private void UpdateGhost()
    {
        if (ghost != null && activePiece != null)
        {
            ghost.Update(board, CurrentData, activePiece, plane);
        }
    }

    private TetrominoData CurrentData => tetrominoes[activePiece.Type];

    private static int GetLineScore(int lines)
    {
        return lines switch { 1 => 100, 2 => 300, 3 => 500, 4 => 800, _ => 0 };
    }

    private void RestartScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    private void OnDestroy()
    {
        ghost?.Destroy();
        orderLabelRenderer?.Destroy();
        pathPointRenderer?.Destroy();
        pathLineRenderer?.Destroy();
        stagePresentation?.Destroy();
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(20, 20, 400, 30), $"Score: {score}  Lines: {clearedLines}");
        if (pathResult != null) GUI.Label(new Rect(20, 50, 400, 30), $"Path: {pathResult.ReachableCells.Count} cells");
        if (gameClear) GUI.Label(new Rect(20, 80, 400, 30), "GAME CLEAR");
        if (!gameOver)
        {
            return;
        }

        GUIStyle gameOverStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 42,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        GUI.Label(new Rect(0, Screen.height / 2f - 60f, Screen.width, 120f), $"GAME OVER\n{restartKey} 키를 눌러 재시작", gameOverStyle);
    }
}
