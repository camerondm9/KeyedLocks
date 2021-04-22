# KeyedLocks
Sync and async locking based on a key

## Usage
This library provides the `KeyedLock<T>` type, which can be used like this:
```csharp
var locks = new KeyedLock<string>();
using (locks.Lock("key"))
{
    //Do the important thing
}
```

The using block ensures that the lock will be released after.

In `async` code, you can also wait to acquire the lock asynchronously:
```csharp
var locks = new KeyedLock<string>();
using (await locks.LockAsync("key"))
{
    //Do the important thing
}
```
## Testing
`KeyedLock<T>` is internally based on a `Dictionary<T, SemaphoreSlim>`.
Semaphores are created and destroyed as needed.

This code has been tested with [Microsoft Coyote](https://github.com/microsoft/coyote) to try to eliminate concurrency bugs.

## Changelog

### 1.0.0
Initial release
