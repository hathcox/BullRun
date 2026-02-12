#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

/// <summary>
/// Pipeline executor for the Setup-Oriented Generation Framework.
/// Discovers all [SetupClass] types via reflection, sorts by phase then order,
/// deletes Assets/_Generated/, and executes each class's static Execute() method.
/// F5 hotkey bound via [Shortcut] attribute.
/// </summary>
public static class SetupPipeline
{
    [MenuItem("BullRun/F5 Rebuild")]
    [Shortcut("BullRun/F5 Rebuild", KeyCode.F5)]
    public static void RunAll()
    {
        Debug.Log("[Setup] F5 rebuild starting...");

        var setupClasses = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return Type.EmptyTypes; }
            })
            .Where(t => t.GetCustomAttribute<SetupClassAttribute>() != null)
            .OrderBy(t => t.GetCustomAttribute<SetupClassAttribute>().Phase)
            .ThenBy(t => t.GetCustomAttribute<SetupClassAttribute>().Order)
            .ToList();

        if (setupClasses.Count == 0)
        {
            Debug.LogWarning("[Setup] No [SetupClass] types found. Nothing to execute.");
            return;
        }

        // Delete and recreate _Generated folder
        if (AssetDatabase.IsValidFolder("Assets/_Generated"))
            AssetDatabase.DeleteAsset("Assets/_Generated");
        AssetDatabase.CreateFolder("Assets", "_Generated");
        AssetDatabase.CreateFolder("Assets/_Generated", "Scenes");

        int executed = 0;
        foreach (var type in setupClasses)
        {
            var attr = type.GetCustomAttribute<SetupClassAttribute>();
            var method = type.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);

            if (method == null)
            {
                Debug.LogWarning($"[Setup] Skipping {type.Name} — no public static parameterless Execute() method found.");
                continue;
            }

            try
            {
                method.Invoke(null, null);
                Debug.Log($"[Setup] Executed: {type.Name} (Phase: {attr.Phase}, Order: {attr.Order})");
                executed++;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Setup] FAILED: {type.Name} — {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"[Setup] F5 rebuild complete. {executed}/{setupClasses.Count} setup classes executed.");
    }
}
#endif
