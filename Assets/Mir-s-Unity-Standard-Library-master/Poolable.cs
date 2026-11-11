using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Poolable : MMono
{
    public void SetSpawnInfo(Vector2 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
    }

    public void SetPosition(Vector2 position)
    {
        transform.position = position;
    }
}
