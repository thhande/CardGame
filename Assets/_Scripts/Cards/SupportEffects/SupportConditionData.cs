using UnityEngine;

public abstract class SupportConditionData : ScriptableObject, ISupportCondition
{
    public abstract string Description { get; }

    public abstract bool IsMet(MatchState state, PlayerSide actor);
}
