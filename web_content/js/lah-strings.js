(() => {
    const DEFAULT_LOCALE = "en";

    let uiStrings = {
        "ui_hello": {
            "en": "Hello",
            "de": "Hallo"
        },
        "ui_hello_x": {
            "en": "Hello, {0}",
            "de": "Hallo, {0}"
        },
        "ui_waiting_for_players": {
            "en": "Waiting for more players",
            "de": "Warte auf weitere Spieler"
        },
        "ui_fullscreen": {
            "en": "Fullscreen",
            "de": "Vollbildmodus"
        },
        "ui_options": {
            "en": "Options",
            "de": "Optionen"
        },
        "ui_about": {
            "en": "About",
            "de": "Über"
        },
        "ui_round_num": {
            "en": "Round {0}",
            "de": "Runde {0}"
        },
        "ui_card_czar": {
            "en": "Card Czar",
            "de": "Zar"
        },
        "ui_score": {
            "en": "Score",
            "de": "Punktzahl"
        },
        "ui_pending_players": {
            "en": "Pending players",
            "de": "Ausstehende Spieler"
        },
        "ui_player_count": {
            "en": "Player count",
            "de": "Spieleranzahl"
        },
        "ui_btn_play": {
            "en": "PLAY",
            "de": "FERTIG"
        },
        "ui_blank_card_prompt": {
            "en": "Write your card here.",
            "de": "Kartentext hier eingeben."
        },
        "ui_choose_best_play": {
            "en": "Choose the best play.",
            "de": "Wähle die beste Antwort aus."
        },
        "ui_czar_deciding": {
            "en": "Card Czar is deciding...",
            "de": "Der Zar entscheidet sich..."
        },
        "ui_nobody_wins_round": {
            "en": "Nobody wins the round.",
            "de": "Keiner hat die Runde gewonnen."
        },
        "ui_x_wins_round": {
            "en": "{0} wins the round!",
            "de": "{0} hat die Runde gewonnen!"
        },
        "ui_you_win_round": {
            "en": "You win the round!",
            "de": "Du hast die Runde gewonnen!"
        },
        "ui_you_are_czar": {
            "en": "You're the Carz Czar.",
            "de": "Du bist der Zar."
        },
        "ui_sub_nobody": {
            "en": "Nobody",
            "de": "Keiner"
        },
        "ui_x_is_czar": {
            "en": "{0} is the Card Czar.",
            "de": "{0} ist der Zar."
        },
        "ui_winner_left_nobody_scores": {
            "en": "Round winner left. Nobody scores.",
            "de": "Der Rundensieger hat das Spiel verlassen. Punkte für keinen!"
        },
        "ui_waiting_for_other_players": {
            "en": "Waiting for other players...",
            "de": "Warte auf andere Spieler..."
        },
        "ui_waiting_for_x": {
            "en": "Waiting for {0}...",
            "de": "Warte auf {0}..."
        },
        "ui_list_and": {
            "en": "and ",
            "de": "und "
        },
        "ui_list_comma": {
            "en": ", ",
            "de": ", "
        },
        "ui_list_space": {
            "en": " ",
            "de": " "
        },
        "ui_game_over": {
            "en": "Game Over!",
            "de": "Spielende"
        },
        "ui_username_placeholder": {
            "en": "Who are you?",
            "de": "Wer bist du?"
        },
        "ui_nickname": {
            "en": "Nickname",
            "de": "Spitzname"
        },
        "ui_options_apply": {
            "en": "Apply",
            "de": "Speichern"
        },
        "ui_game_results": {
            "en": "Game Results",
            "de": "Spielergebnisse"
        },
        "ui_accent_color": {
            "en": "Accent color",
            "de": "Akzentfarbe"
        },
        "ui_accent_color_placeholder": {
            "en": "Type a color name",
            "de": "Farbename hier eingeben"
        }
    };

    let fmt = function (format) {
        var args = Array.prototype.slice.call(arguments, 1);
        return format.replace(/{(\d+)}/g, function (match, number) {
            return typeof args[number] != 'undefined'
                ? args[number]
                : match;
        })
    };

    getUiString = function (key) {
        let entry = uiStrings[key];
        if (!entry) return key;
        let lang = navigator.language;
        let fmtStr = entry[lang] || entry[lang.replace(/(.*)-[a-z0-9_\-]+/i, (m, p) => p)] || entry[DEFAULT_LOCALE] || Object.values(entry)[0];
        if (!fmtStr) return key;
        return fmt(fmtStr, ...(Array.prototype.slice.call(arguments, 1) || []));
    }

    // Localize text contents
    const localizables = document.querySelectorAll("*[data-ui-string]");
    for(let e of localizables) {
        e.textContent = getUiString(e.getAttribute("data-ui-string"));
    }

    // Localize attributes
    const localizePfx = "data-localize-";
    const attrLocalizables = document.querySelectorAll("body *");
    for(let e of attrLocalizables) {
        for(let attr of e.attributes) {
            if (attr.nodeName.startsWith(localizePfx)) {
                e.setAttribute(attr.nodeName.substring(localizePfx.length), getUiString(attr.nodeValue));
            }
        }
    }
})();