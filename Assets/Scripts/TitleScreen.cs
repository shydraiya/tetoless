using UnityEngine;

public class TitleScreen : MonoBehaviour
{
    [SerializeField] private GameObject backgroundPrefab;
    [SerializeField] private string titleText = "TETOLESS";
    [SerializeField] private string promptText = "PRESS ANY KEY TO START";

    private void Start()
    {
        SetupBackground();
    }

    private void SetupBackground()
    {
        if (backgroundPrefab == null)
        {
            return;
        }

        GameObject background = Instantiate(backgroundPrefab, Vector3.zero, Quaternion.identity);
        background.name = "Title Background";

        SpriteRenderer renderer = background.GetComponentInChildren<SpriteRenderer>();
        Camera mainCamera = Camera.main;
        if (renderer == null || renderer.sprite == null || mainCamera == null)
        {
            return;
        }

        float cameraHeight = mainCamera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * mainCamera.aspect;
        Vector2 spriteSize = renderer.sprite.bounds.size;
        float scale = Mathf.Max(cameraWidth / spriteSize.x, cameraHeight / spriteSize.y);

        background.transform.position = new Vector3(mainCamera.transform.position.x, mainCamera.transform.position.y, 4f);
        background.transform.localScale = Vector3.one * scale;
        renderer.sortingOrder = -10;
    }

    private void OnGUI()
    {
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 64,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };

        GUIStyle promptStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.75f, 1f, 0.75f) }
        };

        GUI.Label(new Rect(0, Screen.height * 0.36f - 60f, Screen.width, 120f), titleText, titleStyle);
        GUI.Label(new Rect(0, Screen.height * 0.62f - 30f, Screen.width, 60f), promptText, promptStyle);
    }
}
