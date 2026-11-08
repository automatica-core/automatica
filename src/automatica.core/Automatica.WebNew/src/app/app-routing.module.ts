import { NgModule } from "@angular/core";
import { Routes, RouterModule } from "@angular/router";
import { LoginFormComponent } from "./shared/components/login-form/login-form.component";
import { VisualizationComponent } from "./visualization/visualization.component";
import { AdminComponent } from "./admin/admin.component";
import { L10nLazyResolver } from "./services/l10n-lazy-resolver";
import { HasRoleGuard } from "./services/login.service";

const routes: Routes = [
    {
        path: "",
        redirectTo: "visualization",
        pathMatch: "full"
    },
    {
        path: "admin",
        component: AdminComponent,
        loadChildren: () => import("./admin/admin.module").then(m => m.AdminModule),
        // every admin page needs a login and the administrator role (or the role in its route data)
        canActivateChild: [HasRoleGuard],
        resolve: { l10n: L10nLazyResolver },
        data: {
            l10nProviders: [{ name: "webapi", asset: "./webapi/localization", options: { type: "webapi" } }]
        }
    },
    {
        path: "visualization",
        component: VisualizationComponent,
        children: [
            {
                path: "",
                redirectTo: "page",
                pathMatch: "full"
            }, {
                path: "page",
                loadChildren: () => import("./visualization/mobile/mobile-view.module").then(m => m.MobileViewModule),
                resolve: { l10n: L10nLazyResolver },
                data: {
                    l10nProviders: [{ name: "webapi", asset: "./webapi/localization", options: { type: "webapi" } }]
                }
            }, {
                // automations the admins released for the visualization
                path: "automations",
                loadChildren: () => import("./visualization/automations/automations-visu.module").then(m => m.AutomationsVisuModule),
                resolve: { l10n: L10nLazyResolver },
                data: {
                    l10nProviders: [{ name: "webapi", asset: "./webapi/localization", options: { type: "webapi" } }]
                }
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
