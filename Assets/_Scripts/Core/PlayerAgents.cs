using System;
using System.Collections.Generic;

public interface IPlayerAgent
{
    void RequestAction(MatchState state, Action<MatchAction> submitAction);
}

public sealed class RandomPlayerAgent : IPlayerAgent
{
    private readonly Random random;

    public RandomPlayerAgent(int seed)
    {
        random = new Random(seed);
    }

    public void RequestAction(MatchState state, Action<MatchAction> submitAction)
    {
        if (submitAction == null) throw new ArgumentNullException(nameof(submitAction));
        if (state == null || state.Turn != TurnState.EnemyTurn)
            throw new ArgumentException("The random agent requires an active enemy turn.", nameof(state));

        for (int i = 0; i < state.EnemyHand.Count; i++)
        {
            CardInstance card = state.EnemyHand[i];
            if (card.Definition.IsSupportCard
                && MatchRules.CanPlaySupportCard(state, PlayerSide.Enemy, card.Id, out var targetCell))
            {
                submitAction(targetCell is { } cell
                    ? new PlaySupportCardAction(state.Id, PlayerSide.Enemy, card.Id, cell.X, cell.Y)
                    : new PlaySupportCardAction(state.Id, PlayerSide.Enemy, card.Id));
                return;
            }
        }

        if (TryFindEquipPlay(state, out PlayEquipCardAction equipPlay))
        {
            submitAction(equipPlay);
            return;
        }

        long playableCardId = -1;
        for (int i = 0; i < state.EnemyHand.Count; i++)
        {
            if (state.EnemyHand[i].Definition.Type == CardType.Monster)
            {
                playableCardId = state.EnemyHand[i].Id;
                break;
            }
        }

        List<int> openCellIndices = new List<int>();
        for (int y = 0; y < MatchState.Height; y++)
        {
            for (int x = 0; x < MatchState.Width; x++)
            {
                if (MatchRules.CanPlaceAt(state, x, y))
                    openCellIndices.Add(y * MatchState.Width + x);
            }
        }

        if (state.EnemySummonsRemaining <= 0 || playableCardId < 0 || openCellIndices.Count == 0)
        {
            if (TryFindAttack(state, out long attackerId, out CardDirection direction))
            {
                submitAction(new AttackCardAction(state.Id, PlayerSide.Enemy, attackerId,
                    direction.X, direction.Y));
                return;
            }

            submitAction(new EndTurnAction(state.Id, PlayerSide.Enemy));
            return;
        }

        int cellIndex = openCellIndices[random.Next(openCellIndices.Count)];
        submitAction(new PlaceCardAction(state.Id, PlayerSide.Enemy, playableCardId,
            cellIndex % MatchState.Width, cellIndex / MatchState.Width));
    }

    private static bool TryFindEquipPlay(MatchState state, out PlayEquipCardAction action)
    {
        foreach (CardInstance equipment in state.EnemyHand)
        {
            if (equipment.Definition.Type != CardType.Equip) continue;
            for (int y = 0; y < MatchState.Height; y++)
            {
                for (int x = 0; x < MatchState.Width; x++)
                {
                    CardInstance target = state.GetCardAt(x, y);
                    if (target == null || !MatchRules.CanPlayEquipCard(state, PlayerSide.Enemy, equipment.Id, target.Id))
                        continue;

                    action = new PlayEquipCardAction(state.Id, PlayerSide.Enemy, equipment.Id, target.Id);
                    return true;
                }
            }
        }

        action = null;
        return false;
    }

    private static bool TryFindAttack(MatchState state, out long attackerId, out CardDirection direction)
    {
        for (int y = 0; y < MatchState.Height; y++)
        {
            for (int x = 0; x < MatchState.Width; x++)
            {
                CardInstance card = state.GetCardAt(x, y);
                if (card == null || state.GetCellOwner(card.X, card.Y) != PlayerSide.Enemy
                    || card.Definition.Type != CardType.Monster)
                    continue;

                for (int i = 0; i < card.AttackDirections.Count; i++)
                {
                    CardDirection candidate = card.AttackDirections[i];
                    if (MatchRules.CanAttack(state, PlayerSide.Enemy, card.Id, candidate))
                    {
                        attackerId = card.Id;
                        direction = candidate;
                        return true;
                    }
                }
            }
        }

        attackerId = -1;
        direction = default(CardDirection);
        return false;
    }
}
