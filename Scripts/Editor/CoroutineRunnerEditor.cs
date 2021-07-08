#if UNITY_EDITOR

using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Lazy.Utility
{

    [CustomEditor(typeof(CoroutineRunner))]
    internal class CoroutineRunnerEditor : Editor
    {

        void OnEnable() => CoroutineUtility.coroutineCompleted += CoroutineUtility_coroutineCompleted;
        void OnDisable() => CoroutineUtility.coroutineCompleted -= CoroutineUtility_coroutineCompleted;

        void CoroutineUtility_coroutineCompleted(GlobalCoroutine couroutine) => Repaint();

        static readonly Dictionary<(MethodBase method, string file, int line), bool> expanded = new Dictionary<(MethodBase method, string file, int line), bool>();
        public override void OnInspectorGUI()
        {

            UnfocusOnClick();

            EditorGUILayout.Space();

            if (target is CoroutineRunner runner)
                foreach (var coroutine in runner.coroutines)
                    DrawCoroutine(coroutine);

        }

        void DrawCoroutine(GlobalCoroutine coroutine)
        {

            if (!expanded.ContainsKey(coroutine.caller))
                expanded.Add(coroutine.caller, false);

            var header = string.IsNullOrWhiteSpace(coroutine.debugText)
                    ? coroutine.caller.method?.Name
                    : coroutine.debugText;

            if (coroutine.isPaused)
                header += " [Paused]";

            EditorGUILayout.BeginHorizontal();
            expanded[coroutine.caller] = EditorGUILayout.BeginFoldoutHeaderGroup(
                foldout: expanded[coroutine.caller],
                content: header);

            if (expanded[coroutine.caller])
            {

                if (!coroutine.isPaused && GUILayout.Button("Pause", GUILayout.ExpandWidth(false)))
                    coroutine.Pause();
                else if (coroutine.isPaused && GUILayout.Button("Resume", GUILayout.ExpandWidth(false)))
                    coroutine.Resume();

                if (GUILayout.Button("View in code editor", GUILayout.ExpandWidth(false)))
                    coroutine.ViewCallerInCodeEditor();
                EditorGUILayout.EndHorizontal();

                var file = coroutine.caller.file.Remove(0, Application.dataPath.Replace("/Assets", "").Length + 1);

                EditorGUILayout.LabelField("Method:", coroutine.caller.method.DeclaringType.FullName + "." + coroutine.caller.method.Name + "()");
                EditorGUILayout.LabelField("File:", file, new GUIStyle(EditorStyles.label) { wordWrap = true });
                EditorGUILayout.LabelField("Line:", coroutine.caller.line.ToString());

                EditorGUILayout.Space();

            }
            else
                EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndFoldoutHeaderGroup();

        }

        void UnfocusOnClick()
        {
            if (Event.current.type == EventType.MouseDown)
                GUI.FocusControl("");
        }

    }

}
#endif
