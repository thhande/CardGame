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

        if (state.EnemySummonsRemaining > 0
            && TryFindPlacement(state, out PlaceCardAction placement))
        {
            submitAction(placement);
            return;
        }

        if (TryFindAttack(state, out long attackerId, out CardDirection direction))
        {
            submitAction(new AttackCardAction(state.Id, PlayerSide.Enemy, attackerId, direction.X, direction.Y));
            return;
        }

        submitAction(new EndTurnAction(state.Id, PlayerSide.Enemy));
    }

    private bool TryFindPlacement(MatchState state, out PlaceCardAction action)
    {
        List<(int X, int Y)> targets = new List<(int, int)>();
        foreach (CardInstance card in state.EnemyHand)
        {
            if (card.Definition.Type != CardType.Monster) continue;
            for (int y = 0; y < MatchState.Height; y++)
            {
                for (int x = 0; x < MatchState.Width; x++)
                {
                    if (MatchRules.CanPlaceAt(state, card, PlayerSide.Enemy, x, y)) targets.Add((x, y));
                }
            }

            int sacrificeCount = card.Definition.SacrificeCount;
            if (targets.Count < Math.Max(1, sacrificeCount)) continue;
            if (sacrificeCount <= 1)
            {
                var target = targets[random.Next(targets.Count)];
                action = new PlaceCardAction(state.Id, PlayerSide.Enemy, card.Id, target.X, target.Y);
                return true;
            }

            long[] sacrificeIds = new long[sacrificeCount];
            (int X, int Y) destination = default;
            for (int i = 0; i < sacrificeIds.Length; i++)
            {
                int index = random.Next(targets.Count);
                destination = targets[index];
                targets.RemoveAt(index);
                sacrificeIds[i] = state.GetCardAt(destination.X, destination.Y).Id;
            }
            action = new PlaceCardAction(state.Id, PlayerSide.Enemy, card.Id, destination.X, destination.Y, sacrificeIds);
            return true;
        }

        action = null;
        return false;
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
