using UnityEngine;
using System.Collections;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Lazy.Utility
{

    /// <summary>An utility class that helps with running coroutines detached from <see cref="MonoBehaviour"/>.</summary>
    public static class CoroutineUtility
    {

        /// <summary>Occurs when a <see cref="GlobalCoroutine"/> is started.</summary>
        public static event Action<GlobalCoroutine> coroutineStarted;

        /// <summary>Occurs when a <see cref="GlobalCoroutine"/> is completed.</summary>
        public static event Action<GlobalCoroutine> coroutineCompleted;

        internal static void RaiseCoroutineStarted(GlobalCoroutine coroutine) => coroutineStarted?.Invoke(coroutine);
        internal static void RaiseCoroutineCompleted(GlobalCoroutine coroutine) => coroutineCompleted?.Invoke(coroutine);

        internal static CoroutineRunner m_runner;
        static CoroutineRunner Runner()
        {

            if (m_runner)
                return m_runner;

            var obj = new GameObject("Coroutine runner");
            Object.DontDestroyOnLoad(obj);
            m_runner = obj.AddComponent<CoroutineRunner>();
            return m_runner;

        }

        /// <summary>
        /// <para>Runs the coroutine using <see cref="CoroutineUtility"/>, which means it won't be tied to this <see cref="MonoBehaviour"/> and will persist through scene close.</para>
        /// <para>You may yield return this method.</para>
        /// </summary>
        public static GlobalCoroutine StartCoroutineGlobal(this MonoBehaviour _, IEnumerator coroutine, Action onComplete = null, string debugText = "", [CallerFilePath] string callerFile = "", [CallerLineNumber] int callerLine = 0) =>
            StartCoroutine(coroutine, onComplete, debugText, callerFile, callerLine);

        /// <summary>
        /// <para>Runs the coroutine using <see cref="CoroutineUtility"/>, which means it won't be tied to this <see cref="MonoBehaviour"/> and will persist through scene close.</para>
        /// <para>You may yield return this method.</para>
        /// </summary>
        public static GlobalCoroutine StartCoroutine(this IEnumerator coroutine, Action onComplete = null, string debugText = "", [CallerFilePath] string callerFile = "", [CallerLineNumber] int callerLine = 0)
        {

            if (!Application.isPlaying)
                return null;

            if (coroutine == null)
                return null;

            var c = GlobalCoroutine.Get(onComplete, (GetCaller(), callerFile.Replace("\\", "/"), callerLine), debugText);
            Runner().Add(coroutine, c);

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

            var stackTrace = new StackTrace();
            var stackFrames = stackTrace.GetFrames();
            var callingFrame = stackFrames[2];

            return callingFrame.GetMethod();

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
            var coroutine = coroutines.Select(c => c.StartCoroutine(debugText: debugText)).ToArray();
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
