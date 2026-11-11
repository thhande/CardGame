using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "NewMonsterCard", menuName = "CardGame/MonsterCardData")]
public class MonsterCardData : CardData
{

    [field: SerializeField] public int Attack { get; private set; }
    [field: SerializeField] public int Defense { get; private set; }
    [field: SerializeField] public int Health { get; private set; }
    [SerializeField] private EffectEntry[] effects = System.Array.Empty<EffectEntry>();
    [SerializeField, HideInInspector, FormerlySerializedAs("<EffectType>k__BackingField")]
    private MonsterEffectType legacyEffectType;
    [SerializeField, HideInInspector, FormerlySerializedAs("<Effect>k__BackingField")]
    private MonsterEffectData legacyEffect;

    [System.Serializable]
    private sealed class EffectEntry
    {
        public MonsterEffectType activateType;
        public MonsterEffectData effect;
    }

    public HashSet<AttackDirection> AttackDirections => attackDirections.ToHashSet();
    [SerializeField] private AttackDirection[] attackDirections = new AttackDirection[8];

    public override CardDefinition ToDefinition()
    {
        CardDirection[] directions = attackDirections
            .Where(direction => direction != null)
            .Select(direction => new CardDirection(direction.X, -direction.Y))
            .ToArray();
        if (effects.Length == 0)
            return new CardDefinition(CardName, CardName, Attack, Defense, Health, Type,
                legacyEffectType, legacyEffect != null ? legacyEffect : null, directions);

        MonsterEffectEntry[] monsterEffects = effects
            .Where(entry => entry != null && entry.effect != null)
            .Select(entry => new MonsterEffectEntry(entry.activateType, entry.effect))
            .ToArray();
        return new CardDefinition(CardName, CardName, Attack, Defense, Health, Type, monsterEffects, directions);
    }

    public MonsterCardData()
    {
        type = CardType.Monster;
    }
}


[System.Serializable]
public class AttackDirection
{
    [field: SerializeField, Range(-1, 1)] public int X { get; private set; }
    [field: SerializeField, Range(-1, 1)] public int Y { get; private set; }
}
