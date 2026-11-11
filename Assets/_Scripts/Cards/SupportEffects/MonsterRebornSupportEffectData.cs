using System;
using UnityEngine;

[CreateAssetMenu(fileName = "MonsterRebornEffect", menuName = "CardGame/Support Effects/Resurrect Monster")]
public class MonsterRebornSupportEffectData : SupportEffectData, ICellTargetedSupportEffect
{
    [field: SerializeField, Range(0.01f, 1f)] public float HealthGainRatio { get; private set; } = 0.5f;

    public override string Description => $"Revive your monster at {HealthGainRatio:P0} max HP and reclaim its tile.";
    public string TargetPrompt => "Choose one of your defeated monsters";

    public bool CanTargetCell(MatchState state, PlayerSide actor, int x, int y)
    {
        return HealthGainRatio > 0 && HealthGainRatio <= 1 && CanReviveMonster(state.GetCardAt(x, y), actor);
    }

    internal static bool CanReviveMonster(CardInstance card, PlayerSide actor)
    {
        return card != null && card.Definition.Type == CardType.Monster
            && card.Owner == actor && !card.IsAlive && card.MaxHealth > 0;
    }

    public override void Resolve(SupportEffectContext context)
    {
        (int x, int y) = context.TargetCell.Value;
        context.AddOperation(new MonsterRebornOperation(x, y, HealthGainRatio));
    }
}

public sealed class MonsterRebornOperation : ISupportEffectOperation
{
    private readonly int x;
    private readonly int y;
    private readonly float healthGainRatio;

    public MonsterRebornOperation(int x, int y, float healthGainRatio)
    {
        if (!(healthGainRatio > 0 && healthGainRatio <= 1))
            throw new ArgumentOutOfRangeException(nameof(healthGainRatio));
        this.x = x;
        this.y = y;
        this.healthGainRatio = healthGainRatio;
    }

    public void Apply(MatchState state, PlayerSide actor)
    {
        CardInstance monster = state.GetCardAt(x, y);
        if (!MonsterRebornSupportEffectData.CanReviveMonster(monster, actor))
            throw new InvalidOperationException("Only a defeated monster belonging to the actor can be revived.");

        int health = Math.Max(1, (int)(monster.MaxHealth * (double)healthGainRatio));
        monster.ForceSetHealth(health);
        state.SetCellOwner(x, y, actor);
    }
}
