import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";

import { ConfigService } from "./config.service"
import { RuleEngineService } from "./ruleengine.service"
import { PingService } from "./ping.service";
import { DataService } from "./data.service";
import { DesignTimeDataService } from "./design-time-data.service";
import { CategoryService } from "./categories.service";
import { AreaService } from "./areas.service";
import { LoginService } from "./login.service";
import { NotifyService } from "./notify.service";
import { RuleInstanceVisuService } from "./rule-visu.service";
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

@NgModule({
  imports: [
    CommonModule
  ],
  declarations: [],
  providers: [
    AreaService,
    CategoryService,
    ConfigService,
    DataService,
    DesignTimeDataService,
    LoginService,
    NotifyService,
    PingService,
    RuleInstanceVisuService,
    RuleEngineService,
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
    AppService

  ]
})
export class ServicesModule {
  constructor(model: ModelDecoratorService) {

  }
}
