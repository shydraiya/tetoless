using UnityEngine;
using UnityEngine.Rendering;

public sealed class TetrisGhost
{
    private readonly Color color;
    private readonly float depthOffset;
    private GameObject root;

    public TetrisGhost(Color color, float depthOffset)
    {
        this.color = color;
        this.depthOffset = depthOffset;
    }

    public void Create(TetrominoData data, float cellSize)
    {
        Destroy();
        root = Object.Instantiate(data.Prefab);
        root.name = $"Ghost {data.Name}";
        root.transform.localScale = Vector3.one * cellSize;

        foreach (Collider blockCollider in root.GetComponentsInChildren<Collider>())
        {
            blockCollider.enabled = false;
        }

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            ConfigureMaterial(renderer.material);
        }
    }

    public void Update(TetrisBoard board, TetrominoData data, ActiveTetrisPiece piece, Transform plane)
    {
        if (root == null)
        {
            return;
        }

        Vector2Int landing = piece.Position;
        while (board.IsValid(data, landing + Vector2Int.up, piece.Rotation))
        {
            landing += Vector2Int.up;
        }

        root.transform.position = board.CellToWorld(landing) - plane.up * depthOffset;
        root.transform.rotation = plane.rotation * Quaternion.Euler(0f, piece.Rotation * 90f, 0f);
    }

    public void Hide()
    {
        if (root != null)
        {
            root.SetActive(false);
        }
    }

    public void Destroy()
    {
        if (root != null)
        {
            Object.Destroy(root);
            root = null;
        }
    }

    private void ConfigureMaterial(Material material)
    {
        material.color = color;
        material.renderQueue = (int)RenderQueue.Transparent;
        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
    }
}
