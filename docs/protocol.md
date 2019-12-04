
# Game Protocol

## Overview

Clients use the WebSocket protocol to communicate with the server.

The server listens for client connections on `./game:3000` by default.

Clients are provided certain query string options:

* `name`: Sets your name to the specified string upon successful connection.

There is no explicit concept of 'responses' in the protocol. While the server may send a message in response to a particular client message, it is not required to explicitly state that it is a response.

Messages are encoded as UTF-8 JSON objects. Each object must contain at least a `msg` property, which is a string describing what kind of message is being sent.

Client message names are prefixed with `c_` and server messages with `s_` for clarity when discussing message types.

## Cards

In order to keep messages as small as possible, cards are only referenced by their ID strings during play.
ID strings must follow the following conventions:

* Non-custom IDs may only contain alphanumeric characters and underscores.
* White card IDs start with `w_`.
* Black card IDs start with `b_`.
* Custom card IDs start with `custom_` followed by the card text encoded from UTF8 to Base64.

## Client messages

### c_playcards

Sent by the client during an active game when the player has selected their white card(s) for the current round. The cards are represented using their card IDs, and are in the order of the blanks they are to appear in.

```json
{
    "msg": "c_playcards",
    "cards": ["w_example1", "w_example2"]
}
```

### c_judgecards

Sent by the client when the client's player is judging the current round and they have selected the winning play. 
The round winner is identified by the index of the play from the play list provided by the server.
The client does not know whose play it is until the round ends.

```json
{
    "msg": "c_judgecards",
    "play_index": 0
}
```

### c_updateinfo

Sent by the user to update client-specific information, such as username.

List of possible user info keys:
|Key|Description|
|`name`|Name of player.|

```json
{
    "msg": "c_updateinfo",
    "userinfo": {
        "name": "Berkin"
    }
}
```

### c_upgradecard

Sent by the user when they request to spend Aux Points to upgrade a card to a higher tier.

```json
{
    "msg": "c_upgradecard",
    "card_id": "w_example"
}
```

### c_discardcard

Sent by the user when they request to discard a card in their hand.

```json
    "msg": "c_discardcard",
    "card_id": "w_example"
```

### c_vote_skip

Sent by the user when they change their vote to skip the current black card.

```json
{
    "msg": "c_vote_skip",
    "voted": true // sets vote status
}
```

### c_chat_msg

Sent by the user when they send a message in chat.

```json
{
    "msg": "c_chat_msg",
    "body": "AutoRodney sucks"
}
```

### c_ready_up

Sent by the user when they set their "ready up" status.

```json
{
    "msg": "c_ready_up",
    "status": true // True when ready, false when not ready
}
```

## Server messages

### s_allcards

The server sends this to a client in response to a `c_getallcards` message. It contains every card in use by the current game. Black and white cards are listed in separate arrays for convenience.

```json
{
    "msg": "s_allcards",
    "packs": [
        {
            "id": "berkins_deck",
            "name": "Berkin's Big Deck",
            "accent": "red",
            "cards": [
                {
                    "id": "w_example",
                    "content": {
                        "en-US": "An example card.",
                        "de-DE": "Eine Beispielkarte."
                    }
                }
            ]
        }
    ]
}
```

### s_gamestate

Sent by the server to inform the client which stage the game is currently in.

The possible strings for the `stage` property are listed below:

|Stage ID|Description|
|--------|-----------|
|`game_starting`|The game is starting and waiting for players.|
|`playing`|A round has started and players are choosing cards.|
|`judging`|Judging of the round has begun and the judge will be prompted.|
|`round_end`|The round has concluded and the winning play is displayed.|
|`game_end`|The game has concluded and the winning player is displayed.|

```json
{
    "msg": "s_gamestate",
    "stage": "game_starting",
    "ready_up": true, // Players are currently in Ready-Up mode (game_start, round_end)

    "round": 1, // Round number. (all stages)

    // Black card for current round. (playing, judging, round_end, game_end)
    "black_card": "b_example",

    // Player ID of round judge. (playing, judging, round_end, game_end)
    "judge": 123,

    // White cards played. (judging, round_end)
    "plays": [["w_example1"], ["w_example2"]],

    // Players that haven't played their cards yet. (playing)
    "pending_players": [123, 456],

    // Index of winning play. (round_end, game_end)
    "winning_play": 0,

    // ID of round winner. (round_end)
    "winning_player": 123,

    // Final results of game. Null if stage is not game_end.
    "game_results": {
        "winners": [123], // multiple if tie
        "trophy_winners": [
            {
                "id": 123,
                "trophies": [
                    { ... }
                ]
            }
        ]
    }
}
```

### s_players

Sent by the server to provide clients with the current list of players and their information.

```json
{
    "msg": "s_players",
    "players": [
        {
            "name": "Berkin",
            "id": 123,
            "score": 2,
            "upgrade_points": 1,
            "voted_skip": false,
            "idle": false,
            "ready_up": true,
        }
    ]
}
```

### s_auxclientdata

Provides auxiliary client data to a single player, such as upgrade points.

```json
{
    "msg": "s_auxclientdata",
    "aux_points": 3
}
```

### s_notify_skipped

Informs a client that the previous black card was skipped.

```json
{
    "msg": "s_notify_skipped",
    "skipped_id": "b_oldcard",
    "replacement_id": "b_newcard"
}
```

### s_hand

Sent to a client to inform them of the current contents of their hand.

Sent in response to the following actions:
* Game has started.
* Player plays a card.
* Player discards a card.

```json
{
    "msg": "s_hand",
    "blanks": 2, // Number of available blank cards
    "discards": 10, // Number of available discards
    "hand": ["w_example1", "w_example2"]
}
```

### s_cardsplayed

Sent to a client to inform them of the cards they have played for the current round.

```json
{
    "msg": "s_cardsplayed",
    "selection": ["w_example1", "w_example2"]
}
```

### s_clientinfo

Contains information that identifies a client. Sent by server when the player changes their client options.

```json
{
    "msg": "s_clientinfo",
    "player_id": 123,
    "player_name": "Berkin",
    "player_token": "XXXXXXXX"
}
```

### s_rejectclient

Used when the server rejects a client from connecting to the game.
For rejected clients, this is the first and only message sent.
After sending, the client is immediately disconnected.

```json
{
    "msg": "s_rejectclient",
    "reason": "reject_server_full",
    "desc": "Extended rejection description goes here"
}
```

#### Reason IDs

|Reason ID|Description|
|---------|-----------|
|`reject_server_full`|The server is full and cannot accept any more players.|
|`reject_banned`|The server has banned the connecting client.|
|`reject_duplicate`|The server has detected that the client is attempting to open more than one instance of the game.|
|`reject_afk`|The server has disconnected the client due to an extended period of inactivity.|
|`reject_bad_password`|The server has rejected the client because they provided the wrong password.|

### s_chat_msg

Sent to clients when a new chat message is posted.

```json
{
    "msg": "s_chat_msg",
    "author": "Berkin",
    "body": "Example text"
}
```