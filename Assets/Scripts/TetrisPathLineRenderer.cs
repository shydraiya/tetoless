using UnityEngine;
using DigitalRuby.LightningBolt;

public sealed class TetrisPathLineRenderer
{
    private const float EffectWidthMultiplier = 2.5f;
    private static readonly Color EffectColor = new Color(1.4f, 1.75f, 2f, 1f);

    private readonly TetrisBoard board;
    private readonly Transform plane;
    private readonly Color color;
    private readonly float thickness;
    private readonly float heightOffset;
    private readonly GameObject effectPrefab;
    private readonly Material material;
    private readonly Mesh lineMesh;
    private readonly Transform root;

    public TetrisPathLineRenderer(
        TetrisBoard board,
        Transform plane,
        Color color,
        float thickness,
        float heightOffset,
        GameObject effectPrefab)
    {
        this.board = board;
        this.plane = plane;
        this.color = color;
        this.thickness = thickness;
        this.heightOffset = heightOffset;
        this.effectPrefab = effectPrefab;
        material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        material.renderQueue = 5000;
        if (material.HasProperty("_ZTest"))
        {
            material.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        }

        if (material.HasProperty("_ZWrite"))
        {
            material.SetInt("_ZWrite", 0);
        }

        lineMesh = CreateLineMesh();
        root = new GameObject("Tetris Path Lines").transform;
    }

    public void Draw(TetrisPathResult result)
    {
        Clear();
        if (result == null)
        {
            return;
        }

        foreach (TetrisPathEdge edge in result.Edges)
        {
            DrawEdge(edge);
        }
    }

    public void Destroy()
    {
        if (root != null)
        {
            Object.Destroy(root.gameObject);
        }

        if (material != null)
        {
            Object.Destroy(material);
        }

        if (lineMesh != null)
        {
            Object.Destroy(lineMesh);
        }
    }

    private void DrawEdge(TetrisPathEdge edge)
    {
        if (effectPrefab != null)
        {
            DrawEffectEdge(edge);
            return;
        }

        GameObject lineObject = new GameObject("Path Line");
        lineObject.transform.SetParent(root, false);

        Vector3 start = GetPoint(edge.From);
        Vector3 end = GetPoint(edge.To);
        Vector3 delta = end - start;

        lineObject.transform.position = (start + end) * 0.5f;
        lineObject.transform.rotation = Quaternion.LookRotation(delta.normalized, plane.up);
        lineObject.transform.localScale = new Vector3(thickness, thickness, delta.magnitude);

        MeshRenderer renderer = lineObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        MeshFilter filter = lineObject.AddComponent<MeshFilter>();
        filter.sharedMesh = lineMesh;
    }

    private void DrawEffectEdge(TetrisPathEdge edge)
    {
        Vector3 start = GetPoint(edge.From);
        Vector3 end = GetPoint(edge.To);

        GameObject effect = Object.Instantiate(effectPrefab, root);
        effect.name = $"Path Lightning {edge.From.x},{edge.From.y} -> {edge.To.x},{edge.To.y}";
        effect.transform.position = Vector3.zero;
        effect.transform.rotation = Quaternion.identity;
        effect.transform.localScale = Vector3.one;

        LightningBoltScript lightning = effect.GetComponentInChildren<LightningBoltScript>();
        if (lightning != null)
        {
            lightning.StartObject = null;
            lightning.EndObject = null;
            lightning.StartPosition = start;
            lightning.EndPosition = end;
            lightning.ManualMode = false;
            lightning.UseOrthographicMode = false;
            lightning.Duration = Mathf.Max(0.01f, lightning.Duration);
        }

        LineRenderer lineRenderer = effect.GetComponentInChildren<LineRenderer>();
        if (lineRenderer != null)
        {
            lineRenderer.widthMultiplier = Mathf.Max(0.25f, thickness * EffectWidthMultiplier);
            lineRenderer.useWorldSpace = true;
            lineRenderer.startColor = EffectColor;
            lineRenderer.endColor = EffectColor;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
            if (lineRenderer.material != null)
            {
                if (lineRenderer.material.HasProperty("_Color"))
                {
                    lineRenderer.material.SetColor("_Color", EffectColor);
                }

                if (lineRenderer.material.HasProperty("_EmissionColor"))
                {
                    lineRenderer.material.SetColor("_EmissionColor", EffectColor);
                }

                lineRenderer.material.renderQueue = 5000;
            }
        }
    }

    private Vector3 GetPoint(Vector2Int point)
    {
        return board.GridPointToWorld(point) + plane.up * heightOffset;
    }

    private void Clear()
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Object.Destroy(root.GetChild(i).gameObject);
        }
    }

    private static Mesh CreateLineMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, 0f, -0.5f),
            new Vector3(0.5f, 0f, -0.5f),
            new Vector3(-0.5f, 0f, 0.5f),
            new Vector3(0.5f, 0f, 0.5f)
        };
        mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateNormals();
        return mesh;
    }
}
