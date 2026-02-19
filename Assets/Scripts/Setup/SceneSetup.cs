#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creates the base scene during F5 rebuild: camera, root canvas, and saves to _Generated.
/// Runs first in SceneComposition phase so all other setup classes can parent to the scene.
/// </summary>
[SetupClass(SetupPhase.SceneComposition, order: 0)]
public static class SceneSetup
{
    public static void Execute()
    {
        // Create a fresh empty scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);

        // Main Camera — 2D orthographic for URP 2D
        var camGo = new GameObject("MainCamera");
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0f, 0f, -10f); // Pull back so z=0 objects are visible
        var cam = camGo.AddComponent<Camera>();
        camGo.AddComponent<AudioListener>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.backgroundColor = CRTThemeData.Background; // Story 14.6: CRT dark background
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100f;

        // Root game canvas — ScreenSpaceOverlay, all UI parents to this
        var canvasGo = new GameObject("GameCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Save scene to _Generated
        EditorSceneManager.SaveScene(scene, "Assets/_Generated/Scenes/MainScene.unity");

        Debug.Log("[Setup] SceneSetup: Camera + GameCanvas created, scene saved.");
    }
}
#endif
