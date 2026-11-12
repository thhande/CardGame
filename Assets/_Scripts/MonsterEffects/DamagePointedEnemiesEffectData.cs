using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "DamagePointedEnemies", menuName = "CardGame/Monster Effects/Damage Pointed Enemies")]
public sealed class DamagePointedEnemiesEffectData : MonsterEffectData
{
    public override string Description =>
        "At the start of your turn, deal 2 damage to each enemy monster this card points to.";

    public override void Resolve(MonsterEffectContext context)
    {
        context.AddOperation(new DamagePointedEnemiesOperation());
    }

    private sealed class DamagePointedEnemiesOperation : IMonsterEffectOperation
    {
        public void Apply(MatchState state, PlayerSide actor, CardInstance source)
        {
            foreach (CardDirection direction in source.AttackDirections.Distinct())
            {
                if (!direction.IsAttackDirection) continue;
                int x = source.X + direction.X;
                int y = source.Y + direction.Y;
                if (x < 0 || x >= MatchState.Width || y < 0 || y >= MatchState.Height) continue;
                if (state.GetCellOwner(x, y) == actor) continue;

                state.DamageBoardMonster(x, y, 2);
            }
        }
    }
}
