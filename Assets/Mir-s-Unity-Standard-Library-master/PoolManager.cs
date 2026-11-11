//I made a pool manager as a singleton so replacing Instantiate and Destroy will be easier

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PoolManager : Singleton<PoolManager>
{
    public List<PoolInfo> pools = new List<PoolInfo>();
    List<GameObject> physPool = new List<GameObject>();

    public Poolable SpawnObject(Poolable objectToSpawn, Vector2 spawnPos, Quaternion spawnRotation)
    {
        PoolInfo pool = pools.Find(p => p.LookUpString == objectToSpawn.gameObject.name);
        if (pool == null)
        {
            pool = new PoolInfo() { LookUpString = objectToSpawn.gameObject.name };
            pools.Add(pool);
            CreateNewPhysPool(objectToSpawn.name);
        }
        Poolable spawnable = pool.InactiveObjects.FirstOrDefault();
        if (spawnable == null)
        {
            spawnable = Instantiate(objectToSpawn, spawnPos, spawnRotation);
        }
        else
        {
            spawnable.SetSpawnInfo(spawnPos, spawnRotation);
            pool.InactiveObjects.Remove(spawnable);
            spawnable.gameObject.SetActive(true);
            spawnable.transform.parent = null;
        }
        return spawnable;
    }

    public Poolable SpawnObject(Poolable objectToSpawn)
    {
        PoolInfo pool = pools.Find(p => p.LookUpString == objectToSpawn.gameObject.name);
        if (pool == null)
        {
            pool = new PoolInfo() { LookUpString = objectToSpawn.gameObject.name };
            pools.Add(pool);
            CreateNewPhysPool(objectToSpawn.name);
        }
        Poolable spawnable = pool.InactiveObjects.FirstOrDefault();
        if (spawnable == null)
        {
            spawnable = Instantiate(objectToSpawn, transform.position, transform.rotation);
        }
        else
        {
            pool.InactiveObjects.Remove(spawnable);
            spawnable.gameObject.SetActive(true);
            spawnable.transform.parent = null;
        }
        return spawnable;
    }


    public void ReturnToPool(Poolable obj)
    {
        string goName = obj.gameObject.name.Substring(0, obj.name.Length - 7);
        PoolInfo pool = pools.Find(p => p.LookUpString == goName);
        if (pool == null) Destroy(obj);
        else
        {
            obj.gameObject.SetActive(false);
            pool.InactiveObjects.Add(obj);
            Transform parent = physPool.Find(p => p.name == goName).transform;
            obj.transform.parent = parent;
        }
    }

    private GameObject CreateNewPhysPool(string name)
    {
        GameObject newPool = new GameObject(name);
        newPool.transform.parent = this.transform;
        physPool.Add(newPool);
        return newPool;
    }
}

[System.Serializable]
public class PoolInfo
{
    public string LookUpString;
    public List<Poolable> InactiveObjects = new List<Poolable>();
}
