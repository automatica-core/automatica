import { NgModule } from "@angular/core";
import { DataHubService } from "./hubs/data-hub.service";
import { TelegramHubService } from "./hubs/telegram-monitor-hub.service";
import { UpdateHubService } from "./hubs/update-hub.service";
import { HubConnectionService } from "./hubs/hub-connection.service";
import { LogHubService } from "./hubs/log-hub.service";

@NgModule({
  imports: [
  ],
  declarations: [],
  exports: [],
  providers: [
    DataHubService,
    TelegramHubService,
    UpdateHubService,
    HubConnectionService,
    LogHubService
  ]
})
export class AutomaticaCommunicationModule { }
