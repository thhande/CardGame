//In many situations, child components will have to interact with others, these components will interact with others through a core, which is connected to every child components

using UnityEngine;

public abstract class CoreBase<TCore> : MMono where TCore : CoreBase<TCore>
{

    protected T LoadComponent<T>(ref T field, bool searchInChildren = false)
        where T : Component
    {
        if (field != null) return field;

        field = searchInChildren
            ? GetComponentInChildren<T>(true)
            : GetComponent<T>();

        if (field == null)
            Debug.LogError($"[{GetType().Name}] Missing: {typeof(T).Name}", this);

        return field;
    }


    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadIDatas();
        InitComponents();
    }

    protected virtual void LoadIDatas()
    {

    }

    protected virtual void InitComponents()
    {

    }
}
