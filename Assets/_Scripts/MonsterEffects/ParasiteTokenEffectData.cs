using UnityEngine;

[CreateAssetMenu(fileName = "ParasiteToken", menuName = "CardGame/Monster Effects/Parasite Token")]
public sealed class ParasiteTokenEffectData : MonsterEffectData
{
    private static readonly CardDefinition Token =
        new CardDefinition("parasite-token", "Parasite Token", 1, 0, 1, CardType.Monster);

    public override string Description =>
        "On defeat, discard this monster and summon a Parasite Token (1 ATK, 0 DEF, 1 HP) in its place. Keep control of the tile.";

    public override void Resolve(MonsterEffectContext context)
    {
        context.AddOperation(new ReplaceWithTokenOperation());
    }

    private sealed class ReplaceWithTokenOperation : IMonsterEffectOperation
    {
        public void Apply(MatchState state, PlayerSide actor, CardInstance source)
        {
            state.ReplaceDefeatedMonster(source, Token);
        }
    }
}
