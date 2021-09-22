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
    public static class CoroutineUtility
    {

        public static class Events
        {

            static bool m_enableEvents;
            /// <summary>Enables or disables events. Setter not available, and getter always returns false, in build. Default is <see langword="false"/>.</summary>
            public static bool enableEvents
            {
                get
                {
#if UNITY_EDITOR
                    return m_enableEvents;
#else
                    return false;
#endif
                }
#if UNITY_EDITOR
                set => m_enableEvents = value;
#endif
            }

            public delegate void CoroutineEvent(GlobalCoroutine coroutine);
            public delegate object CoroutineFrameStartEvent(GlobalCoroutine coroutine, object data, int level, object parentUserData, bool isPause);
            public delegate object CoroutineFrameEndEvent(GlobalCoroutine coroutine, object userData);

            public static CoroutineEvent onCreated;
            public static CoroutineEvent onDestroyed;
            public static CoroutineEvent onStopped;

            public static CoroutineEvent onCoroutineStarted;
            public static CoroutineEvent onCoroutineEnded;
            public static CoroutineFrameStartEvent onCoroutineFrameStart;
            public static CoroutineFrameEndEvent onCoroutineFrameEnd;

        }

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
        public static GlobalCoroutine StartCoroutineGlobal(this MonoBehaviour _, IEnumerator coroutine, Action onComplete = null, string description = "", [CallerFilePath] string callerFile = "", [CallerLineNumber] int callerLine = 0) =>
            StartCoroutine(coroutine, onComplete, description, callerFile, callerLine);

        /// <summary>
        /// <para>Runs the coroutine using <see cref="CoroutineUtility"/>, which means it won't be tied to this <see cref="MonoBehaviour"/> and will persist through scene close.</para>
        /// <para>You may yield return this method.</para>
        /// </summary>
        public static GlobalCoroutine StartCoroutine(this IEnumerator coroutine, Action onComplete = null, string description = "", [CallerFilePath] string callerFile = "", [CallerLineNumber] int callerLine = 0)
        {

            if (!Application.isPlaying)
                return null;

            if (coroutine == null)
                return null;

            var c = GlobalCoroutine.Get(onComplete, (GetCaller(), callerFile.Replace("\\", "/"), callerLine), description);
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
