((g) => {
    "use strict";
    const TIME_A = 1;
    const TIME_B = 1;
    const MAX_RETRY_TIME = 30000;
    const clamp = (a, min, max) => a < min ? min : a > max ? max : a;
    const timeoutFuncs = {
        "linear": (a, b) => clamp(b + 1, 0, MAX_RETRY_TIME),
        "fibonacci": (a, b) => clamp(a + b, 0, MAX_RETRY_TIME)
    }
    const defaultTimeoutFunc = timeoutFuncs["fibonacci"];

    g.SuperiorWebSocket = class {
        constructor(url, retryKind) {
            this._url = url;
            this._retryFunc = timeoutFuncs[retryKind] || defaultTimeoutFunc;
            this._userOnOpen = null;
            this._userOnClose = null;
            this._userOnMessage = null;
            this._sendQueue = [];
            this._isOpen = false;
            this._hasError = false;
            this._retryTimer = null;
            this._retryTimeA = TIME_A;
            this._retryTimeB = TIME_B;
            this._manualClose = false;
        }
    
        _onOpen() {
            this._isOpen = true;
            this._hasError = false;
            this._stopRetry();
            if (this._userOnOpen) this._userOnOpen();
        };
    
        _onClose(e) {
            this._isOpen = false;
            this._ws = null;
            if (this._userOnClose) this._userOnClose();
    
            if (this._manualClose !== true && e.code !== 1000 && e.code !== 1001) {
                this._startRetry();
            }
        }
    
        _onMessage(msg) {
            if (this._userOnMessage) this._userOnMessage(msg.data);
        }
    
        _onError(error) {
            this._hasError = true;
        }
    
        _createWebsocket() {
            let cl = this;
            this._ws = new WebSocket(this._url);
            this._ws.onerror = (error) => cl._onError(error);
            this._ws.onopen = () => cl._onOpen();
            this._ws.onclose = (e) => cl._onClose(e);
            this._ws.onmessage = (msg) => cl._onMessage(msg);
        }
    
        _startRetry() {
            this._stopRetry();
            let time = this._retryTimeB;
            console.log("Retrying connection in " + time + "s");
            this._retryTimer = setTimeout(() => {
                let nextTime = this._retryFunc(this._retryTimeA, this._retryTimeB);
                this._retryTimeA = this._retryTimeB;
                this._retryTimeB = nextTime;
                this._retry();
            }, time * 1000);
        }
    
        _stopRetry() {
            if (!this._retryTimer) return;
            clearTimeout(this._retryToken);
            this._retryTimer = null;
        }
    
        _retry() {
            console.log("Retrying connection...");
            this.connect();
        }

        get url() {
            return this._url;
        }

        set url(value) {
            this._url = value;
        }
    
        get isOpen() {
            return this._ws && this._isOpen && this._ws.readyState == WebSocket.OPEN;
        }
    
        get onopen() {
            return this._userOnOpen;
        }
    
        set onopen(handler) {
            this._userOnOpen = handler;
        }
    
        get onclose() {
            return this._userOnClose;
        }
    
        set onclose(handler) {
            this._userOnClose = handler;        
        }
    
        get onmessage() {
            return this._userOnMessage;
        }
    
        set onmessage(handler) {
            this._userOnMessage = handler;
        }
    
        connect() {
            this._manualClose = false;
            this._createWebsocket();
        }
    
        send(msg) {
            if (!this.isOpen) return;
            if (msg === Object(msg)) {
                this._ws.send(JSON.stringify(msg));
            } else if (typeof msg === "string") {
                this._ws.send(msg);
            }
        }
    
        close(code, reason) {
            if (this.isOpen) {
                this._manualClose = true;
                this._ws.close(code, reason);
            }
        }
    }
})(this);