using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

/// <summary>Adapts Unity input and presentation to the platform-independent match rules.</summary>
public class MatchManager : Singleton<MatchManager>
{
    private const int DeckSize = 20;
    private const int OpeningHandSize = 5;
    private const float EnemyActionDelay = 0.5f;

    [Header("References")]
    public BoardManager boardManager;
    public List<CardData> playerTestDeck = new();
    public List<CardData> enemyTestDeck = new();

    [SerializeField] private HandUI handUI;

    private MatchState matchState;
    private IPlayerAgent enemyAgent;
    private CardInstance selectedCard;
    private SynchronizationContext unityContext;
    private readonly Dictionary<CardDefinition, Sprite> cardArtwork = new Dictionary<CardDefinition, Sprite>();

    public MatchState State => matchState;
    public TurnState CurrentState => matchState != null ? matchState.Turn : TurnState.StartGame;
    public int NormalSummonThisTurn => matchState != null ? matchState.PlayerSummonsRemaining : 0;
    public IReadOnlyList<CardInstance> PlayerDeck => matchState != null ? matchState.PlayerDeck : emptyCards;
    public IReadOnlyList<CardInstance> PlayerHand => matchState != null ? matchState.PlayerHand : emptyCards;
    public IReadOnlyList<CardInstance> EnemyDeck => matchState != null ? matchState.EnemyDeck : emptyCards;
    public IReadOnlyList<CardInstance> EnemyHand => matchState != null ? matchState.EnemyHand : emptyCards;
    public CardInstance SelectedHandCard => selectedCard;

    private static readonly IReadOnlyList<CardInstance> emptyCards = new List<CardInstance>();

    protected override void Awake()
    {
        base.Awake();
        unityContext = SynchronizationContext.Current;
        if (enemyAgent == null) enemyAgent = new RandomPlayerAgent(Random.Range(0, int.MaxValue));
    }

    public void SetEnemyAgent(IPlayerAgent agent)
    {
        if (agent == null)
        {
            Debug.LogError("Cannot assign a null player agent.");
            return;
        }

        enemyAgent = agent;
    }

    public void SetupMatch()
    {
        if (boardManager == null)
        {
            Debug.LogError("MatchManager needs a BoardManager reference.");
            return;
        }

        if (playerTestDeck == null || playerTestDeck.Count == 0)
        {
            Debug.LogError("MatchManager needs at least one card in playerTestDeck.");
            return;
        }

        if (enemyTestDeck == null || enemyTestDeck.Count == 0)
        {
            Debug.LogError("MatchManager needs at least one card in enemyTestDeck.");
            return;
        }


        boardManager.SetMatchManager(this);
        if (!boardManager.GenerateBoard()) return;
        boardManager.ResetBoard();

        cardArtwork.Clear();
        matchState = MatchRules.CreateMatch(
            playerTestDeck.Select(CreateCardDefinition).ToList(),
            enemyTestDeck.Select(CreateCardDefinition).ToList(),
            DeckSize, OpeningHandSize,
            Random.Range(0, int.MaxValue));
        selectedCard = null;
        if (enemyAgent == null) enemyAgent = new RandomPlayerAgent(Random.Range(0, int.MaxValue));
        SyncBoard();
        if (handUI != null) handUI.Initialize(this);
        else Debug.LogError("MatchManager needs a Match HUD reference.", this);
    }

    private CardDefinition CreateCardDefinition(CardData card)
    {
        CardDefinition definition = card.ToDefinition();
        cardArtwork[definition] = card.Artwork;
        return definition;
    }

    internal Sprite GetArtwork(CardDefinition definition)
    {
        cardArtwork.TryGetValue(definition, out Sprite artwork);
        return artwork;
    }

    internal bool ShowCardPreview(CardInstance card, CardHoldPreview source)
    {
        if (handUI == null || card == null) return false;
        return handUI.ShowCardPreview(card, source);
    }

    internal void HideCardPreview(CardHoldPreview source)
    {
        if (handUI != null) handUI.HideCardPreview(source);
    }

    public void SelectHandCard(CardInstance card)
    {
        if (CurrentState != TurnState.PlayerTurn || card == null || !IsInPlayerHand(card)) return;
        CardType type = card.Definition.Type;
        if (type != CardType.Equip && (type != CardType.Monster || NormalSummonThisTurn <= 0)) return;

        selectedCard = type == CardType.Equip && selectedCard == card ? null : card;
        RefreshUI();
    }

    public void OnBoardSlotClicked(BoardSlot slot)
    {
        if (slot == null || CurrentState != TurnState.PlayerTurn) return;
        if (selectedCard?.Definition.Type == CardType.Equip
            || selectedCard?.Definition.SupportEffect is ICellTargetedSupportEffect)
        {
            TryPlaceCardOnBoard(slot);
            return;
        }

        CardInstance boardCard = slot.placedCard;
        if (boardCard != null)
        {
            if (matchState.GetCellOwner(boardCard.X, boardCard.Y) == PlayerSide.Player && selectedCard != null)
            {
                selectedCard = null;
                RefreshUI();
            }
            return;
        }

        if (selectedCard != null)
            TryPlaceCardOnBoard(slot);
    }

    public void TryAttack(BoardSlot attackerSlot, CardDirection direction)
    {
        if (CurrentState != TurnState.PlayerTurn || attackerSlot == null || attackerSlot.placedCard == null) return;
        CardInstance attacker = attackerSlot.placedCard;
        SubmitAction(new AttackCardAction(matchState.Id, PlayerSide.Player, attacker.Id, direction.X, direction.Y));
    }

    public void TryPlaceCardOnBoard(BoardSlot targetSlot)
    {
        if (CurrentState != TurnState.PlayerTurn || targetSlot == null || selectedCard == null) return;
        if (selectedCard.Definition.SupportEffect is ICellTargetedSupportEffect)
        {
            SubmitAction(new PlaySupportCardAction(matchState.Id, PlayerSide.Player, selectedCard.Id,
                targetSlot.x, targetSlot.y));
            return;
        }

        if (selectedCard.Definition.Type == CardType.Equip)
        {
            if (targetSlot.placedCard != null)
                SubmitAction(new PlayEquipCardAction(matchState.Id, PlayerSide.Player, selectedCard.Id, targetSlot.placedCard.Id));
            return;
        }

        SubmitAction(new PlaceCardAction(matchState.Id, PlayerSide.Player, selectedCard.Id, targetSlot.x, targetSlot.y));
    }

    public void TryActivateMonsterEffect(BoardSlot sourceSlot)
    {
        if (CurrentState != TurnState.PlayerTurn || sourceSlot == null || sourceSlot.placedCard == null) return;
        SubmitAction(new ActivateMonsterEffectAction(
            matchState.Id, PlayerSide.Player, sourceSlot.placedCard.Id));
    }

    public void PlaySupportCard(CardInstance card)
    {
        if (CurrentState != TurnState.PlayerTurn || card == null
            || !card.Definition.IsSupportCard || !IsInPlayerHand(card)) return;

        if (card.Definition.SupportEffect is ICellTargetedSupportEffect)
        {
            if (!MatchRules.CanPlaySupportCard(matchState, PlayerSide.Player, card.Id)) return;
            selectedCard = selectedCard == card ? null : card;
            RefreshUI();
            return;
        }

        SubmitAction(new PlaySupportCardAction(matchState.Id, PlayerSide.Player, card.Id));
    }

    public void OnEndTurnButtonClicked()
    {
        if (matchState == null) return;
        SubmitAction(new EndTurnAction(matchState.Id, PlayerSide.Player));
    }

    public bool SubmitAction(MatchAction action)
    {
        if (!MatchRules.TryApply(matchState, action)) return false;

        if (action is PlaceCardAction placement)
        {
            if (placement.Actor == PlayerSide.Player) selectedCard = null;
            string actor = placement.Actor == PlayerSide.Player ? "Player" : "Opponent";
            Debug.Log($"{actor} played a card at ({placement.X}, {placement.Y}).");
        }
        else if (action is PlaySupportCardAction supportPlay)
        {
            if (supportPlay.Actor == PlayerSide.Player && selectedCard?.Definition.IsSupportCard == true)
                selectedCard = null;
            string actor = supportPlay.Actor == PlayerSide.Player ? "Player" : "Enemy";
            Debug.Log($"{actor} played a support card.");
        }
        else if (action is PlayEquipCardAction equipPlay)
        {
            if (equipPlay.Actor == PlayerSide.Player) selectedCard = null;
        }
        else if (action is ActivateMonsterEffectAction monsterEffect)
        {
            string actor = monsterEffect.Actor == PlayerSide.Player ? "Player" : "Enemy";
            Debug.Log($"{actor} activated a monster effect.");
        }
        else if (action is AttackCardAction attack)
        {
            string actor = attack.Actor == PlayerSide.Player ? "Player" : "Enemy";
            Debug.Log($"{actor} attacked in direction ({attack.Direction.X}, {attack.Direction.Y}).");
        }
        else if (action is EndTurnAction)
        {
            selectedCard = null;
            if (CurrentState == TurnState.EnemyTurn) StartCoroutine(EnemyTurnRoutine());
        }

        SyncBoard();
        if (CurrentState == TurnState.GameOver) SetGameOver();
        else RefreshUI();
        return true;
    }

    private IEnumerator EnemyTurnRoutine()
    {
        MatchState activeState = matchState;
        Debug.Log("--- OPPONENT TURN ---");
        while (matchState == activeState && activeState.Turn == TurnState.EnemyTurn)
        {
            if (unityContext == null)
            {
                Debug.LogError("Cannot receive player agent actions without Unity's main-thread context.");
                yield break;
            }

            MatchAction submittedAction = null;
            bool responseReceived = false;
            enemyAgent.RequestAction(activeState, action =>
            {
                unityContext.Post(_ =>
                {
                    if (matchState != activeState || responseReceived) return;
                    submittedAction = action;
                    responseReceived = true;
                }, null);
            });

            while (!responseReceived && matchState == activeState) yield return null;
            if (matchState != activeState) yield break;
            if (submittedAction == null)
            {
                Debug.LogError("The enemy player agent completed its request without an action.");
                yield break;
            }

            yield return new WaitForSeconds(EnemyActionDelay);
            if (!SubmitAction(submittedAction))
            {
                Debug.LogError("The enemy player agent submitted an invalid action.");
                yield break;
            }
        }

        if (matchState != activeState || activeState.Turn == TurnState.GameOver) yield break;

        selectedCard = null;
        RefreshUI();
        Debug.Log("--- PLAYER TURN ---");
    }

    private void SetGameOver()
    {
        int playerCells = MatchRules.CountOwnedCards(matchState, PlayerSide.Player);
        int enemyCells = MatchRules.CountOwnedCards(matchState, PlayerSide.Enemy);
        string winner = playerCells > enemyCells ? "Player" : enemyCells > playerCells ? "Enemy" : "Draw";
        selectedCard = null;
        RefreshUI();
        Debug.Log($"=== GAME OVER, WINNER: {winner} ===");
    }

    private void SyncBoard()
    {
        boardManager.SetBoard(matchState);
    }

    private bool IsInPlayerHand(CardInstance card)
    {
        foreach (CardInstance handCard in matchState.PlayerHand)
        {
            if (handCard == card) return true;
        }
        return false;
    }

    private void RefreshUI()
    {
        if (handUI != null) handUI.Refresh(this);
    }
}
