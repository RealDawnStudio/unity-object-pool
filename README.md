# Warning
Please use this script carefully as I cannot guarantee for it being free of bugs. This more of an educational thing to show how it could be done.

# Unity Object Pooling Script
Object Pooling script to make pooling easier. The handler takes any instance of a *MonoBehaviour* that has implemented the *IPoolable* interface and keeps a list of instantiated instances for reuse. Fetching and returning a pool instance enables and disables the GameObject of each pool instance respectively.

The script can work with both scene instances and prefabs. Just drop the reference into an ObjectPool and start instantiating.

# Implementation
Before you can create a pool for an object, you need to create a *MonoBehaviour* controller attached to this object that implements *IPoolable*. The default implementation should look something like this:

```
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolExample : MonoBehaviour, IPoolable<ObjectPoolExample>
{
    private ObjectPool<ObjectPoolExample> _pool;

    public void SetPool(ObjectPool<ObjectPoolExample> pool)
    {
        _pool = pool;
    }

    public ObjectPool<ObjectPoolExample> GetPool()
    {
        return _pool;
    }

    public void DestroySafe()
    {
        //your destroy logic
            
        if (_pool == null)
        {
            Destroy(gameObject);
        }
        else
        {
            _pool.ReturnInstance(this);
        }
    }
}
```

# Local Usage
You can instantiate a new object pool simply by calling the following code line, given that the Type you are using for *T* is implementing the *IPoolable* interface:

```
var pool = new ObjectPool<ObjectPoolExample>(poolPrefab);
var instanceObject = pool.GetPoolInstance();
```

# Global Usage
You can also use shared pools accessible from anywhere in your code. They work either by reference or by string. If you are using the reference method, a global pool will be automatically created if it is not already existing.

```
//per object ref
var instanceObject = ObjectPool.GetGlobalPool(poolPrefab).GetPoolInstance();

//per string name
ObjectPool.RegisterGlobalPool("example", poolPrefab);
var instanceObject = ObjectPool.GetGlobalPool("example").GetPoolInstance();
```

# Returning Instances to the Pool
After an instance has done it's job (like an effect being finished or an enemy being detroyed), you can return the instance by calling the *ReturnInstance* method on its *ObjectPool* container. 

```
var pool = new ObjectPool<ObjectPoolExample>(poolPrefab);
var instanceObject = pool.ReturnInstance(instanceObject);

//do stuff

//return per pool call
pool.ReturnInstance(instanceObject);

//or return per instance call
instanceObject.DestroySafe();
```

The *DestroySafe* method implementation is ment to be a shortcut for this in respect to the object having a pool or not. See the example class for how an instance is returned to the pool.

```
public void DestroySafe()
{
    ObjectPool.ClearPoolablesBeforeDestroy(gameObject);

    StopAllCoroutines();
    if (_pool == null)
    {
        Destroy(gameObject);
    }
    else
    {
        _pool.ReturnInstance(this);
    }
}

```

# Limitations and Remarks
When using the global object pool functionality, a list of instances is created. When these objects are destroyed through other means than the *DestroySafe* method from *IPoolable* (like a new scene being loaded), then there will be unasigned values. This is what the static *ClearGlobalNulls* method from *ObjectPool* is for. Call it between scene transitions or when you want to make sure that the list is clear of null or missing refs. Use it like this:

```
ObjectPool.ClearGlobalNulls();
```

For scene transitions you can use the *sceneUnloaded* event from *SceneManager*. Just call *ClearGlobalNulls* and enjoy a null-free instance list.

```
SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;

//implement somewhere in a class
private void SceneManager_sceneUnloaded(Scene arg0)
{
    ObjectPool.ClearGlobalNulls();
}
```

You may have also noticed the *ClearPoolablesBeforeDestroy* method, which is called inside *DestroySafe* on the example class. This is a helper method ment for the case of a pool instance being instatiated as the child of another pool instance. The method will call *DestroySafe* on any *IPoolable* instance in the objects childs, effectively returning them to their respective pools.

```
ObjectPool.ClearPoolablesBeforeDestroy(gameObject);
```