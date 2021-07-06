using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Lazy.Utility
{

    /// <summary>
    /// <para>An utility for running actions on the main thread.</para>
    /// <para>Only usable in play mode.</para>
    /// </summary>
    public static class MainThreadUtility
    {

        #region Invoke

        /// <summary>Queues the function to be run on the main thread, during the next frame.</summary>
        public static T Invoke<T>(Func<T> func) =>
            Invoke(func, mainThread: true);

        /// <summary>Queues the action to be run on the main thread, during the next frame.</summary>
        public static void Invoke(Action action) =>
            Invoke(action, mainThread: true);

        /// <param name="mainThread">Queues the function to be run on the main thread, during the next frame.</param>
        public static T Invoke<T>(this Func<T> func, bool mainThread = false)
        {

            if (func == null)
                return default;

            if (!mainThread)
                return func.Invoke();
            else
            {
                T value = default;
                Invoke(action: () => value = func.Invoke());
                return value;
            }

        }

        /// <param name="mainThread">Queues the action to be run on the main thread, during the next frame.</param>
        public static void Invoke(this Action action, bool mainThread = false)
        {

            if (action == null)
                return;

            if (mainThread)
                lock (executeOnMainThread)
                    executeOnMainThread.Add(action);
            else
                action.Invoke();

        }

        #endregion
        #region Coroutine

        static GlobalCoroutine coroutine;

        [RuntimeInitializeOnLoadMethod]
        static void OnLoad() =>
            Start();

        /// <summary>Starts main thread utility coroutine.</summary>
        public static void Start()
        {
            if (!IsRunning)
                coroutine = Coroutine().StartCoroutine(debugText: "Main Thread Dispatcher");
        }

        /// <summary>Stops main thread utility coroutine.</summary>
        public static void Stop() =>
            coroutine?.Stop();

        /// <summary>Gets if main thread utility is running.</summary>
        public static bool IsRunning =>
            coroutine?.isRunning ?? false;

        static IEnumerator Coroutine()
        {
            while (true)
            {
                Update();
                yield return null;
            }
        }

        #endregion

        //Invoke adds actions to here
        static readonly List<Action> executeOnMainThread = new List<Action>();

        //Update() copies actions to here, using lock, and then executes them
        static readonly List<Action> executeCopiedOnMainThread = new List<Action>();

        //Called from coroutine
        static void Update()
        {

            if (!executeOnMainThread.Any())
                return;

            executeCopiedOnMainThread.Clear();
            lock (executeOnMainThread)
            {
                executeCopiedOnMainThread.AddRange(executeOnMainThread);
                executeOnMainThread.Clear();
            }

            for (int i = 0; i < executeCopiedOnMainThread.Count; i++)
                executeCopiedOnMainThread[i]?.Invoke();

        }

    }

}
