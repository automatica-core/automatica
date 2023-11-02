import { RouterModule, Routes } from "@angular/router";
import { HomeComponent } from "../pages/home/home.component";
import { ConfigComponent } from "../pages/config/config.component";
import { LogicEditorComponent } from "../pages/logic-editor/logic-editor.component";
import { NgModule } from "@angular/core";
import { Role } from "../base/model/user/role";
import { AreaConfigComponent } from "../pages/area-config/area-config.component";
import { AreasEtsImportComponent } from "../pages/area-config/areas-ets-import/areas-ets-import.component";
import { CategoryConfigComponent } from "../pages/category-config/category-config.component";
import { LicenseComponent } from "../pages/license/license.component";
import { LogsComponent } from "../pages/logs/logs.component";
import { PluginsComponent } from "../pages/plugins/plugins.component";
import { SatelliteConfigComponent } from "../pages/satellite-config/satellite-config.component";
import { SystemComponent } from "../pages/system/system.component";
import { TelegramMonitorComponent } from "../pages/telegram-monitor/telegram-monitor.component";
import { UserConfigComponent } from "../pages/user-config/user-config.component";
import { UsergroupConfigComponent } from "../pages/usergroup-config/usergroup-config.component";
import { VisualisationEditComponent } from "../pages/visualisation-edit/visualisation-edit.component";

const routes: Routes =[
    {
        path: "",
        redirectTo: "home",
        pathMatch: "full"
    }, {
        path: "home",
        component: HomeComponent
    }, {
        path: "config",
        component: ConfigComponent
    }, 
    {
        path: "logic-editor", redirectTo: "logic-editor/default", pathMatch: "full"
    },
    {
        path: "logic-editor/:id",
        component: LogicEditorComponent
    }, {
        path: "visualisation",
        component: VisualisationEditComponent,
        data: {
            editable: true,
            requiresRole: Role.VISU_ROLE
        }
    }, {
        path: "area-config",
        component: AreaConfigComponent, children: [
            {
                path: ":id/import-ets",
                component: AreasEtsImportComponent
            }
        ]
    }, {
        path: "category-config",
        component: CategoryConfigComponent
    }, {
        path: "user-config",
        component: UserConfigComponent
    }, {
        path: "usergroup-config",
        component: UsergroupConfigComponent
    }, {
        path: "telegram-monitor",
        component: TelegramMonitorComponent
    }, {
        path: "license",
        component: LicenseComponent
    }, {
        path: "system",
        component: SystemComponent
    }, {
        path: "plugins",
        component: PluginsComponent
    }, {
        path: "satellites",
        component: SatelliteConfigComponent
    }, {
        path: "logs",
        component: LogsComponent
    }];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule]
})
export class AdminRoutingModule { }
