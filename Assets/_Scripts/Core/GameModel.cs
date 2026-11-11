using System;
using System.Collections.Generic;
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
    Field
}

public interface ISupportEffect
{
    string Description { get; }
    void Resolve(SupportEffectContext context);
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
        Action<IMonsterEffectOperation> applyOperation)
    {
        Actor = actor;
        Source = source ?? throw new ArgumentNullException(nameof(source));
        TurnHistory = turnHistory;
        this.applyOperation = applyOperation ?? throw new ArgumentNullException(nameof(applyOperation));
    }

    public void AddOperation(IMonsterEffectOperation operation)
    {
        if (operation == null) throw new ArgumentNullException(nameof(operation));
        applyOperation(operation);
    }
}

public abstract class MonsterEffectData : ScriptableObject, IMonsterEffect
{
    public abstract string Description { get; }
    public abstract void Resolve(MonsterEffectContext context);
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

    internal SupportEffectContext(PlayerSide actor, IReadOnlyList<MatchTurn> turnHistory,
        Action<ISupportEffectOperation> applyOperation)
    {
        Actor = actor;
        TurnHistory = turnHistory;
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
}

public sealed class CardInstance
{
    public long Id { get; }
    public CardDefinition Definition { get; }
    public PlayerSide Owner { get; private set; }
    public int CurrentHealth { get; internal set; }
    public int X { get; internal set; }
    public int Y { get; internal set; }
    public int RemainAttacks { get; internal set; } = 1;

    internal CardInstance(long id, CardDefinition definition, PlayerSide owner)
    {
        Id = id;
        Definition = definition;
        Owner = owner;
        CurrentHealth = definition.Health;
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
    private readonly CardInstance[,] board = new CardInstance[Width, Height];
    private readonly PlayerSide[,] cellOwners = new PlayerSide[Width, Height];
    private readonly List<MatchTurn> turnHistory = new List<MatchTurn>();

    public IReadOnlyList<CardInstance> PlayerDeck => playerDeck;
    public IReadOnlyList<CardInstance> PlayerHand => playerHand;
    public IReadOnlyList<CardInstance> EnemyDeck => enemyDeck;
    public IReadOnlyList<CardInstance> EnemyHand => enemyHand;

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

    public CardInstance GetCardAt(int x, int y)
    {
        if (x < 0 || x >= Width) throw new System.ArgumentOutOfRangeException(nameof(x));
        if (y < 0 || y >= Height) throw new System.ArgumentOutOfRangeException(nameof(y));
        return board[x, y];
    }

    public PlayerSide GetCellOwner(int x, int y)
    {
        GetCardAt(x, y);
        return cellOwners[x, y];
    }

    internal void SwitchCellOwner(int x, int y)
    {
        if (GetCardAt(x, y) == null) return;
        cellOwners[x, y] = cellOwners[x, y] == PlayerSide.Player ? PlayerSide.Enemy : PlayerSide.Player;
    }

    internal void PlaceCard(int x, int y, CardInstance card, PlayerSide cellOwner)
    {
        card.X = x;
        card.Y = y;
        board[x, y] = card;
        cellOwners[x, y] = cellOwner;
    }

    internal void RemoveCardAt(int x, int y)
    {
        CardInstance card = board[x, y];
        if (card == null) return;
        card.X = -1;
        card.Y = -1;
        board[x, y] = null;
        cellOwners[x, y] = default(PlayerSide);
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
        random = new System.Random(seed);
    }
}
