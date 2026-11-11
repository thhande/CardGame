# Directional Card Attacks Design

## Intent
Allow a player to select a placed card and use one of its configured direction buttons to attack an adjacent enemy card once during the player's turn.

## Goals
- Keep attack legality and damage in `Game.Core`.
- Use the eight existing arrow buttons in the board-slot prefab.
- Allow only directions authored on the attacking card, and require an enemy in the adjacent target cell.
- Allow each player-side card at most one attack per player turn; reset this when the next player turn begins.
- Keep attack actions match-scoped and route them through `MatchManager.SubmitAction`.

## Assumptions
- Board coordinates use row-major UI layout with `y` increasing downward. Authored positive Y means up, so Unity card data is converted to board direction `(x, -y)`.
- Attacks reduce target Health directly by the attacker's Attack. If the target has an authored direction pointing back at the attacker, its Defense is reflected as damage to the attacker's Health. Both effects resolve together, so a defeated target still reflects its Defense. Cards at zero or less Health are removed from the board.
- This pass enables player-side attacks only. The random opponent does not attack.
- Attacks target exactly one adjacent cell in one of the eight directions.

## Non-goals
- Ranged attacks, turn-end scoring changes, enemy AI combat decisions, and counterattacks beyond the direction-based Defense reflection.

## Architecture
- Add a Unity-free `CardDirection` value type and `AttackCardAction` in `Game.Core`.
- `MatchRules` validates ownership, turn, once-per-turn use, configured direction, and target; it applies damage and removes defeated cards.
- `BoardSlot` binds its named arrow buttons at runtime, shows configured directions for an eligible selected card, and submits an action when one is clicked.
- `MatchManager` owns attacker selection and clears it after a valid action or turn transition.
