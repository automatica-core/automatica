import { NgModule } from "@angular/core";
import { SkeletonModule } from "./skeleton/skeleton.module";
import { CommonModule, NgSwitch } from "@angular/common";
import { FormsModule } from "@angular/forms";

import { ConfigTreeComponent } from "./config-tree/config-tree.component";
import { RuleEditorComponent } from "./ruleeditor/ruleeditor.component";
import { PropertyEditorComponent } from "./propertyeditor/propertyeditor.component";
import { ConfigMenuComponent } from "./config-menu/config-menu.component";
import { DxMenuModule, DxTreeViewModule, DxTemplateModule, DxTreeListModule, DxDataGridModule, DxTextBoxModule, DxCheckBoxModule, DxButtonModule, DxSelectBoxModule, DxNumberBoxModule, DxPopoverComponent, DxPopoverModule, DxValidatorModule, DxColorBoxModule, DxPopupModule, DxBoxModule, DxDropDownBoxModule, DxListModule, DxLoadPanelModule, DxFileUploaderModule, DxDateBoxModule, DxScrollViewModule, DxContextMenuModule, DxSchedulerModule, DxTooltipModule, DxTabsModule, DxTextAreaModule, DxTagBoxModule } from "devextreme-angular";
import { RadioStationPickerComponent } from "./radio-station-picker/radio-station-picker.component";
import { DndModule } from "p3root-angular-dnd";
import { L10nTranslationModule } from "angular-l10n";
import { NodeValueSelectorComponent } from "./node-value-selector/node-value-selector.component";
import { VisuPageSelectorComponent } from "./visu-page-selector/visu-page-selector.component";
import { VisuPageListComponent } from "./visu-page-list/visu-page-list.component";
import { FontAwesomeModule } from "@fortawesome/angular-fontawesome";
import { NodeInstanceImportComponent } from "./propertyeditor/node-instance-ets-import/node-instance-import.component";
import { NodeInstanceImportSerivce } from "./propertyeditor/node-instance-ets-import/node-instance-import.service";

@NgModule({
    imports: [
        SkeletonModule,
        FormsModule,
        CommonModule,

        DxMenuModule,
        DxTreeViewModule,
        DxTemplateModule,
        DxTreeListModule,
        DxDataGridModule,
        DxTextBoxModule,
        DxCheckBoxModule,
        DndModule,
        DxButtonModule,
        DxSelectBoxModule,
        DxNumberBoxModule,
        L10nTranslationModule,
        DxPopoverModule,
        DxValidatorModule,
        DxColorBoxModule,
        DxPopupModule,
        DxBoxModule,
        DxDropDownBoxModule,
        DxListModule,
        DxLoadPanelModule,
        DxTreeViewModule,
        DxFileUploaderModule,
        FontAwesomeModule,
        DxDateBoxModule,
        DxScrollViewModule,
        DxContextMenuModule,
        DxSchedulerModule,
        DxTooltipModule,
        DxTabsModule,
        DxTextAreaModule,
        DxTagBoxModule
    ],
    declarations: [
        PropertyEditorComponent,
        ConfigMenuComponent,
        ConfigTreeComponent,
        RuleEditorComponent,
        NodeValueSelectorComponent,
        VisuPageSelectorComponent,
        VisuPageListComponent,
        NodeInstanceImportComponent,
        RadioStationPickerComponent],
    exports: [
        SkeletonModule,
        PropertyEditorComponent,
        ConfigMenuComponent,
        ConfigTreeComponent,
        RuleEditorComponent,
        RadioStationPickerComponent
    ],
    // no provideHttpClient here: the lazy admin module would get its own HttpClient without the root interceptors
    providers: [
        NodeInstanceImportSerivce
    ]
})

export class SharedModule { }
