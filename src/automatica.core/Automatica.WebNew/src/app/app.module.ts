import { BrowserModule } from "@angular/platform-browser";
import { NgModule, APP_INITIALIZER, Injectable } from "@angular/core";

import { AppComponent } from "./app.component";

import { AppRoutingModule } from "./app-routing.module";
import { RouterModule } from "@angular/router";
import { L10nTranslationModule, L10nLoader, L10nValidationModule } from "angular-l10n";
import { TranslationConfigService, HttpTranslationLoader, LocalLanguageConfigration } from "./services/translation-config.service";
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
import { MobileModule } from "./visualization/mobile/mobile.module";
import { HasRoleGuard } from "./services/login.service";
import { CommonModule } from "@angular/common";
import { StartingOverlayModule } from "./shared/starting-overlay/starting-overlay.module";

import { DeviceDetectorService  } from "ngx-device-detector";
import { DeviceService } from "./services/device/device.service";
import { ThemeService } from "./services/theme.service";
import { L10nLazyResolver } from "./services/l10n-lazy-resolver";
import { CacheModule } from "ionic-cache";  
import { IonicStorageModule } from "@ionic/storage-angular";
import { Drivers } from '@ionic/storage';

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
    L10nTranslationModule.forRoot(LocalLanguageConfigration, {
      translationLoader: HttpTranslationLoader
    }),
    L10nValidationModule.forRoot(),
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
    DxHtmlEditorModule,
    IonicStorageModule.forRoot({
      name: 'localStorage',
      driverOrder: [Drivers.LocalStorage]
    }),
    CacheModule.forRoot({keyPrefix: "cache"})
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
    ThemeService,
    L10nLazyResolver
  ],
  bootstrap: [
    AppComponent
  ],
  declarations: [
    AppComponent
  ]
})
export class AppModule {
  constructor(translate: TranslationConfigService) {
    translate.init();
  }
}
