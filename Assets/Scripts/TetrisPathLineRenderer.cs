using UnityEngine;

public sealed class TetrisPathLineRenderer
{
    private readonly TetrisBoard board;
    private readonly Transform plane;
    private readonly Color color;
    private readonly float thickness;
    private readonly float heightOffset;
    private readonly Material material;
    private readonly Mesh lineMesh;
    private readonly Transform root;

    public TetrisPathLineRenderer(TetrisBoard board, Transform plane, Color color, float thickness, float heightOffset)
    {
        this.board = board;
        this.plane = plane;
        this.color = color;
        this.thickness = thickness;
        this.heightOffset = heightOffset;
        material = new Material(Shader.Find("Unlit/Color"));
        material.color = color;
        material.renderQueue = 5000;
        material.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        material.SetInt("_ZWrite", 0);
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
