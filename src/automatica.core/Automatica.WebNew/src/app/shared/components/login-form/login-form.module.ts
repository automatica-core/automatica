import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { DxButtonModule, DxTextBoxModule, DxValidatorModule, DxValidationGroupModule, DxLoadPanelModule } from "devextreme-angular";
import { L10nTranslationModule } from "angular-l10n";
import { LoginFormComponent } from "./login-form.component";

@NgModule({
    imports: [
        CommonModule,
        DxButtonModule,
        DxTextBoxModule,
        DxValidatorModule,
        DxValidationGroupModule,
        L10nTranslationModule,
        DxLoadPanelModule
    ],
    declarations: [ LoginFormComponent ],
    exports: [ LoginFormComponent ]
})
export class LoginFormModule { }