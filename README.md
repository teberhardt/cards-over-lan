# LAN Against Humanity

A Cards Against Humanity clone for hosting on your home network.

**This software is a work-in-progress. It may be missing functionality or contain bugs.**


## Features

* **Completely Local** - Host on any old LAN. No Internet connection necessary.
* **Mobile Friendly** - Designed to adapt for mobile browsers, so it's almost like you're really playing cards and not sitting in a sad circle staring at screens.
* **Custom Decks** - Write your own decks using a simple JSON format. Mix and match cards by adding multiple decks to your server.
* **Localizable Cards** - Cards can be written in multiple languages and your device will display them in your set browser language. This means you can even have many people playing on the same server in different languages.
* **Trophies** - At the end of each game, see what kind of awful each of your friends is.


## How it works

The game server consists of a NancyFx web server and WebSocket server. The web server dishes out the webapp to anyone accessing the game in a browser. The webapp connects to the WebSocket server, which connects players to the actual game.

## How to use it

The root directory contains a few important folders.

* `/packs`: Contains all the decks/trophies that will go in your server.
* `/web_content`: Contains the webapp.
* `/LahServer`: Contains the server code.

### Prerequisites

You need Visual Studio 2017 and .NET Framework 4.7.2.

### Building

Open the `LahServer` project in Visual Studio and build it.

The build will contain copies of the `decks` and `web_content` folders.
It also contains a `settings.json` file that contains the server settings. See below for how to configure this file.

After building, run LahServer.exe to start the server.

### Firewall settings

Your firewall settings may prevent the game from working properly.
Make sure that TCP port 80 (or whatever port you set in the host URL) as well as TCP port 3000 are whitelisted for the server.

### Configuring settings.json

The settings.json file contains a number of properties that control how the server and game behave.

|Property|Type|Description|
|--------|----|-----------|
|`host`|String|The URL and port that the server will be hosted on.|
|`max_players`|Integer|Maximum number of players that the server can hold.|
|`min_players`|Integer|Minimum required players in order for the game to start.|
|`max_player_name_length`|Integer|Maximum number of characters that a player name can have.|
|`hand_size`|Integer|Number of cards dealt to each player.|
|`blank_cards`|Integer|Number of blank cards given to each player. These are not counted by `hand_size`.|
|`round_end_timeout`|Integer|Time, in milliseconds, to wait before starting the next round.|
|`game_end_timeout`|Integer|Time, in milliseconds, to wait before starting a new game.|
|`max_points`|Integer|Points required for a player to win the game.|
|`perma_czar`|Boolean|One lucky winner is selected to be the Card Czar for the entire game.|
|`afk_time_seconds`|Integer|Number of seconds a player can be idle before becoming AFK.|
|`afk_recovery_time_seconds`|Integer|Number of seconds an AFK player must play within in order to not be AFK anymore.|
|`exclude_content`|Array|Array with content flag strings to exclude cards by. Use this to filter out specific types of cards.|


## FAQ

### Why?

I was bored and wanted a fun project to work on over winter break.

### But we already have things like PYX, Azala, etc.  Why another one?

And those are great, there's nothing wrong with them. As mentioned above, this is just a project I did for fun and decided was worth sharing.

### Can I add my own cards?

Yes, decks are written using a simple JSON format. Add them to the `decks` folder before starting up the server.

### Can I host this on a public webserver?

I really don't recommend it. It's only designed to host one game at a time. It also doesn't support HTTPS at the moment, so frankly that would be a pretty unwise thing to do.

### Your webdev skills suck! I could do so much better!

Isn't that the great thing about open source software?

### You should use React/Angular/Bootstrap/etc.

No.

### There's a feature I want you to add.

Submit an issue and we'll see what can be done.

### Why don't you include the CAH cards?

CAH is licensed under a CC BY-NC-SA 2.0 license.
If I distributed my software with their IP included, I would have to place all of my code under that same license. Since Creative Commons is not designed for software,
it's much easier for everyone if I decouple the CAH content from the game.

### Can I filter out cards that mortally offend me?

Use the `exclude_content` property in settings.json.

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

The game isn't done and nothing is set in stone, so I'm reluctant to invite code contributions until the project is more mature.

However, if you speak a language that isn't English and want to help me translate the cards, feel free to submit a pull request with your translations!

## Legal

LAN Against Humanity is a clone of Cards Against Humanity. The original game, available at [cardsagainsthumanity.com](https://cardsagainsthumanity.com), is available under a [CC BY-NC-SA license](https://creativecommons.org/licenses/by-nc-sa/2.0/). This project is in no way endorsed or sponsored by Cards Against Humanity. 

For project license information, see [LICENSE](LICENSE). 

For third-party licenses, see [LICENSES_THIRD_PARTY](LICENSES_THIRD_PARTY.md).