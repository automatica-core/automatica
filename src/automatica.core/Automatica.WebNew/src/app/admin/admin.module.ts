import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { AdminComponent } from "./admin.component";
import { SideNavOuterToolbarModule, SideNavInnerToolbarModule } from "../layouts";
import { FooterModule } from "../shared/components/footer/footer.component";
import { RouterModule } from "@angular/router";
import { LogsComponent } from "../pages/logs/logs.component";
import { AppComponent } from "../app.component";
import { AreaConfigComponent } from "../pages/area-config/area-config.component";
import { AreasEtsImportComponent } from "../pages/area-config/areas-ets-import/areas-ets-import.component";
import { CategoryConfigComponent } from "../pages/category-config/category-config.component";
import { ConfigComponent } from "../pages/config/config.component";
import { HomeComponent } from "../pages/home/home.component";
import { LicenseComponent } from "../pages/license/license.component";
import { LogicEditorComponent } from "../pages/logic-editor/logic-editor.component";
import { PluginsComponent } from "../pages/plugins/plugins.component";
import { SatelliteConfigComponent } from "../pages/satellite-config/satellite-config.component";
import { SystemComponent } from "../pages/system/system.component";
import { TelegramMonitorComponent } from "../pages/telegram-monitor/telegram-monitor.component";
import { UserConfigComponent } from "../pages/user-config/user-config.component";
import { UsergroupConfigComponent } from "../pages/usergroup-config/usergroup-config.component";
import { VisualisationEditComponent } from "../pages/visualisation-edit/visualisation-edit.component";
import { L10nTranslationModule } from "angular-l10n";
import { AngularSplitModule } from "angular-split";
import { DxLoadPanelModule, DxTabPanelModule, DxCheckBoxModule, DxTemplateModule, DxMenuModule, DxTreeListModule, DxPopupModule, DxFileUploaderModule, DxButtonModule, DxDataGridModule, DxColorBoxModule, DxTextAreaModule, DxProgressBarModule, DxTextBoxModule, DxScrollViewModule, DxListModule, DxHtmlEditorModule } from "devextreme-angular";
import { DndModule } from "p3root-angular-dnd";
import { StartingOverlayModule } from "../shared/starting-overlay/starting-overlay.module";
import { MobileModule } from "../visualization/mobile/mobile.module";
import { VisualizationModule } from "../visualization/visualization.module";
import { FontAwesomeModule } from "@fortawesome/angular-fontawesome";
import { ServicesModule } from "../services/services.module";
import { SharedModule } from "../shared/shared.module";
import { AdminRoutingModule } from "./admin-routing.module";

@NgModule({
  declarations: [
    AdminComponent,
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
    LogsComponent],
  imports: [
    AdminRoutingModule,
    CommonModule,
    SideNavOuterToolbarModule,
    SideNavInnerToolbarModule,
    FooterModule,
    RouterModule,
    L10nTranslationModule,
    DxLoadPanelModule,
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
    FontAwesomeModule,
    ServicesModule,
    SharedModule
  ],
  exports: [
    AdminComponent
  ]
})
export class AdminModule { }
