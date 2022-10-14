import { NgModule } from "@angular/core";
import { DxButtonModule, DxSliderModule } from "devextreme-angular";
import { SwitchButtonComponent } from "./switch-button/switch-button.component";
import { L10nTranslationModule } from "angular-l10n";
import { CommonModule } from "@angular/common";
import { SliderComponent } from "./slider/slider.component";


@NgModule({
    imports: [
        CommonModule,
        DxButtonModule,
        DxSliderModule,
        L10nTranslationModule
    ],
    declarations: [
        SwitchButtonComponent,
        SliderComponent
    ],
    exports: [
        SwitchButtonComponent,
        SliderComponent
    ]
})

export class ComponentsModule { }
