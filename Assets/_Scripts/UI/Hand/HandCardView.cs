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
    private bool allowPlay;

    public CardInstance Card { get; private set; }

    private void Awake()
    {
        button.onClick.AddListener(OnClicked);
        holdPreview.Show = () => matchManager != null && matchManager.ShowCardPreview(Card, holdPreview);
        holdPreview.Hide = () => matchManager?.HideCardPreview(holdPreview);
    }

    public void Bind(CardInstance card, MatchManager manager, bool allowPlay = true)
    {
        holdPreview.Cancel();
        Card = card;
        matchManager = manager;
        this.allowPlay = allowPlay;
        CardDefinition definition = card.Definition;
        bool isSupport = definition.IsSupportCard;
        bool selected = allowPlay && manager.SelectedHandCard == card;

        frame.color = selected ? Brass : Rim;
        float inset = selected ? 3 : 1;
        surface.rectTransform.offsetMin = Vector2.one * inset;
        surface.rectTransform.offsetMax = -Vector2.one * inset;
        button.interactable = !allowPlay || manager.CurrentState == TurnState.PlayerTurn
            && (isSupport
                ? MatchRules.CanPlaySupportCard(manager.State, PlayerSide.Player, card.Id)
                : definition.Type == CardType.Equip || manager.NormalSummonThisTurn > 0);

        SetPortrait(artwork, manager.GetArtwork(definition));
        title.text = definition.Name;
        details.text = isSupport
            ? $"<b>{(definition.Type == CardType.HiSupporter ? "HI SUPPORT" : "SUPPORT")}</b>\n{definition.SupportEffect?.Description ?? "Effect missing"}\n{definition.SupportCondition?.Description ?? "Condition missing"}"
            : definition.Type == CardType.Equip
                ? $"<b>EQUIP</b>\n{definition.EquipEffect?.Description}"
                : $"{definition.Attack} ATK   {card.Defense} DEF   {card.CurrentHealth} HP";
    }

    private void OnClicked()
    {
        if (!allowPlay) return;
        if (Card.Definition.IsSupportCard) matchManager.PlaySupportCard(Card);
        else matchManager.SelectHandCard(Card);
    }
}
