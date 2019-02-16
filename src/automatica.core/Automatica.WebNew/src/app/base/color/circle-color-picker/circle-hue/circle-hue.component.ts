import { Component, ElementRef, forwardRef } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

import { MouseHandlerOutput } from '../../shared/mouse-handler/mouse-handler-output';
import { Vector } from '../../vector';
import { IMAGE } from './circle-hue-image';

@Component({
    selector: 'app-hue',
    templateUrl: './circle-hue.component.html',
    styles: [
        `
        :host {
            display: block;
            width: 100%;
            height: 100%;
        }

        .hue {
            cursor: pointer;
            width: 100%;
            height: 100%;
            border: none;
            background-size: 100% 100%;
            background-image: url(${IMAGE});
        }
    `,
    ],
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => CircleHueComponent),
            multi: true,
        },
    ],
})
export class CircleHueComponent implements ControlValueAccessor {
    public cursorPosition: Vector;
    private onTouchedCallback: () => void;
    private onChangeCallback: (_: number) => void;

    constructor(private el: ElementRef) {
        this.cursorPosition = {
            x: 0,
            y: 0,
        };
        this.onTouchedCallback = () => {};
        this.onChangeCallback = () => {};
    }

    public writeValue(hue: any): void {
        const angle = -(hue * (2 * Math.PI) - Math.PI);
        const radius = this.el.nativeElement.offsetWidth / 2;

        this.cursorPosition = this.getCoordinateFromRadius(radius, angle);
    }

    public registerOnChange(fn: any): void {
        this.onChangeCallback = fn;
    }

    public registerOnTouched(fn: any): void {
        this.onTouchedCallback = fn;
    }

    public setHue(mouseEvent: MouseHandlerOutput): void {
        const coordsFromCenter = {
            x: mouseEvent.realWorld.x - this.el.nativeElement.offsetWidth / 2,
            y: mouseEvent.realWorld.y - this.el.nativeElement.offsetHeight / 2,
        };
        const distanceFromCenter = Math.sqrt(Math.pow(coordsFromCenter.x, 2) + Math.pow(coordsFromCenter.y, 2));
        const outerMaxRadius = this.el.nativeElement.offsetWidth / 2;
        const innerMaxRadius = this.el.nativeElement.offsetWidth / 3.1;
        const angle = Math.atan2(coordsFromCenter.x, coordsFromCenter.y);

        if (distanceFromCenter > outerMaxRadius) {
            this.cursorPosition = this.getCoordinateFromRadius(outerMaxRadius, angle);
        } else if (distanceFromCenter < innerMaxRadius) {
            this.cursorPosition = this.getCoordinateFromRadius(innerMaxRadius, angle);
        } else {
            this.cursorPosition = {
                x: mouseEvent.realWorld.x,
                y: mouseEvent.realWorld.y,
            };
        }

        const hue = (-angle + Math.PI) / (2 * Math.PI);
        this.onChangeCallback(hue);
    }

    private getCoordinateFromRadius(radius: number, angle: number): Vector {
        return {
            x: radius * Math.sin(angle) + this.el.nativeElement.offsetWidth / 2,
            y: radius * Math.cos(angle) + this.el.nativeElement.offsetHeight / 2,
        };
    }
}
