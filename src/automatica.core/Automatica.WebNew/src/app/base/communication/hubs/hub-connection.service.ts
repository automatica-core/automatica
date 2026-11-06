import { Injectable, NgZone } from "@angular/core";
import { Router } from "@angular/router";
import { Capacitor } from "@capacitor/core";
import { App } from "@capacitor/app";
import { DataHubService } from "./data-hub.service";
import { UpdateHubService } from "./update-hub.service";
import { TelegramHubService } from "./telegram-monitor-hub.service";
import { LogHubService } from "./log-hub.service";
import { BaseHub, isUnauthorizedHubError } from "../base-hub";
import { LoginService } from "src/app/services/login.service";

@Injectable()
export class HubConnectionService {

    private _isInitializing = false;
    private _retryTimer: any;
    private _handlingUnauthorized = false;
    private _lastResume = 0;

    constructor(private dataHubService: DataHubService,
        private telegramMonitorService: TelegramHubService,
        private updateHubService: UpdateHubService,
        private loggingHubService: LogHubService,
        private loginService: LoginService,
        private router: Router,
        private ngZone: NgZone) {

        for (const hub of this.hubs) {
            hub.unauthorized.subscribe(() => this.onUnauthorized());
        }

        if (Capacitor.isNativePlatform()) {
            // the os suspends the app in the background, the socket and the values are stale when it comes back
            App.addListener("appStateChange", (state) => {
                if (state.isActive) {
                    this.ngZone.run(() => this.onResume());
                }
            }).catch((error) => console.error(error));
            App.addListener("resume", () => this.ngZone.run(() => this.onResume()))
                .catch((error) => console.error(error));
        }
    }

    private get hubs(): BaseHub[] {
        return [this.dataHubService, this.telegramMonitorService, this.updateHubService, this.loggingHubService];
    }

    async init() {

        if (this._isInitializing) {
            return;
        }
        this._isInitializing = true;
        clearTimeout(this._retryTimer);
        this._retryTimer = undefined;

        try {
            // start() is a no-op for a hub that is connected to the current server with the current token,
            // and rebuilds the connection if the server or the token changed
            for (const hub of this.hubs) {
                await hub.start();
            }
        } catch (error) {
            console.error(error);
            // a rejected token is handled by onUnauthorized, retrying would never succeed
            if (!isUnauthorizedHubError(error) && localStorage.getItem("jwt")) {
                // a failed first start never raises onclose, so retry from here
                this._retryTimer = setTimeout(() => this.init(), this.dataHubService.reconnectTime);
            }
        } finally {
            this._isInitializing = false;
        }
    }

    async stop() {
        clearTimeout(this._retryTimer);
        this._retryTimer = undefined;
        for (const hub of this.hubs) {
            await hub.stop();
        }
    }

    private async onUnauthorized() {
        if (this._handlingUnauthorized) {
            return;
        }
        this._handlingUnauthorized = true;

        try {
            console.warn("signalr: token rejected by the server, back to the login");
            await this.stop();
            this.loginService.logout();
            await this.router.navigate(["/login"]);
        } catch (error) {
            console.error(error);
        } finally {
            this._handlingUnauthorized = false;
        }
    }

    private async onResume() {
        // appStateChange and resume both fire when the app comes back
        const now = Date.now();
        if (now - this._lastResume < 1000) {
            return;
        }
        this._lastResume = now;

        if (!localStorage.getItem("jwt")) {
            return;
        }

        if (this.dataHubService.connected) {
            // the socket may have survived, but values that changed meanwhile were never received
            await this.dataHubService.refreshValues();
        }
        // reconnects hubs whose socket died in the background (onConnected refreshes the values then)
        await this.init();
    }
}
