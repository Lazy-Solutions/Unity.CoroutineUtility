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

        /// <summary>Provides events for coroutine events.</summary>
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

            /// <param name="coroutine">The coroutine that this event was called for.</param>
            public delegate void CoroutineEvent(GlobalCoroutine coroutine);

            /// <param name="coroutine">The coroutine that this event was called for.</param>
            /// <param name="data">The object returned from <see cref="IEnumerator.Current"/>.</param>
            /// <param name="level">The level, or depth, of the current subroutine.</param>
            /// <param name="parentUserData">The userdata of the subroutine above this one, depth-wise.</param>
            /// <param name="isPause"><see cref="GlobalCoroutine.Pause"/> is reported as a subroutine, this is true when that is the case.</param>
            public delegate object CoroutineFrameStartEvent(GlobalCoroutine coroutine, object data, int level, object parentUserData, bool isPause);

            /// <param name="coroutine">The coroutine that this event was called for.</param>
            /// <param name="userData">The userdata that was passed to <see cref="onSubroutineStart"/>.</param>
            public delegate void CoroutineFrameEndEvent(GlobalCoroutine coroutine, object userData);

            /// <summary>Occurs when created. Note that <see cref="GlobalCoroutine"/> is pooled, the same object instance will be used multiple times, and this event is called when the pooled instance is 'constructed', meaning this event will be called multiple times for the same object instance.</summary>
            public static CoroutineEvent onCreated;

            /// <summary>Occurs when a <see cref="GlobalCoroutine"/> is 'destroyed'. Note that <see cref="GlobalCoroutine"/> is pooled, the same object instance will be used multiple times, and this event is called when the pooled instance is 'destroyed', meaning this event will be called multiple times for the same object instance.</summary>
            public static CoroutineEvent onDestroyed;

            /// <summary>Occurs when a <see cref="GlobalCoroutine"/> is started.</summary>
            public static CoroutineEvent onCoroutineStarted;
            /// <summary>Occurs when a <see cref="GlobalCoroutine"/> is ended.</summary>
            public static CoroutineEvent onCoroutineEnded;

            /// <summary>
            /// <para>Occurs before a subroutine in an executing <see cref="GlobalCoroutine"/> is started.</para>
            /// <para>A user object can be returned, which is then passed to <see cref="onSubroutineEnd"/>.</para>
            /// </summary>
            public static CoroutineFrameStartEvent onSubroutineStart;

            /// <summary>Occurs when a subroutine in an executing <see cref="GlobalCoroutine"/> has ended.</summary>
            public static CoroutineFrameEndEvent onSubroutineEnd;

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

        /// <summary>Runs the action after the specified time.</summary>
        public static void Run(Action action, TimeSpan after) =>
            Run(action, (float)after.TotalSeconds);

        /// <summary>Runs the action after the specified time.</summary>
        public static void Run(Action action, float after)
        {

            Coroutine()?.StartCoroutine();
            IEnumerator Coroutine()
            {
                yield return new WaitForSeconds(after);
                action?.Invoke();
            }

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
                method?.Invoke(null, new[] { CoroutineRunner.RunCoroutine(coroutine, c) });

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
