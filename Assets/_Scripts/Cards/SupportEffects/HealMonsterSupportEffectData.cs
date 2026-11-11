using System;
using UnityEngine;

[CreateAssetMenu(fileName = "HealMonsterEffect", menuName = "CardGame/Support Effects/Heal Monster")]
public class HealMonsterSupportEffectData : SupportEffectData, ICellTargetedSupportEffect
{
    [SerializeField, Min(1)] private int healthToRestore = 3;

    public override string Description => $"Restore {healthToRestore} HP to a monster.";
    public string TargetPrompt => "Choose a wounded monster";

    public bool CanTargetCell(MatchState state, PlayerSide actor, int x, int y)
    {
        return healthToRestore > 0 && state.GetCardAt(x, y)?.CanHeal == true;
    }

    public override void Resolve(SupportEffectContext context)
    {
        (int x, int y) = context.TargetCell.Value;
        context.AddOperation(new HealMonsterOperation(x, y, healthToRestore));
    }
}

public sealed class HealMonsterOperation : ISupportEffectOperation
{
    private readonly int x;
    private readonly int y;
    private readonly int amount;

    public HealMonsterOperation(int x, int y, int amount)
    {
        this.x = x;
        this.y = y;
        this.amount = amount;
    }

    public void Apply(MatchState state, PlayerSide actor)
    {
        CardInstance monster = state.GetCardAt(x, y);
        if (monster == null) throw new InvalidOperationException("The target cell has no monster.");
        monster.Heal(amount);
    }
}
