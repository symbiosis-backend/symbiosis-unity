using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MahjongGame.EditorTools
{
    [InitializeOnLoad]
    public static class EntryStartupSmokeTest
    {
        private const string ScenePath = "Assets/Scenes/Entry.unity";
        private const string EnvironmentFlag = "CODEX_ENTRY_SMOKE";
        private const double MaxSeconds = 8.0;

        private static double startTime;
        private static bool playModeEntered;
        private static bool failed;
        private static bool armed;

        static EntryStartupSmokeTest()
        {
            if (Environment.GetEnvironmentVariable(EnvironmentFlag) != "1")
                return;

            if (armed)
                return;

            armed = true;
            EditorApplication.delayCall += Run;
        }

        public static void Run()
        {
            Debug.Log("[EntryStartupSmokeTest] Starting.");
            failed = false;
            playModeEntered = false;
            startTime = EditorApplication.timeSinceStartup;

            Application.logMessageReceived += OnLogMessageReceived;
            EditorApplication.update += OnUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                playModeEntered = true;
                startTime = EditorApplication.timeSinceStartup;
                Debug.Log("[EntryStartupSmokeTest] Play mode entered.");
            }
        }

        private static void OnUpdate()
        {
            if (!playModeEntered)
                return;

            if (EditorApplication.timeSinceStartup - startTime < MaxSeconds)
                return;

            Finish();
        }

        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
                return;

            failed = true;
            Debug.LogError("[EntryStartupSmokeTest] Failure: " + condition + Environment.NewLine + stackTrace);
        }

        private static void Finish()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
            EditorApplication.update -= OnUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            if (EditorApplication.isPlaying)
                EditorApplication.ExitPlaymode();

            Debug.Log("[EntryStartupSmokeTest] Finished. Failed=" + failed);
            EditorApplication.Exit(failed ? 1 : 0);
        }
    }
}
