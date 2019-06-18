![Cards Over LAN logo](https://i.imgur.com/97o7ZO4.png)

A Cards Against Humanity clone you can host on your own network.

![Official Discord Server](https://img.shields.io/badge/discord-official%20server-blueviolet.svg?style=for-the-badge&logo=discord&color=7289da)

<p align="center">
    <img alt="Gameplay Example" src="https://thumbs.gfycat.com/HealthyInformalGalapagoshawk-size_restricted.gif" />
</p>

|**Heads up!** This software is a work-in progress. It may contain bugs or eat your dog or something. Use at your own risk.|
|---|

## Features

* **Completely local.** Host it on any old system. No Internet connection necessary!
* **Mobile friendly.** The game was designed in a responsive manner, so it's almost like you're really playing the game and not sitting in a sad circle staring at screens.
* **Custom decks.** Write your own decks with a simple JSON format. You can also mix and match cards by adding multiple decks to your server.
* **Localizable cards.** Cards written in multiple languages will adapt to the system language of your device.
* **Trophies.** At the end of each game, see what Special Kind of Awful™ your friends really are.
* **Bots.** Add fake player to your game that pick random cards which are still funnier than you.
* **Card upgrades.** Some card may be "upgraded" by using Card Coins, which you get when you win a round. They might have more uses later, or removed entirely! Isn't that fun?
* **Skippable cards.** If you don't like the current black card, you can vote to skip it with the press of a button.
* **Idle detection.** If players are idle, the server ignores them to make them feel socially awkward.
* **Player preservation.** In conjunction with idle detection, if a player returns within a set time limit, they continue as if they never left the game.

## How does it work?

The game server is a [Nancy](http://nancyfx.org/) web server combined with a [WebSocket-Sharp](https://github.com/sta/websocket-sharp) real-time communication system.
The game server dishes out the static assets like Emeril Lagasse with his Baby Bam, and the WebSocket server keeps the player sessions running like Hell's Kitchen. ***[BAM!](https://www.youtube.com/watch?v=XvazQUYG1kE)***

## How do I use it?

The root directory of the project contains three important folders:

* `/packs` contains all of the JSON-formatted card decks, trophies and taunts for the game.
* `/web_content` contains the web UI code.
* `/CardsOverLan` contains the server that keeps the game kickin'.

## Building the Server

The zeroth step is the dependencies. 

If you're on Linux, you're gonna need the latest stable version of .NET Core, preferably 2.2.

If you're on Windows, skip down to the Visual Studio section to kickstart the process.

First of all, clone the server from GitHub to get the latest version.

The following steps vary by method of work.

### Command Line

Next, enter the "solution" directory (the root folder of the project) and run the following command:

```dotnet restore```

This installs all of the dependencies from NuGet for you.

Finally, to get this party started, raise a glass and type:

```dotnet run```

This launches the server on whatever port you specified on the startup of the project.

### Visual Studio

Open the `CardsOverLan` solution file in Visual Studio. (You need to have Visual Studio 2017's .NET Core support added, see [here](https://docs.microsoft.com/en-us/dotnet/core/windows-prerequisites?tabs=netcore2x) for instructions.

Build the project after it loads itself. You should find a copy of the server in one of the subdirectories of the newly-created `bin` folder.

### Rider

After installing .NET Core and ensuring Rider can find it, load the solution in Rider, click the build button and let it run.

## Other Considerations

You'll need to whitelist whatever port you chose for the webserver (defaults to 80) and whatever port you chose for the web socket server (defaults to 3000) in your firewall to make the game work as intended.

## Settings

The `settings.json` file is the place to be when messing around with how the game functions. Here's a table describing everything each property does, since [JSON does not support inline comments](https://stackoverflow.com/a/244858):

|**Property**|**Type**|**Description**|
|------------|--------|---------------|
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

The following table explains the dials on the bot's brain. All values are in milliseconds.

|**Property**|**Type**|**Description**|
|------------|--------|---------------|
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

## Frequently Asserted Questions

### Why?

I was bored and wanted a fun project to work on over winter break.

### There's already other projects that do this. Why not just use those?

They're all good in their own way, and each of them has different strengths. Below is a (non-exhaustive) comparison table:

|                       |**PYX**|**Azala**|**Cardcast**|**Cards Over LAN**|
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

Yes.

### Can I host this on a public web server?

Yes. But you should do it behind a remote proxy with HTTPS forwarding to be safe.

### You suck at web development! I could do this much better!

That's not a question.

### You should use React/Angular/Bootstrap/Vue.

That's not a question either. Also the answer is no.

### There's a feature I want you to add.

Submit an issue and I'll send your request to the great and powerful Oz for pondering.

### Why don't you include the original cards from the game?

The original deck is licensed under the Creative Commons BY-NC-SA 2.0 license.

I would have to place all the code under this license otherwise, but the CC isn't designed for code, so I decoupled the base deck from the server and placed it [here](https://github.com/cardsoverlan/cah-packs).

### Can I filter cards out of the deck that morally offend me?

You can use the `exclude_content` property in the settings file for this.

If you hate all violence and sexual content and are okay with having virtually no content in your deck, then you can do this:

```json
{
  "exclude_content": ["v", "s"]
}
```

Likewise, if you only hate cards that have *both* violence and sexual content, then you do this:

```json
{
  "exclude_content": ["v s"]
}
```

Reload the server after making any changes and enjoy your morally superior version of a purposely offensive card game.

### How can I contribute?

Regarding code contributions, send a pull request with any changes and I'll take a look whenever I have the chance.

If you want to translate some of the deck or the phrases used in the UI, feel free to send a pull request for that too!

### Where's the legal stuff?

Cards Over LAN is a clone of the card game Cards Against Humanity. The original game, available at [their website](https://cardsagainsthumanity.com), is generously made available to the public under the [Creative Commons BY-NC-SA 2.0 license](https://creativecommons.org/licenses/by-nc-sa/2.0).
This project is ***not*** endorsed by Cards Against Humanity, LLC.

All contents of this repository, including all of the dependencies (trust me, I checked) are made available under the [MIT license](LICENSE.md).