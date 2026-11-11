# Directional Card Attacks Implementation Plan

> **For agentic workers:** Implement inline in the current Unity workspace. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add once-per-turn attacks from player cards to adjacent enemies in configured arrow directions.

**Architecture:** Add Unity-free direction and attack-action types to `Game.Core`. Keep attack validation in `MatchRules`; connect the existing arrow buttons through `BoardSlot` and `MatchManager.SubmitAction`.

**Tech Stack:** Unity 2022.3, C# 9, existing Unity Test Framework.

**Spec:** `docs/superpowers/specs/2026-11-07-directional-card-attacks-design.md`

## Constraints
- Preserve existing placement, turn, draw, and game-over behavior.
- Keep `Game.Core` free of Unity API references.
- Use the existing arrow buttons and card direction data; add no runtime dependencies.

## Review Focus
- Unconfigured direction, empty cell, friendly card, wrong owner, wrong turn, and stale match action are rejected without state changes.
- A successful attack damages or removes only the adjacent enemy in the selected direction.
- A card cannot attack twice in one player turn and becomes available again on the next player turn.
- Only player-side cards attack in this pass.
- Existing scene and prefab references remain valid.

### Task 1: Core attack rules

**Files:** Modify `Assets/_Scripts/Core/GameModel.cs`, `MatchRules.cs`, `Assets/_Scripts/CardData.cs`; modify `Assets/Tests/EditMode/MatchRulesTests.cs`.

- [x] Add failing tests for configured direction, adjacent enemy target, damage/defeat, once-per-turn reset, and Defense reflection. The reflection behavior harness failed before the rule change and passed afterward.
- [x] Implement core direction data, match-scoped attack action, and rule validation.
- [x] Convert serialized card directions into core directions without Unity references.
- [x] Build the core and test assemblies and run core behavior checks.

### Task 2: Arrow button flow

**Files:** Modify `Assets/_Scripts/BoardSlot.cs`, `BoardManager.cs`, `MatchManager.cs`, and `Assets/_Prefabs/BoardSlot.prefab` only if serialization needs a focused update.

- [x] Bind button names to grid offsets, show configured attack directions for selected eligible player cards, and route clicks through `SubmitAction`.
- [x] Clear selection and arrow state after attacks, placement, match setup, and turn changes.
- [x] Build the Unity C# project and verify the prefab/scene script references. Unity's EditMode test runner could not start because the licensing service refused its connection.
