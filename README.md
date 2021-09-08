## Coroutine Utility

A Unity package that provides some additional functionality to coroutines.

Can be installed through the Unity package manager, using git url:
```
https://github.com/Lazy-Solutions/Unity.CoroutineUtility.git
```
</br>

> Using [Advanced Scene Manager](https://github.com/Lazy-Solutions/advanced-scene-manager)?\
> Coroutine Utility is already added as a dependency to Advanced Scene Manager, so no need to download this separately.\
> Coroutine utility classes can be found in AdvancedSceneManager.Utility namespace.

###### CoroutineUtility
Run coroutines detached from MonoBehaviours and scenes which makes working with coroutines in certain circumstances a lot easier.

```csharp
void Start()
{

    //Unity, attached with script and will stop when
    //scene or object is unloaded.
    StartCoroutine(Coroutine());

    //Coroutine Utility, runs detached from script and
    //won't stop when scene or object is unloaded
    var coroutine = Coroutine().StartCoroutine();

    //Pauses coroutine (yields null until Resume())
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
