using UnityEngine;

[CreateAssetMenu(fileName = "NewEquipCard", menuName = "CardGame/EquipCardData")]
public class EquipCardData : CardData
{
    [SerializeField, Min(0)] private int defenseBonus;
    [SerializeField, Min(0)] private int healthBonus;
    [SerializeField] private bool removesRandomAttackArrow;

    public override CardType Type => CardType.Equip;

    public override CardDefinition ToDefinition()
    {
        return new CardDefinition(CardName, CardName,
            new EquipEffect(defenseBonus, healthBonus, removesRandomAttackArrow, CardDescription));
    }
}
