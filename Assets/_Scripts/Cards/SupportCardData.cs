using UnityEngine;

[CreateAssetMenu(fileName = "NewSupportCard", menuName = "CardGame/SupportCardData")]
public class SupportCardData : CardData
{
    [field: SerializeField] public SupportEffectData Effect { get; private set; }
    [field: SerializeField] public SupportConditionData ActiveCondition { get; private set; }
    [SerializeField] private bool isHiSupporter;

    public override CardType Type => isHiSupporter ? CardType.HiSupporter : base.Type;

    public override CardDefinition ToDefinition()
    {
        return new CardDefinition(CardName, CardName, 0, 0, 0, Type, Effect, ActiveCondition);
    }

    public SupportCardData()
    {
        type = CardType.Supporter;
    }
}
