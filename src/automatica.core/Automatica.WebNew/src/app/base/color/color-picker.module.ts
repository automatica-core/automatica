import { CommonModule } from "@angular/common";
import { ModuleWithProviders, NgModule } from "@angular/core";
import { FormsModule } from "@angular/forms";

import { BoxColorPickerModule } from "./box-color-picker/box-color-picker.module";
import { CircleColorPickerModule } from "./circle-color-picker/circle-color-picker.module";

@NgModule({
    imports: [CommonModule, FormsModule],
    declarations: [],
    exports: [CircleColorPickerModule, BoxColorPickerModule],
})
export class NgColorModule {
    public static forRoot(): ModuleWithProviders {
        return {
            ngModule: NgColorModule,
            providers: [],
        };
    }
}
