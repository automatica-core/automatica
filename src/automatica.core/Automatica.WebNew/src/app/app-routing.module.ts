import { NgModule } from "@angular/core";
import { Routes, RouterModule } from "@angular/router";
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
import { AdminComponent } from "./admin/admin.component";
import { LoginFormComponent } from "./shared/components/login-form/login-form.component";
import { AreasEtsImportComponent } from "./pages/area-config/areas-ets-import/areas-ets-import.component";
import { VisualizationComponent } from "./visualization/visualization.component";
import { Role } from "./base/model/user/role";
import { SatelliteConfigComponent } from "./pages/satellite-config/satellite-config.component";
import { NodeInstanceImportComponent } from "./shared/propertyeditor/node-instance-ets-import/node-instance-import.component";
import { LogsComponent } from "./pages/logs/logs.component";

const routes: Routes = [
    {
        path: "",
        redirectTo: "visualization",
        pathMatch: "full"
    },
    {
        path: "admin", component: AdminComponent, children: [
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
            }, {
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
            }]
    },
    {
        path: "visualization", component: VisualizationComponent, children: [
            {
                path: "",
                redirectTo: "page",
                pathMatch: "full"
            }, {
                path: "page", loadChildren: () => import("./visualization/mobile/mobile-view.module").then(m => m.MobileViewModule)
            }
        ],
    },
    {
        path: "login", component: LoginFormComponent
    }];

@NgModule({
    imports: [RouterModule.forRoot(routes)],
    exports: [RouterModule]
})
export class AppRoutingModule { }
