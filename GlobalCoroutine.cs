using System;
using System.Reflection;
using UnityEngine;
using static Lazy.Utility.CoroutineUtility;
using Object = UnityEngine.Object;

namespace Lazy.Utility
{

    public class GlobalCoroutine : CustomYieldInstruction
    {

        internal GlobalCoroutine(CoroutineRunner helper, Action onComplete, (MethodBase method, string file, int line) caller, string debugText)
        {
            this.helper = helper;
            this.onComplete = onComplete;
            this.caller = caller;
            this.debugText = debugText;
            coroutineStarted?.Invoke(this);
        }

        internal CoroutineRunner helper { get; }
        public Action onComplete { get; }
        public bool isPaused { get; private set; }
        public bool isComplete { get; private set; }
        public bool isRunning => helper;
        public bool wasCancelled { get; private set; }
        public (MethodBase method, string file, int line) caller { get; }
        public string debugText { get; }

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

        public override string ToString() =>
            caller.ToString();

    }

}
