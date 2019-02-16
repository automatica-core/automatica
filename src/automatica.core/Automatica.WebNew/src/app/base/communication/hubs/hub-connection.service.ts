import { Injectable } from "@angular/core";
import { DataHubService } from "./data-hub.service";
import { UpdateHubService } from "./update-hub.service";
import { TelegramHubService } from "./telegram-monitor-hub.service";

@Injectable()
export class HubConnectionService {

    constructor(private dataHubService: DataHubService, private telegramMonitorService: TelegramHubService, private updateHubService: UpdateHubService) {


    }

    async init() {
        await this.dataHubService.start();
        await this.telegramMonitorService.start();
        await this.updateHubService.start();
    }

    async stop() {
        await this.dataHubService.stop();
        await this.telegramMonitorService.stop();
        await this.updateHubService.stop();
    }
}
