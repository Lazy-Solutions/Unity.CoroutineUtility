﻿using System.Collections;
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Lazy.Utility
{

    [ExecuteAlways]
    [AddComponentMenu("")]
    internal partial class CoroutineRunner : MonoBehaviour
    {

#if UNITY_EDITOR

        void Start()
        {

            EditorApplication.playModeStateChanged += (mode) =>
            {
                if (mode == PlayModeStateChange.ExitingPlayMode)
                    if (this && gameObject)
                        Destroy(gameObject);
            };

        }

#endif

        readonly Dictionary<GlobalCoroutine, Coroutine> m_coroutines = new Dictionary<GlobalCoroutine, Coroutine>();
        public IReadOnlyCollection<GlobalCoroutine> coroutines => m_coroutines.Keys;

        public void Add(IEnumerator enumerator, GlobalCoroutine coroutine)
        {
            m_coroutines.Add(coroutine, null);
            Run(enumerator, coroutine);
        }

        public void Clear()
        {
            foreach (var coroutine in coroutines)
                coroutine.Stop(isCancel: true);
            m_coroutines.Clear();
        }

        internal void Stop(GlobalCoroutine coroutine)
        {
            if (m_coroutines.TryGetValue(coroutine, out var c))
            {
                StopCoroutine(c);
                m_coroutines.Remove(coroutine);
            }
        }

        public void Run(IEnumerator enumerator, GlobalCoroutine coroutine)
        {

            m_coroutines[coroutine] = StartCoroutine(RunCoroutine(enumerator));

            IEnumerator RunCoroutine(IEnumerator c)
            {

                coroutine.OnStart();

                object rootUserData = null;
                if (CoroutineUtility.Events.enableEvents)
                {
                    CoroutineUtility.Events.onCoroutineStarted?.Invoke(coroutine);
                    rootUserData = CoroutineUtility.Events.onSubroutineStart?.Invoke(coroutine, null, level: 0, null, isPause: false);
                }

                yield return RunSub(c, 0, rootUserData);

                m_coroutines.Remove(coroutine);

                if (CoroutineUtility.Events.enableEvents)
                {
                    CoroutineUtility.Events.onSubroutineEnd(coroutine, rootUserData);
                    CoroutineUtility.Events.onCoroutineEnded(coroutine);
                }

                coroutine.Stop(isCancel: false);

                IEnumerator RunSub(IEnumerator sub, int level, object parentUserData)
                {

                    while (sub.MoveNext())
                    {

                        if (coroutine.isComplete)
                            yield break;

                        if (coroutine.isPaused)
                        {

                            var pauseUserData = CoroutineUtility.Events.enableEvents
                                ? CoroutineUtility.Events.onSubroutineStart?.Invoke(coroutine, null, level, parentUserData, isPause: true)
                                : null;

                            while (coroutine.isPaused)
                                yield return null;

                            if (CoroutineUtility.Events.enableEvents)
                                CoroutineUtility.Events.onSubroutineEnd?.Invoke(coroutine, pauseUserData);

                        }

                        var userData = CoroutineUtility.Events.enableEvents
                            ? CoroutineUtility.Events.onSubroutineStart?.Invoke(coroutine, sub.Current, level + 1, parentUserData, isPause: false)
                            : null;

                        if (sub.Current is IEnumerator subroutine)
                            yield return RunSub(subroutine, level + 1, userData);
                        else
                            yield return sub.Current;

                        if (CoroutineUtility.Events.enableEvents)
                            CoroutineUtility.Events.onSubroutineEnd?.Invoke(coroutine, userData);

                    }

                }

            }

        }

    }

}
