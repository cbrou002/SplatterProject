using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

namespace Unity.AI.Assistant.PlayModeTest
{
    [InitializeOnLoad]
    internal static class PlayModeTestRunner
    {
        private const string StateKey = "PlayModeTest.State";
        private const string ResultKey = "PlayModeTest.Result";
        private const string ScriptPathKey = "PlayModeTest.ScriptPath";
        private const string SentinelLog = "PLAY_MODE_TEST_COMPLETE";

        private static readonly int WaitFrames = SessionState.GetInt("PlayModeTest.WaitFrames", 10);
        private static readonly float TestTimeout = SessionState.GetFloat("PlayModeTest.TestTimeout", 15.0f);

        private static List<string> _capturedLogs = new List<string>();
        private const int MaxCapturedLogs = 50;

        static PlayModeTestRunner()
        {
            string state = SessionState.GetString(StateKey, "Idle");

            switch (state)
            {
                case "Idle":
                    break;

                case "WaitingForCompile":
                    EditorApplication.delayCall += () =>
                    {
                        SessionState.SetString(StateKey, "EnteringPlayMode");
                        EditorApplication.isPlaying = true;
                    };
                    break;

                case "EnteringPlayMode":
                    if (EditorApplication.isPlaying)
                    {
                        SessionState.SetString(StateKey, "InPlayMode");
                        EditorApplication.update += WaitFramesThenRun;
                    }
                    break;

                case "InPlayMode":
                    if (EditorApplication.isPlaying)
                    {
                        EditorApplication.update += WaitFramesThenRun;
                    }
                    break;

                case "Done":
                    Debug.Log(SentinelLog);
                    EditorApplication.delayCall += SelfDestruct;
                    break;
            }
        }

        private static int _frameCount = 0;
        private static bool _setupDone = false;
        private static bool _testDone = false;
        private static double _testStartTime = 0;

        private static void WaitFramesThenRun()
        {
            _frameCount++;
            if (_frameCount < WaitFrames) return;
            if (_testDone) return;

            if (!_setupDone)
            {
                _setupDone = true;
                Application.logMessageReceived += OnLogMessage;
                _testStartTime = EditorApplication.timeSinceStartup;
                try { Setup(); }
                catch (System.Exception e) { FinishTest(true, e.Message); }
                return;
            }

            float elapsed = (float)(EditorApplication.timeSinceStartup - _testStartTime);
            bool timedOut = elapsed >= TestTimeout;

            try
            {
                bool complete = Tick(elapsed);
                if (complete || timedOut)
                {
                    FinishTest(timedOut && !complete, timedOut ? "Test timed out" : null);
                }
            }
            catch (System.Exception e) { FinishTest(true, e.Message); }
        }

        private static void FinishTest(bool isError, string errorMessage)
        {
            _testDone = true;
            EditorApplication.update -= WaitFramesThenRun;
            Application.logMessageReceived -= OnLogMessage;
            string resultJson = GetResult();
            SessionState.SetString(ResultKey, resultJson);
            SessionState.SetString(StateKey, "Done");
            EditorApplication.isPlaying = false;
        }

        private static void OnLogMessage(string message, string stackTrace, LogType type)
        {
            if (_capturedLogs.Count >= MaxCapturedLogs) return;
            _capturedLogs.Add("[" + type + "] " + message);
        }

        private static void SelfDestruct()
        {
            string scriptPath = SessionState.GetString(ScriptPathKey, "");
            if (!string.IsNullOrEmpty(scriptPath) && AssetDatabase.AssetPathExists(scriptPath))
                AssetDatabase.DeleteAsset(scriptPath);
            SessionState.EraseString(StateKey);
            SessionState.EraseString(ScriptPathKey);
        }

        [System.Serializable]
        private class TestResult
        {
            public bool success;
            public int dropletCount;
            public string error;
            public string[] logs;
        }

        private static GameObject _dripInstance;

        private static void Setup()
        {
            Debug.Log("[Test] Starting PlayMode diagnosis...");
            
            // 1. Create a floor if none exists near origin
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(10, 1, 10);
            floor.name = "TestFloor";
            
            // 2. Load the prefab
            string prefabPath = "Assets/Prefabs/BloodDrip.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (prefab == null) {
                Debug.LogError("[Test] BloodDrip prefab not found at " + prefabPath);
                return;
            }

            // 3. Spawn the drip 2 meters above the floor
            _dripInstance = Object.Instantiate(prefab, new Vector3(0, 2f, 0), Quaternion.identity);
            Debug.Log("[Test] Spawned BloodDrip at " + _dripInstance.transform.position);
        }

        private static bool Tick(float elapsed)
        {
            // Wait for 6 seconds to allow multiple drips
            if (elapsed < 6.0f) return false;
            
            int count = GameObject.FindObjectsByType<DecalProjector>(FindObjectsSortMode.None).Length;
            // Subtract any projectors that might be on the drip itself if they existed (none expected)
            // but we named droplets "BloodDroplet" in the spawner.
            
            int dropletNameCount = 0;
            foreach(var go in GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None)) {
                if (go.name.Contains("BloodDroplet")) dropletNameCount++;
            }

            Debug.Log("[Test] Found " + count + " total DecalProjectors in scene.");
            Debug.Log("[Test] Found " + dropletNameCount + " objects named 'BloodDroplet'.");
            
            return true;
        }

        private static string GetResult()
        {
            int dropletNameCount = 0;
            foreach(var go in GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None)) {
                if (go.name.Contains("BloodDroplet")) dropletNameCount++;
            }

            var result = new TestResult
            {
                success = dropletNameCount > 0,
                dropletCount = dropletNameCount,
                logs = _capturedLogs.ToArray()
            };
            return JsonUtility.ToJson(result);
        }
    }
}
