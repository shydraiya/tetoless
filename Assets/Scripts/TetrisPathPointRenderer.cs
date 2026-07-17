using UnityEngine;

public sealed class TetrisPathPointRenderer
{
    private readonly TetrisBoard board;
    private readonly Transform plane;
    private readonly float markerSize;
    private readonly float heightOffset;
    private readonly Transform root;
    private readonly Material startMaterial;
    private readonly Material endMaterial;
    private GameObject startMarker;
    private GameObject endMarker;

    public TetrisPathPointRenderer(
        TetrisBoard board,
        Transform plane,
        Color startColor,
        Color endColor,
        float markerSize,
        float heightOffset)
    {
        this.board = board;
        this.plane = plane;
        this.markerSize = markerSize;
        this.heightOffset = heightOffset;
        root = new GameObject("Tetris Path Points").transform;
        startMaterial = CreateMaterial(startColor);
        endMaterial = CreateMaterial(endColor);
    }

    public void Draw(Vector2Int startPoint, Vector2Int endPoint)
    {
        startMarker = DrawMarker(startMarker, startPoint, startMaterial, "Start Point");
        endMarker = DrawMarker(endMarker, endPoint, endMaterial, "End Point");
    }

    public void Destroy()
    {
        if (root != null)
        {
            Object.Destroy(root.gameObject);
        }

        Object.Destroy(startMaterial);
        Object.Destroy(endMaterial);
    }

    private GameObject DrawMarker(GameObject marker, Vector2Int point, Material material, string markerName)
    {
        if (marker == null)
        {
            marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = markerName;
            marker.transform.SetParent(root, false);
            if (marker.TryGetComponent(out Collider markerCollider))
            {
                Object.Destroy(markerCollider);
            }
        }

        marker.transform.position = board.GridPointToWorld(point) + plane.up * heightOffset;
        marker.transform.rotation = plane.rotation;
        marker.transform.localScale = new Vector3(markerSize, 0.03f, markerSize);
        marker.GetComponent<Renderer>().sharedMaterial = material;
        return marker;
    }

    private Material CreateMaterial(Color color)
    {
        Material material = new Material(Shader.Find("Standard"));
        material.color = color;
        return material;
    }
}
