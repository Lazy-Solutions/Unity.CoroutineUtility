using System.Collections;
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
                yield return RunSub(c, 0);
                m_coroutines.Remove(coroutine);
                coroutine.Stop(isCancel: false);

                IEnumerator RunSub(IEnumerator sub, int level)
                {

                    while (sub.MoveNext())
                    {

                        if (coroutine.isComplete)
                            yield break;

                        if (coroutine.isPaused)
                            while (coroutine.isPaused)
                                yield return null;

                        if (sub.Current is IEnumerator subroutine)
                            yield return RunSub(subroutine, level + 1);
                        else
                            yield return sub.Current;

                    }

                }

            }

        }

    }

}
