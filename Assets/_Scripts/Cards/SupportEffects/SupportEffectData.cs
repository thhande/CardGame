using UnityEngine;

public abstract class SupportEffectData : ScriptableObject, ISupportEffect
{
    public abstract string Description { get; }

    public abstract void Resolve(SupportEffectContext context);
}
