using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolExample : MonoBehaviour, IPoolable<ObjectPoolExample>
{
    private ObjectPool<ObjectPoolExample> _pool;

    public GameObject GetGameObject()
    {
        return gameObject;
    }

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
}