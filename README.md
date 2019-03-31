# Cards Over LAN

A Cards Against Humanity clone for hosting on your home network.

[Official Discord Server](https://discord.gg/VsZkprN)

<p align="center"><img src="https://thumbs.gfycat.com/HealthyInformalGalapagoshawk-size_restricted.gif"><p>

|**This software is a work-in-progress. It may contain bugs.**|
|---|

## Features

* **Completely Local** - Host on any old LAN. No Internet connection necessary.
* **Mobile Friendly** - Designed to adapt for mobile browsers, so it's almost like you're really playing cards and not sitting in a sad circle staring at screens.
* **Custom Decks** - Write your own decks using a simple JSON format. Mix and match cards by adding multiple decks to your server.
* **Localizable Cards** - Cards can be written in multiple languages and your device will display them in your set browser language. This means you can even have many people playing on the same server in different languages.
* **Trophies** - At the end of each game, see what kind of awful each of your friends is.
* **Bots** - Add fake players to your game that pick random cards but are probably still funnier than you.
* **Card Upgrades** - Some cards may be "upgraded" by using Card Coins. You earn Card Coins with each round you win. They might have more uses later, or be removed entirely. Who knows what the future may hold?
* **Skipping** - If you don't like the current black card, you can vote to skip it with the press of a button.
* **Idle Detection** - If players are idle for a set amount of time, the server ignores them, allowing the game to continue without them.
* **Player Preserves** - If a player loses connection, they can reconnect within a certain time limit without losing their cards/points.


## How it works

The game server consists of a NancyFx web server and WebSocket server. The web server dishes out the webapp to anyone accessing the game in a browser. The webapp connects to the WebSocket server, which connects players to the actual game.

## How to use it

The root directory contains a few important folders.

* `/packs`: Contains all the decks/trophies that will go in your server.
* `/web_content`: Contains the webapp.
* `/CardsOverLan`: Contains the server code.

### Prerequisites

To build the server, you need Visual Studio 2017 and .NET Framework 4.7.2.

### Building

Open the `CardsOverLan` project in Visual Studio and build it.

The build will contain copies of the `packs` and `web_content` folders.
It also contains a `settings.json` file that contains the server settings. See below for how to configure this file.

After building, run CardsOverLan.exe to start the server.

### Firewall settings

Your firewall settings may prevent the game from working properly.
Make sure that TCP port 80 (or whatever port you set in the host URL) as well as TCP port 3000 are whitelisted for the server.

### Configuring settings.json

The settings.json file contains a number of properties that control how the server and game behave.

|Property|Type|Description|
|--------|----|-----------|
|`afk_recovery_time_seconds`|Integer|Number of seconds an AFK player must play within in order to not be AFK anymore.|
|`afk_time_seconds`|Integer|Number of seconds a player can be idle before becoming AFK.|
|`allow_duplicates`|Boolean|Specifies whether to allow multiple clients from the same IP address.|
|`allow_skips`|Boolean|Specifies whether players are allowed to skip black cards.|
|`blank_cards`|Integer|Number of blank cards given to each player. These are not counted by `hand_size`.|
|`bot_config`|Object|Configures bot behavior. **See below.**|
|`bot_count`|Integer|Number of bots to add to the game.|
|`bot_czars`|Boolean|Specifies whether to allow bots to be Card Czars.|
|`bot_names`|String[]|List of names to assign to bots.|
|`client_ws_port`|Integer|Sets the client-facing port of the WebSocket server that they will connect to. Useful if hosting behind a reverse proxy.|
|`discards`|Integer|The number of discards allowed per player.|
|`enable_afk`|Boolean|Specifies whether AFK timers are enabled.|
|`enable_bot_taunts`|Boolean|Specifies whether bot taunts are enabled. Overridden by `enable_chat`.|
|`enable_chat`|Boolean|Specifies whether in-game chat is enabled.|
|`enable_idle_kick`|Boolean|Specifies whether idle kicking is enabled.|
|`enable_upgrades`|Boolean|Specifies whether cards can be upgraded. Disabling this feature will fully upgrade all cards.|
|`enable_trophies`|Boolean|Specifies whether players can earn trophies.|
|`exclude_content`|String[]|Array with content flag strings to exclude cards by. Use this to filter out specific types of cards.|
|`exclude_packs`|String[]|Array of pack IDs. Forces the server to not load any packs in this array. Overrides any included packs in `use_packs`.|
|`game_end_timeout`|Integer|Time, in milliseconds, to wait before starting a new game.|
|`hand_size`|Integer|Number of cards dealt to each player.|
|`host_url`|String|The endpoint that the webserver will listen on.|
|`max_blank_card_length`|Integer|Maximum number of characters allowed in blank cards.|
|`max_player_name_length`|Integer|Maximum number of characters that a player name can have.|
|`max_players`|Integer|Maximum number of players that the server can hold.|
|`max_points`|Integer|Points required for a player to win the game.|
|`max_rounds`|Integer|Maximum number of rounds before game ends.|
|`max_spectators`|Integer|Maximum number of spectators allowed.|
|`min_players`|Integer|Minimum required players in order for the game to start.|
|`perma_czar`|Boolean|One lucky winner is selected to be the Card Czar for the entire game.|
|`pick_one_only`|Boolean|Specifies whether to prevent black cards with multiple blanks from being drawn.|
|`require_languages`|String[]|Excludes any cards that don't support all of the specified language codes. Leave empty to disable.|
|`round_end_timeout`|Integer|Time, in milliseconds, to wait before starting the next round.|
|`server_name`|String|The server name displayed in on the join screen.|
|`server_password`|String|The password to the server. Leave blank to disable.|
|`use_packs`|String[]|Array of pack IDs. Forces the server to only load packs in this array. Leave empty to load all available packs.|
|`web_root`|String|The path to the webapp directory. Leave blank to default to `./web_content`.|
|`ws_url`|String|The endpoint that the game's WebSocket server will listen on.|
|`winner_czar`|Boolean|When set to `true`, the Card Czar will always be the previous round winner. Overridden by `bot_czars` and `perma_czar`.|

#### bot_config

All time properties are in milliseconds.

|Property|Type|Description|
|--------|----|-----------|
|`play_min_base_delay`|Integer|Minimum baseline delay before bot plays.|
|`play_max_base_delay`|Integer|Maximum baseline delay before bot plays.|
|`play_min_per_card_delay`|Integer|Minimum additional delay per card played.|
|`play_max_per_card_delay`|Integer|Maximum additional delay per card played.|
|`judge_min_per_play_delay`|Integer|Minimum delay per play before bot picks winner.|
|`judge_max_per_play_delay`|Integer|Maximum delay per play before bot picks winner.|
|`judge_min_per_card_delay`|Integer|Minimum additional delay per card before bot picks winner.|
|`judge_max_per_card_delay`|Integer|Maximum additional delay per card before bot picks winner.|
|`min_typing_interval`|Integer|Minimum interval between keystrokes when typing chat messages.|
|`max_typing_interval`|Integer|Maximum interval between keystrokes when typing chat messages.|

## FAQ

### Why?

I was bored and wanted a fun project to work on over winter break.

### There's already PYX, Azala, Cardcast, etc. Why should I use your thing?

They're all good in their own way, and each of them has different strengths. Below is a (non-exhaustive) comparison table:

|                       |PYX|Azala|Cardcast|Cards Over LAN|
|-----------------------|:-:|:---:|:------:|:------------:|
|**Desktop support**    |✔️️️|✔️|❌|✔️|
|**Mobile support**     |❌|✔️|✔️|✔️|
|**Self-hosting**       |✔️|❌|✔️|✔️|
|**Offline play**       |❌|❌|❌²|✔️|
|**Discards**           |❌|✔️|❌|✔️|
|**Chat**               |✔️¹|✔️|❌|✔️|
|**Blank cards**        |✔️¹|✔️|❌|✔️|
|**Localization**       |❌|❌|❌|✔️|
|**Bots**               |❌|✔️|✔️|✔️|
|**Black card skipping**|❌|✔️|❌|✔️|
|**Trophies**           |❌|❌|❌|✔️|
|**Multiple games**     |✔️|✔️|❌|❌|
|**License**            |BSD 2-Clause|Closed Source|Closed Source|MIT|

_¹ Chat and blank cards only available in third-party PYX servers._

_² Internet connection required to download Cardcast decks._

### Can I add my own cards?

Yes, decks are written using a simple JSON format. Add them to the `packs` folder before starting up the server.

### Can I host this on a public webserver?

Yes! I recommend hosting behind a reverse proxy such as Nginx or Apache.
Although the game server doesn't directly support HTTPS, you can forward HTTPS traffic to it using a load balancer or similar.

### You suck at webdev! I could do this much better!

Isn't that the great thing about open source software?

### You should use React/Angular/Bootstrap/Vue/etc.

No.

### There's a feature I want you to add.

Please submit an issue detailing what you're looking for.

### Why don't you include the CAH cards?

CAH is licensed under a CC BY-NC-SA 2.0 license.
This means that I cannot distribute my software with their IP included without placing all of my code under that same license. Since Creative Commons is not designed for software,
it simplifies things greatly to decouple the CAH cards from the game.

If you want the CAH cards, you can get the packs [here](https://github.com/cardsoverlan/cah-packs).

### Can I filter out cards that mortally offend me?

You can use the `exclude_content` property in your settings for this.

Example: if you hate all violence and sexual content, you can do this to exclude any cards mentioning such things:

```json
"exclude_content": ["v", "s"]
```

If you only hate cards including both violence _and_ sexual content, but are fine with one or the other, combine the flags in one string.
The order doesn't matter.

```json
"exclude_content": ["v s"]
```

After saving your changes, relaunch the server and enjoy your ten-card deck!

### How can I contribute?

Regarding code contributions, I will have guidelines for this up soon.

If you speak a language that isn't English and want to help me translate the cards, feel free to submit a pull request with your translations!

## Legal

Cards Over LAN is a clone of Cards Against Humanity. The original game, available at [cardsagainsthumanity.com](https://cardsagainsthumanity.com), is available under a [CC BY-NC-SA license](https://creativecommons.org/licenses/by-nc-sa/2.0/). This project is in no way endorsed or sponsored by Cards Against Humanity. 

For project license information, see [LICENSE](LICENSE). 

For third-party licenses, see [LICENSES_THIRD_PARTY](LICENSES_THIRD_PARTY.md).
