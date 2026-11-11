using UnityEngine;

[CreateAssetMenu(fileName = "BlockCellEffect", menuName = "CardGame/Support Effects/Block Cell")]
public class BlockCellSupportEffectData : SupportEffectData, ICellTargetedSupportEffect
{
    public override string Description => "Block an empty tile on your opponent's next turn.";
    public string TargetPrompt => "Choose an empty tile to block";

    public bool CanTargetCell(MatchState state, PlayerSide actor, int x, int y)
    {
        return state.GetCellState(x, y) != BoardCellState.Occupied;
    }

    public override void Resolve(SupportEffectContext context)
    {
        (int x, int y) = context.TargetCell.Value;
        context.AddOperation(new BlockCellPlacementOperation(x, y));
    }
}

public sealed class BlockCellPlacementOperation : ISupportEffectOperation
{
    private readonly int x;
    private readonly int y;

    public BlockCellPlacementOperation(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public void Apply(MatchState state, PlayerSide actor)
    {
        state.BlockPlacementNextTurn(x, y);
    }
}
