using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Object pool container class.</summary>
public abstract class ObjectPool
{
    private static Dictionary<string, ObjectPool> _globalPools;
    private static Dictionary<IPoolable, ObjectPool> _globalObjectPools;

    /// <summary>Registers a new global pool, identified by the string <paramref name="name"/>.</summary>
    /// <param name="name">The name key of the pool.</param>
    /// <param name="prefab">The object that will be used as the base for every instance.</param>
    /// <param name="preSpawn">Immediatly create the given amount of instances.</param>
    public static ObjectPool<T> RegisterGlobalPool<T>(string name, T prefab, int preSpawn = 0) where T : class, IPoolable<T>
    {
        var pool = GetGlobalPool<T>(name);
        if (pool == null)
        {
            pool = new ObjectPool<T>(prefab, preSpawn);
            _globalPools.Add(name, pool);
        }
        return pool;
    }

    /// <summary>Get the container object of a global pool, identified by the string <paramref name="name"/>.</summary>
    /// <param name="name">The name key of the pool.</param>
    public static ObjectPool<T> GetGlobalPool<T>(string name) where T : class, IPoolable<T>
    {
        if (_globalPools == null)
            _globalPools = new Dictionary<string, ObjectPool>();

        if (_globalPools.ContainsKey(name))
            return (ObjectPool<T>)_globalPools[name];

        return null;
    }

    /// <summary>Get the container object of a global pool, identified by the reference <paramref name="prefab"/>.</summary>
    /// <param name="prefab">The reference key of the pool.</param>
    public static ObjectPool<T> GetGlobalPool<T>(T prefab) where T : class, IPoolable<T>
    {
        if (_globalObjectPools == null)
            _globalObjectPools = new Dictionary<IPoolable, ObjectPool>();

        if (!_globalObjectPools.ContainsKey(prefab))
        {
            var pool = new ObjectPool<T>(prefab);
            _globalObjectPools.Add(prefab, pool);
        }

        return (ObjectPool<T>)_globalObjectPools[prefab];
    }

    /// <summary>Calls DestroySafe on all IPoolable instances that are a child of <paramref name="target"/>.</summary>
    /// <param name="target">The GameObject to scan through.</param>
    public static void ClearPoolablesBeforeDestroy(GameObject target)
    {
        var poolables = target.GetComponentsInChildren<IPoolable>(true);

        foreach(IPoolable poolable in poolables)
        {
            if (poolable.GetGameObject() != target)
            {
                poolable.DestroySafe();
            }
        }
    }

    /// <summary>Rreturn all currently active global pool instances</summary>
    public static void ReturnGlobalInstances()
    {
        if (_globalPools != null)
        {
            foreach (KeyValuePair<string, ObjectPool> kvp in _globalPools)
            {
                kvp.Value.ReturnAllInstances();
            }
        }
        if (_globalObjectPools != null)
        {
            foreach (KeyValuePair<IPoolable, ObjectPool> kvp in _globalObjectPools)
            {
                kvp.Value.ReturnAllInstances();
            }
        }
    }

    /// <summary>Clear all null values from the instance list of all global pools.</summary>
    public static void ClearGlobalNulls()
    {
        if (_globalPools != null)
        {
            foreach (KeyValuePair<string, ObjectPool> pool in _globalPools)
            {
                pool.Value.ClearNulls();
            }
        }
        if (_globalObjectPools != null)
        {
            foreach (KeyValuePair<IPoolable, ObjectPool> pool in _globalObjectPools)
            {
                pool.Value.ClearNulls();
            }
        }
    }

    /// <summary>Clear all null values from the instance list.</summary>
    public abstract void ClearNulls();

    /// <summary>Rreturn all currently active pool instances</summary>
    public abstract void ReturnAllInstances();
}

/// <summary>Object pool handler class.</summary>
public class ObjectPool<T> : ObjectPool where T : class, IPoolable<T>
{
    public List<T> Pool { get => _pool; }
    private List<T> _pool;
    private List<T> _freePool;
    private T _prefab;
    private int _countInstances;
    private int _maxInstances;
    private Transform _parent;

    /// <summary>Create a new ObjectPool for <paramref name="prefab"/>.</summary>
    /// <param name="prefab">The GameObject to scan through.</param>
    /// <param name="preSpawn">Immediatly create the given amount of instances.</param>
    /// <param name="parent">The transform that every new instance should be automatially added to as a child.</param>
    public ObjectPool(T prefab, int preSpawn = 0, Transform parent = null)
    {
        _prefab = prefab;

        _pool = new List<T>();
        _freePool = new List<T>();

        _countInstances = 0;
        _maxInstances = -1;
        _parent = parent;

        AddPoolInstances(preSpawn);
    }

    /// <summary>Create the given <paramref name="amount"/> of instances.</summary>
    /// <param name="amount">The amount of instances that should be created.</param>
    public void AddPoolInstances(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            AddPoolInstance();
        }
    }

    /// <summary>Create a new pool instance.</summary>
    public T AddPoolInstance()
    {
        if (_maxInstances == -1 || _countInstances < _maxInstances)
        {
            var go = GameObject.Instantiate(_prefab.GetGameObject());
            go.SetActive(false);
            T inst = go.GetComponent<T>();
            inst.SetPool(this);
            _pool.Add(inst);
            _freePool.Add(inst);
            _countInstances++;

            if (_parent != null)
                go.transform.SetParent(_parent);

            return inst;
        }

        return null;
    }

    /// <summary>Fetch a pool instance. If none is found and the limit of active instances for this pool is not reached, a new instance will be created automatically. The GameObject of the instance will also be activated.</summary>
    public T GetPoolInstance()
    {
        T inst = null;
        foreach (T t_inst in _freePool)
        {
            if (t_inst != null)
            {
                inst = t_inst;
                break;
            }
        }

        if (inst == null)
        {
            inst = AddPoolInstance();
        }

        if (inst != null)
        {
            inst.GetGameObject().SetActive(true);
            _freePool.Remove(inst);
        }

        return inst;
    }

    /// <summary>Return the given <paramref name="instance"/> to the pool (and deactivate the GameObject).</summary>
    /// <param name="amount">The amount of instances that should be created.</param>
    public void ReturnInstance(T instance)
    {
        if (_freePool.Contains(instance))
            return;
        if (!_pool.Contains(instance))
            return;

        var go = instance.GetGameObject();
        go.SetActive(false);
        _freePool.Add(instance);
    }

    /// <summary>Rreturn all currently active pool instances</summary>
    public override void ReturnAllInstances()
    {
        foreach(T instance in _pool)
        {
            if (!_freePool.Contains(instance))
            {
                instance.DestroySafe();
            }
        }
    }

    /// <summary>Clear all null values from the instance list.</summary>
    public override void ClearNulls()
    {
        _pool = _pool.Where(f => !(f == null || (f is UnityEngine.Object obj && obj == null))).ToList();
        _pool = _pool.Where(f => f.GetGameObject() != null).ToList();
        _freePool = _freePool.Where(f => !(f == null || (f is UnityEngine.Object obj && obj == null))).ToList();
        _freePool = _freePool.Where(f => f.GetGameObject() != null).ToList();

        _countInstances = _pool.Count;
    }

    /// <summary>Set the given <paramref name="max"/> limit of active instances.</summary>
    /// <param name="max">The amount of instances that should be created.</param>
    public void SetMaxInstances(int max)
    {
        if (max > 0 || max == -1)
            _maxInstances = max;
    }

    /// <summary>Reset the limit of maximum active instances to infinity.</summary>
    public void ClearMaxInstanecs()
    {
        _maxInstances = -1;
    }
}
