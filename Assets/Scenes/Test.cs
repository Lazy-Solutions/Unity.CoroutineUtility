using Lazy.Utility;
using System.Collections;
using UnityEngine;

public class Test : MonoBehaviour
{

    //public bool start;
    //void OnValidate()
    //{
    //    if (start)
    //    {
    //        start = false;
    //        Debug.Log("Starting in editor");
    //        Coroutine().StartCoroutine();
    //    }
    //}

    void Start()
    {
        Debug.Log("Starting in runtime");
        Coroutine().StartCoroutine(debugText: "Coroutine test");
    }

    IEnumerator Coroutine()
    {
        for (int i = 1; i < 11; i++)
        {
            Debug.Log(i);
            yield return new WaitForSecondsRealtime(1);
        }
    }

}
