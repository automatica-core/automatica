import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { L10nTranslationModule } from "angular-l10n";
import { FontAwesomeModule } from "@fortawesome/angular-fontawesome";
import { DxButtonModule, DxCheckBoxModule, DxDateBoxModule, DxNumberBoxModule, DxSelectBoxModule, DxTextBoxModule } from "devextreme-angular";
import { SharedModule } from "src/app/shared/shared.module";
import { OperatingModesPageComponent } from "./operating-modes-page/operating-modes-page.component";

/** the operating modes page (admin): modes, public holidays, operating times */
@NgModule({
    declarations: [
        OperatingModesPageComponent
    ],
    imports: [
        CommonModule,
        L10nTranslationModule,
        FontAwesomeModule,
        SharedModule,
        DxButtonModule,
        DxCheckBoxModule,
        DxDateBoxModule,
        DxNumberBoxModule,
        DxSelectBoxModule,
        DxTextBoxModule
    ],
    exports: [
        OperatingModesPageComponent
    ]
})
export class OperatingModesModule { }
