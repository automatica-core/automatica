import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";

import { ConfigService } from "./config.service"
import { LogicEngineService } from "./logicengine.service"
import { PingService } from "./ping.service";
import { DataService } from "./data.service";
import { DesignTimeDataService } from "./design-time-data.service";
import { CategoryService } from "./categories.service";
import { AreaService } from "./areas.service";
import { LoginService } from "./login.service";
import { NotifyService } from "./notify.service";
import { LogicInstanceVisuService } from "./logic-visu.service";
import { ServerStateService } from "./server-state.service";
import { SettingsService } from "./settings.service";
import { ModelDecoratorService } from "./model-decorator.service";
import { VisuService } from "./visu.service";
import { GroupsService } from "./groups.service";
import { UsersService } from "./users.service";
import { TelegramMonitorService } from "./telegram-monitor.service";
import { LicenseService } from "./license.service";
import { SystemService } from "./system.service";
import { PluginsService } from "./plugins.service";
import { AppService } from "./app.service";
import { SatelliteService } from "./satellite.services";
import { NodeInstanceService } from "./node-instance.service";
import { NodeTemplateService } from "./node-template.service";
import { LogsService } from "./logs.service";
import { HyperSeriesService } from "./hyperseries.service";
import { ControlsService } from "./controls.service";

@NgModule({
  imports: [
    CommonModule
  ],
  declarations: [],
  providers: [
    AreaService,
    CategoryService,
    ConfigService,
    NodeInstanceService,
    DataService,
    DesignTimeDataService,
    LoginService,
    NotifyService,
    PingService,
    LogicInstanceVisuService,
    LogicEngineService,
    ServerStateService,
    SettingsService,
    ModelDecoratorService,
    VisuService,
    GroupsService,
    UsersService,
    TelegramMonitorService,
    LicenseService,
    SystemService,
    PluginsService,
    SatelliteService,
    NodeTemplateService,
    LogsService,
    HyperSeriesService,
    ControlsService

  ]
})
export class ServicesModule {
  constructor(model: ModelDecoratorService) {

  }
}
