using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Lazy.Utility
{

    internal static class GlobalCoroutinePool
    {

        static readonly List<GlobalCoroutine> pool = new List<GlobalCoroutine>();

        /// <summary>Gets a recycled instance of <see cref="GlobalCoroutine"/>, if none exist, a new instance will be created.</summary>
        internal static GlobalCoroutine Get(Action onComplete, (MethodBase method, string file, int line) caller, string description)
        {
            Get(out var coroutine);
            coroutine.Construct(onComplete, caller, description);
            return coroutine;
        }

        //Called by GlobalCoroutine finalizer
        internal static void ReturnToPool(GlobalCoroutine coroutine) =>
            pool.Add(coroutine);

        //Retreive or create instance
        static void Get(out GlobalCoroutine coroutine)
        {

            coroutine = null;

            if (pool.Contains(null))
                pool.RemoveAll(obj => obj == null);

            if (pool.Count > 0)
            {
                coroutine = pool[0];
                pool.RemoveAt(0);
            }

            if (coroutine is null)
                coroutine = new GlobalCoroutine();

        }

    }

}
