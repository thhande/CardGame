using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PlayerSide
{
    Player,
    Enemy
}

public enum CardType
{
    Monster,
    Supporter,
    HiSupporter,
    Field,
    Equip
}

public sealed class EquipEffect
{
    public int DefenseBonus { get; }
    public int HealthBonus { get; }
    public bool RemovesRandomAttackArrow { get; }
    public string Description { get; }

    public EquipEffect(int defenseBonus, int healthBonus, bool removesRandomAttackArrow, string description)
    {
        DefenseBonus = defenseBonus;
        HealthBonus = healthBonus;
        RemovesRandomAttackArrow = removesRandomAttackArrow;
        Description = description;
    }
}

public interface ISupportEffect
{
    string Description { get; }
    void Resolve(SupportEffectContext context);
}

public interface ICellTargetedSupportEffect : ISupportEffect
{
    string TargetPrompt { get; }
    bool CanTargetCell(MatchState state, PlayerSide actor, int x, int y);
}

public interface ISupportEffectOperation
{
    void Apply(MatchState state, PlayerSide actor);
}

public interface ISupportCondition
{
    string Description { get; }
    bool IsMet(MatchState state, PlayerSide actor);
}

public enum MonsterEffectType
{
    AutoTrigger,
    ChooseToTrigger,
    PlacedDown,
    ToDiscardPile,
    Die
}

public sealed class MonsterEffectEntry
{
    public MonsterEffectType ActivateType { get; }
    public IMonsterEffect Effect { get; }

    public MonsterEffectEntry(MonsterEffectType activateType, IMonsterEffect effect)
    {
        ActivateType = activateType;
        Effect = effect ?? throw new ArgumentNullException(nameof(effect));
    }
}

public interface IMonsterEffect
{
    string Description { get; }
    bool ActiveCondition(MonsterEffectContext context);
    void Resolve(MonsterEffectContext context);
}

public interface IMonsterEffectOperation
{
    void Apply(MatchState state, PlayerSide actor, CardInstance source);
}

public sealed class MonsterEffectContext
{
    private readonly Action<IMonsterEffectOperation> applyOperation;

    public PlayerSide Actor { get; }
    public CardInstance Source { get; }
    public IReadOnlyList<MatchTurn> TurnHistory { get; }

    internal MonsterEffectContext(PlayerSide actor, CardInstance source, IReadOnlyList<MatchTurn> turnHistory,
        Action<IMonsterEffectOperation> applyOperation = null)
    {
        Actor = actor;
        Source = source ?? throw new ArgumentNullException(nameof(source));
        TurnHistory = turnHistory;
        this.applyOperation = applyOperation;
    }

    public void AddOperation(IMonsterEffectOperation operation)
    {
        if (operation == null) throw new ArgumentNullException(nameof(operation));
        if (applyOperation == null) throw new InvalidOperationException("Operations can only be added while resolving a monster effect.");
        applyOperation(operation);
    }
}

public abstract class MonsterEffectData : ScriptableObject, IMonsterEffect
{
    public abstract string Description { get; }
    public abstract void Resolve(MonsterEffectContext context);
    public virtual bool ActiveCondition(MonsterEffectContext context) => true;
}

public sealed class ReturnMonsterToDeckAndDrawOperation : IMonsterEffectOperation
{
    private readonly int cardsToDraw;

    public ReturnMonsterToDeckAndDrawOperation(int cardsToDraw)
    {
        if (cardsToDraw < 0) throw new ArgumentOutOfRangeException(nameof(cardsToDraw));
        this.cardsToDraw = cardsToDraw;
    }

    public void Apply(MatchState state, PlayerSide actor, CardInstance source)
    {
        state.ReturnBoardCardToDeck(source, actor);
        state.DrawCards(actor, cardsToDraw);
    }
}

public sealed class SupportEffectContext
{
    private readonly Action<ISupportEffectOperation> applyOperation;

    public PlayerSide Actor { get; }
    public IReadOnlyList<MatchTurn> TurnHistory { get; }
    public (int X, int Y)? TargetCell { get; }

    internal SupportEffectContext(PlayerSide actor, IReadOnlyList<MatchTurn> turnHistory,
        (int X, int Y)? targetCell, Action<ISupportEffectOperation> applyOperation)
    {
        Actor = actor;
        TurnHistory = turnHistory;
        TargetCell = targetCell;
        this.applyOperation = applyOperation ?? throw new ArgumentNullException(nameof(applyOperation));
    }

    public void AddOperation(ISupportEffectOperation operation)
    {
        if (operation == null) throw new ArgumentNullException(nameof(operation));
        applyOperation(operation);
    }
}



public enum TurnState
{
    StartGame,
    PlayerTurn,
    EnemyTurn,
    GameOver
}

public readonly struct CardDirection : IEquatable<CardDirection>
{
    public int X { get; }
    public int Y { get; }
    public bool IsAttackDirection => X >= -1 && X <= 1 && Y >= -1 && Y <= 1 && (X != 0 || Y != 0);

    public CardDirection(int x, int y)
    {
        X = x;
        Y = y;
    }

    public bool Equals(CardDirection other) => X == other.X && Y == other.Y;
    public override bool Equals(object obj) => obj is CardDirection other && Equals(other);
    public override int GetHashCode() => (X * 397) ^ Y;
}

public sealed class CardDefinition
{
    public string Id { get; }
    public string Name { get; }
    public int Attack { get; }
    public int Defense { get; }
    public int Health { get; }
    public CardType Type { get; }
    public bool IsSupportCard => Type == CardType.Supporter || Type == CardType.HiSupporter;
    public IReadOnlyList<CardDirection> AttackDirections { get; }
    public ISupportEffect SupportEffect { get; }
    public ISupportCondition SupportCondition { get; }
    public EquipEffect EquipEffect { get; }
    public IReadOnlyList<MonsterEffectEntry> MonsterEffects { get; } = Array.Empty<MonsterEffectEntry>();

    public CardDefinition(string id, string name, int attack, int defense, int health, CardType type,
        params CardDirection[] attackDirections)
    {
        Id = id;
        Name = name;
        Attack = attack;
        Defense = defense;
        Health = health;
        Type = type;
        AttackDirections = Array.AsReadOnly(attackDirections ?? Array.Empty<CardDirection>());
        SupportEffect = null;
        SupportCondition = null;
    }

    public CardDefinition(string id, string name, int attack, int defense, int health, CardType type,
        ISupportEffect supportEffect, ISupportCondition supportCondition, params CardDirection[] attackDirections)
    {
        Id = id;
        Name = name;
        Attack = attack;
        Defense = defense;
        Health = health;
        Type = type;
        AttackDirections = Array.AsReadOnly(attackDirections ?? Array.Empty<CardDirection>());
        SupportEffect = supportEffect;
        SupportCondition = supportCondition;
    }

    public CardDefinition(string id, string name, int attack, int defense, int health, CardType type,
        MonsterEffectType monsterEffectType, IMonsterEffect monsterEffect, params CardDirection[] attackDirections)
        : this(id, name, attack, defense, health, type,
            monsterEffect == null ? Array.Empty<MonsterEffectEntry>() : new[] { new MonsterEffectEntry(monsterEffectType, monsterEffect) },
            attackDirections)
    {
    }

    public CardDefinition(string id, string name, int attack, int defense, int health, CardType type,
        IReadOnlyList<MonsterEffectEntry> monsterEffects, CardDirection[] attackDirections)
        : this(id, name, attack, defense, health, type, attackDirections)
    {
        if (monsterEffects == null) throw new ArgumentNullException(nameof(monsterEffects));
        MonsterEffects = new List<MonsterEffectEntry>(monsterEffects).AsReadOnly();
    }

    public CardDefinition(string id, string name, EquipEffect equipEffect)
        : this(id, name, 0, 0, 0, CardType.Equip)
    {
        EquipEffect = equipEffect ?? throw new ArgumentNullException(nameof(equipEffect));
    }
}

public sealed class CardInstance
{
    public long Id { get; }
    public CardDefinition Definition { get; }
    public PlayerSide Owner { get; private set; }
    public int Defense { get; private set; }
    public int MaxHealth { get; private set; }
    public int CurrentHealth { get; private set; }
    public bool CanHeal => Definition.Type == CardType.Monster && CurrentHealth > 0 && CurrentHealth < MaxHealth;
    public IReadOnlyList<CardDirection> AttackDirections { get; private set; }
    public IReadOnlyList<CardInstance> Equipment { get; private set; } = Array.Empty<CardInstance>();
    public int X { get; internal set; }
    public int Y { get; internal set; }
    public int RemainAttacks { get; internal set; } = 1;

    private List<CardInstance> equipment;

    public bool IsAlive => CurrentHealth > 0;

    internal CardInstance(long id, CardDefinition definition, PlayerSide owner)
    {
        Id = id;
        Definition = definition;
        Owner = owner;
        Defense = definition.Defense;
        MaxHealth = CurrentHealth = definition.Health;
        AttackDirections = definition.AttackDirections;
    }

    public void Heal(int amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (!CanHeal) throw new InvalidOperationException("Only wounded, living monsters can be healed.");
        CurrentHealth += Math.Min(amount, MaxHealth - CurrentHealth);
    }

    internal void TakeDamage(int amount)
    {
        CurrentHealth = Math.Max(0, CurrentHealth - amount);
    }

    internal void AttachEquipment(CardInstance card, System.Random random)
    {
        EquipEffect effect = card.Definition.EquipEffect;
        if (effect.RemovesRandomAttackArrow)
        {
            List<CardDirection> directions = AttackDirections.Where(direction => direction.IsAttackDirection)
                .Distinct().ToList();
            if (directions.Count > 0) directions.RemoveAt(random.Next(directions.Count));
            AttackDirections = directions.AsReadOnly();
        }

        if (equipment == null)
        {
            equipment = new List<CardInstance>();
            Equipment = equipment.AsReadOnly();
        }
        equipment.Add(card);
        Defense += effect.DefenseBonus;
        MaxHealth += effect.HealthBonus;
        CurrentHealth += effect.HealthBonus;
    }

    internal void ClearEquipment()
    {
        if (equipment == null || equipment.Count == 0) return;
        if (CurrentHealth > 0)
            CurrentHealth = Math.Max(1, CurrentHealth - (MaxHealth - Definition.Health));
        MaxHealth = Definition.Health;
        Defense = Definition.Defense;
        AttackDirections = Definition.AttackDirections;
        equipment.Clear();
    }

    public void ForceSetHealth(int val)
    {
        CurrentHealth = val;
    }
}

public sealed class MatchTurn
{
    private readonly List<MatchAction> actions = new List<MatchAction>();

    public int Number { get; }
    public PlayerSide Actor { get; }
    public bool IsComplete { get; internal set; }
    public bool HasPlayedHiSupporter { get; internal set; }
    public IReadOnlyList<MatchAction> Actions { get; }

    internal MatchTurn(int number, PlayerSide actor)
    {
        Number = number;
        Actor = actor;
        Actions = actions.AsReadOnly();
    }

    internal void RecordAction(MatchAction action)
    {
        actions.Add(action);
    }
}

public enum BoardCellState
{
    Empty,
    Occupied,
    Blocked
}

public sealed class MatchState
{
    public const int Width = 4;
    public const int Height = 4;

    public Guid Id { get; }
    public TurnState Turn { get; internal set; }
    public int TurnNumber => turnHistory.Count;
    public int CompletedTurnCount => TurnNumber == 0 || CurrentTurn.IsComplete ? TurnNumber : TurnNumber - 1;
    public IReadOnlyList<MatchTurn> TurnHistory { get; }
    public int PlayerSummonsRemaining { get; internal set; }
    public int EnemySummonsRemaining { get; internal set; }
    private readonly List<CardInstance> playerDeck = new List<CardInstance>();
    private readonly List<CardInstance> playerHand = new List<CardInstance>();
    private readonly List<CardInstance> enemyDeck = new List<CardInstance>();
    private readonly List<CardInstance> enemyHand = new List<CardInstance>();
    private readonly List<CardInstance> playerDiscardPile = new List<CardInstance>();
    private readonly List<CardInstance> enemyDiscardPile = new List<CardInstance>();
    private readonly BoardCell[,] board = new BoardCell[Width, Height];
    private readonly List<MatchTurn> turnHistory = new List<MatchTurn>();

    private struct BoardCell
    {
        public CardInstance Card;
        public PlayerSide Owner;
        public int BlockedFromTurn;
        public int BlockedThroughTurn;

        public bool IsBlockedOnTurn(int turn)
        {
            return BlockedFromTurn > 0 && turn >= BlockedFromTurn && turn <= BlockedThroughTurn;
        }
    }

    public IReadOnlyList<CardInstance> PlayerDeck => playerDeck;
    public IReadOnlyList<CardInstance> PlayerHand => playerHand;
    public IReadOnlyList<CardInstance> EnemyDeck => enemyDeck;
    public IReadOnlyList<CardInstance> EnemyHand => enemyHand;
    public IReadOnlyList<CardInstance> PlayerDiscardPile { get; }
    public IReadOnlyList<CardInstance> EnemyDiscardPile { get; }

    internal List<CardInstance> MutablePlayerDeck => playerDeck;
    internal List<CardInstance> MutablePlayerHand => playerHand;
    internal List<CardInstance> MutableEnemyDeck => enemyDeck;
    internal List<CardInstance> MutableEnemyHand => enemyHand;
    internal MatchTurn CurrentTurn => turnHistory[TurnNumber - 1];

    internal void StartTurn(PlayerSide actor)
    {
        Turn = actor == PlayerSide.Player ? TurnState.PlayerTurn : TurnState.EnemyTurn;
        turnHistory.Add(new MatchTurn(TurnNumber + 1, actor));
    }

    public BoardCellState GetCellState(int x, int y)
    {
        BoardCell cell = GetCell(x, y);
        if (cell.Card != null) return BoardCellState.Occupied;
        return cell.IsBlockedOnTurn(TurnNumber) ? BoardCellState.Blocked : BoardCellState.Empty;
    }

    public bool IsPlacementBlocked(int x, int y) => GetCell(x, y).IsBlockedOnTurn(TurnNumber);

    public bool IsPlacementBlockedNextTurn(int x, int y) => GetCell(x, y).IsBlockedOnTurn(TurnNumber + 1);

    public void BlockPlacementNextTurn(int x, int y)
    {
        ref BoardCell cell = ref GetCell(x, y);
        if (cell.Card != null) throw new InvalidOperationException("Only empty cells can be blocked.");

        int nextTurn = TurnNumber + 1;
        // Blocking an already blocked cell extends the restriction without opening it this turn.
        if (!cell.IsBlockedOnTurn(TurnNumber)) cell.BlockedFromTurn = nextTurn;
        cell.BlockedThroughTurn = nextTurn;
    }

    private ref BoardCell GetCell(int x, int y)
    {
        if (x < 0 || x >= Width) throw new System.ArgumentOutOfRangeException(nameof(x));
        if (y < 0 || y >= Height) throw new System.ArgumentOutOfRangeException(nameof(y));
        return ref board[x, y];
    }


    public CardInstance GetCardAt(int x, int y) => GetCell(x, y).Card;

    public PlayerSide GetCellOwner(int x, int y) => GetCell(x, y).Owner;

    public void SetCellOwner(int x, int y, PlayerSide owner)
    {
        ref BoardCell cell = ref GetCell(x, y);
        if (cell.Card == null) throw new InvalidOperationException("Only an occupied cell can have its owner changed.");
        cell.Owner = owner;
    }

    internal void SwitchCellOwner(int x, int y)
    {
        ref BoardCell cell = ref GetCell(x, y);
        if (cell.Card == null) return;
        cell.Owner = cell.Owner == PlayerSide.Player ? PlayerSide.Enemy : PlayerSide.Player;
    }

    internal void AttachEquipment(CardInstance target, CardInstance equipment)
    {
        target.AttachEquipment(equipment, random);
    }

    internal void PlaceCard(int x, int y, CardInstance card, PlayerSide cellOwner)
    {
        card.X = x;
        card.Y = y;
        ref BoardCell cell = ref GetCell(x, y);
        cell.Card = card;
        cell.Owner = cellOwner;
    }

    internal void RemoveCardAt(int x, int y)
    {
        ref BoardCell cell = ref GetCell(x, y);
        CardInstance card = cell.Card;
        if (card == null) return;
        DiscardEquipment(card);
        card.X = -1;
        card.Y = -1;
        cell.Card = null;
        cell.Owner = default(PlayerSide);
    }

    internal void DiscardCard(CardInstance card)
    {
        List<CardInstance> pile = card.Owner == PlayerSide.Player ? playerDiscardPile : enemyDiscardPile;
        pile.Add(card);
    }

    internal void DiscardEquipment(CardInstance card)
    {
        foreach (CardInstance equipment in card.Equipment)
            DiscardCard(equipment);
        card.ClearEquipment();
    }

    internal void ReturnBoardCardToDeck(CardInstance card, PlayerSide owner)
    {
        if (card == null || card.Owner != owner) throw new ArgumentException("Card does not belong to the actor.", nameof(card));
        if (GetCardAt(card.X, card.Y) != card) throw new InvalidOperationException("Card is not on the board.");

        int x = card.X;
        int y = card.Y;
        RemoveCardAt(x, y);
        List<CardInstance> deck = owner == PlayerSide.Player ? playerDeck : enemyDeck;
        deck.Add(card);
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int index = random.Next(i + 1);
            CardInstance shuffled = deck[i];
            deck[i] = deck[index];
            deck[index] = shuffled;
        }
    }

    public void DrawCards(PlayerSide actor, int count)
    {
        List<CardInstance> deck = actor == PlayerSide.Player ? playerDeck : enemyDeck;
        List<CardInstance> hand = actor == PlayerSide.Player ? playerHand : enemyHand;
        for (int i = 0; i < count && deck.Count > 0; i++)
        {
            hand.Add(deck[0]);
            deck.RemoveAt(0);
        }
    }

    private readonly System.Random random;

    internal MatchState(int seed)
    {
        Id = Guid.NewGuid();
        Turn = TurnState.StartGame;
        TurnHistory = turnHistory.AsReadOnly();
        PlayerDiscardPile = playerDiscardPile.AsReadOnly();
        EnemyDiscardPile = enemyDiscardPile.AsReadOnly();
        random = new System.Random(seed);
    }
}
