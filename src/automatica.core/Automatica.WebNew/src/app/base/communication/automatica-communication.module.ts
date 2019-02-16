import { NgModule } from "@angular/core";
import { DataHubService } from "./hubs/data-hub.service";
import { TelegramHubService } from "./hubs/telegram-monitor-hub.service";
import { UpdateHubService } from "./hubs/update-hub.service";
import { HubConnectionService } from "./hubs/hub-connection.service";

@NgModule({
  imports: [
  ],
  declarations: [],
  exports: [],
  providers: [
    DataHubService,
    TelegramHubService,
    UpdateHubService,
    HubConnectionService
  ]
})
export class AutomaticaCommunicationModule { }
