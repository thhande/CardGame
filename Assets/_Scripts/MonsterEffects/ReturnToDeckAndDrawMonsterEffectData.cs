using UnityEngine;

[CreateAssetMenu(fileName = "ReturnToDeckDrawTwoMonsterEffect",
    menuName = "CardGame/Monster Effects/Return To Deck And Draw Two")]
public sealed class ReturnToDeckAndDrawMonsterEffectData : MonsterEffectData
{
    public override string Description => "Shuffle this monster into the deck, then draw 2 cards";

    public override bool ActiveCondition(MonsterEffectContext context) => context.Source.CurrentHealth > 0;

    public override void Resolve(MonsterEffectContext context)
    {
        context.AddOperation(new ReturnMonsterToDeckAndDrawOperation(1));
    }
}
