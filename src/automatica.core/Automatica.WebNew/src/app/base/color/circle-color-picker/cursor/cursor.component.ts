import { Component, Input } from "@angular/core";

import { Vector } from "../../vector";

@Component({
    selector: "app-cursor",
    templateUrl: "./circle-cursor.component.html",
    styles: [
        `
        .cursor {
            cursor: pointer;
            position: relative;
            border-radius: 50%;
            width: 20px;
            height: 20px;
            box-shadow: 0px 0px 0px 2px #222 inset;
            transform: translate(-10px, -10px);
        }
    `,
    ],
})
export class CursorComponent {
    @Input() public position: Vector;

    constructor() {}
}
