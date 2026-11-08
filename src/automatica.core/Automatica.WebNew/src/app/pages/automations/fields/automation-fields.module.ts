import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { L10nTranslationModule } from "angular-l10n";
import { DxDateBoxModule, DxNumberBoxModule, DxSelectBoxModule } from "devextreme-angular";
import { AutomationDaysPickerComponent, AutomationDurationInputComponent, AutomationTimeInputComponent } from "./automation-fields";

/**
 * The plain inputs of the automation editor, shared with the visualization page of released automations. The
 * duration input needs an AutomationEditorContext from the page.
 */
@NgModule({
    declarations: [
        AutomationDaysPickerComponent,
        AutomationTimeInputComponent,
        AutomationDurationInputComponent
    ],
    imports: [
        CommonModule,
        L10nTranslationModule,
        DxDateBoxModule,
        DxNumberBoxModule,
        DxSelectBoxModule
    ],
    exports: [
        AutomationDaysPickerComponent,
        AutomationTimeInputComponent,
        AutomationDurationInputComponent
    ]
})
export class AutomationFieldsModule { }
