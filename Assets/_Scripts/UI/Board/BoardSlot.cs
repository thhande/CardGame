using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static MatchVisuals;

public class BoardSlot : MonoBehaviour
{
    private static readonly Color EmptySurface = new Color32(22, 29, 30, 228);
    private static readonly Color PlayerFrame = new Color32(44, 105, 165, 255);
    private static readonly Color EnemyFrame = new Color32(165, 57, 66, 255);

    public int x;
    public int y;
    [NonSerialized] public CardInstance placedCard;

    [Header("UI References")]
    public Image cardImage;
    public TextMeshProUGUI statsText;
    [SerializeField] private Image tileSurface;
    [SerializeField] private Image portrait;
    [SerializeField] private Color defeatedPortraitTint;
    [SerializeField] private Image statsBacking;
    [SerializeField] private TMP_Text coordinatesLabel;
    [SerializeField] private GameObject attackArrows;
    [SerializeField] private Button monsterEffectButton;
    [SerializeField] private CardHoldPreview holdPreview;

    private readonly Dictionary<CardDirection, Button> attackButtons = new Dictionary<CardDirection, Button>();
    private MatchManager matchManager;

    public bool IsEmpty => placedCard == null;

    public void Init(int xPos, int yPos)
    {
        x = xPos;
        y = yPos;
        BindAttackButtons();
        holdPreview.Show = () => matchManager != null && matchManager.ShowCardPreview(placedCard, holdPreview);
        holdPreview.Hide = () => matchManager?.HideCardPreview(holdPreview);
        ClearSlot();
    }

    public void SetMatchManager(MatchManager manager)
    {
        matchManager = manager;
    }

    public void PlaceCard(CardInstance card, MatchState state)
    {
        if (placedCard != card) holdPreview?.Cancel();
        placedCard = card;
        if (cardImage != null) cardImage.color = Ink;
        if (tileSurface != null)
        {
            PlayerSide cellOwner = state.GetCellOwner(x, y);
            tileSurface.color = cellOwner == PlayerSide.Player ? PlayerFrame : EnemyFrame;
        }
        if (portrait != null)
        {
            portrait.transform.parent.gameObject.SetActive(true);
            SetPortrait(portrait, matchManager != null ? matchManager.GetArtwork(card.Definition) : null);
            portrait.color = card.CurrentHealth <= 0 ? defeatedPortraitTint : Color.white;
        }
        if (statsText != null)
        {
            statsText.enabled = true;
            statsText.text = $"{MatchRules.GetAttack(state, card)}\u00A0ATK  {card.Defense}\u00A0DEF  {card.CurrentHealth}\u00A0HP";
        }
        if (statsBacking != null) statsBacking.enabled = true;
        if (coordinatesLabel != null) coordinatesLabel.enabled = false;
        RefreshAttackButtons(state, card);
        if (monsterEffectButton != null)
            monsterEffectButton.gameObject.SetActive(MatchRules.CanActivateMonsterEffect(state, PlayerSide.Player, card.Id));
    }

    public void ClearSlot()
    {
        ClearSlot(BoardCellState.Empty, false);
    }

    internal void ClearSlot(BoardCellState state, bool blocksNextTurn)
    {
        bool blocked = state == BoardCellState.Blocked;
        holdPreview?.Cancel();
        HideAttackButtons();
        if (monsterEffectButton != null) monsterEffectButton.gameObject.SetActive(false);
        placedCard = null;
        if (cardImage != null) cardImage.color = blocked ? Copper : blocksNextTurn ? Brass : Rim;
        if (tileSurface != null) tileSurface.color = EmptySurface;
        if (portrait != null) portrait.transform.parent.gameObject.SetActive(false);
        if (statsBacking != null) statsBacking.enabled = false;
        if (statsText != null) statsText.enabled = false;
        if (coordinatesLabel != null)
        {
            coordinatesLabel.text = $"{(char)('A' + x)}{y + 1}";
            if (blocked) coordinatesLabel.text += "\n<size=12>BLOCKED</size>";
            else if (blocksNextTurn) coordinatesLabel.text += "\n<size=12>BLOCKS NEXT TURN</size>";
            coordinatesLabel.enabled = true;
        }
    }

    public void OnSlotClicked()
    {
        if (matchManager != null) matchManager.OnBoardSlotClicked(this);
    }

    public void OnMonsterEffectButtonClicked()
    {
        if (matchManager != null) matchManager.TryActivateMonsterEffect(this);
    }

    private void RefreshAttackButtons(MatchState state, CardInstance attacker)
    {
        if (attackArrows == null || attacker == null) return;

        attackArrows.SetActive(true);
        foreach (KeyValuePair<CardDirection, Button> entry in attackButtons)
        {
            bool supported = HasAttackDirection(attacker.AttackDirections, entry.Key);
            entry.Value.gameObject.SetActive(supported);
            bool canAttack = state != null && supported && MatchRules.CanAttack(
                state, state.GetCellOwner(attacker.X, attacker.Y), attacker.Id, entry.Key);
            entry.Value.interactable = canAttack;
            TMP_Text arrow = entry.Value.GetComponentInChildren<TMP_Text>(true);
            if (arrow != null) arrow.color = canAttack ? Ink : Cream;
        }
    }

    public void HideAttackButtons()
    {
        if (attackArrows != null) attackArrows.SetActive(false);
    }

    private void BindAttackButtons()
    {
        if (attackArrows == null) return;
        Transform arrows = attackArrows.transform;

        BindAttackButton(arrows, "Left", new CardDirection(-1, 0));
        BindAttackButton(arrows, "Right", new CardDirection(1, 0));
        BindAttackButton(arrows, "Up", new CardDirection(0, -1));
        BindAttackButton(arrows, "Down", new CardDirection(0, 1));
        BindAttackButton(arrows, "NorthWest", new CardDirection(-1, -1));
        BindAttackButton(arrows, "NorthEast", new CardDirection(1, -1));
        BindAttackButton(arrows, "SouthWest", new CardDirection(-1, 1));
        BindAttackButton(arrows, "SouthEast", new CardDirection(1, 1));
    }

    private void BindAttackButton(Transform arrows, string buttonName, CardDirection direction)
    {
        Transform buttonTransform = arrows.Find(buttonName);
        Button button = buttonTransform != null ? buttonTransform.GetComponent<Button>() : null;
        if (button == null)
        {
            Debug.LogWarning($"BoardSlot prefab is missing the {buttonName} attack button.", this);
            return;
        }

        button.onClick.AddListener(() =>
        {
            if (matchManager != null) matchManager.TryAttack(this, direction);
        });
        attackButtons.Add(direction, button);
    }

    private static bool HasAttackDirection(IReadOnlyList<CardDirection> directions, CardDirection requested)
    {
        for (int i = 0; i < directions.Count; i++)
        {
            if (directions[i].Equals(requested)) return true;
        }
        return false;
    }
}
