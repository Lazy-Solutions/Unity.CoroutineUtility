using System;
using System.Reflection;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Lazy.Utility
{

    /// <summary>Represents a <see cref="IEnumerator"/> coroutine started using <see cref="CoroutineUtility"/>.</summary>
    public class GlobalCoroutine : CustomYieldInstruction
    {

        #region Pooled construction

        /// <summary>Gets <see cref="GlobalCoroutine"/> from pool.</summary>
        internal static GlobalCoroutine Get(Action onComplete, (MethodBase method, string file, int line) caller, string debugText) =>
            GlobalCoroutinePool.Get(onComplete, caller, debugText);

        /// <summary>Don't use this, <see cref="GlobalCoroutine"/> is pooled using <see cref="GlobalCoroutinePool"/>. Use <see cref="Get"/> instead.</summary>
        internal GlobalCoroutine()
        { }

        /// <summary>Clears out the fields of this <see cref="GlobalCoroutine"/>, used to prepare before returning to <see cref="GlobalCoroutinePool"/>.</summary>
        internal void Clear() =>
            Construct(null, default, null);

        /// <summary>'Constructs' an instance of <see cref="GlobalCoroutine"/>, <see cref="GlobalCoroutine"/> is pooled using <see cref="GlobalCoroutinePool"/>, this means the instances are recycled, so instead of using constructor, we call this.</summary>
        internal void Construct(Action onComplete, (MethodBase method, string file, int line) caller, string debugText)
        {
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

        /// <summary>The callback that is executed when coroutine is finished.</summary>
        public Action onComplete { get; private set; }

        /// <summary>Gets whatever this coroutine is paused.</summary>
        public bool isPaused { get; private set; }

        /// <summary>Gets whatever this coroutine is completed.</summary>
        public bool isComplete { get; private set; }

        /// <summary>Gets whatever this coroutine is currently running. This will still return <see langword="true"/> when paused.</summary>
        public bool isRunning { get; private set; }

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

        public void OnStart()
        {
            isRunning = true;
            CoroutineUtility.RaiseCoroutineStarted(this);
        }

        /// <summary>Stops the coroutine.</summary>
        public void Stop() =>
            Stop(isCancel: true);

        /// <summary>Stops the coroutine.</summary>
        internal void Stop(bool isCancel)
        {

            if (isComplete)
                return;

            if (CoroutineUtility.m_runner)
                CoroutineUtility.m_runner.Stop(this);

            wasCancelled = isCancel;
            isComplete = true;
            isRunning = false;
            CoroutineUtility.RaiseCoroutineCompleted(this);
            onComplete?.Invoke();

        }

        /// <inheritdoc cref="Object.ToString"/>/>
        public override string ToString() =>
            caller.ToString();

#if UNITY_EDITOR
        /// <summary>View caller in code editor, only accessible from editor.</summary>
        public void ViewCallerInCodeEditor()
        {

            var relativePath =
                caller.file.Contains("/Packages/")
                ? caller.file.Substring(caller.file.IndexOf("/Packages/") + 1)
                : "Assets" + caller.file.Replace(Application.dataPath, "");

            if (AssetDatabase.LoadAssetAtPath<Object>(relativePath))
                AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<MonoScript>(relativePath), caller.line, 0);
            else
                Debug.LogError($"Could not find '{relativePath}'");

        }
#endif

    }

}
