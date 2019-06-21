((g) => {
    "use strict";

    // Constants
    const IS_HTTPS = location.protocol === "https:";
    const STAGE_GAME_STARTING = "game_starting";
    const STAGE_PLAYING = "playing";
    const STAGE_JUDGING = "judging";
    const STAGE_ROUND_END = "round_end";
    const STAGE_GAME_END = "game_end";
    const WS_DEFAULT_PORT = 3000;
    const DOMAIN = document.domain;
    const WS_PROTOCOL = IS_HTTPS ? "wss://" : "ws://";
    const DEFAULT_LOCALE = "en";
    const MAX_CHAT_MESSAGES = 100;

    // DOM function aliases
    const elem = document.querySelector.bind(document);
    const elems = document.querySelectorAll.bind(document);
    const make = document.createElement.bind(document);
    const div = () => document.createElement("div");
    const span = () => document.createElement("span");

    // Base64 helper functions
    function base64Encode(str) {
        return btoa(encodeURIComponent(str).replace(/%([0-9A-F]{2})/g, (m, p) => String.fromCharCode("0x" + p)));
    }

    function base64Decode(str) {
        return decodeURIComponent(atob(str).split("").map(c => "%" + ("00" + c.charCodeAt(0).toString(16)).slice(-2)).join(""));
    }

    // Promise helpers
    Promise.delay = (ms) => new Promise(resolve => setTimeout(resolve, ms));

    class Card {
        constructor(type, id, content, draw, pick, pack, tier, tierCost, nextTierId) {
            this._id = id;
            this._content = content;
            this._type = type;
            this._draw = draw;
            this._pick = pick;
            this._pack = pack;
            this._tier = tier;
            this._tierCost = tierCost;
            this._nextTierId = nextTierId;
        }

        get id() {
            return this._id;
        }

        get type() {
            return this._type;
        }

        get draw() {
            return this._draw;
        }

        get pick() {
            return this._pick;
        }

        get pack() {
            return this._pack;
        }

        get tier() {
            return this._tier;
        }

        get tierCost() {
            return this._tierCost;
        }

        get nextTierId() {
            return this._nextTierId;
        }

        getContent(langCode) {
            if (!langCode) return "???";
            return this._content[langCode] || this._content[langCode.replace(/(.*)-[a-z0-9_\-]+/i, (m, p) => p)] || this._content[DEFAULT_LOCALE] || "???";
        }

        getLocalContent() {
            return this.getContent(navigator.language);
        }

        toString() {
            this._content[DEFAULT_LOCALE] || "???";
        }

        static fromObject(json) {
            return new Card(
                json["id"].startsWith("b_") ? "black" : "white",
                json["id"],
                json["content"],
                json["draw"] || 0,
                json["pick"] || 1,
                json["pack"] || "",
                json["tier"] || 0,
                json["tier_cost"] || 0,
                json["next_tier_id"] || ""
            );
        }
    }

    Array.prototype.indexOfWhere = function(predicate) {
        for(let i = 0; i < this.length; i++) {
            if (predicate(this[i])) {
                return i;
            }
        }
        return -1;
    }

    Array.prototype.count = function(predicate) {
        let n = 0;
        for(let i = 0; i < this.length; i++) {
            if (predicate(this[i])) {
                n++;
            }
        }
        return n;
    }

    if (typeof g.lah === "undefined") {
        var lah = g.lah = {};
    }

    lah.score = 0;
    lah.round = 0;
    lah.whiteCards = {};
    lah.blackCards = {};
    lah.packMetadata = {};
    lah.playerHand = []; // Card[] - Array of current cards in hand
    lah.playerHandSelection = []; // [{blankIndex:number, id:string}] - Array of client's currently selected cards
    lah.clientPlayedCards = []; // string[] - Array of client's currently played card IDs
    lah.currentBlackCard = null; // Card - Current black card on table
    lah.localPlayerName = ""; // Local player name
    lah.localPlayerId = -1; // Local player ID
    lah.currentJudgeId = -1; // Player ID of current judge
    lah.judgeVotedSelf = false;
    lah.isClientJudge = false; // boolean - Am I the judge right now?
    lah.roundPlays = []; // string[][] - Array of card IDs played by all users
    lah.stage = STAGE_GAME_STARTING; // Current game stage ID
    lah.pendingPlayers = []; // List of player IDs who still need to play cards
    lah.playerList = [];
    lah.winningPlayerId = -1;
    lah.winningPlayIndex = -1;
    lah.selectedPlayIndex = -1;
    lah.isWaitingOnPlayer = false;
    lah.numBlanks = 0;
    lah.blankCards = []; // Array of strings containing blank card contents
    lah.gameResults = null;
    lah.lastRejectReason = "";
    lah.lastRejectDesc = "";
    lah.auxPoints = 0;
    lah.discards = 0;
    lah.serverInfo = {};
    lah.spectating = false;
    lah.clientVotedSkip = false;

    let gameArea = elem("#game");
    let handArea = elem("#hand-area");
    let readyUpArea = elem("#ready-up-area");
    let readyUpVotesValue = elem("#ready-up-votes-value");
    let handCardsContainer = elem("#hand-cards-container");
    let handCardsScrollArea = elem("#hand-cards-scroll-area");
    let playArea = elem("#play-area");
    let playCardsArea = elem("#play-cards-area");
    let playCardsScrollArea = elem("#play-cards-scroll-area");
    let blackCardArea = elem("#black-card-area");
    let judgeStatusCardText = elem("#judge-status-card-text");
    let judgeMessageBody = elem("#judge-message-body");
    let gameEndScreen = elem("#game-end-screen");
    let gameEndScoreboardEntries = elem("#game-end-scoreboard-entries");
    let gameEndTrophiesList = elem("#game-end-trophies-list");
    let btnPlay = elem("#btn-play");
    let btnPick = elem("#btn-judge-pick");
    let btnReadyUp = elem("#btn-ready-up");
    let playerList = elem("#player-list");
    let playerChat = elem("#player-chat-messages");

    let ws = new SuperiorWebSocket(null, "fibonacci");

    // Sanitizes HTML content for card text
    function createContentHtml(str) {
        return str
            .replace(/\s&\s/g, " &amp; ")
            .replace(/\'/g, "&apos;")
            .replace(/\"/g, "&quot;")
            .replace(/\(/g, "&lpar;")
            .replace(/\)/g, "&rpar;")
            .replace(/\n/g, "<br/>");
    }

    // fn(part, i, len)
    function joinString(arr, fn) {
        let s = "";
        let n = arr.length;
        for(let i = 0; i < n; i++) {
            s += fn(arr[i], i, n);
        }
        return s;
    }

    function mdToHtml(str) {
        return str
        .replace(/\^\^(.+?)\^\^/g, (m, t) => mdToHtml(joinString([...t], (p, i, n) => (i % 2 == 0 ? "<span class='juggle-a'>" : "<span class='juggle-b'>") + p + "</span>")))
        .replace(/\/\/(.+?)\/\//g, (m, t) => mdToHtml(joinString([...t], (p, i, n) => "<span class='tilt'>" + p + "</span>")))
        .replace(/\^(.+?)\^/g, (m, t) => "<span class=\"bounce\">" + mdToHtml(t) + "</span>")
        .replace(/\@\@(.+?)\@\@/g, (m, t) => "<span class=\"rage\">" + mdToHtml(t) + "</span>")
        .replace(/\~\~(.+?)\~\~/g, (m, t) => "<strike>" + mdToHtml(t) + "</strike>")
        .replace(/\*\*\*(.+?)\*\*\*/g, (m, t) => "<strong><em>" + mdToHtml(t) + "</em></strong>")
        .replace(/\*\*(.+?)\*\*/g, (m, t) => "<strong>" + mdToHtml(t) + "</strong>")
        .replace(/\*(.+?)\*/g, (m, t) => "<em>" + mdToHtml(t) + "</em>")
        ;
    }

    function getLocalString(localizedStringObject) {
        return localizedStringObject[navigator.language] || localizedStringObject[DEFAULT_LOCALE] || Object.values(localizedStringObject)[0] || "???";
    }

    // Make an HTMLElement from the specified Card object
    function makeCardElement(card, options) {
        let el = make("card");
        let packInfo = lah.packMetadata[card.pack];
        el.setAttribute("data-card", card.id);
        if (packInfo) el.setAttribute("data-packid", packInfo.id);
        el.setAttribute(card.type, "");

        // Card text
        el.innerHTML = createContentHtml(card.getLocalContent());

        if (options !== undefined) {
            if (card.type == "black" && options.showSkipControls === true) {
                let btnSkip = make("input");
                btnSkip.setAttribute("type", "button");
                btnSkip.setAttribute("value", getUiString("ui_btn_skip"));
                btnSkip.setAttribute("title", getUiString("ui_skip_tooltip"));
                btnSkip.classList.add("btn-skip", "player-only");
                btnSkip.addEventListener("click", e => lah.voteForSkip(!lah.clientVotedSkip));
                el.appendChild(btnSkip);
            }

            // Upgrade/discard controls
            if (options.showHandControls === true) {
                // Upgrade controls
                if (card.tier > 0) {
                    el.setAttribute("data-tier", card.tier);
                }
                if (card.nextTierId) {
                    let nextTierCard = lah.whiteCards[card.nextTierId];
                    el.setAttribute("data-upgrade", card.nextTierId);
                    el.setAttribute("data-upgrade-cost", (nextTierCard && nextTierCard.tierCost) || 0);

                    let btnUpgrade = div();
                    btnUpgrade.setClass("btn-upgrade", true);
                    let cost = nextTierCard.tierCost;
                    btnUpgrade.innerHTML = getUiString("ui_upgrade_button", cost);
                    btnUpgrade.addEventListener("mouseover", e => e.stopPropagation());
                    btnUpgrade.addEventListener("click", e => {
                        if (lah.auxPoints < cost) {
                            btnUpgrade.setClass("nope", true);
                            setTimeout(() => {
                                btnUpgrade.setClass("nope", false);
                            }, 250);
                        } else {
                            lah.upgradeCard(card.id);
                        }
                        e.stopPropagation();
                    });

                    el.appendChild(btnUpgrade);
                }

                // Discard controls
                if (lah.discards > 0) {
                    let btnDiscard = div();
                    btnDiscard.classList.add("btn-discard");
                    btnDiscard.setAttribute("title", getUiString("ui_discard_tooltip", lah.discards));
                    btnDiscard.addEventListener("click", e => {
                        e.stopPropagation();
                        el.classList.add("disabled");
                        el.classList.add("collapse");
                        // Backup timeout in case browser doesn't support animationend event
                        let timeoutToken = setTimeout(() => {
                            lah.discardCard(card.id);
                        }, 500);
                        el.addEventListener("animationend", () => {
                            clearTimeout(timeoutToken);
                            lah.discardCard(card.id);
                        });
                    });
                    el.appendChild(btnDiscard);
                }
            }
        }

        // Pack info ribbon
        if (packInfo) {
            let ribbon = div();
            ribbon.classList.add("ribbon");
            ribbon.setAttribute("data-packname", (packInfo && packInfo.name) || "");
            el.appendChild(ribbon);
        }

        // Add draw # if applicable
        if (card.draw > 0) {
            let divDraw = div();
            divDraw.classList.add("draw");
            let spanDrawNum = make("span");
            spanDrawNum.classList.add("num");
            spanDrawNum.innerText = card.draw.toString();
            divDraw.appendChild(spanDrawNum);
            el.appendChild(divDraw);
        }

        // Add pick # if applicable
        if (card.pick > 1) {
            let divPick = div();
            divPick.classList.add("pick");
            let spanPickNum = make("span");
            spanPickNum.classList.add("num");
            spanPickNum.innerText = card.pick.toString();
            divPick.appendChild(spanPickNum);
            el.appendChild(divPick);
        }
        
        return el;
    }

    // Make an HTMLElement representing a blank card
    function makeBlankCardElement(index) {
        let el = make("card");
        el.setAttribute("data-card", "blank");
        el.setAttribute("data-blank-index", index !== undefined ? index : -1);
        el.setAttribute("white", "");

        // Create textarea container
        let txtAreaDiv = div();
        txtAreaDiv.classList.add("blank-card-text-container");
        // Create textarea
        let txtArea = make("textarea");
        txtArea.setAttribute("aria-label", "Custom card text");
        txtArea.setAttribute("wrap", "hard");
        txtArea.setAttribute("placeholder", getUiString("ui_blank_card_prompt"));
        txtArea.setAttribute("spellcheck", "false");
        txtArea.onclick = e => e.stopPropagation();

        // Add any existing blank card text
        if (index !== undefined && index >= 0 && index < lah.numBlanks) {
            txtArea.value = lah.blankCards[index];
        }

        // Update stored copy of blank card text when textarea is changed
        txtArea.addEventListener("input", () => {
            if (index >= 0 && index < lah.numBlanks) {
                lah.blankCards[index] = txtArea.value.trim();
            }
        });

        txtArea.classList.add("blank-card-text");
        // Add the textarea to its container
        txtAreaDiv.appendChild(txtArea);
        // Add the container to the card
        el.appendChild(txtAreaDiv);

        let logo = div();
        logo.classList.add("logo");
        logo.setAttribute("aria-hidden", "true");
        el.appendChild(logo);
        return el;
    }

    function makePlayerListElement(player) {
        let e = div();
        e.classList.add("player-list-entry");
        if (lah.currentJudgeId === player.id) {
            e.classList.add("is-judge");
        }
        if (player.id == lah.localPlayerId) {
            e.classList.add("is-you");
        }
        if (player.idle === true) {
            e.classList.add("is-idle");
        }
        if (player.is_bot === true) {
            e.classList.add("is-bot");
        }
        if (player.voted_skip === true) {
            e.classList.add("voted-skip");
        }
        if (lah.pendingPlayers.includes(player.id)) {
            e.classList.add("is-pending");
        }
        if (player.ready_up && lah.readyUpEnabled === true) {
            e.classList.add("is-ready");
        }

        // Create name col
        let colName = make("span");
        colName.classList.add("col-name");

        // Add badge element
        let colNameBadge = make("span");
        colNameBadge.classList.add("badge");
        colName.appendChild(colNameBadge);

        // Set name text
        let nameText = player.name;
        if (player.idle === true) nameText += " (" + getUiString("ui_idle") + ")";
        let nameTextElem = document.createTextNode(nameText);
        colName.appendChild(nameTextElem);

        // Create score col
        let colScore = make("span");
        colScore.classList.add("col-score");
        colScore.innerText = player.score.toString();

        e.appendChild(colName);
        e.appendChild(colScore);

        return e;
    }

    function makeFinalScoreboardElement(player, isWinner) {
        if (!lah.gameResults || !player) return null;

        let trophyInfo = lah.gameResults.trophy_winners.find(w => w.id == player.id);

        let e = div();
        e.classList.add("scoreboard-entry");
        if (isWinner) {
            e.classList.add("winner");
        }
        if (player.id == lah.localPlayerId) {
            e.classList.add("is-you");
        }

        let eNameCol = div();
        eNameCol.classList.add("text", "name");
        eNameCol.setAttribute("data-text", player.name);
        e.appendChild(eNameCol);

        let eScoreCol = div();
        eScoreCol.classList.add("text", "score");
        eScoreCol.setAttribute("data-text", player.score.toString());
        e.appendChild(eScoreCol);

        let eTrophiesCol = div();
        eTrophiesCol.classList.add("trophies", "text");
        eTrophiesCol.setAttribute("data-text", (trophyInfo && trophyInfo.trophies.length) || "0");
        e.appendChild(eTrophiesCol);

        return e;
    }

    function makeTrophyElement(trophyData) {
        let e = div();
        e.classList.add("trophy");
        let trophyName = getLocalString(trophyData.name);
        let trophyDesc = getLocalString(trophyData.desc);
        let trophyId = trophyData.id;
        e.setAttribute("data-trophy-name", trophyName);
        e.setAttribute("data-trophy-desc", trophyDesc);
        e.setAttribute("data-trophy-id", trophyId);
        let eIcon = div();
        eIcon.classList.add("trophy-icon");
        e.appendChild(eIcon);

        return e;
    }

    function clearObject(o) {
        Object.keys(o).forEach(k => delete o[k]);
    }

    // Handlers for server message types
    let responseHandlers = {
        "s_allcards": msg => {
            clearObject(lah.whiteCards);
            clearObject(lah.blackCards);
            clearObject(lah.packMetadata);
            msg.packs.forEach(packData => {
                lah.packMetadata[packData.id] = {
                    id: packData.id,
                    name: packData.name,
                    accent_color: packData.accent_color,
                    accent_background: packData.accent_background,
                    accent_text_decoration: packData.accent_text_decoration,
                    accent_font_weight: packData.accent_font_weight
                };
                packData.cards.forEach(cardData => {
                    cardData.pack = packData.id;
                    let card = Card.fromObject(cardData);
                    if (cardData.id.startsWith("b_")) {
                        lah.blackCards[cardData.id] = card;
                    } else {
                        lah.whiteCards[cardData.id] = card;
                    }
                });
            });
            updatePackStyles();
        },
        "s_gamestate": msg => {
            let roundChanged = lah.round !== msg.round;
            let stageChanged = lah.stage != msg.stage;
            lah.round = msg.round;
            lah.readyUpEnabled = !!msg.ready_up;
            lah.stage = msg.stage;
            lah.pendingPlayers = msg.pending_players;
            lah.currentJudgeId = msg.judge;
            lah.judgeVotedSelf = msg.judge_voted_self;
            lah.roundPlays = msg.plays;
            lah.winningPlayerId = msg.winning_player;
            lah.winningPlayIndex = msg.winning_play;
            lah.isWaitingOnPlayer = msg.pending_players.includes(lah.localPlayerId);
            lah.gameResults = msg.game_results;

            // Update black card if necessary
            updateBlackCard(msg.black_card);
            // Update stage-related stuff
            onStateChanged();
            // Update play area
            updatePlayedCardsArea();

            if (roundChanged) onRoundChanged();
            if (stageChanged) onStageChanged(msg.stage);
            populatePlayerList();
        },
        "s_players": msg => {
            lah.playerList = msg.players;
            for(let p of msg.players) {
                if (p.id == lah.localPlayerId) {
                    if (p.score != lah.score) {
                        lah.score = p.score;
                        onClientScoreChanged();
                    }
                    lah.clientVotedSkip = p.voted_skip === true;
                    gameArea.setClass("lah-voted-skip", lah.clientVotedSkip);
                    let btnSkip = elem(".btn-skip");
                    if (btnSkip) {
                        btnSkip.setAttribute("value", getUiString(lah.clientVotedSkip ? "ui_btn_skip_undo": "ui_btn_skip"));
                    }
                    break;
                }
            }
            onPlayerListChanged();
        },
        "s_clientinfo": msg => {
            lah.localPlayerId = msg.player_id;
            onClientScoreChanged();
            setPlayerName(msg.player_name, true);
            // Set token cookie
            Cookies.set("player_token", msg.player_token.toString().trim());
        },
        "s_hand": msg => {
            lah.playerHand = msg.hand;
            lah.numBlanks = msg.blanks;   
            lah.discards = msg.discards; 
            if (lah.blankCards.length > msg.blanks) {
                lah.blankCards.length = msg.blanks;
            } else if (lah.blankCards.length < msg.blanks) {
                while (lah.blankCards.length < msg.blanks) {
                    lah.blankCards.push("");
                }
            }
            updateHandCardsArea();
            updateHandCardSelection();
        },
        "s_cardsplayed": msg => {
            lah.clientPlayedCards = msg.selection;
            onPlayedCardsChanged();
            onStateChanged();
        },
        "s_rejectclient": msg => {
            lah.lastRejectReason = msg.reason;
            lah.lastRejectDesc = msg.desc;
            console.log("Rejected by server: " + msg.reason);
        },
        "s_auxclientdata": msg => {
            lah.auxPoints = msg.aux_points;
            onAuxDataChanged();
        },
        "s_notify_skipped": msg => {
            showBannerMessage(getUiString("ui_card_skipped_msg"));
        },
        "s_chat_msg": msg => {
            onChatMessageReceived(msg.author, msg.body);
        }
    };

    function getRoundCardFromId(cardId) {
        let customMatch = cardId.match(/^\s*custom_\s*(.*)\s*$/m);
        let card = null;
        if (customMatch) {
            let customCardDecodedText;
            try {
                customCardDecodedText = base64Decode(customMatch[1]);
            } catch (err) {
                console.error("Failed to parse custom card '" + customMatch[1] + "': " + err);
                customCardDecodedText = "ERROR";
            }
            card = new Card("white", cardId, {"en": customCardDecodedText || "???"});
        } else {
            card = lah.whiteCards[cardId];
        }
        return card;
    }

    // Update the play area to contain the right cards according to the game stage
    function updatePlayedCardsArea() {
        playCardsArea.killChildren();
        switch (lah.stage) {
            case STAGE_PLAYING:
                if (!lah.isClientJudge) {
                    for (let cardId of lah.clientPlayedCards) {
                        let card = getRoundCardFromId(cardId);
                        let e = makeCardElement(card);
                        playCardsArea.appendChild(e);
                    }
                }
                break;
            case STAGE_JUDGING:
                {
                    let i = 0;
                    for (let play of lah.roundPlays) {
                        let groupElement = div();
                        groupElement.classList.add("card-group");
                        groupElement.setAttribute("data-play-index", i);
                        let playIndex = i;
                        groupElement.onclick = () => onPlayGroupClicked(playIndex, groupElement);
                        for (let cardId of play) {
                            let cardElement = makeCardElement(getRoundCardFromId(cardId));
                            groupElement.appendChild(cardElement);
                        }
                        playCardsArea.appendChild(groupElement);
                        i++;
                    }
                    break;
                }
            case STAGE_ROUND_END:
                {
                    let i = 0;
                    for (let play of lah.roundPlays) {
                        if (i == lah.winningPlayIndex) {
                            let groupElement = div();
                            groupElement.classList.add("card-group");
                            groupElement.setAttribute("data-play-index", i);
                            groupElement.classList.add("winner");

                            for (let cardId of play) {
                                let cardElement = makeCardElement(getRoundCardFromId(cardId));
                                groupElement.appendChild(cardElement);
                            }
                            playCardsArea.appendChild(groupElement);
                            break;
                        }
                        i++;
                    }
                    break;
                }
        }
    }

    // Repopulates hand cards
    function updateHandCardsArea() {
        handCardsContainer.killChildren();

        // Add all white cards in hand
        for (let cardId of lah.playerHand) {
            let card = lah.whiteCards[cardId];
            let id = cardId;
            if (card) {
                let e = makeCardElement(card, {showHandControls: true});
                e.onclick = () => onHandCardClicked(id, e);
                handCardsContainer.appendChild(e);
            }
        }

        // Add all blank cards in hand
        for(let i = 0; i < lah.numBlanks; i++) {
            let e = makeBlankCardElement(i);
            let blankIndex = i;
            e.onclick = () => onHandCardClicked(null, e, blankIndex);
            handCardsContainer.appendChild(e);
        }
    }

    // Sets the current black card to the card with the specified ID
    function updateBlackCard(cardId) {
        if (lah.currentBlackCard && lah.currentBlackCard.id == cardId) return;
        blackCardArea.killChildren();
        lah.currentBlackCard = lah.blackCards[cardId] || null;
        if (lah.currentBlackCard) {
            let e = makeCardElement(lah.currentBlackCard, {showSkipControls: true});
            blackCardArea.appendChild(e);
        }
    }

    // Raised when a card in the play area is clicked by a judge
    function onPlayGroupClicked(playIndex, groupElement) {
        let canJudge = lah.isClientJudge && lah.stage == STAGE_JUDGING;
        if (!canJudge) return;
        lah.selectedPlayIndex = playIndex;
        onJudgeSelectionChanged();
        updateJudgeCardSelection();
        updateUiState();
    }

    function selectionContainsCard(cardId) {
        if (cardId === undefined || cardId === null) return false;
        return lah.playerHandSelection.find(s => cardId && s.id == cardId) !== undefined;
    }

    function selectionContainsBlank(blankIndex) {
        if (blankIndex === undefined || blankIndex < 0 || blankIndex >= lah.numBlanks) return false;
        return lah.playerHandSelection.find(s => s.blankIndex === blankIndex) !== undefined;
    }

    // Raised when a card in the player's hand is clicked
    function onHandCardClicked(cardId, cardElement, blankCardIndex) {
        // deselecting
        if (selectionContainsCard(cardId) || selectionContainsBlank(blankCardIndex)) {
            let removeIndex = lah.playerHandSelection.indexOfWhere(s => (cardId && s.id === cardId) || s.blankIndex === blankCardIndex);
            if (removeIndex >= 0) lah.playerHandSelection.splice(removeIndex, 1);
        }
        // selecting
        else {
            // Make sure the selection is not too big
            if (lah.playerHandSelection.length >= lah.currentBlackCard.pick) {
                lah.playerHandSelection.splice(0, lah.playerHandSelection.length - lah.currentBlackCard.pick + 1);
            }
            lah.playerHandSelection.push({id: cardId, blankIndex: blankCardIndex});
        }
        onSelectionChanged();
        updateHandCardSelection();
    }

    // Sets the status bar text to a specific string
    function setStatusText(statusText) {
        document.getElementById("status").innerHTML = statusText;
    }

    // Ensures that the selection numbers on the hand card elements are accurate
    function updateHandCardSelection() {
        var cardElements = Array.from(handCardsContainer.children).filter(c => c.tagName.toLowerCase() == "card");
        if (cardElements.length == 0) return;
        for (let el of cardElements) {
            el.removeAttribute("data-selection");
        }

        let selectionDirty = false;
        // Clear out any cards not in the hand
        for(let selectedCard of [...lah.playerHandSelection]) {
            if (selectedCard.blankIndex !== undefined) continue;
            let index = lah.playerHand.indexOf(selectedCard.id);
            if (index < 0) {
                lah.playerHandSelection.splice(index, 1);
                selectionDirty = true;
            }
        }

        if (selectionDirty) onSelectionChanged();

        for (let i = 0; i < lah.playerHandSelection.length; i++) {
            let selection = lah.playerHandSelection[i];
            let el = cardElements.find(e => { 
                let attrBlank = e.getAttribute("data-blank-index");
                let attrCardId = e.getAttribute("data-card");
                return (attrCardId === selection.id) || (attrCardId === "blank" && attrBlank == selection.blankIndex);
            });
            if (el) {
                el.setAttribute("data-selection", i + 1);
            }
        }
    }

    function updateJudgeCardSelection() {
        var groupElements = Array.from(playCardsArea.children).filter(c => c.classList.contains("card-group"));
        if (groupElements.length == 0) return;

        for (let el of groupElements) {
            let attrIndex = el.getAttribute("data-play-index");
            el.setClass("judge-selected", attrIndex == lah.selectedPlayIndex);
        }
    }

    function setPlayerName(name, noSave) {
        if (name == lah.localPlayerName) return;
        lah.localPlayerName = name;
        if (!noSave) Cookies.set("name", name, { expires: 365 });
        onPlayerNameChanged();
    }

    function loadOptions() {
        lah.localPlayerName = Cookies.get("name") || getUiString("ui_default_player_name");
        gameui.loadDisplayPrefs();
        elem("#txt-username").value = lah.localPlayerName;
        elem("#txt-join-username").value = lah.localPlayerName;
        elem("#myname").textContent = lah.localPlayerName;
    }

    // Sends c_clientinfo message to the server
    function sendClientInfo() {
        const requestedName = document.getElementById("txt-username").value;
        sendMessage({
            "msg": "c_updateinfo",
            "userinfo": {
                "name": requestedName
            }
        });
    }

    g.lah.judgeCards = function (playIndex) {
        sendMessage({
            "msg": "c_judgecards",
            "play_index": playIndex
        });
    }

    g.applyOptions = function () {
        setPlayerName(elem("#txt-username").value);
        gameui.setDisplayPrefs({
            "anim_text": elem("#chk-anim-text").checked,
            "accent_color": elem("#txt-accent-color").value,
            "display_notifications": elem("#chk-notifications").checked
        });
        gameui.saveDisplayPrefs();
        hideModal("modal-options");
        sendClientInfo();
    }

    // Sends JSON message to server
    function sendMessage(msg) {
        console.log("Sending " + msg.msg);
        ws.send(msg);
    }

    // Raised when connection closes
    function onConnectionClosed() {
        console.log("disconnected");
        setStatusText(!lah.lastRejectReason ? getUiString("ui_not_connected") : getUiString("ui_disconnected", getUiString("ui_" + lah.lastRejectReason)));
    };

    // Raised when connection opens
    function onConnectionOpened() {
        console.log("connected");
        refreshServerInfo();
    }

    // Raised when the websocket receives a message
    function onDataReceived(data) {
        let json = JSON.parse(data);
        let type = json["msg"];
        // console.log("Received " + type);
        let handler = responseHandlers[type];
        if (handler) {
            handler(json);
        }
    };

    function onPlayerListChanged() {
        elem("#player-count").textContent = lah.playerList.length;
        populatePlayerList();
        updateReadyUpArea();
    }

    function updateReadyUpArea() {
        let readyUpAreaEnabled = lah.readyUpEnabled && lah.stage == STAGE_GAME_STARTING;       
        if (readyUpAreaEnabled) {
            let readyUpVotesLeft = lah.playerList.count(p => !p.is_bot && !p.ready_up);
            let amReady = getPlayer(lah.localPlayerId).ready_up;
            readyUpVotesValue.textContent = readyUpVotesLeft.toString();
            btnReadyUp.value = getUiString(amReady ? "ui_btn_ready_up_voted" : "ui_btn_ready_up");
            btnReadyUp.disabled = amReady;            
        }
        readyUpArea.setVisible(readyUpAreaEnabled);
    }

    // Make sure the correct elements are visible/enabled
    function updateUiState() {
        let handEnabled = lah.isWaitingOnPlayer && !lah.isClientJudge;
        
        let pendingPlayerCount = lah.pendingPlayers.length;

        gameArea.setClass("lah-stage-game-starting", lah.stage == STAGE_GAME_STARTING);
        gameArea.setClass("lah-stage-playing", lah.stage == STAGE_PLAYING);
        gameArea.setClass("lah-stage-judging", lah.stage == STAGE_JUDGING);
        gameArea.setClass("lah-stage-round-end", lah.stage == STAGE_ROUND_END);
        gameArea.setClass("lah-stage-gane-end", lah.stage == STAGE_GAME_END);
        gameArea.setClass("lah-judge", lah.isClientJudge);
        gameEndScreen.setVisible(lah.stage == STAGE_GAME_END);

        switch (lah.stage) {
            case STAGE_GAME_STARTING:
                disable("hand-cards-area");
                disable("btn-play");
                btnPlay.setVisible(false);
                playArea.setVisible(false);
                handArea.setVisible(false);
                blackCardArea.setVisible(false);
                break;
            case STAGE_PLAYING:
                setEnabled("hand-cards-area", handEnabled);
                setEnabled("btn-play", handEnabled);
                blackCardArea.setVisible(true);
                btnPlay.setVisible(lah.isWaitingOnPlayer);
                handArea.setVisible(lah.isWaitingOnPlayer && !lah.isClientJudge);
                playArea.setVisible(!lah.isWaitingOnPlayer && !lah.isClientJudge);
                elem("#pending-players").textContent = lah.pendingPlayers.length.toString();
                if (lah.isClientJudge) {
                    judgeStatusCardText.innerHTML = pendingPlayerCount.toString();
                    if (pendingPlayerCount > 3) {
                        judgeMessageBody.innerHTML = "<span class='highlight'>" + getUiString("ui_you_are_czar") + "</span><br/><smaller>" + getUiString("ui_waiting_for_other_players") + "</smaller>";
                    } else {
                        let strRemainingPlayers = "";
                        for (let i = 0; i < pendingPlayerCount; i++) {
                            if (pendingPlayerCount > 1) {
                                if (i > 0) strRemainingPlayers += pendingPlayerCount > 2 ? getUiString("ui_list_comma") : getUiString("ui_list_space");
                                if (i == pendingPlayerCount - 1) strRemainingPlayers += getUiString("ui_list_and");
                            }
                            let p = getPlayer(lah.pendingPlayers[i]);
                            strRemainingPlayers += (p && p.name) || "???";
                        }
                        judgeMessageBody.innerHTML = "<span class='highlight'>" + getUiString("ui_you_are_czar") + "</span><br/><smaller>" + getUiString("ui_waiting_for_x", strRemainingPlayers) + "</smaller>";
                    }
                }
                break;
            case STAGE_JUDGING:
                disable("hand-cards-area");
                disable("btn-play");
                btnPlay.setVisible(false);
                playArea.setVisible(true);
                handArea.setVisible(false);
                blackCardArea.setVisible(true);
                setEnabled("btn-judge-pick", lah.stage == STAGE_JUDGING && lah.isClientJudge && lah.selectedPlayIndex > -1);
                break;
            case STAGE_ROUND_END:
                disable("hand-cards-area");
                disable("btn-play");
                btnPlay.setVisible(false);
                playArea.setVisible(true);
                handArea.setVisible(false);
                blackCardArea.setVisible(true);
                break;
            case STAGE_GAME_END:
                handArea.setVisible(false);
                playArea.setVisible(false);
                btnPlay.setVisible(false);
                blackCardArea.setVisible(false);
                break;
        }

        handCardsScrollArea.updateScrollTracking();
        playCardsScrollArea.updateScrollTracking();
    }

    function populatePlayerList() {
        playerList.killChildren();
        let rankedPlayers = [...lah.playerList].sort((p1, p2) => p2.score - p1.score);
        for(let player of rankedPlayers) {
            let eEntry = makePlayerListElement(player);
            playerList.appendChild(eEntry);
        }
    }

    function populateGameEndScoreboard() {
        gameEndScoreboardEntries.killChildren();
        gameEndTrophiesList.killChildren();

        if (!lah.gameResults) return;

        let rankedPlayers = [...lah.playerList].sort((p1, p2) => p2.score - p1.score);

        for(let player of rankedPlayers) {
            let isWinner = lah.gameResults.winners.includes(player.id);
            let eEntry = makeFinalScoreboardElement(player, isWinner);
            if (eEntry) gameEndScoreboardEntries.appendChild(eEntry);
        }

        let myTrophies = lah.gameResults.trophy_winners.find(tw => tw.id == lah.localPlayerId);
        if (myTrophies) {
            for(let trophyData of myTrophies.trophies) {
                let eTrophy = makeTrophyElement(trophyData);
                if (eTrophy) gameEndTrophiesList.appendChild(eTrophy);
            }
        }
    }

    function getPlayer(id) {
        return lah.playerList.find(p => p.id == id);
    }

    // Called when game state is updated via s_gamestate
    function onStateChanged() {
        updateJudgeInfo();
        updateUiState();
        updateStatus();
        onSelectionChanged();
        updateReadyUpArea();
    }

    // Called when s_cardsplayed received
    function onPlayedCardsChanged() {
        updatePlayedCardsArea();
    }

    // Updates the lah.isClientJudge flag
    function updateJudgeInfo() {
        if (!lah.isClientJudge) {
            if (lah.currentJudgeId == lah.localPlayerId) {
                lah.isClientJudge = true;
                lah.selectedPlayIndex = -1;
            }
        } else {
            if (lah.currentJudgeId != lah.localPlayerId) {
                lah.isClientJudge = false;
            }
        }        
        let judge = getPlayer(lah.currentJudgeId);
        elem("#czar").textContent = (judge && judge.name) || "--";
    }

    // Sets the status bar text to a string determined by the local game state
    function updateStatus() {
        if (!ws.isOpen) {
            let ckName = Cookies.get("name");
            setStatusText(ckName ? getUiString("ui_hello_x", ckName) : getUiString("ui_hello"));
            return;
        }

        switch (lah.stage) {
            case STAGE_GAME_STARTING:
                setStatusText(getUiString(lah.readyUpEnabled ? "ui_readying_up" : "ui_waiting_for_players"));
                break;
            case STAGE_PLAYING:
                setStatusText(getUiString("ui_round_num", lah.round));
                break;
            case STAGE_JUDGING:
                if (lah.isClientJudge) {
                    setStatusText(getUiString("ui_choose_best_play"));
                } else {
                    let judge = getPlayer(lah.currentJudgeId);
                    setStatusText(getUiString("ui_czar_deciding", (judge && judge.name) || "???"));
                }
                break;
            case STAGE_ROUND_END:
                if (lah.winningPlayerId == lah.localPlayerId) {
                    setStatusText("<span class='highlight'>" + getUiString("ui_you_win_round") + "</span>");
                } else {
                    let winningPlayer = getPlayer(lah.winningPlayerId);
                    if (winningPlayer) {
                        setStatusText(getUiString("ui_x_wins_round", winningPlayer.name));
                    } else {
                        setStatusText(getUiString("ui_nobody_wins_round"));
                    }
                }
                break;
            case STAGE_GAME_END:
                setStatusText(getUiString("ui_game_over"));
                break;
        }
    }

    function onStageChanged(stage) {
        switch (stage) {
            case STAGE_PLAYING:
            {
                if (lah.isClientJudge) {
                    showBannerMessage(getUiString("ui_round_num", lah.round) + "<br/><small>" + getUiString("ui_you_are_czar") + "</small>");
                    gameui.notify(getUiString("ui_game_title"), getUiString("ui_notify_round_start", lah.round) + "\n" + getUiString("ui_you_are_czar"));
                } else {
                    let judge = lah.playerList.find(p => p.id == lah.currentJudgeId);
                    let judgeName = (judge && judge.name) || getUiString("ui_sub_nobody");
                    showBannerMessage(getUiString("ui_round_num", lah.round) + "<br><small>" + getUiString("ui_x_is_czar", judgeName) + "</small>");
                    gameui.notify(getUiString("ui_game_title"), getUiString("ui_notify_round_start", lah.round) + "\n" + getUiString("ui_x_is_czar", judgeName));
                }
                break;
            }
            case STAGE_JUDGING:
            {
                gameui.notify(getUiString("ui_game_title"), getUiString(lah.isClientJudge ? "ui_notify_czar_judging" : "ui_notify_player_judging"));
                break;
            }
            case STAGE_ROUND_END:
            {
                let roundEndMsg;
                if (lah.currentJudgeId == lah.localPlayerId && lah.judgeVotedSelf === true) {
                    roundEndMsg = getUiString("ui_voted_self");
                } else if (lah.winningPlayerId == lah.localPlayerId) {
                    roundEndMsg = getUiString("ui_you_win_round");                    
                } else {
                    let winningPlayer = lah.playerList.find(p => p.id == lah.winningPlayerId);
                    if (winningPlayer) {
                        roundEndMsg = getUiString("ui_x_wins_round", winningPlayer.name);
                    } else {
                        roundEndMsg = getUiString("ui_winner_left_nobody_scores");
                    }
                }
                showBannerMessage(roundEndMsg, 3);
                gameui.notify(getUiString("ui_game_title"), roundEndMsg);
            }
            case STAGE_GAME_END:
            {
                populateGameEndScoreboard();   
            }
        }
    }

    // Raised when the PLAY button is clicked
    function onPlayClicked() {        
        sendMessage({ 
            msg: "c_playcards", 
            cards: lah.playerHandSelection.map(s => {
                if (s.id) {
                    return s.id;
                } else if (s.blankIndex !== undefined && s.blankIndex >= 0 && s.blankIndex < lah.numBlanks) {
                    let customCardId = "custom_" + base64Encode(lah.blankCards[s.blankIndex]);
                    return customCardId;
                } else {
                    return null;
                }
            }) 
        });

        lah.playerHandSelection.length = 0;
        updateHandCardSelection();
    };

    g.lah.upgradeCard = function(cardId) {
        sendMessage({
            msg: "c_upgradecard",
            card_id: cardId
        });
    }

    g.lah.discardCard = function(cardId) {
        sendMessage({
            msg: "c_discardcard",
            card_id: cardId
        });
    }

    g.lah.voteForSkip = function(voteState) {
        sendMessage({
            msg: "c_vote_skip",
            voted: voteState
        });
    }

    g.lah.sendChatMessage = function(msg) {
        sendMessage({
            msg: "c_chat_msg",
            body: msg
        });
    }

    function onChatMessageReceived(author, msg) {
        let msgHtml = mdToHtml(msg);

        if (!msgHtml || msgHtml.trim().length == 0) return;
        let isChatAtBottom = playerChat.scrollHeight - playerChat.scrollTop - playerChat.offsetHeight < 1;

        // Post new chat message
        let el = div();
        el.classList.add("chat-msg");
        let elAuthor = div();
        elAuthor.classList.add("chat-msg-author");
        elAuthor.textContent = author;
        let elBody = div();
        elBody.classList.add("chat-msg-body");
        elBody.innerHTML = msgHtml;
        if (elBody.textContent.trim().length == 0) {
            elBody.textContent = msg;
        }
        el.appendChild(elAuthor);
        el.appendChild(elBody);
        playerChat.appendChild(el);

        // Scroll chat
        if (isChatAtBottom) {
            playerChat.scrollTop = playerChat.scrollHeight;
        }

        // Limit chat messages
        while (playerChat.childNodes.length > MAX_CHAT_MESSAGES) {
            playerChat.removeChild(playerChat.childNodes[0]);
        }
    }

    function updatePackStyles() {
        let elPackStyles = elem("#card-styles");
        let packStyles = elPackStyles.sheet;
        packStyles.clearRules();
        for(let packId of Object.keys(lah.packMetadata)) {
            let pack = lah.packMetadata[packId];
            packStyles.insertRule("card[data-packid=\'" + pack.id + "\'] .ribbon:after {}", 0);
            let rule = packStyles.cssRules[0];
            if (pack.accent_color) rule.style.setProperty("color", pack.accent_color, "important");
            if (pack.accent_background) rule.style.setProperty("background", pack.accent_background, "important");
            if (pack.accent_text_decoration) rule.style.setProperty("text-decoration", pack.accent_text_decoration, "important");
            if (pack.accent_font_weight) rule.style.setProperty("font-weight", pack.accent_font_weight, "important");
        }
    }

    // Raised when the VOTE button is clicked
    function onJudgePickClicked() {
        lah.judgeCards(lah.selectedPlayIndex);
    }

    function onReadyUpClicked() {
        sendMessage({
            msg: "c_ready_up",
            status: true
        });
    }

    function onPlayerNameChanged() {
        elem("#txt-username").value = lah.localPlayerName;
        elem("#myname").textContent = lah.localPlayerName;
    }

    function onAuxDataChanged() {
        elem("#stat-list #coins").textContent = lah.auxPoints.toString();
    }

    function onSelectionChanged() {
        setEnabled("btn-play", (lah.currentBlackCard && lah.playerHandSelection.length == lah.currentBlackCard.pick));
    }

    function onRoundChanged() {

    }

    function onJudgeSelectionChanged() {

    }

    function onClientScoreChanged() {
        elem("#score").textContent = lah.score.toString();
    }

    window.onbeforeunload = function (e) {
        ws.close(1000, "Exiting game");
        return null;
    }

    async function refreshServerInfo() {
        const maxAttempts = 4;
        const retryDelay = 1000;

        for(let i = 0; i < maxAttempts; i++) {
            try {
                let response = await fetch("/gameinfo", {"method": "GET"});
                if (response.ok) {
                    lah.serverInfo = await response.json();
                    onServerInfoReceived();
                    return;
                } else {
                    console.log("Failed to retrieve server info: \"" + response.statusText + "\"");
                }
            } catch (err) {
                console.log("Failed to retrieve server info: \"" + err + "\"");
            }

            await Promise.delay(retryDelay);
        }
        
        onServerUnreachable();
    }

    function onServerUnreachable() {
        elem("#join-screen-server-name").textContent = getUiString("ui_server_unreachable");
    }

    function onServerInfoReceived() {
        let info = lah.serverInfo;
        elem("#game").setClass("chat-disabled", !info.chat_enabled);
        elem("#join-screen-server-name").textContent = info.server_name;
        elem("#join-screen-player-limit .value").textContent = 
            getUiString("ui_join_player_limit", info.min_players, info.current_player_count, info.max_players);
        elem("#join-screen-pack-count .value").textContent = info.pack_info.length;
        elem("#join-screen-white-card-count .value").textContent = info.white_card_count;
        elem("#join-screen-black-card-count .value").textContent = info.black_card_count;

        const mqSep = "\u00a0\u00a0\u2014\u00a0\u00a0";
        const onoff = b => getUiString(b ? "ui_feature_on" : "ui_feature_off");
        const zeroOff = x => x > 0 ? x.toString() : getUiString("ui_feature_off");
        var marqueeText = getUiString("ui_join_mq_goal", info.max_points, info.max_rounds)
        + mqSep + getUiString("ui_join_mq_hand_size", info.hand_size)
        + mqSep + getUiString("ui_join_mq_upgrades", onoff(info.upgrades_enabled))
        + mqSep + getUiString("ui_join_mq_bot_count", zeroOff(info.bot_count))
        + mqSep + getUiString("ui_join_mq_bot_czars", onoff(info.bot_czars))
        + mqSep + (info.perma_czar ? getUiString("ui_join_mq_perma_czar") : getUiString("ui_join_mq_winner_czar", onoff(info.winner_czar)))
        + mqSep + getUiString("ui_join_mq_blanks", zeroOff(info.blank_cards))
        + mqSep + getUiString("ui_join_mq_discards", zeroOff(info.discards))
        + mqSep + getUiString("ui_join_mq_allow_skips", onoff(info.allow_skips))
        + mqSep + getUiString("ui_join_mq_chat", onoff(info.chat_enabled));
        elem("#join-marquee").setAttribute("data-marquee-text", marqueeText);
    }

    function connectToGame(path, isSpectator) {
        setPlayerName(elem("#txt-join-username").value);
        Cookies.set("cookie_consent", 1, { expires: 365 });
        Cookies.set("game_password", elem("#txt-join-password").value);
        Cookies.set("client_lang", navigator.language || DEFAULT_LOCALE);
        lah.spectating = !!isSpectator;  
        gameArea.setClass("lah-spectating", !!isSpectator);   
        ws.url = WS_PROTOCOL + DOMAIN + ":" + (lah.serverInfo.game_port || WS_DEFAULT_PORT) + path;
        ws.connect();
        hideModal("modal-join");
    }

    function onSpectateGameClicked() {                   
        connectToGame("/spectate", true);
    }

    function onPlayGameClicked() {
        connectToGame("/play");
    }

    async function loadGame() {
        // Load string resources
        try {
            await loadStringResources("/etc/strings.json");
        } catch (reason) {
            console.error("Failed to load game resources: " + reason);
        }

        // Check if user has already agreed to cookies
        if (Cookies.get("cookie_consent")) {
            loadOptions();
        } else {
            elem("#cookie-notice").setClass("hidden", false);
        }

        // Fetch server info
        refreshServerInfo();

        updateStatus();
        updateUiState();

        // Set events
        btnPlay.onclick = onPlayClicked;
        btnPick.onclick = onJudgePickClicked;
        btnReadyUp.onclick = onReadyUpClicked;

        elem("#btn-play-game").onclick = onPlayGameClicked;
        elem("#btn-spectate-game").onclick = onSpectateGameClicked;

        if ("WebSocket" in window) {
            ws.onclose = onConnectionClosed;
            ws.onopen = onConnectionOpened;
            ws.onmessage = onDataReceived;
        } else {
            showModal("modal-no-ws");
        }
    }

    loadGame();
})(this);