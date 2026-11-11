//for me singleton is the most used design pattern in game development, and i had to rewrite this many times before i made this
//many tutorials will have the dontdestroy in the last line of awake, but i want some singletons just exsist on its specified scenes. So you will have to add the line yourself by overrding method

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton<T> : MMono where T : MonoBehaviour
{
    public static T Instance { get; private set; }
    private void InitialInstance()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this as T;
    }

    protected virtual void Awake()
    {
        InitialInstance();


    }
}
