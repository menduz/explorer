using UnityEngine;

public static class CommonScriptableObjects
{
    private static FloatVariable renderTimeValue;
    public static FloatVariable renderTime => GetOrLoad(ref renderTimeValue, "ScriptableObjects/RenderTime");

    private static Vector3Variable playerUnityPositionValue;
    public static Vector3Variable playerUnityPosition => GetOrLoad(ref playerUnityPositionValue, "ScriptableObjects/PlayerUnityPosition");

    private static Vector3Variable playerUnityEulerAnglesValue;
    public static Vector3Variable playerUnityEulerAngles => GetOrLoad(ref playerUnityEulerAnglesValue, "ScriptableObjects/PlayerUnityEulerAngles");

    private static Vector3Variable playerUnityToWorldOffsetValue;
    public static Vector3Variable playerUnityToWorldOffset => GetOrLoad(ref playerUnityToWorldOffsetValue, "ScriptableObjects/PlayerUnityToWorldOffset");

    private static Vector2IntVariable playerCoordsValue;
    public static Vector2IntVariable playerCoords => GetOrLoad(ref playerCoordsValue, "ScriptableObjects/PlayerCoords");

    private static StringVariable sceneIDValue;
    public static StringVariable sceneID => GetOrLoad(ref sceneIDValue, "ScriptableObjects/SceneID");

    private static FloatVariable minimapZoomValue;
    public static FloatVariable minimapZoom => GetOrLoad(ref minimapZoomValue, "ScriptableObjects/MinimapZoom");

    private static Vector3NullableVariable characterForwardValue;
    public static Vector3NullableVariable characterForward => GetOrLoad(ref characterForwardValue, "ScriptableObjects/CharacterForward");

    private static Vector3Variable cameraForwardValue;
    public static Vector3Variable cameraForward => GetOrLoad(ref cameraForwardValue, "ScriptableObjects/CameraForward");

    private static Vector3Variable cameraPositionValue;
    public static Vector3Variable cameraPosition => GetOrLoad(ref cameraPositionValue, "ScriptableObjects/CameraPosition");

    public static Vector3Variable cameraRight => GetOrLoad(ref cameraRightValue, "ScriptableObjects/CameraRight");
    private static Vector3Variable cameraRightValue;

    private static T GetOrLoad<T>(ref T variable, string path) where T : Object
    {
        if (variable == null)
        {
            variable = Resources.Load<T>(path);
        }

        return variable;
    }
}
