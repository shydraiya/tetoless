using UnityEngine;
using System.Collections.Generic;

public sealed class TetrisPathPointRenderer
{
    private readonly TetrisBoard board;
    private readonly Transform plane;
    private readonly float markerSize;
    private readonly float heightOffset;
    private readonly GameObject dangerPrefab;
    private readonly float dangerPrefabScale;
    private readonly Vector3 dangerPrefabRotation;
    private readonly Transform root;
    private readonly Material startMaterial;
    private readonly Material endMaterial;
    private readonly Material dangerMaterial;
    private GameObject startMarker;
    private GameObject endMarker;
    private readonly List<GameObject> startMarkers = new List<GameObject>();
    private readonly List<GameObject> endMarkers = new List<GameObject>();
    private readonly List<GameObject> dangerMarkers = new List<GameObject>();

    public IReadOnlyList<GameObject> StartMarkers => startMarkers;
    public IReadOnlyList<GameObject> EndMarkers => endMarkers;
    public IReadOnlyList<GameObject> DangerMarkers => dangerMarkers;

    public TetrisPathPointRenderer(
        TetrisBoard board,
        Transform plane,
        Color startColor,
        Color endColor,
        Color dangerColor,
        float markerSize,
        float heightOffset,
        GameObject dangerPrefab,
        float dangerPrefabScale,
        Vector3 dangerPrefabRotation)
    {
        this.board = board;
        this.plane = plane;
        this.markerSize = markerSize;
        this.heightOffset = heightOffset;
        this.dangerPrefab = dangerPrefab;
        this.dangerPrefabScale = dangerPrefabScale;
        this.dangerPrefabRotation = dangerPrefabRotation;
        root = new GameObject("Tetris Path Points").transform;
        startMaterial = CreateMaterial(startColor);
        endMaterial = CreateMaterial(endColor);
        dangerMaterial = CreateMaterial(dangerColor);
    }

    public void Draw(Vector2Int startPoint, Vector2Int endPoint, Vector2Int[] dangerZones)
    {
        startMarker = DrawMarker(startMarker, startPoint, startMaterial, "Start Point");
        endMarker = DrawMarker(endMarker, endPoint, endMaterial, "End Point");
        startMarkers.Clear();
        endMarkers.Clear();
        startMarkers.Add(startMarker);
        endMarkers.Add(endMarker);
        DrawDangerZones(dangerZones);
    }

    public void Destroy()
    {
        if (root != null)
        {
            Object.Destroy(root.gameObject);
        }

        Object.Destroy(startMaterial);
        Object.Destroy(endMaterial);
        Object.Destroy(dangerMaterial);
    }

    private void DrawDangerZones(Vector2Int[] dangerZones)
    {
        for (int i = 0; i < dangerMarkers.Count; i++)
        {
            if (dangerMarkers[i] != null)
            {
                Object.Destroy(dangerMarkers[i]);
            }
        }

        dangerMarkers.Clear();
        for (int i = 0; i < dangerZones.Length; i++)
        {
            dangerMarkers.Add(DrawDangerMarker(dangerZones[i]));
        }
    }

    private GameObject DrawDangerMarker(Vector2Int point)
    {
        if (dangerPrefab == null)
        {
            return DrawMarker(null, point, dangerMaterial, "Danger Zone");
        }

        GameObject marker = Object.Instantiate(dangerPrefab, root);
        marker.name = "Danger Zone Effect";
        marker.transform.position = board.GridPointToWorld(point) + plane.up * heightOffset;
        marker.transform.rotation = plane.rotation * Quaternion.Euler(dangerPrefabRotation);
        marker.transform.localScale = Vector3.one * dangerPrefabScale;
        return marker;
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
        Material material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        return material;
    }
}
