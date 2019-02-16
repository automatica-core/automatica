// tslint:disable:component-selector
import { Component, forwardRef, Input, OnInit, Output, EventEmitter } from "@angular/core";
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from "@angular/forms";

import { ColorOutput } from "../shared/color-utility/color-output";
import { ColorUtilityService } from "../shared/color-utility/color-utility.service";
import { SaturationLightness } from "../shared/hsl/saturation-lightness";

@Component({
    selector: "ng-color-circle",
    styleUrls: ["./circle-color-picker.component.scss"],
    templateUrl: "./circle-color-picker.component.html",
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => CircleColorPickerComponent),
            multi: true,
        },
    ],
})
export class CircleColorPickerComponent implements ControlValueAccessor, OnInit {
    @Input() public startHex: string;
    public hue: number;
    public saturationLightness: SaturationLightness;
    private onTouchedCallback: () => void;
    private onChangeCallback: (_: ColorOutput) => void;

    @Output() colorChanged = new EventEmitter<ColorOutput>();

    constructor(private colorUtility: ColorUtilityService) {
        this.saturationLightness = {
            saturation: 0,
            lightness: 0,
        };
        this.hue = 0;
        this.onTouchedCallback = () => {};
        this.onChangeCallback = () => {};
    }

    public ngOnInit(): void {
        const hsl = this.colorUtility.calculateHslFromHex(this.startHex || "ff0000");
        this.hue = hsl.hue;
        this.saturationLightness = {
            saturation: hsl.saturation,
            lightness: hsl.lightness,
        };
        const colorOutput = this.colorUtility.createColorOutput(
            this.hue * 360,
            this.saturationLightness.saturation * 100,
            this.saturationLightness.lightness * 100,
        );
        this.onChangeCallback(colorOutput);
    }

    public calculateColor(): void {
        const colorOutput = this.colorUtility.createColorOutput(
            this.hue * 360,
            this.saturationLightness.saturation * 100,
            this.saturationLightness.lightness * 100,
        );
        this.onChangeCallback(colorOutput);
        this.colorChanged.emit(colorOutput);
    }

    public writeValue(obj: ColorOutput): void {
        // TODO
    }
    public registerOnChange(fn: (_: ColorOutput) => void): void {
        this.onChangeCallback = fn;
    }
    public registerOnTouched(fn: () => void): void {
        this.onTouchedCallback = fn;
    }
}
