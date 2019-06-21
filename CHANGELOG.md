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

### Webapp

- ADD: In-game chat
    - Supports basic Markdown (bold, italic, strikethrough)
    - Custom Mardown for animated text
        - `@@TEXT@@` - Angry text!
        - `^Text^` - Bouncing text!
        - `^^Text^^` - Jiggling text?
        - `//Text//` - Tilting text!
        - Parsing is pretty naive, and doesn't always nest well. May improve later.
    - Enable with `enable_chat` setting.
- ADD: Option to enable/disable animated text
- ADD: Notifications
    - You can turn them off in the game options.
- ADD: Cookie notification
- ADD: 404 page
- ADD: Content Security Policy
- ADD: New logo

- CHANGE: Refresh server info on websocket connection/reconnection
- CHANGE: Moved display prefs to local storage instead of cookies

- FIX: Wonky layout issues on Firefox. Might still be mildly wonky.

- REMOVE: jQuery

- WONTFIX: Edge incompatibility. Edge still doesn't support `for ... of` loops and I'm not about to rewrite half my code to accommodate for that.

### Server

- ADD: Bot taunts
    - Enable with `enable_bot_taunts` setting.
    - Fully moddable!
- ADD: `exclude_packs` setting to blacklist packs
- ADD: `use_packs` setting to whitelist packs
- ADD: `max_blank_card_length` setting
- ADD: `bot_config` setting
- ADD: Server password support
- ADD: Customizable HTTP and WS endpoints
- ADD: Configurable client-side WebSocket port
    - Configure with `client_ws_port` setting.
- ADD: Client idle kick
    - Enable with `enable_idle_kick` setting.
- ADD: Gameplay analytics. 
    - Pretty basic right now, might add more stuff later.
- ADD: Player Preserves
    - Players who lose connection and reconnect within a set time limit will have their points/cards restored.

- FIX: Occasional deadlock when accessing player list with many clients
- FIX: Faulty duplicate prevention
- FIX: Disappearing czar bug (fingers crossed)

## 1.0.0b4

This is a slightly smaller update, which includes some much-needed additions and bugfixes.

### Content

- ADD: A whole lotta new cards, as usual.

- CHANGE: Trophy requirements relaxed a bit.

### Client

- ADD: Bots now have a special badge by their name, because some people seem to think they're actual humans.
- ADD: Some new styling options for deck ribbons
    - `accent_text_decoration`: Corresponds to CSS `text-decoration` property
    - `accent_font_style`: Corresponds to CSS `font-style` property
    - `accent_font_weight`: Corresponds to CSS `font-weight` property

### Server

- ADD: More Granular timeouts
    - Players and judges now have separate timeout settings
- ADD: Game Ready-Up Feature
    - Allow players to collectively choose when to start the next game.
- ADD: Bots now have a special name badge in the player list so they can be more easily identified
- ADD: `judge_per_card_timeout_bonus` setting
- ADD: `player_per_card_timeout_bonus` setting

- FIX: If a czar disconnected or went idle, the new czar used to be able to select their own play. This is no longer possible.
    - If a czar tries to self-vote, another play will be randomly selected instead.