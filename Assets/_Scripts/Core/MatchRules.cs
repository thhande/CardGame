using System;
using System.Collections.Generic;

public abstract class MatchAction
{
    public Guid MatchId { get; }
    public PlayerSide Actor { get; }

    protected MatchAction(Guid matchId, PlayerSide actor)
    {
        MatchId = matchId;
        Actor = actor;
    }
}

public sealed class PlaceCardAction : MatchAction
{
    public long CardId { get; }
    public int X { get; }
    public int Y { get; }
    public IReadOnlyList<long> SacrificeIds { get; }

    public PlaceCardAction(Guid matchId, PlayerSide actor, long cardId, int x, int y,
        IEnumerable<long> sacrificeIds = null) : base(matchId, actor)
    {
        CardId = cardId;
        X = x;
        Y = y;
        SacrificeIds = sacrificeIds == null ? Array.Empty<long>() : new List<long>(sacrificeIds).AsReadOnly();
    }
}

public sealed class PlaySupportCardAction : MatchAction
{
    public long CardId { get; }
    public (int X, int Y)? TargetCell { get; }

    public PlaySupportCardAction(Guid matchId, PlayerSide actor, long cardId) : base(matchId, actor)
    {
        CardId = cardId;
    }

    public PlaySupportCardAction(Guid matchId, PlayerSide actor, long cardId, int x, int y)
        : this(matchId, actor, cardId)
    {
        TargetCell = (x, y);
    }
}

public sealed class PlayEquipCardAction : MatchAction
{
    public long CardId { get; }
    public long TargetId { get; }

    public PlayEquipCardAction(Guid matchId, PlayerSide actor, long cardId, long targetId) : base(matchId, actor)
    {
        CardId = cardId;
        TargetId = targetId;
    }
}

public sealed class ActivateMonsterEffectAction : MatchAction
{
    public long CardId { get; }

    public ActivateMonsterEffectAction(Guid matchId, PlayerSide actor, long cardId)
        : base(matchId, actor)
    {
        CardId = cardId;
    }
}

public sealed class AttackCardAction : MatchAction
{
    public long AttackerId { get; }
    public CardDirection Direction { get; }

    public AttackCardAction(Guid matchId, PlayerSide actor, long attackerId, int directionX, int directionY)
        : base(matchId, actor)
    {
        AttackerId = attackerId;
        Direction = new CardDirection(directionX, directionY);
    }
}

public sealed class EndTurnAction : MatchAction
{
    public EndTurnAction(Guid matchId, PlayerSide actor) : base(matchId, actor) { }
}

public static class MatchRules
{
    private const int PlayerSummonsPerTurn = 1;
    private const int EnemySummonsPerTurn = 1;

    public static MatchState CreateMatch(List<CardDefinition> definition, int deckSize, int openingHandSize, int seed)
    {
        return CreateMatch(definition, definition, deckSize, openingHandSize, seed);
    }

    public static MatchState CreateMatch(List<CardDefinition> playerDefinitions,
        List<CardDefinition> enemyDefinitions, int deckSize, int openingHandSize, int seed)
    {
        if (playerDefinitions == null) throw new ArgumentNullException(nameof(playerDefinitions));
        if (enemyDefinitions == null) throw new ArgumentNullException(nameof(enemyDefinitions));
        if (deckSize < 0) throw new ArgumentOutOfRangeException(nameof(deckSize));
        if (openingHandSize < 0 || openingHandSize > deckSize)
            throw new ArgumentOutOfRangeException(nameof(openingHandSize));

        MatchState state = new MatchState(seed);
        Random random = new Random(seed);
        BuildDeck(state, state.MutablePlayerDeck, playerDefinitions, PlayerSide.Player, random);
        BuildDeck(state, state.MutableEnemyDeck, enemyDefinitions, PlayerSide.Enemy, random);
        DrawCards(state.MutablePlayerDeck, state.MutablePlayerHand, openingHandSize);
        DrawCards(state.MutableEnemyDeck, state.MutableEnemyHand, openingHandSize);
        BeginPlayerTurn(state, false);
        return state;
    }

    public static bool TryApply(MatchState state, MatchAction action)
    {
        if (state == null || action == null || action.MatchId != state.Id
            || state.Turn == TurnState.StartGame || state.Turn == TurnState.GameOver) return false;

        MatchTurn turn = state.CurrentTurn;
        if (!TryApplyAction(state, action)) return false;

        turn.RecordAction(action);
        if (action is EndTurnAction) turn.IsComplete = true;
        return true;
    }

    private static bool TryApplyAction(MatchState state, MatchAction action)
    {
        if (action is EndTurnAction endTurn)
        {
            if (endTurn.Actor != state.CurrentTurn.Actor) return false;

            if (IsBoardFull(state) || BothRanOutOfCard(state)) state.Turn = TurnState.GameOver;
            else if (endTurn.Actor == PlayerSide.Player) StartEnemyTurn(state);
            else BeginPlayerTurn(state, true);
            return true;
        }

        if (action is AttackCardAction attack)
            return TryApplyAttack(state, attack);

        if (action is PlaySupportCardAction supportPlay)
            return TryApplySupportCard(state, supportPlay);

        if (action is PlayEquipCardAction equipPlay)
            return TryApplyEquipCard(state, equipPlay);

        if (action is ActivateMonsterEffectAction monsterEffect)
            return TryApplyMonsterEffect(state, monsterEffect);

        if (!(action is PlaceCardAction placement)) return false;

        List<CardInstance> hand;
        int summons;
        if (placement.Actor == PlayerSide.Player && state.Turn == TurnState.PlayerTurn)
        {
            hand = state.MutablePlayerHand;
            summons = state.PlayerSummonsRemaining;
        }
        else if (placement.Actor == PlayerSide.Enemy && state.Turn == TurnState.EnemyTurn)
        {
            hand = state.MutableEnemyHand;
            summons = state.EnemySummonsRemaining;
        }
        else
        {
            return false;
        }

        if (summons <= 0) return false;
        int handIndex = hand.FindIndex(card => card.Id == placement.CardId);
        if (handIndex < 0) return false;
        CardInstance card = hand[handIndex];
        if (card.Owner != placement.Actor || card.Definition.Type != CardType.Monster) return false;
        if (!TryPayPlacementCost(state, card, placement)) return false;

        state.PlaceCard(placement.X, placement.Y, card, placement.Actor);
        hand.RemoveAt(handIndex);
        if (placement.Actor == PlayerSide.Player) state.PlayerSummonsRemaining--;
        else state.EnemySummonsRemaining--;

        return true;
    }

    public static bool BothRanOutOfCard(MatchState state)
    {
        return state.EnemyDeck.Count == 0 && state.EnemyHand.Count == 0
        && state.PlayerHand.Count == 0 && state.PlayerDeck.Count == 0;
    }

    public static bool CanPlaySupportCard(MatchState state, PlayerSide actor, long cardId)
    {
        return CanPlaySupportCard(state, actor, cardId, out _);
    }

    internal static bool CanPlaySupportCard(MatchState state, PlayerSide actor, long cardId,
        out (int X, int Y)? targetCell)
    {
        targetCell = null;
        int handIndex = FindPlayableSupportCard(state, actor, cardId, out List<CardInstance> hand);
        if (handIndex < 0) return false;
        if (!(hand[handIndex].Definition.SupportEffect is ICellTargetedSupportEffect effect)) return true;

        for (int y = 0; y < MatchState.Height; y++)
        {
            for (int x = 0; x < MatchState.Width; x++)
            {
                if (!effect.CanTargetCell(state, actor, x, y)) continue;
                targetCell = (x, y);
                return true;
            }
        }

        return false;
    }

    public static bool CanPlaceAt(MatchState state, int x, int y)
    {
        return state != null && IsOnBoard(x, y)
            && state.GetCellState(x, y) == BoardCellState.Empty;
    }

    internal static bool CanPlaceAt(MatchState state, CardInstance card, PlayerSide actor, int x, int y)
    {
        return card.Definition.SacrificeCount == 0
            ? CanPlaceAt(state, x, y)
            : IsOnBoard(x, y) && CanSacrifice(state, actor, state.GetCardAt(x, y));
    }

    public static bool CanSacrifice(MatchState state, PlayerSide actor, CardInstance card)
    {
        return state != null && card != null && card.IsAlive && card.Definition.Type == CardType.Monster
            && IsOnBoard(card.X, card.Y) && state.GetCardAt(card.X, card.Y) == card
            && state.GetCellOwner(card.X, card.Y) == actor;
    }

    private static bool TryPayPlacementCost(MatchState state, CardInstance card, PlaceCardAction placement)
    {
        if (!CanPlaceAt(state, card, placement.Actor, placement.X, placement.Y)) return false;
        int count = card.Definition.SacrificeCount;
        if (count == 0) return placement.SacrificeIds.Count == 0;

        CardInstance destination = state.GetCardAt(placement.X, placement.Y);
        if (count == 1 && placement.SacrificeIds.Count == 0)
        {
            Sacrifice(state, destination);
            return true;
        }

        if (placement.SacrificeIds.Count != count || placement.SacrificeIds[count - 1] != destination.Id)
            return false;

        List<CardInstance> sacrifices = new List<CardInstance>();
        foreach (long id in placement.SacrificeIds)
        {
            CardInstance sacrifice = FindBoardCard(state, id);
            if (!CanSacrifice(state, placement.Actor, sacrifice) || sacrifices.Contains(sacrifice)) return false;
            sacrifices.Add(sacrifice);
        }

        foreach (CardInstance sacrifice in sacrifices) Sacrifice(state, sacrifice);
        return true;
    }

    private static void Sacrifice(MatchState state, CardInstance sacrifice)
    {
        state.RemoveCardAt(sacrifice.X, sacrifice.Y);
        state.DiscardCard(sacrifice);
    }

    private static bool IsOnBoard(int x, int y)
    {
        return x >= 0 && x < MatchState.Width && y >= 0 && y < MatchState.Height;
    }

    public static bool CanAttack(MatchState state, PlayerSide actor, long attackerId, CardDirection direction)
    {
        return TryGetAttackTarget(state, actor, attackerId, direction, out _, out _);
    }

    public static bool CanPlayEquipCard(MatchState state, PlayerSide actor, long cardId, long targetId)
    {
        return FindEquipTarget(state, actor, cardId, targetId, out _) != null;
    }

    public static bool CanActivateMonsterEffect(MatchState state, PlayerSide actor, long cardId)
    {
        return FindActivatableMonster(state, actor, cardId, out _) != null;
    }

    private static void StartEnemyTurn(MatchState state)
    {
        if (state == null || state.Turn == TurnState.GameOver) return;
        state.StartTurn(PlayerSide.Enemy);
        state.EnemySummonsRemaining = EnemySummonsPerTurn;
        ResetAttackAvailability(state, PlayerSide.Enemy);
        DrawCards(state.MutableEnemyDeck, state.MutableEnemyHand, 1);
        ResolveTurnStartEffects(state, PlayerSide.Enemy);
    }

    private static bool TryApplySupportCard(MatchState state, PlaySupportCardAction action)
    {
        int handIndex = FindPlayableSupportCard(state, action.Actor, action.CardId, out List<CardInstance> hand);
        if (handIndex < 0) return false;

        CardInstance card = hand[handIndex];
        CardDefinition definition = card.Definition;
        if (definition.SupportEffect is ICellTargetedSupportEffect effect)
        {
            if (!action.TargetCell.HasValue) return false;
            (int x, int y) = action.TargetCell.Value;
            if (!IsOnBoard(x, y) || !effect.CanTargetCell(state, action.Actor, x, y)) return false;
        }
        else if (action.TargetCell.HasValue) return false;

        hand.RemoveAt(handIndex);
        state.DiscardCard(card);
        if (definition.Type == CardType.HiSupporter) state.CurrentTurn.HasPlayedHiSupporter = true;
        SupportEffectContext context = new SupportEffectContext(
            action.Actor, state.TurnHistory, action.TargetCell,
            operation => operation.Apply(state, action.Actor));
        definition.SupportEffect.Resolve(context);
        return true;
    }

    private static int FindPlayableSupportCard(MatchState state, PlayerSide actor, long cardId,
        out List<CardInstance> hand)
    {
        hand = null;
        if (state == null || (actor != PlayerSide.Player && actor != PlayerSide.Enemy)
            || state.Turn != (actor == PlayerSide.Player ? TurnState.PlayerTurn : TurnState.EnemyTurn))
            return -1;

        hand = actor == PlayerSide.Player ? state.MutablePlayerHand : state.MutableEnemyHand;
        int index = hand.FindIndex(card => card.Id == cardId);
        return index >= 0 && IsSupportCardPlayable(state, actor, hand[index]) ? index : -1;
    }

    private static bool IsSupportCardPlayable(MatchState state, PlayerSide actor, CardInstance card)
    {
        CardDefinition definition = card.Definition;
        if (card.Owner != actor || !definition.IsSupportCard
            || definition.SupportEffect == null || definition.SupportCondition == null)
            return false;
        if (definition.Type == CardType.HiSupporter && state.CurrentTurn.HasPlayedHiSupporter)
            return false;
        return definition.SupportCondition.IsMet(state, actor);
    }

    private static bool TryApplyEquipCard(MatchState state, PlayEquipCardAction action)
    {
        CardInstance target = FindEquipTarget(state, action.Actor, action.CardId, action.TargetId, out int handIndex);
        if (target == null) return false;

        List<CardInstance> hand = action.Actor == PlayerSide.Player ? state.MutablePlayerHand : state.MutableEnemyHand;
        state.AttachEquipment(target, hand[handIndex]);
        hand.RemoveAt(handIndex);
        return true;
    }

    private static CardInstance FindEquipTarget(MatchState state, PlayerSide actor, long cardId, long targetId,
        out int handIndex)
    {
        handIndex = -1;
        if (state == null || (actor != PlayerSide.Player && actor != PlayerSide.Enemy)
            || state.Turn != (actor == PlayerSide.Player ? TurnState.PlayerTurn : TurnState.EnemyTurn))
            return null;

        List<CardInstance> hand = actor == PlayerSide.Player ? state.MutablePlayerHand : state.MutableEnemyHand;
        handIndex = hand.FindIndex(card => card.Id == cardId && card.Owner == actor
            && card.Definition.Type == CardType.Equip && card.Definition.EquipEffect != null);
        if (handIndex < 0) return null;

        CardInstance target = FindBoardCard(state, targetId);
        return target != null && target.Definition.Type == CardType.Monster && target.CurrentHealth > 0
            && state.GetCellOwner(target.X, target.Y) == actor ? target : null;
    }

    private static bool TryApplyMonsterEffect(MatchState state, ActivateMonsterEffectAction action)
    {
        CardInstance card = FindActivatableMonster(state, action.Actor, action.CardId, out IMonsterEffect effect);
        if (card == null) return false;

        MonsterEffectContext context = new MonsterEffectContext(
            action.Actor, card, state.TurnHistory,
            operation => operation.Apply(state, action.Actor, card));
        effect.Resolve(context);
        return true;
    }

    private static CardInstance FindActivatableMonster(MatchState state, PlayerSide actor, long cardId,
        out IMonsterEffect effect)
    {
        effect = null;
        if (state == null || (actor != PlayerSide.Player && actor != PlayerSide.Enemy)
            || (actor == PlayerSide.Player && state.Turn != TurnState.PlayerTurn)
            || (actor == PlayerSide.Enemy && state.Turn != TurnState.EnemyTurn))
            return null;

        CardInstance card = FindBoardCard(state, cardId);
        if (card == null || card.Owner != actor || card.Definition.Type != CardType.Monster)
            return null;

        MonsterEffectContext context = null;
        foreach (MonsterEffectEntry entry in card.Definition.MonsterEffects)
        {
            if (entry == null || entry.ActivateType != MonsterEffectType.ChooseToTrigger) continue;
            context ??= new MonsterEffectContext(actor, card, state.TurnHistory);
            if (!entry.Effect.ActiveCondition(context)) continue;

            effect = entry.Effect;
            return card;
        }

        return null;
    }

    private static CardInstance FindBoardCard(MatchState state, long cardId)
    {
        for (int y = 0; y < MatchState.Height; y++)
        {
            for (int x = 0; x < MatchState.Width; x++)
            {
                CardInstance card = state.GetCardAt(x, y);
                if (card != null && card.Id == cardId) return card;
            }
        }
        return null;
    }

    private static void BeginPlayerTurn(MatchState state, bool drawCard)
    {
        if (state == null || (state.Turn != TurnState.StartGame && state.Turn != TurnState.EnemyTurn)) return;
        state.StartTurn(PlayerSide.Player);
        state.PlayerSummonsRemaining = PlayerSummonsPerTurn;
        ResetAttackAvailability(state, PlayerSide.Player);
        if (drawCard) DrawCards(state.MutablePlayerDeck, state.MutablePlayerHand, 1);
        ResolveTurnStartEffects(state, PlayerSide.Player);
    }

    private static void ResolveTurnStartEffects(MatchState state, PlayerSide actor)
    {
        for (int y = 0; y < MatchState.Height; y++)
        {
            for (int x = 0; x < MatchState.Width; x++)
            {
                CardInstance card = state.GetCardAt(x, y);
                if (card == null || !card.IsAlive || state.GetCellOwner(x, y) != actor) continue;
                foreach (MonsterEffectEntry entry in card.Definition.MonsterEffects)
                {
                    if (entry?.ActivateType != MonsterEffectType.TurnStarted) continue;
                    MonsterEffectContext context = new MonsterEffectContext(actor, card, state.TurnHistory,
                        operation => operation.Apply(state, actor, card));
                    if (entry.Effect.ActiveCondition(context)) entry.Effect.Resolve(context);
                }
            }
        }
    }

    private static bool TryApplyAttack(MatchState state, AttackCardAction action)
    {
        if (!TryGetAttackTarget(state, action.Actor, action.AttackerId, action.Direction,
                out CardInstance attacker, out CardInstance target))
            return false;

        attacker.RemainAttacks -= 1;
        if (!PreventsAttackDamage(target, attacker))
            target.TakeDamage(GetAttackDamage(state, attacker, target));
        if (HasDirection(target.AttackDirections, new CardDirection(-action.Direction.X, -action.Direction.Y))
            && !PreventsReflectDamage(state, attacker))
        {
            attacker.TakeDamage(target.Defense);
        }

        if (!target.IsAlive) state.ResolveDefeat(target);
        if (!attacker.IsAlive) state.ResolveDefeat(attacker);
        return true;
    }

    private static int GetAttackDamage(MatchState state, CardInstance attacker, CardInstance target)
    {
        foreach (MonsterEffectEntry entry in attacker.Definition.MonsterEffects)
        {
            if (entry?.ActivateType == MonsterEffectType.OnAttack
                && entry.Effect is IAttackDamageOverride damageOverride
                && damageOverride.TryGetAttackDamage(attacker, target, out int damage))
                return damage;
        }
        return GetAttack(state, attacker);
    }

    public static int GetAttack(MatchState state, CardInstance card)
    {
        int attack = card.Definition.Attack;
        if (state == null || !card.IsAlive || card.Definition.Type != CardType.Monster
            || !IsOnBoard(card.X, card.Y) || state.GetCardAt(card.X, card.Y) != card) return attack;

        foreach (MonsterEffectEntry entry in card.Definition.MonsterEffects)
        {
            if (entry?.Effect is IMonsterCombatModifier modifier && modifier.IsActive(state, card))
                attack += modifier.AttackBonus;
        }

        PlayerSide controller = state.GetCellOwner(card.X, card.Y);
        for (int y = 0; y < MatchState.Height; y++)
        {
            for (int x = 0; x < MatchState.Width; x++)
            {
                CardInstance source = state.GetCardAt(x, y);
                if (source == null || source == card || !source.IsAlive || state.GetCellOwner(x, y) != controller)
                    continue;
                foreach (MonsterEffectEntry entry in source.Definition.MonsterEffects)
                {
                    if (entry?.ActivateType == MonsterEffectType.Aura && entry.Effect is IMonsterAttackAura aura)
                        attack += aura.GetAttackBonus(source, card);
                }
            }
        }
        return attack;
    }

    private static bool PreventsReflectDamage(MatchState state, CardInstance attacker)
    {
        foreach (MonsterEffectEntry entry in attacker.Definition.MonsterEffects)
        {
            if (entry?.Effect is IMonsterCombatModifier modifier
                && modifier.PreventsReflectDamage && modifier.IsActive(state, attacker))
                return true;
        }
        return false;
    }

    private static bool PreventsAttackDamage(CardInstance target, CardInstance attacker)
    {
        foreach (MonsterEffectEntry entry in target.Definition.MonsterEffects)
        {
            if (entry?.ActivateType == MonsterEffectType.OnAttacked
                && entry.Effect is IAttackDamagePrevention prevention
                && prevention.PreventsAttackDamage(target, attacker))
                return true;
        }
        return false;
    }

    private static bool TryGetAttackTarget(MatchState state, PlayerSide actor, long attackerId,
        CardDirection direction, out CardInstance attacker, out CardInstance target)
    {
        attacker = null;
        target = null;
        if (state == null
            || (actor == PlayerSide.Player && state.Turn != TurnState.PlayerTurn)
            || (actor == PlayerSide.Enemy && state.Turn != TurnState.EnemyTurn))
            return false;
        if (!direction.IsAttackDirection) return false;

        for (int y = 0; y < MatchState.Height; y++)
        {
            for (int x = 0; x < MatchState.Width; x++)
            {
                CardInstance candidate = state.GetCardAt(x, y);
                if (candidate != null && candidate.Id == attackerId)
                {
                    attacker = candidate;
                    break;
                }
            }
            if (attacker != null) break;
        }

        if (attacker == null || state.GetCellOwner(attacker.X, attacker.Y) != actor
            || attacker.Definition.Type != CardType.Monster
            || attacker.RemainAttacks <= 0) return false;
        if (attacker.CurrentHealth <= 0) return false;
        if (!HasDirection(attacker.AttackDirections, direction)) return false;

        int targetX = attacker.X + direction.X;
        int targetY = attacker.Y + direction.Y;
        if (targetX < 0 || targetX >= MatchState.Width || targetY < 0 || targetY >= MatchState.Height)
            return false;

        target = state.GetCardAt(targetX, targetY);
        return target != null && target.CurrentHealth > 0
            && state.GetCellOwner(targetX, targetY) != state.GetCellOwner(attacker.X, attacker.Y);
    }

    private static bool HasDirection(IReadOnlyList<CardDirection> directions, CardDirection requested)
    {
        for (int i = 0; i < directions.Count; i++)
        {
            if (directions[i].Equals(requested)) return true;
        }
        return false;
    }

    private static void ResetAttackAvailability(MatchState state, PlayerSide owner)
    {
        for (int y = 0; y < MatchState.Height; y++)
        {
            for (int x = 0; x < MatchState.Width; x++)
            {
                CardInstance card = state.GetCardAt(x, y);
                if (card != null && state.GetCellOwner(x, y) == owner) card.RemainAttacks = 1;
            }
        }
    }

    public static bool IsBoardFull(MatchState state)
    {
        if (state == null) return false;
        for (int y = 0; y < MatchState.Height; y++)
        {
            for (int x = 0; x < MatchState.Width; x++)
            {
                if (state.GetCellState(x, y) != BoardCellState.Occupied) return false;
            }
        }
        return true;
    }

    public static int CountOwnedCards(MatchState state, PlayerSide owner)
    {
        if (state == null) return 0;

        int count = 0;
        for (int y = 0; y < MatchState.Height; y++)
        {
            for (int x = 0; x < MatchState.Width; x++)
            {
                CardInstance card = state.GetCardAt(x, y);
                if (card != null && state.GetCellOwner(x, y) == owner) count++;
            }
        }
        return count;
    }

    private static void BuildDeck(MatchState state, List<CardInstance> deck, List<CardDefinition> definition,
        PlayerSide owner, Random random)
    {
        for (int i = 0; i < definition.Count; i++) deck.Add(state.CreateCard(definition[i], owner));
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int index = random.Next(i + 1);
            CardInstance card = deck[i];
            deck[i] = deck[index];
            deck[index] = card;
        }
    }

    private static void DrawCards(List<CardInstance> deck, List<CardInstance> hand, int count)
    {
        for (int i = 0; i < count && deck.Count > 0; i++)
        {
            hand.Add(deck[0]);
            deck.RemoveAt(0);
        }
    }
}
