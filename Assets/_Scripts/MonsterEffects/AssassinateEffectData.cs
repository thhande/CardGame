using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "Assassinate", menuName = "CardGame/Monster Effects/Assassinate")]
public sealed class AssassinateEffectData : MonsterEffectData, IAttackDamageOverride
{
    public override string Description => "When attacking a monster, deal damage equal to its remaining HP if none of its attack arrows point at this monster.";

    public bool TryGetAttackDamage(CardInstance attacker, CardInstance target, out int damage)
    {
        CardDirection directionToAttacker = new CardDirection(attacker.X - target.X, attacker.Y - target.Y);
        if (target.Definition.Type == CardType.Monster && !target.AttackDirections.Contains(directionToAttacker))
        {
            damage = target.CurrentHealth;
            return true;
        }

        damage = 0;
        return false;
    }

    public override bool ActiveCondition(MonsterEffectContext context) => false;

    public override void Resolve(MonsterEffectContext context)
    {
        // Combat resolves this passive when calculating attack damage.
    }
}
