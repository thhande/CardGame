# Support Cards Design

## Intent
Add cards that resolve an effect when played from hand and never occupy a board cell. The first concrete card demonstrates the path by drawing two cards. Effects and activation conditions are reusable behavior assets referenced by support card assets.

## Goals
- Allow support cards during their owner's turn without spending that turn's monster summon.
- Resolve polymorphic card effects and activation conditions through match rules without central effect or condition enums.
- Make the first support effect draw up to two cards, respecting the remaining deck size.

## Non-goals
- Add further card effects, named conditions, targeting, or new UI presentation in this pass.
- Change monster placement, combat, or turn draw rules.

## Constraints
Reuse the existing `CardType.Supporter` and match model/action rules. Keep Unity presentation in the existing manager and hand UI boundary. Keep the core effect and condition contracts free of Unity dependencies so they remain usable by platform-independent match rules.

## Approaches
- A dedicated draw-two action is the smallest immediate change but requires more special cases for each future effect.
- A support definition with an effect and activation condition uses a shared play path and supports the requested per-card variation. **Chosen:** define core effect/condition interfaces, implement them through reusable Unity assets, and include only Draw Two plus an always-active condition as the first case.

## Behavior
A support play action succeeds only for a card in the acting player's hand during their turn and when its activation condition is met. On success, the effect resolves, the card leaves hand, and no board space or summon is consumed. The hand UI presents support cards as playable without board selection and shows effect/condition descriptions from the assets. The enemy agent can play support cards using the same rules. Failed plays leave the hand and match state unchanged.

## Builder decisions
Use `ISupportEffect` and `ISupportCondition` contracts in the core model. `SupportEffectData` and `SupportConditionData` ScriptableObject assets implement those contracts; the core gives effects a constrained `SupportEffectContext` for match mutations. Deck content remains configured through existing inspector lists.
