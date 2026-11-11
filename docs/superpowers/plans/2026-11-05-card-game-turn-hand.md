# Card Game Turn and Hand Loop Implementation Plan

> **For agentic workers:** Implement inline in the current Unity workspace. Tests are not requested; do not add or run tests.

**Goal:** Add visible player hand selection, turn status, deck counts, and an end-turn control to the existing 4x4 card game.

**Architecture:** `MatchManager` is the authority for match state and turn progression. `GameManager` only starts a match and forwards existing serialized Unity events. `BoardSlot` and `HandUI` call `MatchManager`. A serialized `HandUI` component on the existing Canvas builds and renders the controls. `BoardManager` safely prevents duplicate board generation and supports reset.

**Tech Stack:** Unity 2022.3, C#, uGUI, TextMeshPro.

**Spec:** `docs/superpowers/specs/2026-11-05-card-game-turn-hand-design.md`

## Global Constraints
- Preserve the existing 4x4 board and automated opponent.
- Use the existing default `CardData` asset and Unity UI packages.
- Do not add or run tests.

## Review Focus
- Empty deck: skip a draw and keep the game playable.
- Full board: stop the game and disable turn actions.
- No selected hand card: clicking a board slot must not place anything.
- Repeated setup: do not duplicate board slots or preserve stale hand cards.
- Opponent turn: draw and play at most one card, then return control to the player.

---

### Task 1: Turn progression and board lifecycle

**Files:** Modify `Assets/_Scripts/GameManager.cs`, `Assets/_Scripts/BoardManager.cs`.

- [x] Reset decks/hands and board on setup; opening hand is three cards, with no extra first-turn draw.
- [x] Draw one card at each subsequent player turn and opponent turn start.
- [x] Guard selection, placement, and ending turns by current state; handle full-board game over.
- [x] Make board generation idempotent and resettable.
- [x] Refresh the UI after state changes.

### Task 2: Visible hand and turn UI

**Files:** Create `Assets/_Scripts/HandUI.cs` and `.meta`; modify `Assets/Scenes/SampleScene.unity`.

- [x] Build a bottom hand panel and top status strip under the existing Canvas.
- [x] Render hand cards with name and attack/health; clicking selects a card and highlights it.
- [x] Show player/opponent deck counts and current turn.
- [x] Provide an End Turn button that calls `GameManager.OnEndTurnButtonClicked()`.
- [x] Disable/clear actions after game over.
- [x] Serialize `HandUI` on the scene Canvas and expose layout values in the Inspector; restore the Canvas scale to one so the board and UI render.

## Verification
- Run `dotnet build Assembly-CSharp.csproj --no-restore` after restoring the project assets once.
- Inspect the Canvas component reference, `HandUI` script GUID, and UI state/action guards without running tests.

