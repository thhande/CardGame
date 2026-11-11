# Support Cards Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let support cards resolve from hand without occupying a board cell, beginning with a Draw 2 card.

**Architecture:** Add a support-play action to the pure match model. Store effect and activation condition as core interfaces implemented by reusable Unity ScriptableObject assets, and resolve both through `MatchRules`. Keep the hand UI and enemy agent on the shared play action.

**Tech Stack:** Unity C#, platform-independent match rules, NUnit EditMode tests.

**Spec:** `docs/superpowers/specs/2026-11-07-support-cards-design.md`

## Global Constraints

- Reuse existing `CardType.Supporter` and match model/action rules.
- Keep Unity presentation in the existing manager and hand UI boundary.
- Do not change monster placement, combat, or turn draw rules.

## Review Focus

- Wrong turn, actor, match, card ID, or card type must reject support play without mutating state.
- Draw 2 with fewer than two remaining cards draws only what exists.
- Support play must not consume a monster summon or place a board card.
- Support card selection must trigger play rather than leave the card selected for placement.
- Enemy support play must advance through the existing agent action loop.

---

### Task 1: Match model and support effect resolution

**Files:**
- Modify: `Assets/_Scripts/Core/GameModel.cs`
- Modify: `Assets/_Scripts/Core/MatchRules.cs`
- Modify: `Assets/_Scripts/Core/PlayerAgents.cs`
- Test: `Assets/Tests/EditMode/MatchRulesTests.cs`

**Interfaces:**
- Produce `PlaySupportCardAction(Guid matchId, PlayerSide actor, long cardId) : MatchAction`.
- Add definition references for an `ISupportEffect` and `ISupportCondition`; implement `DrawCardsSupportEffectData` and `AlwaysActiveSupportConditionData` assets as the first pair.
- Add `SupportEffectContext.DrawCards(int count)` as the core effect's supported deck mutation.
- A successful support play removes that instance from hand, executes its condition/effect, consumes no summon, and never changes a board cell.

- [ ] Add EditMode tests for player Draw 2, capped draw when fewer than two cards remain, and invalid support actions preserving state.
- [ ] Run the targeted EditMode test and confirm the expected failures.
- [ ] Implement the action and match-rule resolution using the existing draw helper.
- [ ] Run the targeted tests and then the full EditMode test suite.

### Task 2: Unity support-card authoring and player hand flow

**Files:**
- Create: `Assets/_Scripts/Cards/SupportEffectData.cs`
- Create: `Assets/_Scripts/Cards/SupportConditionData.cs`
- Create: `Assets/_Scripts/Cards/DrawCardsSupportEffectData.cs`
- Create: `Assets/_Scripts/Cards/AlwaysActiveSupportConditionData.cs`
- Modify: `Assets/_Scripts/Cards/SupportCardData.cs`
- Modify: `Assets/_Scripts/MatchManager.cs`
- Modify: `Assets/_Scripts/HandUI.cs`
- Modify: `Assets/Scenes/SampleScene.unity`
- Create: `Assets/_Data/_SupportCard/DrawTwo.asset`
- Create: `Assets/_Data/_SupportCard/DrawTwoEffect.asset`
- Create: `Assets/_Data/_SupportCard/AlwaysActiveCondition.asset`
- Test: Extend `Assets/Tests/EditMode/MatchRulesTests.cs` only if model coverage requires it.

**Interfaces:**
- `SupportCardData.ToDefinition()` creates `CardType.Supporter` definitions with `ISupportEffect` and `ISupportCondition` references.
- Add a `MatchManager` handler that submits `PlaySupportCardAction` for the clicked support card.

- [ ] Add a test for support play on the player's turn while no summons remain, confirming it still resolves.
- [ ] Run the targeted test and confirm the expected failure.
- [ ] Add reusable effect and condition ScriptableObject types, add the Draw Two effect/condition/card assets, and connect support-card buttons to play directly while keeping monster cards selectable for placement.
- [ ] Run the EditMode suite and build the Unity C# project.

### Task 3: Enemy support play and final review

**Files:**
- Modify: `Assets/_Scripts/Core/PlayerAgents.cs`

- [ ] Add a test that the enemy agent can submit a support play and the rules resolve it.
- [ ] Run the targeted test and confirm the expected failure.
- [ ] Teach `RandomPlayerAgent` to play an available support card, then continue its normal action cycle.
- [ ] Run all relevant tests/build checks and review the final diff for unrelated changes.
