# Game Core and Player Agents Implementation Plan

> **For agentic workers:** Implement inline in the current Unity workspace. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Separate match rules/state from Unity and allow replaceable human, AI, and future network decision sources.

**Architecture:** A plain C# `Game.Core` assembly owns match state, commands, and rule validation. Unity retains `CardData`, UI, and coroutine timing; `MatchManager.SubmitAction` adapts UI and callback-based player-agent commands to the shared rule path.

**Tech Stack:** Unity 2022.3, C# 9, .NET Standard 2.1, Unity Test Framework.

**Spec:** `docs/superpowers/specs/2026-11-06-game-core-and-player-agents-design.md`

## Global Constraints
- Preserve 4x4 board and current match behavior.
- Preserve the current three player summons and one opponent summon per turn.
- Keep Unity dependencies out of `Game.Core`.
- Add no runtime package dependencies and no network implementation.

## Review Focus
- Invalid/stale placement command: no state change.
- Empty deck: draw is skipped safely.
- Full board: match reaches game over and rejects further actions.
- Repeated setup: no stale cards or duplicate slots.
- Player agent choosing an unavailable card/slot: validated and safely rejected.

### Task 1: Core model, commands, and rule tests

**Files:** Create `Assets/_Scripts/Core/Game.Core.asmdef`, core model/rule source files and `.meta`; create `Assets/Tests/EditMode/Game.Core.Tests.asmdef` and core rule tests.

- [x] Write tests for legal/illegal placement, turn progression, draw exhaustion, random-agent action, and full-board termination.
- [ ] Run the Unity EditMode tests and confirm the initial failure. Unity refused to start its test runner because its licensing service connection was refused.
- [x] Implement Unity-free match state, card values, commands, and rule service.
- [x] Compile the EditMode test assembly and run the temporary core behavior harness.

### Task 2: Unity adapter and player decision seam

**Files:** Modify `Assets/_Scripts/CardData.cs`, `MatchManager.cs`, `GameManager.cs`, `BoardManager.cs`, `BoardSlot.cs`, `HandUI.cs`; create Unity adapter and random agent files plus `.meta` as required.

- [x] Add tests for agent action generation, stale-match commands, and rule validation.
- [ ] Run the Unity EditMode tests. The Unity licensing service prevented the test runner from starting.
- [x] Route human placement and random opponent placement through the shared core rules; keep turn delays in Unity orchestration.
- [x] Remove direct UI/gameplay dependence on singleton access where a manager reference is available, without changing serialized Unity event entry points.
- [x] Build `Game.Core`, `Game.Core.Tests`, and the Unity-generated C# project; run the core behavior harness.

### Task 3: Review and verification

**Files:** Review all changed game scripts, assembly definitions, and Unity scene references.

- [x] Verify the scene's serialized references and Unity script GUIDs remain valid.
- [x] Review changed scripts, scene serialization, and plan scope.
- [x] Run final available build and core behavior checks; record the unavailable Unity test runner.
