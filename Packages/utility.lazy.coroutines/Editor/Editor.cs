#if UNITY_EDITOR

using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Lazy.Utility
{

    public class Editor : UnityEditor.Editor
    {

        [CustomEditor(typeof(CoroutineRunner))] internal class Editor1 : Editor { }
        [CustomEditor(typeof(CoroutineRoot))] internal class Editor2 : Editor { }

        public override void OnInspectorGUI()
        {

            if (target is CoroutineRunner helper)
                DrawCoroutine(helper.coroutine);
            else if (target is CoroutineRoot root)
                foreach (var coroutine in root.GetComponentsInChildren<CoroutineRunner>().Select(c => c.coroutine).ToArray())
                    DrawCoroutine(coroutine);

        }

        void DrawCoroutine(GlobalCoroutine coroutine)
        {

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("open", GUILayout.ExpandWidth(false)))
                ViewCallerInCodeEditor(coroutine);

            GUILayout.BeginVertical();
            GUILayout.Label(new GUIContent(coroutine.ToString(), coroutine.ToString()));
            if (!string.IsNullOrWhiteSpace(coroutine.debugText))
                GUILayout.Label(coroutine.debugText);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

        }

        public void ViewCallerInCodeEditor(GlobalCoroutine coroutine)
        {

            var relativePath =
                coroutine.caller.file.Contains("/Packages/")
                ? coroutine.caller.file.Substring(coroutine.caller.file.IndexOf("/Packages/") + 1)
                : "Assets" + coroutine.caller.file.Replace(Application.dataPath, "");

            if (AssetDatabase.LoadAssetAtPath<Object>(relativePath))
                AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<MonoScript>(relativePath), coroutine.caller.line, 0);
            else
                Debug.LogError($"Could not find '{relativePath}'");

        }

    }


}
#endif
