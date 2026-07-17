using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class Game2StagePresentation
{
    private readonly Transform owner;
    private readonly TetrisBoard board;
    private readonly Transform plane;
    private readonly int boardWidth;
    private readonly int boardDepth;
    private readonly GameObject stageBackgroundPrefab;
    private readonly bool fitBackgroundToCamera;
    private readonly GameObject stageFramePrefab;
    private readonly Color stageFrameColor;
    private readonly float stageFrameHeightOffset;
    private readonly float stageFrameScale;
    private readonly float stageFrameThickness;
    private readonly GameObject mapRevealEffectPrefab;
    private readonly float mapRevealEffectLifetime;
    private GameObject stageBackgroundView;
    private Transform stageFrameRoot;
    private readonly List<GameObject> leftWallViews = new List<GameObject>();
    private readonly List<GameObject> rightWallViews = new List<GameObject>();
    private readonly List<GameObject> floorViews = new List<GameObject>();

    public IReadOnlyList<GameObject> LeftWallViews => leftWallViews;
    public IReadOnlyList<GameObject> RightWallViews => rightWallViews;
    public IReadOnlyList<GameObject> FloorViews => floorViews;

    public Game2StagePresentation(
        Transform owner,
        TetrisBoard board,
        Transform plane,
        int boardWidth,
        int boardDepth,
        GameObject stageBackgroundPrefab,
        bool fitBackgroundToCamera,
        GameObject stageFramePrefab,
        Color stageFrameColor,
        float stageFrameHeightOffset,
        float stageFrameScale,
        float stageFrameThickness,
        GameObject mapRevealEffectPrefab,
        float mapRevealEffectLifetime)
    {
        this.owner = owner;
        this.board = board;
        this.plane = plane;
        this.boardWidth = boardWidth;
        this.boardDepth = boardDepth;
        this.stageBackgroundPrefab = stageBackgroundPrefab;
        this.fitBackgroundToCamera = fitBackgroundToCamera;
        this.stageFramePrefab = stageFramePrefab;
        this.stageFrameColor = stageFrameColor;
        this.stageFrameHeightOffset = stageFrameHeightOffset;
        this.stageFrameScale = stageFrameScale;
        this.stageFrameThickness = stageFrameThickness;
        this.mapRevealEffectPrefab = mapRevealEffectPrefab;
        this.mapRevealEffectLifetime = mapRevealEffectLifetime;
    }

    public void SetupFrame()
    {
        if (stageFrameRoot != null)
        {
            Object.Destroy(stageFrameRoot.gameObject);
        }

        leftWallViews.Clear();
        rightWallViews.Clear();
        floorViews.Clear();

        stageFrameRoot = new GameObject("Stage Frame").transform;
        stageFrameRoot.SetParent(owner, false);

        for (int z = -1; z <= boardDepth; z++)
        {
            leftWallViews.Add(CreateFrameBlock(new Vector2Int(-1, z), "Left Wall"));
            rightWallViews.Add(CreateFrameBlock(new Vector2Int(boardWidth, z), "Right Wall"));
        }

        for (int x = 0; x < boardWidth; x++)
        {
            floorViews.Add(CreateFrameBlock(new Vector2Int(x, boardDepth), "Floor"));
        }
    }

    public void SetupBackground()
    {
        if (stageBackgroundPrefab == null)
        {
            return;
        }

        if (stageBackgroundView != null)
        {
            Object.Destroy(stageBackgroundView);
        }

        stageBackgroundView = Object.Instantiate(stageBackgroundPrefab, owner);
        stageBackgroundView.name = "Stage Background";

        Vector3 min = board.GridPointToWorld(new Vector2Int(-1, -1));
        Vector3 max = board.GridPointToWorld(new Vector2Int(boardWidth, boardDepth));
        Vector3 center = (min + max) * 0.5f;

        stageBackgroundView.transform.position = center + plane.up * 0.01f;
        stageBackgroundView.transform.rotation = Quaternion.LookRotation(plane.up, plane.forward) * Quaternion.Euler(0f, 0f, 180f);

        if (fitBackgroundToCamera)
        {
            FitBackgroundToCamera();
        }
    }

    public IEnumerator PlayMapIntro(
        bool playIntro,
        float interval,
        IReadOnlyList<GameObject> leftWalls,
        IReadOnlyList<GameObject> rightWalls,
        IReadOnlyList<GameObject> floors,
        IReadOnlyList<GameObject> startMarkers,
        IReadOnlyList<GameObject> endMarkers,
        IReadOnlyList<GameObject> dangerMarkers)
    {
        SetActive(leftWalls, false);
        SetActive(rightWalls, false);
        SetActive(floors, false);
        SetActive(startMarkers, false);
        SetActive(endMarkers, false);
        SetActive(dangerMarkers, false);

        if (!playIntro)
        {
            Reveal(leftWalls);
            Reveal(rightWalls);
            Reveal(floors);
            Reveal(startMarkers);
            Reveal(endMarkers);
            Reveal(dangerMarkers);
            yield break;
        }

        Reveal(leftWalls);
        yield return new WaitForSeconds(interval);

        Reveal(rightWalls);
        yield return new WaitForSeconds(interval);

        Reveal(floors);
        yield return new WaitForSeconds(interval);

        Reveal(startMarkers);
        yield return new WaitForSeconds(interval);

        Reveal(endMarkers);
        yield return new WaitForSeconds(interval);

        Reveal(dangerMarkers);
        yield return new WaitForSeconds(interval);
    }

    public IEnumerator LoadNextStageAfterDelay(string sceneName, float delay)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            yield break;
        }

        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }

    public void Destroy()
    {
        if (stageBackgroundView != null)
        {
            Object.Destroy(stageBackgroundView);
        }

        if (stageFrameRoot != null)
        {
            Object.Destroy(stageFrameRoot.gameObject);
        }
    }

    private GameObject CreateFrameBlock(Vector2Int gridPoint, string label)
    {
        GameObject block = stageFramePrefab != null
            ? Object.Instantiate(stageFramePrefab, stageFrameRoot)
            : GameObject.CreatePrimitive(PrimitiveType.Cube);

        if (stageFramePrefab == null)
        {
            block.transform.SetParent(stageFrameRoot, false);
            Renderer renderer = block.GetComponent<Renderer>();
            if (renderer != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                if (shader != null)
                {
                    renderer.material = new Material(shader)
                    {
                        color = stageFrameColor
                    };
                }
            }
        }

        block.name = $"{label} {gridPoint.x},{gridPoint.y}";
        block.transform.position = board.GridPointToWorld(gridPoint) + plane.up * stageFrameHeightOffset;
        block.transform.rotation = plane.rotation;
        block.transform.localScale = new Vector3(stageFrameScale, stageFrameThickness, stageFrameScale);
        return block;
    }

    private void Reveal(IReadOnlyList<GameObject> markers)
    {
        SetActive(markers, true);
        for (int i = 0; i < markers.Count; i++)
        {
            PlayRevealEffect(markers[i]);
        }
    }

    private void SetActive(IReadOnlyList<GameObject> markers, bool active)
    {
        for (int i = 0; i < markers.Count; i++)
        {
            if (markers[i] != null)
            {
                markers[i].SetActive(active);
            }
        }
    }

    private void PlayRevealEffect(GameObject marker)
    {
        if (mapRevealEffectPrefab == null || marker == null)
        {
            return;
        }

        GameObject effect = Object.Instantiate(mapRevealEffectPrefab, marker.transform.position, Quaternion.identity);
        Object.Destroy(effect, mapRevealEffectLifetime);
    }

    private void FitBackgroundToCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            FitBackgroundToBoard();
            return;
        }

        Vector2 backgroundSize = GetLocalRenderSize(stageBackgroundView);
        if (backgroundSize.x <= 0f || backgroundSize.y <= 0f)
        {
            return;
        }

        float cameraWorldHeight;
        if (mainCamera.orthographic)
        {
            cameraWorldHeight = mainCamera.orthographicSize * 2f;
        }
        else
        {
            float distanceFromCamera = Mathf.Abs(Vector3.Dot(
                stageBackgroundView.transform.position - mainCamera.transform.position,
                mainCamera.transform.forward));
            cameraWorldHeight = 2f * distanceFromCamera * Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        }

        float cameraWorldWidth = cameraWorldHeight * mainCamera.aspect;
        float scale = Mathf.Max(cameraWorldWidth / backgroundSize.x, cameraWorldHeight / backgroundSize.y);
        stageBackgroundView.transform.localScale = Vector3.one * scale;
    }

    private void FitBackgroundToBoard()
    {
        Vector2 backgroundSize = GetLocalRenderSize(stageBackgroundView);
        if (backgroundSize.x <= 0f || backgroundSize.y <= 0f)
        {
            return;
        }

        float boardWorldWidth = Vector3.Distance(
            board.GridPointToWorld(new Vector2Int(-1, 0)),
            board.GridPointToWorld(new Vector2Int(boardWidth, 0)));
        float boardWorldDepth = Vector3.Distance(
            board.GridPointToWorld(new Vector2Int(0, -1)),
            board.GridPointToWorld(new Vector2Int(0, boardDepth)));
        float scale = Mathf.Max(boardWorldWidth / backgroundSize.x, boardWorldDepth / backgroundSize.y);
        stageBackgroundView.transform.localScale = Vector3.one * scale;
    }

    private static Vector2 GetLocalRenderSize(GameObject root)
    {
        SpriteRenderer spriteRenderer = root.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            return spriteRenderer.sprite.bounds.size;
        }

        Renderer renderer = root.GetComponentInChildren<Renderer>();
        if (renderer == null)
        {
            return Vector2.zero;
        }

        Bounds bounds = renderer.localBounds;
        return new Vector2(bounds.size.x, bounds.size.y);
    }
}
