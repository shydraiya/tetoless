using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public sealed class KeyBindingMenu : MonoBehaviour
{
    private enum BindingTarget
    {
        None,
        Hold,
        Clockwise,
        CounterClockwise
    }

    [Header("Panel")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Button Labels")]
    [SerializeField] private TMP_Text holdKeyText;
    [SerializeField] private TMP_Text clockwiseKeyText;
    [SerializeField] private TMP_Text counterClockwiseKeyText;
    [SerializeField] private TMP_Text statusText;

    private Key holdKey;
    private Key clockwiseKey;
    private Key counterClockwiseKey;
    private BindingTarget waitingFor = BindingTarget.None;

    private void Awake()
    {
        LoadSavedBindings();
        RefreshLabels();
        SetStatus(string.Empty);
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (waitingFor == BindingTarget.None || Keyboard.current == null)
        {
            return;
        }

        Key pressedKey = GetPressedKey();
        if (pressedKey == Key.None)
        {
            return;
        }

        TryAssignKey(pressedKey);
    }

    public void Open()
    {
        LoadSavedBindings();
        RefreshLabels();
        SetStatus(string.Empty);
        waitingFor = BindingTarget.None;
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    public void Close()
    {
        waitingFor = BindingTarget.None;
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void RebindHold()
    {
        StartWaiting(BindingTarget.Hold, "Press a key for Store");
    }

    public void RebindClockwise()
    {
        StartWaiting(BindingTarget.Clockwise, "Press a key for Rotate CW");
    }

    public void RebindCounterClockwise()
    {
        StartWaiting(BindingTarget.CounterClockwise, "Press a key for Rotate CCW");
    }

    public void Save()
    {
        KeyBindingSettings.Save(holdKey, clockwiseKey, counterClockwiseKey);
        waitingFor = BindingTarget.None;
        SetStatus("Saved");
    }

    public void ResetToDefaults()
    {
        holdKey = KeyBindingSettings.DefaultHoldKey;
        clockwiseKey = KeyBindingSettings.DefaultClockwiseKey;
        counterClockwiseKey = KeyBindingSettings.DefaultCounterClockwiseKey;
        waitingFor = BindingTarget.None;
        RefreshLabels();
        SetStatus("Defaults restored. Press Save to keep them.");
    }

    private void LoadSavedBindings()
    {
        holdKey = KeyBindingSettings.HoldKey;
        clockwiseKey = KeyBindingSettings.ClockwiseKey;
        counterClockwiseKey = KeyBindingSettings.CounterClockwiseKey;
    }

    private void StartWaiting(BindingTarget target, string message)
    {
        waitingFor = target;
        SetStatus(message);
    }

    private void TryAssignKey(Key key)
    {
        if (IsDuplicate(key, waitingFor))
        {
            SetStatus($"{key} is already assigned.");
            waitingFor = BindingTarget.None;
            return;
        }

        switch (waitingFor)
        {
            case BindingTarget.Hold:
                holdKey = key;
                break;
            case BindingTarget.Clockwise:
                clockwiseKey = key;
                break;
            case BindingTarget.CounterClockwise:
                counterClockwiseKey = key;
                break;
        }

        waitingFor = BindingTarget.None;
        RefreshLabels();
        SetStatus("Changed. Press Save to apply.");
    }

    private bool IsDuplicate(Key key, BindingTarget target)
    {
        return target != BindingTarget.Hold && key == holdKey
            || target != BindingTarget.Clockwise && key == clockwiseKey
            || target != BindingTarget.CounterClockwise && key == counterClockwiseKey;
    }

    private void RefreshLabels()
    {
        if (holdKeyText != null) holdKeyText.text = $"{holdKey}";
        if (clockwiseKeyText != null) clockwiseKeyText.text = $"{clockwiseKey}";
        if (counterClockwiseKeyText != null) counterClockwiseKeyText.text = $"{counterClockwiseKey}";
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private static Key GetPressedKey()
    {
        foreach (KeyControl keyControl in Keyboard.current.allKeys)
        {
            if (keyControl.wasPressedThisFrame)
            {
                return keyControl.keyCode;
            }
        }

        return Key.None;
    }
}
