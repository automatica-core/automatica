import { NgModule } from "@angular/core";
import { Routes, RouterModule } from "@angular/router";
import { LoginFormComponent } from "./shared/components/login-form/login-form.component";
import { VisualizationComponent } from "./visualization/visualization.component";
import { AdminComponent } from "./admin/admin.component";
import { L10nLazyResolver } from "./services/l10n-lazy-resolver";

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
