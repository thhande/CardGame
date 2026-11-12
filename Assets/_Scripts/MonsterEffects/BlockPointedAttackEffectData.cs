using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "BlockPointedAttack", menuName = "CardGame/Monster Effects/Block Pointed Attack")]
public sealed class BlockPointedAttackEffectData : MonsterEffectData, IAttackDamagePrevention
{
    public override string Description => "When attacked, take no damage if one of this monster's attack arrows points at the attacker.";

    public bool PreventsAttackDamage(CardInstance defender, CardInstance attacker)
    {
        CardDirection directionToAttacker = new CardDirection(attacker.X - defender.X, attacker.Y - defender.Y);
        return defender.AttackDirections.Contains(directionToAttacker);
    }

    public override bool ActiveCondition(MonsterEffectContext context) => false;

    public override void Resolve(MonsterEffectContext context)
    {
        // Combat applies this passive before dealing damage.
    }
}
