using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CardPreviewView : MonoBehaviour
{
    [SerializeField] private RectTransform frame;
    [SerializeField] private Image artwork;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text stats;
    [SerializeField] private TMP_Text descriptionLabel;
    [SerializeField] private GameObject[] arrows;

    internal void Show(CardInstance card, Sprite sprite, Vector2 canvasSize, int attack)
    {
        Vector2 frameSize = frame.sizeDelta;
        float scale = Mathf.Min(1, (canvasSize.x - 24) / frameSize.x, (canvasSize.y - 24) / frameSize.y);
        frame.localScale = Vector3.one * scale;

        CardDefinition definition = card.Definition;
        MatchVisuals.SetPortrait(artwork, sprite);
        title.text = definition.Name;
        stats.text = definition.IsSupportCard
            ? definition.Type == CardType.HiSupporter ? "HI SUPPORT" : "SUPPORT"
            : definition.Type == CardType.Equip ? "EQUIP"
            : $"{attack} ATK     {card.Defense} DEF     {card.CurrentHealth} / {card.MaxHealth} HP";

        // The prefab stores arrows in reading order, with no center entry.
        int arrowIndex = 0;
        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                if (x == 0 && y == 0) continue;
                arrows[arrowIndex++].SetActive(card.AttackDirections.Contains(new CardDirection(x, y)));
            }
        }

        StringBuilder description = new StringBuilder();
        if (definition.SacrificeCount == 1)
            description.Append("Sacrifice a living monster you control and summon this card in its place.");
        else if (definition.SacrificeCount > 1)
            description.Append($"Sacrifice {definition.SacrificeCount} living monsters you control and summon this card on the last one's tile.");
        if (definition.IsSupportCard)
        {
            description.AppendLine(definition.SupportEffect?.Description);
            description.Append(definition.SupportCondition?.Description);
        }
        if (definition.Type == CardType.Equip)
            description.Append(definition.EquipEffect?.Description);
        foreach (CardInstance equipment in card.Equipment)
        {
            if (description.Length > 0) description.AppendLine().AppendLine();
            description.Append("<b>Equipped: ").Append(equipment.Definition.Name).AppendLine("</b>")
                .Append(equipment.Definition.EquipEffect.Description);
        }
        foreach (MonsterEffectEntry entry in definition.MonsterEffects)
        {
            if (entry == null) continue;
            if (description.Length > 0) description.AppendLine().AppendLine();
            description.Append("<b>").Append(entry.ActivateType switch
            {
                MonsterEffectType.ChooseToTrigger => "Manual",
                MonsterEffectType.PlacedDown => "On placement",
                MonsterEffectType.ToDiscardPile => "On discard",
                MonsterEffectType.Die => "On defeat",
                MonsterEffectType.OnAttacked => "Passive · When attacked",
                MonsterEffectType.OnAttack => "Passive · When attacking",
                MonsterEffectType.TurnStarted => "Automatic · Turn start",
                MonsterEffectType.Aura => "Passive · Aura",
                _ => "Automatic"
            }).AppendLine("</b>").Append(entry.Effect.Description);
        }

        descriptionLabel.text = description.ToString();
        gameObject.SetActive(true);
    }
}
