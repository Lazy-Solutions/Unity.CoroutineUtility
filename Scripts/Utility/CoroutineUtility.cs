using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Lazy.Utility
{

    /// <summary>An utility class that helps with running coroutines detached from <see cref="MonoBehaviour"/>.</summary>
    public static partial class CoroutineUtility
    {

        internal static CoroutineRunner m_runner;
        static CoroutineRunner Runner()
        {

            if (m_runner)
                return m_runner;

            if (Object.FindObjectOfType<CoroutineRunner>() is CoroutineRunner runner && runner)
            {
                m_runner = runner;
                return m_runner;
            }

            var obj = new GameObject("Coroutine runner");
            Object.DontDestroyOnLoad(obj);
            m_runner = obj.AddComponent<CoroutineRunner>();
            return m_runner;

        }

        /// <summary>Runs the action after the specified time.</summary>
        public static void Run(Action action, TimeSpan after, [CallerFilePath] string callerFile = "", [CallerLineNumber] int callerLine = 0, [CallerMemberName] string callerName = "") =>
            Run(action, after: (float)after.TotalSeconds, false, null, callerFile, callerLine, callerName);

        /// <summary>Runs the action after the specified time.</summary>
        public static void Run(Action action, float? after = null, bool nextFrame = false, Func<bool> when = null, [CallerFilePath] string callerFile = "", [CallerLineNumber] int callerLine = 0, [CallerMemberName] string callerName = "")
        {

            var desc = "Run: " + callerName + "()";
            if (after.HasValue)
                desc += ", " + after.Value + "s";
            else if (nextFrame)
                desc += ", next frame";
            else if (when != null)
                desc += ", when condition is true";

            _ = Coroutine()?.StartCoroutine(null, desc, callerFile, callerLine);
            IEnumerator Coroutine()
            {

                if (after.HasValue)
                    yield return new WaitForSeconds(after.Value);
                else if (nextFrame)
                    yield return null;
                else if (when != null && !when.Invoke())
                    yield return null;

                action?.Invoke();

            }

        }

        /// <summary>Runs the coroutine using <see cref="CoroutineUtility"/>, which means it won't be tied to a <see cref="MonoBehaviour"/> and will persist through scene close.</summary>
        /// <remarks>You may yield return this method.</remarks>
        public static GlobalCoroutine StartCoroutineGlobal(this MonoBehaviour _, IEnumerator coroutine, Action onComplete = null, string description = "", [CallerFilePath] string callerFile = "", [CallerLineNumber] int callerLine = 0) =>
            StartCoroutine(coroutine, onComplete, description, callerFile, callerLine);

        /// <inheritdoc cref="StartCoroutineGlobal(MonoBehaviour, IEnumerator, Action, string, string, int)"/>
        public static GlobalCoroutine StartCoroutine(this IEnumerator coroutine, Action onComplete = null, string description = "", [CallerFilePath] string callerFile = "", [CallerLineNumber] int callerLine = 0)
        {

            if (coroutine == null)
                return null;

            var c = GlobalCoroutine.Get(onComplete, (GetCaller(), callerFile.Replace("\\", "/"), callerLine), description);

            if (Application.isPlaying)
                Runner().Add(coroutine, c);
            else
            {

                //If com.unity.editorcoroutines is installed, then we'll use that to provide editor functionality

                //Unity.EditorCoroutines.EditorCoroutineUtility.StartCoroutineOwnerless(IEnumerator);
                var type = Type.GetType("Unity.EditorCoroutines.Editor.EditorCoroutineUtility, Unity.EditorCoroutines.Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", throwOnError: false);
                var method = type?.GetMethod("StartCoroutineOwnerless");
                _ = (method?.Invoke(null, new[] { CoroutineRunner.RunCoroutine(coroutine, c) }));

            }

            return c;

        }

        /// <summary>Runs the coroutines in sequence, wrapped in a single <see cref="GlobalCoroutine"/>.</summary>
        public static GlobalCoroutine Chain(params Func<IEnumerator>[] coroutines)
        {

            return Coroutine().StartCoroutine();

            IEnumerator Coroutine()
            {
                foreach (var coroutine in coroutines)
                    yield return coroutine?.Invoke();
            }

        }

        /// <summary>Get caller of the current method.</summary>
        static MethodBase GetCaller()
        {

#if UNITY_WEBGL
            return null;
#else
            var stackTrace = new StackTrace();
            var stackFrames = stackTrace.GetFrames();
            var callingFrame = stackFrames[2];

            return callingFrame.GetMethod();
#endif

        }

        /// <summary>Stops the coroutine.</summary>
        public static void StopCoroutine(GlobalCoroutine coroutine) =>
            coroutine?.Stop();

        /// <summary>Stops all global coroutines.</summary>
        public static void StopAllCoroutines() =>
            Runner().Clear();

        /// <summary>Wait for all coroutines to complete.</summary>
        public static IEnumerator WaitAll(params IEnumerator[] coroutines) =>
            coroutines?.WaitAll();

        /// <summary>Wait for all coroutines to complete.</summary>
        public static IEnumerator WaitAll(this IEnumerable<IEnumerator> coroutines, Func<bool> isCancelled = null, string debugText = null)
        {
            var coroutine = coroutines.Select(c => c.StartCoroutine(description: debugText)).ToArray();
            while (coroutine.Any(c => !c.isComplete))
            {
                if (isCancelled?.Invoke() ?? false)
                {
                    foreach (var c in coroutine)
                        c.Stop();
                    yield break;
                }
                yield return null;
            }
        }

        /// <summary>Wait for all coroutines to complete.</summary>
        public static IEnumerator WaitAll(params GlobalCoroutine[] coroutines) =>
            coroutines?.WaitAll();

        /// <summary>Wait for all coroutines to complete.</summary>
        public static IEnumerator WaitAll(this GlobalCoroutine[] coroutines, Func<bool> isCancelled = null)
        {
            while (coroutines.Any(c => !c.isComplete))
            {

                if (isCancelled?.Invoke() ?? false)
                {
                    foreach (var c in coroutines)
                        c.Stop();
                    yield break;
                }

                yield return null;

            }
        }

    }

}
