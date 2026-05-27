using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Unity.AI.Assistant.PlayModeTest
{
    [InitializeOnLoad]
    internal static class PlayModeTestRunner
    {
        private const string StateKey = "PlayModeTest.State";
        private const string ResultKey = "PlayModeTest.Result";
        private const string ScriptPathKey = "PlayModeTest.ScriptPath";

        static PlayModeTestRunner()
        {
            string state = SessionState.GetString(StateKey, "Idle");
            if (state == "WaitingForCompile")
            {
                EditorApplication.delayCall += () =>
                {
                    SessionState.SetString(StateKey, "InPlayMode");
                    EditorApplication.isPlaying = true;
                };
            }
            else if (state == "InPlayMode" && EditorApplication.isPlaying)
            {
                EditorApplication.update += RunTest;
            }
            else if (state == "Done")
            {
                EditorApplication.delayCall += SelfDestruct;
            }
        }

        private static int _frame = 0;
        private static void RunTest()
        {
            _frame++;
            if (_frame == 10)
            {
                Debug.Log("[Test] Forcing Shot...");
                var shotgun = Object.FindAnyObjectByType<ShotgunShoot>();
                var dummy = GameObject.Find("Dummy");
                if (shotgun != null && dummy != null)
                {
                    shotgun.fpsCamera.transform.position = dummy.transform.position + new Vector3(0, 1.5f, -1.5f);
                    shotgun.fpsCamera.transform.LookAt(dummy.transform.position + Vector3.up * 1.0f);
                    
                    var method = typeof(ShotgunShoot).GetMethod("Shoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                    method.Invoke(shotgun, null);
                }
                else
                {
                    Debug.LogError("[Test] Shotgun or Dummy not found!");
                }
            }

            if (_frame == 120) // After 2 seconds approx
            {
                var pool = Object.FindAnyObjectByType<BloodDropletPool>();
                int activeCount = 0;
                if (pool != null)
                {
                    var droplets = pool.GetComponentsInChildren<UnityEngine.Rendering.Universal.DecalProjector>(true);
                    foreach(var d in droplets) if(d.gameObject.activeInHierarchy) activeCount++;
                    Debug.Log($"[Test] Active droplets in pool: {activeCount}");
                }
                
                var res = new { success = activeCount > 0, count = activeCount };
                SessionState.SetString(ResultKey, JsonUtility.ToJson(res));
                SessionState.SetString(StateKey, "Done");
                EditorApplication.isPlaying = false;
            }
        }

        private static void SelfDestruct()
        {
            string path = SessionState.GetString(ScriptPathKey, "");
            if (!string.IsNullOrEmpty(path)) AssetDatabase.DeleteAsset(path);
            SessionState.EraseString(StateKey);
        }
    }
}
