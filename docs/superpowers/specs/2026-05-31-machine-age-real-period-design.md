# Machine Age Real Second Period Design

## Context

The battle currently supports multiple age names, thresholds, stats, unit labels, tower labels, and ambience hooks. The first upgrade advances the numeric state to the second age, but most gameplay visuals still come from `Assets/Resources/Barbarian/...`. As a result, pressing the upgrade buttons changes text and numbers while the base, units, and towers still look like the first period.

The next change adds one real second period: `机械工坊` / Machine Workshop. It must be visibly distinct from `蛮荒部落` / Tribe after the first successful age upgrade.

## Goals

- Make the first successful `升级时代` transition enter a real Machine Workshop period.
- Replace the player's base visual when the upgrade completes.
- Make newly spawned player and enemy units use Machine Workshop unit sprites.
- Make newly built towers use Machine Workshop tower sprites.
- Refresh unit, tower, age power, and upgrade button labels so the UI matches the new period.
- Keep direct `Battle.unity` entry and `MainMenu -> Battle` working.
- Fail visibly and debuggably if an expected Machine asset is missing.

## Non-Goals

- Do not implement the third, fourth, or fifth periods in this pass.
- Do not replace the full battlefield map in this pass. The map is tied to authored route markers and needs a separate layout pass if it changes.
- Do not refactor the whole battle system into ScriptableObjects yet. The current controller can be improved locally without expanding scope.
- Do not use tint-only as the main solution. Tint fallback is allowed only as a missing-asset safety net.

## Asset Contract

The second period will use Resources paths with the same loading style as the existing Barbarian assets.

Required Machine assets:

- `Assets/Resources/Machine/Base/Base.png`
- `Assets/Resources/Machine/Units/GearSoldier/move_01.png` through `move_05.png`
- `Assets/Resources/Machine/Units/GearSoldier/attack_01.png` through `attack_05.png`
- `Assets/Resources/Machine/Units/SteamCrossbow/move_01.png` through `move_05.png`
- `Assets/Resources/Machine/Units/SteamCrossbow/attack_01.png` through `attack_05.png`
- `Assets/Resources/Machine/Units/SiegeRoller/move_01.png` through `move_05.png`
- `Assets/Resources/Machine/Units/SiegeRoller/attack_01.png` through `attack_05.png`
- `Assets/Resources/Machine/Towers/GearTower/attack_01.png` through `attack_05.png`

If polished final Machine art is not available, implementation will still create a real Machine resource set from available generated art or simple machine-styled sprites. The acceptance condition is that these are separate Machine assets loaded from `Resources/Machine/...`, not the old `Resources/Barbarian/...` sprites with only text changes.

## Runtime Design

Add an age visual layer to `BattleGameController`, initially as private controller data to match the existing file's style.

Each `AgeVisualSet` defines:

- age name
- base sprite path
- three unit visual folders
- tower frame folder
- fallback tint

Age index `0` maps to the existing Barbarian visual set. Age index `1` maps to the Machine visual set. Higher ages may temporarily reuse the latest implemented visual set until their real assets are added, but this pass only validates age `1`.

`BuildDefinitions()` will use the current age visual set instead of hard-coded `Barbarian/Units/...` paths. Unit definitions already change by `ageIndex`; their frame folders will now change too.

`BuildTowerDefinitionForAge()` will still provide period-specific tower stats and label, but tower animation frames will come from the current age visual set instead of always loading `Barbarian/Towers/BoneTower/attack_`.

`CreateBaseArt()` will keep references to the player and enemy base renderers. `UpgradeAge()` will call a refresh method after changing `ageIndex` to replace both base sprites with the Machine base sprite and apply the Machine scale/tint rules.

Existing towers will refresh to the current age tower frames when the player upgrades. Existing units may keep their original sprites until they die, because they were spawned in the previous period; all units spawned after the upgrade must use Machine frames. This keeps battlefield history understandable and avoids mutating active unit animation state mid-combat.

## Upgrade Flow

When a player clicks an upgrade button and has enough era value:

1. Increment `ageIndex`.
2. Reset `eraValue`.
3. Recalculate income and base max health.
4. Rebuild player and enemy definitions for the new age.
5. Load the current age tower frames.
6. Refresh base sprites.
7. Refresh existing tower sprites/animation frames.
8. Refresh HUD labels and buttons.
9. Switch ambience as already implemented.
10. Set status text to confirm the selected evolution path and Machine Workshop entry.

This ensures the same button press changes both numbers and visible assets.

## Missing Asset Handling

Sprite loading for required Machine resources will log warnings with the exact missing path. If a Machine sprite is missing, gameplay will continue with the corresponding Barbarian sprite plus Machine fallback tint. This prevents a broken battle scene while still making the missing resource obvious.

The PlayMode tests will require the primary Machine resources to load in the test project. The fallback exists for development safety, not as the expected final state.

## Testing

Add PlayMode coverage for the first upgrade:

- Load `Battle`.
- Start the game state if needed.
- Force enough era value for the first upgrade.
- Invoke the attack or defense upgrade path.
- Assert `ageIndex == 1`.
- Assert the active unit button labels changed to the Machine Workshop unit names.
- Assert the base renderer sprite changed from the Barbarian sprite to a Machine sprite.
- Spawn a new unit and assert its definition/key and first animation sprite come from the Machine visual set.
- Build or configure a tower and assert its tower frames are Machine frames.

Existing layout and main-menu flow tests must still pass.

## Acceptance Criteria

- After pressing `升级时代` for the first time, the HUD says the battle is in `机械工坊`.
- The player base image visibly changes.
- New player units use Machine Workshop sprites.
- New enemy units use Machine Workshop sprites for the current age.
- Newly built and refreshed towers use Machine Workshop sprites.
- Button labels match Machine Workshop unit and tower names.
- The change is verified by PlayMode tests, not only visual inspection.
