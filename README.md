## Coroutine Utility

A Unity package that provides some additional functionality to coroutines.

Can be installed through the Unity package manager, using git url:
```
https://github.com/Lazy-Solutions/Unity.CoroutineUtility.git
```
</br>

> Using [Advanced Scene Manager](https://github.com/Lazy-Solutions/AdvancedSceneManager)?\
> Coroutine Utility is a dependency of [Advanced Scene Manager](https://github.com/Lazy-Solutions/AdvancedSceneManager), so no need to download this separately.

###### CoroutineUtility
Run coroutines detached from MonoBehaviours and scenes which makes working with coroutines in certain circumstances a lot easier.\
Supports [Editor Coroutines](https://docs.unity3d.com/Manual/com.unity.editorcoroutines.html).

```csharp
void Start()
{

    //Unity, attached with script and will stop when
    //scene or object is unloaded.
    StartCoroutine(Coroutine());

    //Coroutine Utility, runs detached from script and
    //won't stop when scene or object is unloaded
    var coroutine = Coroutine().StartCoroutine();

    //Pauses coroutine (automatically yields null until coroutine.Resume() is called)
    coroutine.Pause();

    //Stop coroutine
    coroutine.Stop();

}

IEnumerator Coroutine()
{
    ...
}
```
</br>

###### MainThreadUtility
Provides functionality to invoke code on main thread. Useful when using tasks or threading, and you need to perform action on main thread.

```csharp
async Task Background_Task()
{

    //Time class cannot be accessed from a thread
    //other than the main thread
    var currentTime = MainThreadUtility.Invoke(() => Time.realtimeSinceStartup);

    //Do something with time...

}
```
