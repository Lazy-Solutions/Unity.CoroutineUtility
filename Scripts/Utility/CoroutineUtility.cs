#pragma warning disable IDE0051 // Remove unused private members

using UnityEngine;
using System.Collections;
using System;
using System.Linq;

using Object = UnityEngine.Object;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Lazy.Utility
{

    /// <summary>An utility class that helps with running coroutines unconstrained from <see cref="MonoBehaviour"/>.</summary>
    public static class CoroutineUtility
    {

        public static Action<GlobalCoroutine> coroutineStarted;
        public static Action<GlobalCoroutine> coroutineCompleted;

        internal static CoroutineRoot root;

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

            if (!root)
            {
                root = new GameObject("Coroutine Runner").AddComponent<CoroutineRoot>();
                Object.DontDestroyOnLoad(root.gameObject);
            }

            var obj = new GameObject();
            obj.transform.SetParent(root.transform);
            var runner = obj.AddComponent<CoroutineRunner>();
            var c = GlobalCoroutine.Get(runner, onComplete, (GetCaller(), callerFile.Replace("\\", "/"), callerLine), debugText);
            runner.Run(coroutine, c);

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
        public static void StopAllCoroutines()
        {
            var obj = Object.FindObjectsOfType<CoroutineRoot>();
            foreach (var o in obj)
                Object.DestroyImmediate(o.gameObject);
        }

#if UNITY_EDITOR
        [UnityEditor.Callbacks.DidReloadScripts]
        static void OnScriptsReloaded()
        {
            //Coroutines stop when script and editor state changes, which means that we'll have rogue CoroutineHelper objects in the scene, 
            //so let's remove them
            StopAllCoroutines();
        }
#endif

        public static IEnumerator WaitAll(params GlobalCoroutine[] coroutines) =>
            coroutines?.WaitAll();

        public static IEnumerator WaitAll(params IEnumerator[] coroutines) =>
            coroutines?.WaitAll();

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
