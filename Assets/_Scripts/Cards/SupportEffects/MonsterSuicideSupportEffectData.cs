using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Monster suicide effect", menuName = "CardGame/Support Effects/Monster suicide")]
public sealed class MonsterSuicideSupportEffectData : SupportEffectData, ICellTargetedSupportEffect
{
    public override string Description => "Set HP of an alive controlled monster to 0";

    public string TargetPrompt => "Choose one of your alive monster";

    public bool CanTargetCell(MatchState state, PlayerSide actor, int x, int y)
    {
        return state.GetCardAt(x, y) != null && state.GetCardAt(x, y).IsAlive;
    }

    public override void Resolve(SupportEffectContext context)
    {
        (int x, int y) = context.TargetCell.Value;
        context.AddOperation(new MonsterSuicideOperation(x, y));
    }

}

public sealed class MonsterSuicideOperation : ISupportEffectOperation
{
    private readonly int x;
    private readonly int y;

    public MonsterSuicideOperation(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public void Apply(MatchState state, PlayerSide actor)
    {
        CardInstance targetedCard = state.GetCardAt(x, y);
        targetedCard.ForceSetHealth(0);
    }
}
