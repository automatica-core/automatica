import { Injectable } from "@angular/core";
import { DataHubService } from "./data-hub.service";
import { UpdateHubService } from "./update-hub.service";
import { TelegramHubService } from "./telegram-monitor-hub.service";
import { LogHubService } from "./log-hub.service";
import { HubConnectionState } from "@microsoft/signalr";

@Injectable()
export class HubConnectionService {

private _isInitializing = false;
private _retryTimer: any;

    constructor(private dataHubService: DataHubService, private telegramMonitorService: TelegramHubService, private updateHubService: UpdateHubService, private loggingHubService: LogHubService) {


    }

    async init() {

        if(this._isInitializing) {
            return;
        }
        this._isInitializing = true;
        clearTimeout(this._retryTimer);

        try {
            if (this.dataHubService?.Connection?.state !== HubConnectionState.Connected) {
                await this.dataHubService.start();
            }
            if (this.telegramMonitorService?.Connection?.state !== HubConnectionState.Connected) {
                await this.telegramMonitorService.start();
            }
            if (this.updateHubService?.Connection?.state !== HubConnectionState.Connected) {
                await this.updateHubService.start();
            }
            if (this.loggingHubService?.Connection?.state !== HubConnectionState.Connected) {
                await this.loggingHubService.start();
            }
        } catch (error) {
            console.error(error);
            // a failed first start never raises onclose, so retry from here
            this._retryTimer = setTimeout(() => this.init(), this.dataHubService.reconnectTime);
        } finally {
            this._isInitializing = false;
        }
    }

    async stop() {
        clearTimeout(this._retryTimer);
        this._retryTimer = undefined;
        await this.dataHubService.stop();
        await this.telegramMonitorService.stop();
        await this.updateHubService.stop();
        await this.loggingHubService.stop();
    }
}
