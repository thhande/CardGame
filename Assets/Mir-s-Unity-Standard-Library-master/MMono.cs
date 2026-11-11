using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//auto loadcomponent before enter playtest
//most Unity components will have many child components to do small parts, most developer prefer using serializefield and drag drop in the inspector, instead of drag and drop each component,
//this will help you drag and drop automatically when you click reset or make changes with your code, put the getcomponents method to get the child in the LoadComponents method
public abstract class MMono : MonoBehaviour
{
    protected virtual void LoadComponents()
    {

    }

    protected virtual void Reset()
    {
        LoadComponents();
    }

    protected virtual void OnValidate()
    {
#if UNITY_EDITOR
        LoadComponents();
#endif
    }



}
