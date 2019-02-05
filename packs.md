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
    "accent": "red",
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
|`accent`|The name of the accent to use for card ribbons.|
|`license`|A display name for the license applied to this pack.|
|`license_url`|A URL pointing to the full license text for this pack.|

#### Accents

Valid accent names are: `white`, `black`, `red`, `orange`, `yellow`, `green`, `blue`, `lightblue`, `turquoise`, `navy`, `purple`, `pink`, `brown`, `limegreen`, `rainbow`