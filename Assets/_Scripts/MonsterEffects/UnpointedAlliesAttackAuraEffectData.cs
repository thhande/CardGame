using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "UnpointedAlliesAttackAura", menuName = "CardGame/Monster Effects/Unpointed Allies Attack Aura")]
public sealed class UnpointedAlliesAttackAuraEffectData : MonsterEffectData, IMonsterAttackAura
{
    public override string Description => "Adjacent allied monsters this card does not point to gain +1 ATK.";

    public int GetAttackBonus(CardInstance source, CardInstance ally)
    {
        CardDirection direction = new CardDirection(ally.X - source.X, ally.Y - source.Y);
        return direction.IsAttackDirection && !source.AttackDirections.Contains(direction) ? 1 : 0;
    }

    public override void Resolve(MonsterEffectContext context)
    {
        // Attack calculations query the aura continuously.
    }
}
