using UnityEngine;

[CreateAssetMenu(fileName = "AlwaysActiveCondition", menuName = "CardGame/Support Conditions/Always Active")]
public class AlwaysActiveSupportConditionData : SupportConditionData
{
    public override string Description => "Always active";

    public override bool IsMet(MatchState state, PlayerSide actor) => true;
}
