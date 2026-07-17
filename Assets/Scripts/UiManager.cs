using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class UiManager : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Key openPanelKey = Key.Escape;

    private void Reset()
    {
        if (panel != null)
        {
            return;
        }

        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child != transform && child.name == "Panel")
            {
                panel = child.gameObject;
                return;
            }
        }
    }

    private void Update()
    {
        if (panel == null || Keyboard.current == null)
        {
            return;
        }

        KeyControl key = Keyboard.current[openPanelKey];
        if (key != null && key.wasPressedThisFrame)
        {
            panel.SetActive(!panel.activeSelf);
        }
    }
}
