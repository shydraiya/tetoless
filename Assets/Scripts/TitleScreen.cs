using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TitleScreen : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Game";
    [SerializeField] private GameObject backgroundPrefab;
    [SerializeField] private string titleText = "TETOLESS";
    [SerializeField] private string promptText = "PRESS ANY KEY TO START";
    [SerializeField] private bool useTransitionEffect;
    [SerializeField, Min(0.05f)] private float fadeDuration = 0.45f;
    [SerializeField, Min(0)] private int flashCount = 2;
    [SerializeField, Min(0.02f)] private float flashDuration = 0.08f;

    private float overlayAlpha;
    private bool isTransitioning;

    private void Start()
    {
        SetupBackground();
    }

    private void Update()
    {
        if (isTransitioning || Keyboard.current == null || !Keyboard.current.anyKey.wasPressedThisFrame)
        {
            return;
        }

        if (useTransitionEffect)
        {
            StartCoroutine(PlayTransitionThenLoad());
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    private IEnumerator PlayTransitionThenLoad()
    {
        isTransitioning = true;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            overlayAlpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        for (int i = 0; i < flashCount; i++)
        {
            overlayAlpha = 0f;
            yield return new WaitForSeconds(flashDuration);
            overlayAlpha = 1f;
            yield return new WaitForSeconds(flashDuration);
        }

        overlayAlpha = 1f;
        SceneManager.LoadScene(gameSceneName);
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

        if (overlayAlpha <= 0f)
        {
            return;
        }

        Color previousColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, overlayAlpha);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = previousColor;
    }
}
