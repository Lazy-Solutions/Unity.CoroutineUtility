using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static Lazy.Utility.CoroutineUtility;
using Object = UnityEngine.Object;

namespace Lazy.Utility
{

    internal static class GlobalCoroutinePool
    {

        static readonly List<GlobalCoroutine> pool = new List<GlobalCoroutine>();

        /// <summary>Gets a recycled instance of <see cref="GlobalCoroutine"/>, if none exist, a new instance will be created.</summary>
        internal static GlobalCoroutine Get(CoroutineRunner helper, Action onComplete, (MethodBase method, string file, int line) caller, string debugText)
        {
            Get(out var coroutine);
            coroutine.Construct(helper, onComplete, caller, debugText);
            return coroutine;
        }

        //Called by GlobalCoroutine finalizer
        internal static void ReturnToPool(GlobalCoroutine coroutine) =>
            pool.Add(coroutine);

        //Retreive or create instance
        static void Get(out GlobalCoroutine coroutine)
        {
            if (pool.Any())
            {
                coroutine = pool[0];
                pool.RemoveAt(0);
            }
            else
                coroutine = new GlobalCoroutine();
        }

    }

    public class GlobalCoroutine : CustomYieldInstruction
    {

        #region Pooled construction

        /// <summary>Gets <see cref="GlobalCoroutine"/> from pool.</summary>
        internal static GlobalCoroutine Get(CoroutineRunner helper, Action onComplete, (MethodBase method, string file, int line) caller, string debugText) =>
            GlobalCoroutinePool.Get(helper, onComplete, caller, debugText);

        /// <summary>Don't use this, <see cref="GlobalCoroutine"/> is pooled using <see cref="GlobalCoroutinePool"/>. Use <see cref="Get"/> instead.</summary>
        internal GlobalCoroutine()
        { }

        /// <summary>Clears out the fields of this <see cref="GlobalCoroutine"/>, used to prepare before returning to <see cref="GlobalCoroutinePool"/>.</summary>
        internal void Clear() =>
            Construct(null, null, default, null);

        /// <summary>'Constructs' an instance of <see cref="GlobalCoroutine"/>, <see cref="GlobalCoroutine"/> is pooled using <see cref="GlobalCoroutinePool"/>, this means the instances are recycled, so instead of using constructor, we call this.</summary>
        internal void Construct(CoroutineRunner helper, Action onComplete, (MethodBase method, string file, int line) caller, string debugText)
        {
            this.helper = helper;
            this.onComplete = onComplete;
            isPaused = false;
            isComplete = false;
            wasCancelled = false;
            this.caller = caller;
            this.debugText = debugText;
        }

        ~GlobalCoroutine()
        {
            Clear();
            GlobalCoroutinePool.ReturnToPool(this);
        }

        #endregion

        internal CoroutineRunner helper { get; private set; }

        /// <summary>The callback that is executed when coroutine is finished.</summary>
        public Action onComplete { get; private set; }

        /// <summary>Gets whatever this coroutine is paused.</summary>
        public bool isPaused { get; private set; }

        /// <summary>Gets whatever this coroutine is completed.</summary>
        public bool isComplete { get; private set; }

        /// <summary>Gets whatever this coroutine is currently running. This will still return <see langword="true"/> when paused.</summary>
        public bool isRunning => helper;

        /// <summary>Gets whatever this coroutine was cancelled.</summary>
        public bool wasCancelled { get; private set; }

        /// <summary>Gets the caller info of this coroutine.</summary>
        public (MethodBase method, string file, int line) caller { get; private set; }

        /// <summary>Gets the user defined message that was associated with this coroutine.</summary>
        public string debugText { get; private set; }

        /// <summary><see cref="CustomYieldInstruction.keepWaiting"/>, this is how unity knows if this coroutine is done or not.</summary>
        public override bool keepWaiting => !isComplete;

        /// <summary>Pauses the coroutine. Make sure to not use this from within a coroutine, unless you also make sure to unpause it from outside. No effect if already paused.</summary>
        public void Pause()
        {
            if (!isPaused)
                isPaused = true;
        }

        /// <summary>Resumes a paused coroutine. No effect if not paused.</summary>
        public void Resume()
        {
            if (isPaused)
                isPaused = false;
        }

        public void OnStart() =>
            coroutineStarted?.Invoke(this);

        /// <summary>Stops the coroutine.</summary>
        public void Stop() =>
            Stop(isCancel: true);

        internal void Stop(bool isCancel)
        {
            wasCancelled = isCancel;
            isComplete = true;
            coroutineCompleted?.Invoke(this);
            onComplete?.Invoke();
            if (root)
                root.DestroyIfEmpty();
            if (helper)
                if (Application.isPlaying)
                    Object.Destroy(helper.gameObject);
                else
                    Object.DestroyImmediate(helper.gameObject);
        }

        /// <inheritdoc cref="Object.ToString"/>/>
        public override string ToString() =>
            caller.ToString();

    }

}
