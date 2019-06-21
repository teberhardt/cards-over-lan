((g) => {
    "use strict";
    const DEFAULT_LOCALE = "en";

    let uiStrings = null;

    let fmt = function (format) {
        var args = Array.prototype.slice.call(arguments, 1);
        return format.replace(/{(\d+)}/g, function (match, number) {
            return typeof args[number] != "undefined"
                ? args[number]
                : match;
        })
    };

    g.getUiString = function (key) {
        let entry = uiStrings[key];
        if (!entry) return key;
        if (typeof entry === "string") return entry;
        let lang = navigator.language;
        let fmtStr = entry[lang] || entry[lang.replace(/(.*)-[a-z0-9_\-]+/i, (m, p) => p)] || entry[DEFAULT_LOCALE] || Object.values(entry)[0];
        if (!fmtStr) return key;
        return fmt(fmtStr, ...(Array.prototype.slice.call(arguments, 1) || []));
    }

    function localizeElements() {
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
    }

    g.loadStringResources = function(url) {
        return new Promise(function(resolve, reject) {
            fetch(url, {"method": "GET"})
            .then(response => {
                return response.json();
            })
            .then(json => {
                uiStrings = json;
                localizeElements();
                resolve();
            })
            .catch(error => {
                reject(error);
            });
        });
    }
})(this);