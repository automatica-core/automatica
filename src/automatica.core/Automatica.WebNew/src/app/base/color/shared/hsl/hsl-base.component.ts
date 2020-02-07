import { ElementRef, Input, Directive } from '@angular/core';
import { ControlValueAccessor } from '@angular/forms';

import { Vector } from '../../vector';
import { ColorUtilityService } from '../color-utility/color-utility.service';
import { MouseHandlerOutput } from '../mouse-handler/mouse-handler-output';

export interface SaturationLightness {
    saturation: number;
    lightness: number;
}

@Directive()
export class HslBaseComponent implements ControlValueAccessor {
    public cursorPosition: Vector;
    @Input() public hue: number;
    private saturation: number;
    private lightness: number;
    private onTouchedCallback: () => void;
    private onChangeCallback: (_: SaturationLightness) => void;

    constructor(private el: ElementRef, private readonly colorService: ColorUtilityService) {
        this.cursorPosition = {
            x: 0.5,
            y: 0.5,
        };
        this.saturation = 0.5;
        this.lightness = 0.5;
    }

    public setSaturationLightness(mouseEvent: MouseHandlerOutput): void {
        this.cursorPosition.x = mouseEvent.realWorld.x;
        this.cursorPosition.y = mouseEvent.realWorld.y;

        const yScaleFactor = 1 / (mouseEvent.s + 1);

        const value = {
            saturation: mouseEvent.s,
            lightness: mouseEvent.v * yScaleFactor,
        };

        this.value = value;
    }

    public hsl(): string {
        return this.colorService.calculateHexFromHsl({
            hue: this.hue * 360,
            saturation: 100,
            lightness: 50,
        });
    }

    get value(): SaturationLightness {
        return {
            saturation: this.saturation || 0,
            lightness: this.lightness || 0,
        };
    }

    set value(v: SaturationLightness) {
        this.saturation = v.saturation;
        this.lightness = v.lightness;
        this.onChangeCallback(v);
    }

    public writeValue(v: SaturationLightness): void {
        if (v === undefined || v === null) {
            return;
        }

        if (v.saturation === undefined || v.lightness === undefined) {
            return;
        }

        const yScaleFactor = 1 / (v.saturation + 1);

        this.cursorPosition.x = v.saturation * this.el.nativeElement.offsetWidth;
        this.cursorPosition.y = (1 - v.lightness) * this.el.nativeElement.offsetHeight * yScaleFactor;
        this.saturation = v.saturation;
        this.lightness = v.lightness;
    }

    public registerOnChange(fn: (_: SaturationLightness) => void): void {
        this.onChangeCallback = fn;
    }

    public registerOnTouched(fn: () => void): void {
        this.onTouchedCallback = fn;
    }
}
