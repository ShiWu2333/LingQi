# Combat Data Refactor

## New Configs
- `LevelUpConfig`: centralizes player level-up costs and per-level gains.
- `CombatBalanceConfig`: holds shared combat balance timings such as stagger durations and default hit-stop.

## Updated Scripts
- `PlayerResources`: reads level-up costs/gains from `LevelUpConfig` instead of inline values.
- `PlayerMovement`: movement rotation/lock-on distances now sourced from `PlayerStatsConfig` with fallbacks.
- `PlayerDodgeController`: dodge window timings fully loaded from `PlayerStatsConfig`.
- `EnemyResources`: kill rewards pulled from `EnemyStatsConfig`.
- `EnemyAIController`: stagger and decision tuning pulled from `EnemyStatsConfig` and `CombatBalanceConfig`.
- `EnemyStatsConfig` and `PlayerStatsConfig`: expanded to store moved balance fields.

## Notes
- Default values in configs mirror prior inline literals to preserve gameplay feel.
- `CombatBalanceConfig` and `LevelUpConfig` are created at runtime if missing to keep behaviour consistent until assets are wired.

## Follow-ups
- Create and assign ScriptableObject assets for the new configs in relevant prefabs/scenes.
- Consider migrating any remaining debug-only tunables into configs if designers want full data control from assets.
