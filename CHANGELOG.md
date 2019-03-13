# Cards Over LAN - Changelog

## 1.0.0b1

- Initial beta release.

## 1.0.0b2

- ADD: Discards
- ADD: Black card skipping
- ADD: Card upgrades
- ADD: Player list (desktop)
- ADD: Join screen with server info
- ADD: Spectator mode
- ADD: `bot_czars` setting
    - Allows control over whether bots become Card Czars.
- ADD: `use_packs` setting
    - Controls which packs are used in the game.
- ADD: `allow_skips` setting
    - Enables black card skipping.
- ADD: `max_spectators` setting
    - Controls maximum spectator count.
- ADD: `allow_duplicates` setting
    - Prevents multiple clients connecting from the same IP address.
- ADD: `discards` setting
    - Configures discards per player per game.
- ADD: `enable_upgrades` setting
    - Allows admin to disable card upgrades.
- ADD: `max_rounds` setting
    - Controls maximum rounds per game.
- ADD: Version number now displayed in client
- ADD: More cards!
- ADD: Transcendence Pack

- FIX: Idle plays no longer show blank plays in judging stage

## 1.0.0b3

- ADD: (Webapp) In-game chat
    - Supports basic Markdown (bold, italic, strikethrough)
    - Custom Mardown for animated text
        - `@@TEXT@@` - Angry text!
        - `^Text^` - Bouncing text!
        - `^^Text^^` - Jiggling text?
        - Should I add more?
    - Enable with `enable_chat` setting.
- ADD: (Webapp) Option to enable/disable animated text
- ADD: (Server) Added Content Security Policy
- ADD: Bot taunts
    - Enable with `enable_bot_taunts` setting.
- ADD: `exclude_packs` setting

- FIX: (Webapp) Wonky layout issues on Firefox. Might still be mildly wonky.
- FIX: (Server) Occasional deadlock when accessing player list with many clients

- WONTFIX: Edge incompatability. They don't even support `for ... of` loops yet. Get with the times, Edge.