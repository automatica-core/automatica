import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { L10nTranslationModule } from "angular-l10n";
import { FontAwesomeModule } from "@fortawesome/angular-fontawesome";
import {
    DxButtonModule, DxCheckBoxModule, DxDropDownButtonModule, DxNumberBoxModule, DxPopupModule, DxSelectBoxModule, DxSliderModule, DxSwitchModule, DxTemplateModule,
    DxTextBoxModule
} from "devextreme-angular";
import { SharedModule } from "src/app/shared/shared.module";
import { ShadingPageComponent } from "./shading-page/shading-page.component";
import { ShadingCompassComponent, ShadingNodeFieldComponent, ShadingSignalEditorComponent } from "./fields/shading-fields";

/** the shading page (admin): facades, blinds, sources, live status and preview */
@NgModule({
    declarations: [
        ShadingPageComponent,
        ShadingNodeFieldComponent,
        ShadingSignalEditorComponent,
        ShadingCompassComponent
    ],
    imports: [
        CommonModule,
        L10nTranslationModule,
        FontAwesomeModule,
        SharedModule,
        DxButtonModule,
        DxCheckBoxModule,
        DxDropDownButtonModule,
        DxNumberBoxModule,
        DxPopupModule,
        DxSelectBoxModule,
        DxSliderModule,
        DxSwitchModule,
        DxTemplateModule,
        DxTextBoxModule
    ],
    exports: [
        ShadingPageComponent
    ]
})
export class ShadingModule { }
