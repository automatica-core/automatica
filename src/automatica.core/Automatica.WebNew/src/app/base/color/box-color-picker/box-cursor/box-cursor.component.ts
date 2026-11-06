import { Component, Input, ChangeDetectionStrategy } from '@angular/core';

import { Vector } from '../../vector';

@Component({
    selector: 'app-cursor',
    templateUrl: './box-cursor.component.html',
    styleUrls: ['./box-cursor.component.scss'],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class BoxCursorComponent {
    @Input() public position: Vector;
    @Input() public bothAxis: boolean;
    @Input() public lightness: number;

    constructor() {
        this.bothAxis = false;
    }
}
