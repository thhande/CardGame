using UnityEngine;

[CreateAssetMenu(fileName = "NightStalkerEffect", menuName = "CardGame/Monster Effects/Night Stalker")]
public sealed class NightStalkerEffectData : MonsterEffectData, IMonsterCombatModifier
{
    public override string Description => "On the turn this monster is placed, it gains +1 ATK and takes no reflected damage.";

    public int AttackBonus => 1;
    public bool PreventsReflectDamage => true;

    public bool IsActive(MatchState state, CardInstance card)
    {
        return card.PlacedTurnNumber == state.TurnNumber && state.CompletedTurnCount < state.TurnNumber;
    }

    public override void Resolve(MonsterEffectContext context)
    {
        // Combat queries this bonus during the placement turn.
    }
}
