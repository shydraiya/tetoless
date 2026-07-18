using UnityEngine;
using UnityEngine.InputSystem;

public static class KeyBindingSettings
{
    private const string HoldKeyPref = "KeyBinding.Hold";
    private const string ClockwiseKeyPref = "KeyBinding.Clockwise";
    private const string CounterClockwiseKeyPref = "KeyBinding.CounterClockwise";

    public const Key DefaultHoldKey = Key.S;
    public const Key DefaultClockwiseKey = Key.X;
    public const Key DefaultCounterClockwiseKey = Key.Z;

    public static Key HoldKey => LoadKey(HoldKeyPref, DefaultHoldKey);
    public static Key ClockwiseKey => LoadKey(ClockwiseKeyPref, DefaultClockwiseKey);
    public static Key CounterClockwiseKey => LoadKey(CounterClockwiseKeyPref, DefaultCounterClockwiseKey);

    public static void Save(Key holdKey, Key clockwiseKey, Key counterClockwiseKey)
    {
        PlayerPrefs.SetInt(HoldKeyPref, (int)holdKey);
        PlayerPrefs.SetInt(ClockwiseKeyPref, (int)clockwiseKey);
        PlayerPrefs.SetInt(CounterClockwiseKeyPref, (int)counterClockwiseKey);
        PlayerPrefs.Save();
    }

    public static void ResetToDefaults()
    {
        Save(DefaultHoldKey, DefaultClockwiseKey, DefaultCounterClockwiseKey);
    }

    private static Key LoadKey(string prefName, Key defaultKey)
    {
        int value = PlayerPrefs.GetInt(prefName, (int)defaultKey);
        if (!System.Enum.IsDefined(typeof(Key), value))
        {
            return defaultKey;
        }

        return (Key)value;
    }
}
