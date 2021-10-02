using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Base object pool instance class.</summary>
public interface IPoolable
{
    void DestroySafe();
}

/// <summary>Typed object pool instance class.</summary>
public interface IPoolable<T> : IPoolable where T : MonoBehaviour, IPoolable<T>
{
    void SetPool(ObjectPool<T> pool);
    ObjectPool<T> GetPool();
}
