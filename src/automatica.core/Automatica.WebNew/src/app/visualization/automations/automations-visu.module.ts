import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { RouterModule, Routes } from "@angular/router";
import { L10nTranslationModule } from "angular-l10n";
import { FontAwesomeModule } from "@fortawesome/angular-fontawesome";
import { DxButtonModule, DxNumberBoxModule, DxSelectBoxModule, DxSwitchModule, DxTextBoxModule } from "devextreme-angular";
import { HasRoleGuard } from "src/app/services/login.service";
import { Role } from "src/app/base/model/user/role";
import { AutomationFieldsModule } from "src/app/pages/automations/fields/automation-fields.module";
import { AutomationsVisuComponent } from "./automations-visu.component";

const routes: Routes = [
    {
        path: "",
        component: AutomationsVisuComponent,
        data: {
            title: "Automations",
            requiresRole: Role.VISU_ROLE
        },
        canActivate: [HasRoleGuard]
    }
];

/** /visualization/automations: the automations released for the visualization */
@NgModule({
    declarations: [
        AutomationsVisuComponent
    ],
    imports: [
        CommonModule,
        RouterModule.forChild(routes),
        L10nTranslationModule,
        FontAwesomeModule,
        AutomationFieldsModule,
        DxButtonModule,
        DxNumberBoxModule,
        DxSelectBoxModule,
        DxSwitchModule,
        DxTextBoxModule
    ]
})
export class AutomationsVisuModule { }
