import { Component, ElementRef, forwardRef } from '@angular/core';
import { NG_VALUE_ACCESSOR } from '@angular/forms';

import { ColorUtilityService } from '../../shared/color-utility/color-utility.service';
import { HslBaseComponent } from '../../shared/hsl/hsl-base.component';
import { IMAGE } from '../../shared/hsl/hsl-image';

@Component({
    selector: 'app-hsl',
    templateUrl: './circle-hsl.component.html',
    styles: [
        `
        :host {
            display: block;
            width: 100%;
            height: 100%;
        }

        .saturation-lightness {
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
            useExisting: forwardRef(() => HslComponent),
            multi: true,
        },
    ],
})
export class HslComponent extends HslBaseComponent {
    constructor(el: ElementRef, colorService: ColorUtilityService) {
        super(el, colorService);
    }
}
