using UnityEngine;
using System;

[CreateAssetMenu(fileName = "DrawCardsEffect", menuName = "CardGame/Support Effects/Draw Cards")]
public class DrawCardsSupportEffectData : SupportEffectData
{
    [SerializeField, Min(1)] private int cardsToDraw = 2;

    public override string Description => cardsToDraw == 1
        ? "Draw 1 card"
        : $"Draw {cardsToDraw} cards";

    public override void Resolve(SupportEffectContext context)
    {
        context.AddOperation(new DrawCardsOperation(cardsToDraw));
    }
}

public sealed class DrawCardsOperation : ISupportEffectOperation
{
    private readonly int count;

    public DrawCardsOperation(int count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        this.count = count;
    }

    public void Apply(MatchState state, PlayerSide actor)
    {
        state.DrawCards(actor, count);
    }
}