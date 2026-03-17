using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildScript
{
    private static int _errorCount;

    public static void BuildMac()
    {
        _errorCount = 0;
        Application.logMessageReceived += OnLog;

        try
        {
            Debug.Log("[CI] Start: Addressables + Player");

            PreClean();
            BuildAddressables();
            BuildPlayer();

            Debug.Log("[CI] Build success");

            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorApplication.Exit(1);
        }
        finally
        {
            Application.logMessageReceived -= OnLog;
        }
    }

    static void PreClean()
    {
        Debug.Log("[CI] Pre-clean");

        if (Directory.Exists("Builds"))
            Directory.Delete("Builds", true);

        Directory.CreateDirectory("Builds/Mac");

        try
        {
            AddressableAssetSettings.CleanPlayerContent(null);
        }
        catch
        {
            Debug.Log("[CI] Addressables clean skipped");
        }
    }

    static void BuildAddressables()
    {
        Debug.Log("[CI] Build Addressables");

        var settings = AddressableAssetSettingsDefaultObject.Settings;

        if (settings == null)
        {
            Debug.Log("[CI] No Addressables settings, skipping");
            return;
        }

        AddressableAssetSettings.BuildPlayerContent();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void BuildPlayer()
    {
        Debug.Log("[CI] Build Player");

        var scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
            throw new Exception("No scenes in build settings");

        var report = BuildPipeline.BuildPlayer(
            scenes,
            "Builds/Mac/Game.app",
            BuildTarget.StandaloneOSX,
            BuildOptions.None
        );

        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Player build failed");

        Debug.Log("[CI] Player build complete");
    }

    static void OnLog(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception)
            _errorCount++;
    }
}