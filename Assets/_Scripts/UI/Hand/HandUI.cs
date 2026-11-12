using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MatchVisuals;

/// <summary>Binds the match state to the HUD and hand card prefabs.</summary>
public class HandUI : MonoBehaviour
{
    [SerializeField] private RectTransform handContent;
    [SerializeField] private ScrollRect handScroll;
    [SerializeField] private TMP_Text discardContents;
    [SerializeField] private Button playerDiscardButton;
    [SerializeField] private Button enemyDiscardButton;
    [SerializeField] private LayoutElement handContentWidth;
    [SerializeField] private RectTransform opponentHand;
    [SerializeField] private RectTransform opponentCardBackPrefab;
    [SerializeField] private RectTransform titleRect;
    [SerializeField] private RectTransform playerPanel;
    [SerializeField] private RectTransform enemyPanel;
    [SerializeField] private RectTransform tray;
    [SerializeField] private RectTransform viewportRect;
    [SerializeField] private RectTransform divider;
    [SerializeField] private TMP_Text turnLabel;
    [SerializeField] private TMP_Text promptLabel;
    [SerializeField] private TMP_Text playerDeckLabel;
    [SerializeField] private TMP_Text enemyDeckLabel;
    [SerializeField] private TMP_Text handLabel;
    [SerializeField] private TMP_Text endTurnLabel;
    [SerializeField] private Button endTurnButton;
    [SerializeField] private HandCardView monsterCardPrefab;
    [SerializeField] private HandCardView supportCardPrefab;
    [SerializeField] private CardPreviewView cardPreview;

    private readonly List<HandCardView> handCards = new();
    private readonly List<RectTransform> opponentCardBacks = new();
    private MatchManager matchManager;
    private CanvasScaler canvasScaler;
    private GridLayoutGroup boardGrid;
    private Vector2 boardCellSize;
    private Vector2 boardSize;
    private bool? portraitLayout;
    private Vector2 layoutSize;
    private CardHoldPreview previewSource;
    private PlayerSide? openDiscardPile;

    public void Initialize(MatchManager matchManager)
    {
        openDiscardPile = null;
        Refresh(matchManager);
    }

    internal bool ShowCardPreview(CardInstance card, CardHoldPreview source)
    {
        if (cardPreview == null) return false;
        previewSource = source;
        cardPreview.Show(card, matchManager.GetArtwork(card.Definition), ((RectTransform)transform).rect.size,
            MatchRules.GetAttack(matchManager.State, card));
        return true;
    }

    internal void HideCardPreview(CardHoldPreview source)
    {
        if (previewSource != source || cardPreview == null) return;
        cardPreview.gameObject.SetActive(false);
        previewSource = null;
    }

    public void Refresh(MatchManager matchManager)
    {
        if (matchManager == null) return;
        if (this.matchManager == null) InitializeLayout(matchManager.boardManager);
        this.matchManager = matchManager;
        UpdateLayout();

        MatchState state = matchManager.State;
        int playerCells = MatchRules.CountOwnedCards(state, PlayerSide.Player);
        int enemyCells = MatchRules.CountOwnedCards(state, PlayerSide.Enemy);
        bool playerTurn = matchManager.CurrentState == TurnState.PlayerTurn;
        bool gameOver = matchManager.CurrentState == TurnState.GameOver;
        int sacrificeCount = matchManager.SelectedHandCard?.Definition.SacrificeCount ?? 0;
        turnLabel.text = gameOver
            ? playerCells == enemyCells ? "Draw" : playerCells > enemyCells ? "Victory" : "Defeat"
            : playerTurn ? "Your turn" : "Opponent's turn";
        turnLabel.color = gameOver ? Brass : playerTurn ? Teal : Copper;
        promptLabel.text = gameOver ? (MatchRules.IsBoardFull(state) ? "The board is full" : "Both sides ran out of cards")
            : !playerTurn ? "Waiting for your opponent"
            : matchManager.SelectedHandCard?.Definition.Type == CardType.Equip ? "Choose a friendly monster"
            : sacrificeCount == 1 ? "Choose a living monster you control to sacrifice"
            : sacrificeCount > 1
                ? $"Choose living monsters to sacrifice ({matchManager.SelectedSacrificeCount}/{sacrificeCount})"
            : matchManager.SelectedHandCard?.Definition.SupportEffect is ICellTargetedSupportEffect targetedEffect
                ? targetedEffect.TargetPrompt
            : matchManager.SelectedHandCard != null ? "Choose an empty tile"
            : matchManager.NormalSummonThisTurn > 0 ? "Choose a card from your hand"
            : "Attack, play a card, or end your turn";

        playerDeckLabel.text = FormatScorePanel("<color=#70E2CC>YOU</color>", playerCells,
            matchManager.PlayerDeck.Count, state.PlayerDiscardPile.Count, PlayerSide.Player);
        enemyDeckLabel.text = FormatScorePanel("<color=#FF9B6F>OPPONENT</color>", enemyCells,
            matchManager.EnemyDeck.Count, state.EnemyDiscardPile.Count, PlayerSide.Enemy);
        endTurnButton.interactable = playerTurn;
        endTurnLabel.text = gameOver ? "Match complete" : "End turn";

        RefreshHandCards();
        RefreshOpponentHand(matchManager.EnemyHand.Count);
        RefreshDiscardPile(state);
    }

    private string FormatScorePanel(string side, int cells, int deckCount, int discardCount, PlayerSide owner)
    {
        string discardAction = openDiscardPile == owner ? "Hide discard pile" : "Discard pile";
        return $"{side}   {cells} tiles  /  {deckCount} in deck\n<size=14><u>{discardAction} ({discardCount})</u></size>";
    }

    private void InitializeLayout(BoardManager boardManager)
    {
        canvasScaler = GetComponentInParent<CanvasScaler>();
        boardGrid = boardManager.boardGridParent.GetComponent<GridLayoutGroup>();
        if (boardGrid != null)
        {
            boardCellSize = boardGrid.cellSize;
            boardSize = ((RectTransform)boardGrid.transform).rect.size;
        }
        endTurnButton.onClick.AddListener(() => matchManager.OnEndTurnButtonClicked());
        playerDiscardButton.onClick.AddListener(() => ToggleDiscardPile(PlayerSide.Player));
        enemyDiscardButton.onClick.AddListener(() => ToggleDiscardPile(PlayerSide.Enemy));
    }

    private void ToggleDiscardPile(PlayerSide side)
    {
        openDiscardPile = openDiscardPile == side ? null : side;
        handScroll.StopMovement();
        discardContents.rectTransform.anchoredPosition = Vector2.zero;
        Refresh(matchManager);
    }

    private void RefreshDiscardPile(MatchState state)
    {
        bool showDiscard = openDiscardPile.HasValue;
        RectTransform content = showDiscard ? discardContents.rectTransform : handContent;
        if (handScroll.content != content)
        {
            handScroll.StopMovement();
            handContent.gameObject.SetActive(!showDiscard);
            discardContents.gameObject.SetActive(showDiscard);
            handScroll.content = content;
            handScroll.horizontal = !showDiscard;
            handScroll.vertical = showDiscard;
        }

        if (!showDiscard)
        {
            handLabel.text = $"YOUR HAND   /   {matchManager.PlayerHand.Count:00}";
            return;
        }

        bool player = openDiscardPile == PlayerSide.Player;
        IReadOnlyList<CardInstance> pile = player ? state.PlayerDiscardPile : state.EnemyDiscardPile;
        handLabel.text = player ? "YOUR DISCARD PILE" : "OPPONENT DISCARD PILE";
        discardContents.text = pile.Count == 0 ? "No discarded cards" : string.Join("\n", pile.Select(card => card.Definition.Name));
    }

    private void RefreshHandCards()
    {
        IReadOnlyList<CardInstance> playerHand = matchManager.PlayerHand;
        for (int i = handCards.Count - 1; i >= 0; i--)
        {
            HandCardView view = handCards[i];
            if (playerHand.Contains(view.Card)) continue;
            view.gameObject.SetActive(false);
            Destroy(view.gameObject);
            handCards.RemoveAt(i);
        }

        for (int i = 0; i < playerHand.Count; i++)
        {
            CardInstance card = playerHand[i];
            HandCardView view = handCards.Find(candidate => candidate.Card == card);
            if (view == null)
            {
                CardDefinition definition = card.Definition;
                HandCardView prefab = definition.IsSupportCard || definition.Type == CardType.Equip
                    ? supportCardPrefab : monsterCardPrefab;
                view = Instantiate(prefab, handContent, false);
                handCards.Add(view);
            }
            view.transform.SetSiblingIndex(i);
            view.Bind(card, matchManager);
        }
    }

    private void RefreshOpponentHand(int cardCount)
    {
        while (opponentCardBacks.Count < cardCount)
            opponentCardBacks.Add(Instantiate(opponentCardBackPrefab, opponentHand, false));

        for (int i = 0; i < opponentCardBacks.Count; i++)
            opponentCardBacks[i].gameObject.SetActive(i < cardCount);
    }

    private void LateUpdate()
    {
        if (matchManager != null) UpdateLayout();
    }

    private void UpdateLayout()
    {
        bool portrait = Screen.width < Screen.height * 1.2f;
        Vector2 canvasSize = ((RectTransform)transform).rect.size;
        if (portraitLayout == portrait && layoutSize == canvasSize) return;
        if (portraitLayout != portrait && canvasScaler != null)
            canvasScaler.referenceResolution = portrait ? new Vector2(800, 1320) : new Vector2(1600, 1000);
        portraitLayout = portrait;
        layoutSize = canvasSize;

        Vector2 top = new Vector2(0.5f, 1);
        Vector2 bottom = new Vector2(0.5f, 0);
        Vector2 right = new Vector2(1, 0.5f);
        const float edgeMargin = 24;
        const float boardGap = 8;
        float trayHeight = portrait ? 350 : 246;
        float bottomClearance = edgeMargin + trayHeight + boardGap;
        float trayWidth = Mathf.Min(canvasSize.x - edgeMargin * 2, portrait ? 752 : 1432);
        Pin(tray, bottom, new Vector2(0, edgeMargin + trayHeight / 2), new Vector2(trayWidth, trayHeight));

        float opponentHandTop = portrait ? 108 : 58;
        float opponentHandHeight = opponentHand.rect.height;
        Pin(opponentHand, top, new Vector2(0, -opponentHandTop - opponentHandHeight / 2),
            new Vector2(trayWidth, opponentHandHeight));
        float topClearance = opponentHandTop + opponentHandHeight + boardGap;

        if (boardGrid != null)
        {
            RectTransform board = (RectTransform)boardGrid.transform;
            float cellWidth = portrait ? boardCellSize.x : boardCellSize.y;
            boardGrid.cellSize = new Vector2(cellWidth, boardCellSize.y);
            float width = boardSize.x + (cellWidth - boardCellSize.x) * BoardManager.WIDTH;
            board.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            float availableHeight = canvasSize.y - topClearance - bottomClearance;
            float scale = Mathf.Max(0, Mathf.Min(trayWidth / width, availableHeight / boardSize.y));
            board.localScale = Vector3.one * scale;
            board.anchoredPosition = new Vector2(0, (bottomClearance - topClearance) / 2);
        }

        Pin(titleRect, top, new Vector2(0, -29), new Vector2(600, 38));
        Vector2 panelPosition = portrait ? new Vector2(187, -79) : new Vector2(488, -29);
        Vector2 panelSize = new Vector2(360, 46);
        Pin(playerPanel, top, new Vector2(-panelPosition.x, panelPosition.y), panelSize);
        Pin(enemyPanel, top, panelPosition, panelSize);

        if (portrait)
        {
            Stretch(viewportRect, 20, 102, 20, 38);
            Pin(divider, bottom, new Vector2(0, 88), new Vector2(trayWidth - 40, 1));
            Pin(turnLabel.rectTransform, bottom, new Vector2(-130, 62), new Vector2(336, 34));
            Pin(promptLabel.rectTransform, bottom, new Vector2(-130, 31), new Vector2(336, 28));
            Pin((RectTransform)endTurnButton.transform, new Vector2(1, 0), new Vector2(-146, 47), new Vector2(232, 54));
        }
        else
        {
            Stretch(viewportRect, 20, 12, 310, 38);
            Pin(divider, right, new Vector2(-297, 0), new Vector2(1, trayHeight - 20));
            Pin(turnLabel.rectTransform, right, new Vector2(-148, 49), new Vector2(268, 34));
            Pin(promptLabel.rectTransform, right, new Vector2(-148, 17), new Vector2(268, 28));
            Pin((RectTransform)endTurnButton.transform, right, new Vector2(-148, -40), new Vector2(232, 50));
        }

        handContentWidth.minWidth = viewportRect.rect.width;
    }
}
