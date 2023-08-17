import { BrowserModule } from "@angular/platform-browser";
import { NgModule, APP_INITIALIZER, Injectable } from "@angular/core";

import { AppComponent } from "./app.component";

import { AppRoutingModule } from "./app-routing.module";
import { RouterModule } from "@angular/router";
import { L10nTranslationModule, L10nLoader, L10nValidationModule } from "angular-l10n";
import { TranslationConfigService, TranslationConfiguration, HttpTranslationLoader } from "./services/translation-config.service";
import { HomeComponent } from "./pages/home/home.component";
import { ConfigComponent } from "./pages/config/config.component";
import { LogicEditorComponent } from "./pages/logic-editor/logic-editor.component";
import { VisualisationEditComponent } from "./pages/visualisation-edit/visualisation-edit.component";
import { AreaConfigComponent } from "./pages/area-config/area-config.component";
import { CategoryConfigComponent } from "./pages/category-config/category-config.component";
import { UserConfigComponent } from "./pages/user-config/user-config.component";
import { UsergroupConfigComponent } from "./pages/usergroup-config/usergroup-config.component";
import { TelegramMonitorComponent } from "./pages/telegram-monitor/telegram-monitor.component";
import { LicenseComponent } from "./pages/license/license.component";
import { SystemComponent } from "./pages/system/system.component";
import { PluginsComponent } from "./pages/plugins/plugins.component";
import { AdminModule } from "./admin/admin.module";
import { VisualizationModule } from "./visualization/visualization.module";
import { HttpClientModule } from "@angular/common/http";
import { LoginFormModule } from "./shared/components/login-form/login-form.module";
import { FontAwesomeModule } from "@fortawesome/angular-fontawesome";
import { ServicesModule } from "./services/services.module";
import { SharedModule } from "./shared/shared.module";
import { DxLoadPanelModule, DxTabPanelModule, DxCheckBoxModule, DxTemplateModule, DxMenuModule, DxTreeListModule, DxPopupModule, DxFileUploaderModule, DxButtonModule, DxDataGridModule, DxColorBoxModule, DxTextAreaModule, DxProgressBarModule, DxScrollViewModule, DxListModule, DxTextBoxModule, DxHtmlEditorModule } from "devextreme-angular";
import { AutomaticaCommunicationModule } from "./base/communication/automatica-communication.module";
import { AngularSplitModule } from "angular-split";
import { DndModule, DragDropConfig, DataTransferEffect, DragImage } from "p3root-angular-dnd";
import { AreasEtsImportComponent } from "./pages/area-config/areas-ets-import/areas-ets-import.component";
import { MobileModule } from "./visualization/mobile/mobile.module";
import { HasRoleGuard } from "./services/login.service";
import { CommonModule } from "@angular/common";
import { StartingOverlayModule } from "./shared/starting-overlay/starting-overlay.module";

import { DeviceDetectorService  } from "ngx-device-detector";
import { DeviceService } from "./services/device/device.service";
import { ThemeService } from "./services/theme.service";
import { SatelliteConfigComponent } from "./pages/satellite-config/satellite-config.component";
import { LogsComponent } from "./pages/logs/logs.component";

@Injectable()
export class CustomDragDropConfig extends DragDropConfig {
  public onDragStartClass: string = "dnd-drag-start";
  public onDragEnterClass: string = "dnd-drag-enter";
  public onDragOverClass: string = "dnd-drag-over";
  public onSortableDragClass: string = "dnd-sortable-drag";

  public dragEffect: DataTransferEffect = DataTransferEffect.MOVE;
  public dropEffect: DataTransferEffect = DataTransferEffect.MOVE;
  public dragCursor: string = "move";
  public dragImage: DragImage;
  public defaultCursor: string = "";
}

export function initL10n(l10nLoader: L10nLoader): () => Promise<void> {
  return (): Promise<void> => l10nLoader.init();
}

@NgModule({
  imports: [
    CommonModule,
    RouterModule,
    HttpClientModule,
    BrowserModule,
    AppRoutingModule,
    L10nTranslationModule.forRoot(TranslationConfiguration, {
      translationLoader: HttpTranslationLoader
    }),
    L10nValidationModule.forRoot(),
    AdminModule,
    LoginFormModule,
    FontAwesomeModule,
    ServicesModule,
    SharedModule,
    DxLoadPanelModule,
    AutomaticaCommunicationModule,
    AngularSplitModule,
    DndModule.forRoot(),
    DxTabPanelModule,
    DxCheckBoxModule,
    DxTemplateModule,
    DxMenuModule,
    DxTreeListModule,
    DxPopupModule,
    DxFileUploaderModule,
    DxButtonModule,
    DxDataGridModule,
    DxColorBoxModule,
    DxTextAreaModule,
    DxProgressBarModule,
    DxTextBoxModule,
    VisualizationModule,
    DxScrollViewModule,
    MobileModule,
    StartingOverlayModule,
    DxListModule,
    DxTemplateModule,
    DxHtmlEditorModule
  ],
  providers: [
    {
      provide: APP_INITIALIZER,
      useFactory: initL10n,
      deps: [L10nLoader],
      multi: true
    },
    TranslationConfigService,
    CustomDragDropConfig,
    { provide: DragDropConfig, useValue: new CustomDragDropConfig() },
    HasRoleGuard,
    DeviceService,
    HttpTranslationLoader,
    {
      provide: DeviceDetectorService,
      useClass: DeviceDetectorService
    },
    ThemeService
  ],
  bootstrap: [
    AppComponent
  ],
  declarations: [
    AppComponent,
    HomeComponent,
    ConfigComponent,
    LogicEditorComponent,
    VisualisationEditComponent,
    AreaConfigComponent,
    CategoryConfigComponent,
    UserConfigComponent,
    UsergroupConfigComponent,
    TelegramMonitorComponent,
    LicenseComponent,
    SystemComponent,
    PluginsComponent,
    AreasEtsImportComponent,
    SatelliteConfigComponent,
    LogsComponent
  ]
})
export class AppModule {
  constructor(translate: TranslationConfigService) {
    translate.init();
  }
}
