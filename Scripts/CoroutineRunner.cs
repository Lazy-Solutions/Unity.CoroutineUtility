using System.Collections;
using UnityEngine;

namespace Lazy.Utility
{

    [ExecuteAlways]
    [AddComponentMenu("")]
    internal partial class CoroutineRunner : MonoBehaviour
    {

        public GlobalCoroutine coroutine;

        public void Run(IEnumerator coroutine, GlobalCoroutine helper)
        {

            this.coroutine = helper;
            name = !string.IsNullOrWhiteSpace(helper.debugText)
                ? helper.debugText
                : helper.ToString();

            StartCoroutine(RunCoroutine(coroutine));

            IEnumerator RunCoroutine(IEnumerator c)
            {

                yield return RunSub(c, 0);

                IEnumerator RunSub(IEnumerator sub, int level)
                {

                    while (sub.MoveNext())
                    {

                        if (helper.isComplete)
                            yield break;

                        if (helper.isPaused)
                            while (helper.isPaused)
                                yield return null;

                        if (sub.Current is IEnumerator subroutine)
                            yield return RunSub(subroutine, level + 1);
                        else
                            yield return sub.Current;

                    }

                }

                helper.Stop(isCancel: false);

            }

        }

    }

}
