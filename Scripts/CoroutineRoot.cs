#pragma warning disable IDE0051 // Remove unused private members

using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Lazy.Utility
{

    [ExecuteAlways]
    [AddComponentMenu("")]
    internal class CoroutineRoot : MonoBehaviour
    {

#if UNITY_EDITOR

        private void Start()
        {

            EditorApplication.playModeStateChanged += (mode) =>
            {
                if (mode == PlayModeStateChange.ExitingPlayMode)
                    if (this && gameObject)
                        Destroy(gameObject);
            };

        }

#endif

        public void DestroyIfEmpty()
        {
            if (gameObject)
                StartCoroutine(WaitAndDestroy(gameObject));
        }

        public void Destroy(CoroutineRunner coroutine)
        {
            StartCoroutine(WaitAndDestroy(coroutine.gameObject));
            StartCoroutine(WaitAndDestroy(gameObject, 2));
        }

        IEnumerator WaitAndDestroy(GameObject obj, int frames = 1)
        {

            for (int i = 0; i < frames; i++)
                yield return null;

            if (obj == gameObject && transform.childCount != 0)
                yield break;

            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);

        }

    }

}
