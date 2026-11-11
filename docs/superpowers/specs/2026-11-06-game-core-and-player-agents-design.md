# Game Core and Player Agents Design

## Intent
Refactor the card game so rules and match state have a clear boundary from Unity presentation and turn timing. Make player decisions replaceable so human input, the existing random opponent, and a future network adapter can submit the same validated actions.

## Goals
- Keep card rules and match state in a plain C# assembly with no Unity references.
- Keep Unity `ScriptableObject` assets and scene/UI behavior in the Unity-facing layer.
- Route human and opponent placements through the same rule validation.
- Make opponent decision logic replaceable without putting network transport or a stronger AI into this change.
- Preserve current 4x4 board, opening draws, turn sequence, and presentation behavior.

## Non-goals
- Implementing online/local multiplayer, network protocol, authority model, persistence, combat, or new AI strategy.
- Changing card balance or the current player-facing UI.

## Architecture
- `Game.Core` is a plain C# assembly containing card/match state, commands, rules, and a player-agent contract. It must not reference Unity APIs.
- `CardData` remains the Unity authoring asset and is converted to core card definitions at match setup.
- `MatchManager` is a Unity adapter/orchestrator: it translates UI actions to core commands, coordinates coroutine delays, and refreshes the UI.
- Human UI and the random opponent produce the same core action type. The rules decide whether an action is legal; agents deliver actions through a callback so a future network player can wait for a remote response. `MatchManager.SubmitAction` is the single Unity entry point that validates an action and updates the board and UI.

## Behavior
- Preserve opening hand size of three, later draw timing, up to three player placements and one opponent placement per turn, empty-deck handling, and full-board game over.
- Invalid commands leave match state unchanged.
- Player selection remains presentation/input state and does not grant authority to place a card.
- The random opponent remains the default implementation; randomness is supplied at the agent boundary for controllability.

## Constraints
- Unity 2022.3, C# 9, .NET Standard 2.1, existing uGUI/TextMeshPro dependencies.
- No new runtime package dependencies.
