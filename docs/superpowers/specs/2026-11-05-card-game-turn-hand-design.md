# Card Game Turn and Hand Loop Design

## Intent
Complete the existing Unity card-game template so the player can see and select cards in hand, place one card per turn, end a turn, and draw consistently. Keep the current 4x4 board and simple automated opponent.

## Goals
- Display the player hand, selected card, and player/enemy deck counts through Unity scene UI.
- Let the player select a card, then place it in an empty board slot once per player turn.
- Provide an End Turn button that advances through the opponent turn and back to the player.
- Shuffle each 20-card deck when built; draw three opening cards, then draw one at the start of each later turn for that side.
- Show turn state and handle empty decks and full board without exceptions.

## Non-goals
- Card costs, combat, win scoring, mulligans, online play, or a new art direction.
- Replacing the existing board-slot placement interaction or automated opponent.

## Constraints and current state
- Unity C# project using uGUI and TextMeshPro, with `GameManager`, `BoardManager`, `BoardSlot`, and `CardData` scripts.
- SampleScene contains a Canvas and the board grid, but no hand row or end-turn controls.
- `GameManager` already builds and shuffles decks, draws cards, limits player placement, and runs an opponent routine. The current startup draws four player cards (three opening plus an immediate turn draw), and it auto-selects the first card instead of presenting a selectable hand.

## Responsibility split
- `MatchManager` owns match state, deck construction/shuffling/drawing, card selection/placement, turn progression, opponent actions, and game-over state.
- `GameManager` remains a scene-level bootstrap and compatibility facade for existing serialized Unity events; it forwards setup and legacy actions without owning match rules or state.
- `BoardSlot` and `HandUI` call `MatchManager` directly for gameplay actions and match state.

## Approaches
- Canvas-attached presenter: add a focused `HandUI` component to the existing Canvas; it creates the hand, status, deck-count, and end-turn controls when play starts. Panel height, card width, and panel color remain editable on the scene component. Chosen to avoid fragile hand-authored Unity UI serialization while keeping the main layout tunable in the Inspector.
- Runtime-generated UI: build controls entirely from code. Fewer scene objects, but harder for the template owner to style.

## Proposed behavior
- Opening setup shuffles each deck and draws exactly three cards for each side. Do not draw again during the first player turn; each subsequent turn start draws one card, if available.
- Selecting a hand card highlights it. Clicking an empty board slot places that exact card, removes it from hand, and refreshes the hand display. Selecting or placing is only enabled during the player turn; at most one placement is allowed each turn.
- End Turn is enabled during the player turn and runs the current automated opponent behavior. The opponent draws one card at its turn start and plays at most one card to a random empty slot. Then the next player turn starts and draws one card.
- Display current turn, hand cards, and both deck counts. Empty decks simply skip drawing. A full board ends the game and disables turn actions.

## Files likely to change
- `Assets/_Scripts/MatchManager.cs`: own draw timing, turn progression, hand selection, and game-over/action guards.
- `Assets/_Scripts/GameManager.cs`: start the match and forward existing Unity events.
- `Assets/_Scripts/HandUI.cs` and `Assets/_Scripts/BoardSlot.cs`: use `MatchManager` as the gameplay authority.
- `Assets/_Scripts/BoardManager.cs`: safely rebuild the grid only once and expose board-full state if needed.
- New `Assets/_Scripts/HandUI.cs` (or similarly named presenter): render and select cards from the player's hand and update status/counts.
- `Assets/Scenes/SampleScene.unity`: add and wire the hand row, card button prefab or generated card buttons, status labels, and End Turn button.

## Builder decisions
Use generated card buttons and a scrollable hand under the scene-authored Canvas, without introducing a separate card prefab. Keep the existing default card data and automated opponent. The tradeoff is that individual child controls are created at runtime instead of being rearranged as separate scene objects.
