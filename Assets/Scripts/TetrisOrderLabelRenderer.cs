using UnityEngine;

public sealed class TetrisOrderLabelRenderer
{
    private readonly Color color;
    private readonly float characterSize;
    private readonly float heightOffset;
    private readonly Transform root;

    public TetrisOrderLabelRenderer(Color color, float characterSize, float heightOffset)
    {
        this.color = color;
        this.characterSize = characterSize;
        this.heightOffset = heightOffset;
        root = new GameObject("Order Labels").transform;
    }

    public void AddLabels(TetrominoData data, Transform root, Transform plane)
    {
        Transform[] cubes = TetrominoData.GetCubes(root);
        for (int i = 0; i < cubes.Length && i < data.OrderNumbers.Length; i++)
        {
            AddLabel(cubes[i], data.OrderNumbers[i], plane);
        }
    }

    public void Refresh()
    {
        if (root == null)
        {
            return;
        }

        foreach (TetrisOrderLabel label in root.GetComponentsInChildren<TetrisOrderLabel>())
        {
            label.Refresh();
        }
    }

    public void RemoveOrphanedLabels()
    {
        if (root == null)
        {
            return;
        }

        foreach (TetrisOrderLabel label in root.GetComponentsInChildren<TetrisOrderLabel>())
        {
            if (label.HasTarget)
            {
                continue;
            }

            Object.DestroyImmediate(label.gameObject);
        }
    }

    public void RemoveLabelForTarget(Transform target)
    {
        if (root == null || target == null)
        {
            return;
        }

        foreach (TetrisOrderLabel label in root.GetComponentsInChildren<TetrisOrderLabel>())
        {
            if (!label.Targets(target))
            {
                continue;
            }

            Object.DestroyImmediate(label.gameObject);
            return;
        }
    }

    public void Destroy()
    {
        if (root != null)
        {
            Object.Destroy(root.gameObject);
        }
    }

    private void AddLabel(Transform cube, int order, Transform plane)
    {
        GameObject labelObject = new GameObject("Order Label");
        labelObject.transform.SetParent(root, false);

        TextMesh text = labelObject.AddComponent<TextMesh>();
        text.text = order.ToString();
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.characterSize = characterSize;
        text.fontSize = 64;
        text.color = color;

        MeshRenderer renderer = labelObject.GetComponent<MeshRenderer>();
        renderer.sortingOrder = 10;

        TetrisOrderLabel label = labelObject.AddComponent<TetrisOrderLabel>();
        label.Initialize(cube, plane, heightOffset);
        label.Refresh();
    }
}

public sealed class TetrisOrderLabel : MonoBehaviour
{
    private Transform target;
    private Transform plane;
    private float heightOffset;
    public bool HasTarget => target != null;
    public bool Targets(Transform candidate) => target == candidate;

    public void Initialize(Transform target, Transform plane, float heightOffset)
    {
        this.target = target;
        this.plane = plane;
        this.heightOffset = heightOffset;
    }

    public void Refresh()
    {
        if (target == null || plane == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = target.position + plane.up * heightOffset;
        transform.rotation = GetCameraReadableRotation();
    }

    private Quaternion GetCameraReadableRotation()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return Quaternion.Euler(90f, 0f, 0f);
        }

        return Quaternion.LookRotation(mainCamera.transform.forward, mainCamera.transform.up);
    }
}
