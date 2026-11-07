import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { L10nTranslationModule } from "angular-l10n";
import { FontAwesomeModule } from "@fortawesome/angular-fontawesome";
import {
    DxButtonModule, DxCheckBoxModule, DxDateBoxModule, DxDropDownButtonModule, DxNumberBoxModule, DxPopupModule, DxSelectBoxModule,
    DxSwitchModule, DxTemplateModule, DxTextAreaModule, DxTextBoxModule
} from "devextreme-angular";
import { SharedModule } from "src/app/shared/shared.module";
import { AutomationsListComponent } from "./automations-list/automations-list.component";
import { AutomationEditorComponent } from "./automation-editor/automation-editor.component";
import { AutomationRunHistoryComponent } from "./run-history/automation-run-history.component";
import { AutomationBlockCardComponent } from "./blocks/automation-block-card.component";
import { AutomationTriggerCardComponent } from "./blocks/automation-trigger-card.component";
import { AutomationConditionCardComponent, AutomationConditionListComponent } from "./blocks/automation-condition-list.component";
import { AutomationActionCardComponent, AutomationActionListComponent } from "./blocks/automation-action-list.component";
import {
    AutomationAddMenuComponent, AutomationDaysPickerComponent, AutomationDurationInputComponent, AutomationNodeFieldComponent,
    AutomationTimeInputComponent, AutomationValueInputComponent
} from "./fields/automation-fields";

/** the Automations pages (admin): rule list, editor and run history */
@NgModule({
    declarations: [
        AutomationsListComponent,
        AutomationEditorComponent,
        AutomationRunHistoryComponent,
        AutomationBlockCardComponent,
        AutomationTriggerCardComponent,
        AutomationConditionListComponent,
        AutomationConditionCardComponent,
        AutomationActionListComponent,
        AutomationActionCardComponent,
        AutomationNodeFieldComponent,
        AutomationValueInputComponent,
        AutomationDaysPickerComponent,
        AutomationTimeInputComponent,
        AutomationDurationInputComponent,
        AutomationAddMenuComponent
    ],
    imports: [
        CommonModule,
        L10nTranslationModule,
        FontAwesomeModule,
        SharedModule,
        DxButtonModule,
        DxCheckBoxModule,
        DxDateBoxModule,
        DxDropDownButtonModule,
        DxNumberBoxModule,
        DxPopupModule,
        DxSelectBoxModule,
        DxSwitchModule,
        DxTemplateModule,
        DxTextAreaModule,
        DxTextBoxModule
    ],
    exports: [
        AutomationsListComponent,
        AutomationEditorComponent
    ]
})
export class AutomationsModule { }
