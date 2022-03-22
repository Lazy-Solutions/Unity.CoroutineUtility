#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Lazy.Utility.Editor
{

    [CustomEditor(typeof(CoroutineRunner))]
    internal class CoroutineRunnerEditor : UnityEditor.Editor
    {

        void OnEnable() =>
            CoroutineRunner.OnListChanged += Repaint;

        void OnDisable() =>
            CoroutineRunner.OnListChanged -= Repaint;

        static readonly Dictionary<GlobalCoroutine, bool> expanded = new Dictionary<GlobalCoroutine, bool>();

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

            if (!expanded.ContainsKey(coroutine))
                expanded.Add(coroutine, false);

            var header = string.IsNullOrWhiteSpace(coroutine.description)
                    ? coroutine.caller.method?.Name
                    : coroutine.description;

            if (coroutine.isPaused)
                header += " [Paused]";

            _ = EditorGUILayout.BeginHorizontal();
            expanded[coroutine] = EditorGUILayout.BeginFoldoutHeaderGroup(
                foldout: expanded[coroutine],
                content: header);

            if (expanded[coroutine])
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
