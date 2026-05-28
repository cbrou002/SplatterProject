using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

namespace Unity.AI.Assistant.PlayModeTest
{
    [InitializeOnLoad]
    internal static class PlayModeTestRunner
    {
        private const string StateKey = "PlayModeTest.State";
        private const string ResultKey = "PlayModeTest.Result";
        private const string ScriptPathKey = "PlayModeTest.ScriptPath";

        private static readonly int WaitFrames = SessionState.GetInt("PlayModeTest.WaitFrames", 5);
        private static readonly float TestTimeout = SessionState.GetFloat("PlayModeTest.TestTimeout", 10.0f);

        private static List<string> _capturedLogs = new List<string>();

        static PlayModeTestRunner()
        {
            string state = SessionState.GetString(StateKey, "Idle");
            switch (state)
            {
                case "Idle": break;
                case "WaitingForCompile":
                    EditorApplication.delayCall += () => {
                        SessionState.SetString(StateKey, "EnteringPlayMode");
                        EditorApplication.isPlaying = true;
                    };
                    break;
                case "EnteringPlayMode":
                    if (EditorApplication.isPlaying) {
                        SessionState.SetString(StateKey, "InPlayMode");
                        EditorApplication.update += WaitFramesThenRun;
                    }
                    break;
                case "InPlayMode":
                    if (EditorApplication.isPlaying) EditorApplication.update += WaitFramesThenRun;
                    break;
                case "Done":
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

            if (!_setupDone) {
                _setupDone = true;
                Application.logMessageReceived += OnLogMessage;
                _testStartTime = EditorApplication.timeSinceStartup;
                try { Setup(); } catch (System.Exception e) { FinishTest(true, e.Message); }
                return;
            }

            float elapsed = (float)(EditorApplication.timeSinceStartup - _testStartTime);
            bool timedOut = elapsed >= TestTimeout;
            try {
                bool complete = Tick(elapsed);
                if (complete || timedOut) FinishTest(timedOut && !complete, timedOut ? "Timeout" : null);
            } catch (System.Exception e) { FinishTest(true, e.Message); }
        }

        private static void FinishTest(bool isError, string errorMessage)
        {
            _testDone = true;
            EditorApplication.update -= WaitFramesThenRun;
            Application.logMessageReceived -= OnLogMessage;
            SessionState.SetString(ResultKey, GetResult());
            SessionState.SetString(StateKey, "Done");
            EditorApplication.isPlaying = false;
        }

        private static void OnLogMessage(string message, string stackTrace, LogType type)
        {
            _capturedLogs.Add("[" + type + "] " + message);
        }

        private static void SelfDestruct()
        {
            string scriptPath = SessionState.GetString(ScriptPathKey, "");
            if (!string.IsNullOrEmpty(scriptPath) && AssetDatabase.AssetPathExists(scriptPath)) AssetDatabase.DeleteAsset(scriptPath);
            SessionState.EraseString(StateKey);
        }

        [System.Serializable]
        private class TestResult {
            public bool success;
            public string[] logs;
            public int splatterCount;
        }

        private static ChainsawWeapon _chainsaw;
        private static GameObject _dummy;

        private static void Setup()
        {
            _chainsaw = Object.FindObjectOfType<ChainsawWeapon>();
            _dummy = GameObject.FindWithTag("Dummy");
            if (_chainsaw != null) _chainsaw.SendMessage("SetChainsawActive", true);
        }

        private static bool Tick(float elapsed)
        {
            if (_chainsaw == null || _dummy == null) return true;

            if (_chainsaw.fpsCamera != null) {
                _chainsaw.fpsCamera.transform.position = _dummy.transform.position + Vector3.back * 1.5f + Vector3.up * 1.5f;
                _chainsaw.fpsCamera.transform.LookAt(_dummy.transform.position + Vector3.up * 1.5f);
            }

            var projectors = GameObject.FindObjectsOfType<DecalProjector>();
            foreach(var p in projectors) {
                if (p.gameObject.name.Contains("Splatter")) return true;
            }

            return elapsed > 5.0f;
        }

        private static string GetResult()
        {
            int count = 0;
            var projectors = GameObject.FindObjectsOfType<DecalProjector>();
            foreach(var p in projectors) {
                if (p.gameObject.name.Contains("Splatter")) count++;
            }

            return JsonUtility.ToJson(new TestResult {
                success = count > 0,
                splatterCount = count,
                logs = _capturedLogs.ToArray()
            });
        }
    }
}
