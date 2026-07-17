using System;
using UnityEngine;

[Serializable]
public struct TetrominoData
{
    public string Name { get; }
    public GameObject Prefab { get; }
    public Vector2Int[] Cells { get; }

    private TetrominoData(string name, GameObject prefab, Vector2Int[] cells)
    {
        Name = name;
        Prefab = prefab;
        Cells = cells;
    }

    public static TetrominoData FromPrefab(string name, GameObject prefab)
    {
        Transform[] cubes = GetCubes(prefab.transform);
        Vector2Int[] cells = new Vector2Int[cubes.Length];
        for (int i = 0; i < cubes.Length; i++)
        {
            Vector3 position = prefab.transform.InverseTransformPoint(cubes[i].position);
            cells[i] = new Vector2Int(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z));
        }

        return new TetrominoData(name, prefab, cells);
    }

    public int GetSpawnRow()
    {
        int minimum = int.MaxValue;
        foreach (Vector2Int cell in Cells)
        {
            minimum = Mathf.Min(minimum, cell.y);
        }

        return -minimum;
    }

    public static Vector2Int RotateCell(Vector2Int cell, int rotation)
    {
        // Must match Quaternion.Euler(0, rotation * 90, 0) used by the visual prefab.
        return rotation switch
        {
            1 => new Vector2Int(cell.y, -cell.x),
            2 => new Vector2Int(-cell.x, -cell.y),
            3 => new Vector2Int(-cell.y, cell.x),
            _ => cell
        };
    }

    public static Transform[] GetCubes(Transform root)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>();
        Transform[] cubes = new Transform[children.Length - 1];
        int count = 0;

        foreach (Transform child in children)
        {
            if (child == root || child.GetComponent<Renderer>() == null)
            {
                continue;
            }

            if (count < cubes.Length)
            {
                cubes[count++] = child;
            }
        }

        Array.Resize(ref cubes, count);
        return cubes;
    }
}

public sealed class ActiveTetrisPiece
{
    public int Type;
    public Vector2Int Position;
    public int Rotation;
    public GameObject Root;
}
