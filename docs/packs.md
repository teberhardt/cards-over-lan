# Packs

Packs contain game content like cards and trophies which can be used to customize your game. This content is encoded as JSON data and stored in files.

This document provides an overview of the format of this data as well as how to write your own packs.


## Structure

A pack .json file contains a single JSON object acting as the root of all the pack's contents.

Aside from the contents, there are a number of required and optional metadata fields.

```json
{
    "id": "example_pack",
    "name": "Example Pack",
    "author": "Nicholas Fleck",
    "accent_color": "white",
    "accent_background": "#333",
    "license": "",
    "license_url": "",
    "cards": [],
    "trophies": []
}
```

### Metadata properties

|Property Name|Description|
|-------------|-----------|
|`id`|**(Required)** A unique ID string used internally to identify the pack.|
|`name`|**(Required)** The display name which will be used to to identify the pack to users. This string is also displayed on card ribbons.|
|`author`|A comma-separated list of contributing authors to this pack.|
|`accent_color`|A CSS value defining the text `color` property of the card accent ribbon.|
|`accent_background`|A CSS value defining the `background` property of the card accent ribbon.|
|`license`|A display name for the license applied to this pack.|
|`license_url`|A URL pointing to the full license text for this pack.|

### Cards

The JSON object format for a card is as follows:

```json
{
    "id": "w_example",
    "content": {
        "en": "An example white card.",
        "de": "Eine weiße Beispielkarte."
    },
    "type": "n",
    "tier": 0,
    "next_tier_id": "w_example_tier2",
    "tier_cost": 3,
    "flags": "test"
}
```

#### Properties

|Property Name|Description|
|-------------|-----------|
|`id`|The ID string used internally to identify the card. The `w_` and `b_` prefixes denote white and black cards respectively.|
|`content`|Contains the localized card text, indexed by its locale.|
|`type`| (Optional) The type of concept described by the card (e.g. noun)|
|`tier`|(Optional) Tier of the card. Base tier will have a value of `0`, and higher tiers have higher values.|
|`next_tier_id`|(Optional) The ID of the upgraded version of the card.|
|`tier_cost`|(Optional) The cost of acquiring this card as an upgrade.|
|`flags`|A string defining content flags for this card.|
|`pick`|The number of blank spaces (black cards only).|
|`draw`|The number of extra white cards drawn per player (black cards only).|

#### Types



### Trophies

### Taunts