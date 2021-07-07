## Coroutine Utility

A Unity package that provides some additional functionality to coroutines.

###### CoroutineUtility
Run coroutines detached from MonoBehaviours and scenes which makes working with coroutines in certain circumstances a lot easier.

```csharp
void Start()
{

    //Unity, attached with script and will stop when scene or object is unloaded.
    StartCoroutine(Coroutine());

    //Coroutine Utility, runs detached from script and
    //won't stop when scene or object is unloaded
    Coroutine().StartCoroutine();

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

    //Time class cannot be accessed from a thread other than the main thread
    var currentTime = MainThreadUtility.Invoke(() => Time.time);
    //Do something with time...

}
```
