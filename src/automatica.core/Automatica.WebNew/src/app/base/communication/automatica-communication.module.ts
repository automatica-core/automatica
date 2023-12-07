import { ModuleWithProviders, NgModule } from "@angular/core";
import { DataHubService } from "./hubs/data-hub.service";
import { TelegramHubService } from "./hubs/telegram-monitor-hub.service";
import { UpdateHubService } from "./hubs/update-hub.service";
import { HubConnectionService } from "./hubs/hub-connection.service";
import { LogHubService } from "./hubs/log-hub.service";

@NgModule({
 
})
export class AutomaticaCommunicationModule {
  static forRoot(): ModuleWithProviders<AutomaticaCommunicationModule> {
    return {
      ngModule: AutomaticaCommunicationModule,
      providers: [
        DataHubService,
        TelegramHubService,
        UpdateHubService,
        HubConnectionService,
        LogHubService
      ]
    };
  }
}
