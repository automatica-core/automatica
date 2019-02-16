import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { DxButtonModule, DxTextBoxModule, DxValidatorModule, DxValidationGroupModule } from "devextreme-angular";
import { TranslationModule } from "angular-l10n";
import { LoginFormComponent } from "./login-form.component";

@NgModule({
    imports: [
        CommonModule,
        DxButtonModule,
        DxTextBoxModule,
        DxValidatorModule,
        DxValidationGroupModule,
        TranslationModule
    ],
    declarations: [ LoginFormComponent ],
    exports: [ LoginFormComponent ]
})
export class LoginFormModule { }