# Battle Scene-Authored Layout Design

## Goal

Make `Battle.unity` run correctly as a direct Play entry point before relying on the `MainMenu.unity -> Battle.unity` flow.

The immediate problems to solve are functional, not visual polish:

- Troops must spawn and move along intended routes.
- Headquarters must display at their correct locations and sizes.
- Towers, resource wells, and other battle buildings must display at their correct locations and sizes.
- `Battle.unity` direct Play must initialize a valid default battle without requiring menu state.
- `MainMenu.unity -> Battle.unity` must reuse the same battle setup after the direct scene works.

## Current Context

The project currently has a small runtime code surface for a large gameplay scope:

- `Assets/Scripts/Battle/Core/BattleGameController.cs` is the central battle controller and is over 4,000 lines.
- `Assets/Scripts/MainMenu/MainMenuController.cs` builds the menu UI procedurally.
- `Battle.unity` and `MainMenu.unity` are the two active scenes.
- Many spatial gameplay values are currently encoded in controller constants and arrays, including route nodes, lane paths, base positions, tower slots, resource well slots, and visual scale values.

This makes the scene hard to inspect and tune. When a building or route is wrong, the fix requires editing code instead of moving scene markers in Unity.

## Chosen Approach

Use a scene-authored battle layout.

`Battle.unity` becomes the source of truth for spatial layout. Code remains responsible for gameplay behavior, but it reads placement, route, and sizing data from components and marker objects placed in the scene.

The intended hierarchy is:

```text
Battle Scene Root
  Map
  Layout
    Routes
      Lane_0
      Lane_1
      Lane_2
    Bases
      PlayerBasePoint
      EnemyBasePoint
    Towers
      PlayerTowerSlot_0
      PlayerTowerSlot_1
      PlayerTowerSlot_2
      EnemyTowerSlot_0
      EnemyTowerSlot_1
      EnemyTowerSlot_2
    ResourceWells
      PlayerWellSlot_0
      PlayerWellSlot_1
      EnemyWellSlot_0
      EnemyWellSlot_1
```

The exact number of tower and well slots can match the current gameplay rules, but the positions should be authored in the scene rather than hidden in arrays.

## Runtime Initialization

At Play start, `BattleGameController` should find a scene component named `BattleLayout`.

`BattleLayout` should expose:

- Map bounds or a map renderer reference.
- Player and enemy base points.
- Player and enemy tower slot transforms.
- Player and enemy resource well slot transforms.
- Route waypoint lists for each lane.
- Visual scale settings for units, bases, towers, and resource wells.

`BattleGameController` should then:

1. Ensure a valid default `GameSession` exists when `Battle.unity` is played directly.
2. Read all spatial data from `BattleLayout`.
3. Validate required references before starting the match.
4. Spawn headquarters at base marker positions.
5. Spawn buildings at slot marker positions.
6. Spawn troops at route start markers.
7. Move troops through the route waypoint list in order.
8. Use layout scale values for visual sizing.
9. Keep sorting/layers consistent for map, bases, buildings, troops, route previews, VFX, and UI.

## Validation and Error Handling

If required layout data is missing, the scene should fail loudly and clearly in the Unity Console.

Examples:

- Missing `BattleLayout`.
- Missing player base marker.
- Missing enemy base marker.
- Lane has fewer than two route points.
- A tower or resource well slot list is empty.
- A scale value is zero or negative.

The controller should avoid silently falling back to incorrect hardcoded positions once the layout component exists. Silent fallbacks are acceptable only for direct Play session defaults such as map choice or difficulty.

## Sizing Rules

Visual size should be adjustable from the scene or layout component instead of being scattered through controller constants.

Initial exposed values:

```text
unitVisualScale
baseVisualScale
towerVisualScale
resourceWellVisualScale
```

The implementation can keep existing defaults as initial values, but the final behavior should make common size corrections possible from the Unity Inspector.

## Route Movement

Each lane should be represented as an ordered list of waypoint transforms.

For direct Play acceptance:

- A player unit starts at the player side of the selected route.
- An enemy unit starts at the enemy side of its route.
- Units visibly move from waypoint to waypoint.
- Units do not jump to incorrect map coordinates.
- Units do not appear at an obviously wrong scale.
- Route preview and actual unit movement use the same waypoint data.

## Main Menu Flow

After `Battle.unity` direct Play works, verify the full player flow:

```text
MainMenu.unity
  -> Start Game
  -> SceneManager.LoadScene("Battle")
  -> Battle.unity reads BattleLayout markers
  -> GameSession supplies map and difficulty choices
```

The menu should not provide spatial layout. It only selects gameplay options.

## In Scope

- Add a scene-authored `BattleLayout` component.
- Add route, base, tower slot, and resource well markers in `Battle.unity`.
- Update battle initialization to read marker data.
- Use layout-controlled visual scales.
- Validate missing layout data with clear console errors.
- Confirm `Battle.unity` direct Play works.
- Confirm `MainMenu.unity -> Battle.unity` still works after direct Play is stable.

## Out of Scope

- Full visual redesign.
- New gameplay systems.
- Broad balance tuning.
- Full rewrite of `BattleGameController.cs`.
- Complete conversion of all content to ScriptableObjects.
- Adding all five era content sets.

## Acceptance Criteria

1. Opening `Battle.unity` and pressing Play starts a valid battle without going through the main menu.
2. Player and enemy headquarters display at expected scene marker positions.
3. Headquarters display at an intentional, editable size.
4. Tower slots and resource well sites display at expected marker positions.
5. Built towers and wells inherit the correct marker positions and visual scale.
6. Troops spawn on valid route starts and move through route waypoints in order.
7. Route previews and actual movement use the same layout data.
8. Missing layout markers produce clear Unity Console errors.
9. Starting from `MainMenu.unity` loads `Battle.unity` and uses the same layout behavior.

## Implementation Notes

This design intentionally does not require a large architecture rewrite first. The implementation can introduce `BattleLayout` and adapt `BattleGameController` incrementally. Once the scene runs correctly, later work can split the controller into smaller systems with less risk.
