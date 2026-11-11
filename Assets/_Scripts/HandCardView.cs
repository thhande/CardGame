using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MatchVisuals;

internal sealed class HandCardView : MonoBehaviour
{
    [SerializeField] private Image frame;
    [SerializeField] private Image surface;
    [SerializeField] private Image artwork;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text details;
    [SerializeField] private Button button;
    [SerializeField] private CardHoldPreview holdPreview;

    private MatchManager matchManager;

    public CardInstance Card { get; private set; }

    private void Awake()
    {
        button.onClick.AddListener(OnClicked);
        holdPreview.Show = () => matchManager != null && matchManager.ShowCardPreview(Card, holdPreview);
        holdPreview.Hide = () => matchManager?.HideCardPreview(holdPreview);
    }

    public void Bind(CardInstance card, MatchManager manager)
    {
        holdPreview.Cancel();
        Card = card;
        matchManager = manager;
        CardDefinition definition = card.Definition;
        bool isSupport = definition.IsSupportCard;
        bool selected = manager.SelectedHandCard == card;

        frame.color = selected ? Brass : Rim;
        float inset = selected ? 3 : 1;
        surface.rectTransform.offsetMin = Vector2.one * inset;
        surface.rectTransform.offsetMax = -Vector2.one * inset;
        button.interactable = manager.CurrentState == TurnState.PlayerTurn
            && (isSupport
                ? MatchRules.CanPlaySupportCard(manager.State, PlayerSide.Player, card.Id)
                : manager.NormalSummonThisTurn > 0);

        SetPortrait(artwork, manager.GetArtwork(definition));
        title.text = definition.Name;
        details.text = isSupport
            ? $"<b>{(definition.Type == CardType.HiSupporter ? "HI SUPPORT" : "SUPPORT")}</b>\n{definition.SupportEffect?.Description ?? "Effect missing"}\n{definition.SupportCondition?.Description ?? "Condition missing"}"
            : $"{definition.Attack} ATK   {definition.Defense} DEF   {card.CurrentHealth} HP";
    }

    private void OnClicked()
    {
        if (Card.Definition.IsSupportCard) matchManager.PlaySupportCard(Card);
        else matchManager.SelectHandCard(Card);
    }
}
