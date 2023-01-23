import { Injectable, EventEmitter } from "@angular/core";
import { DeviceDetectorService } from "ngx-device-detector";

export enum Orientation {
    Unknown,
    Landscape,
    Portrait
}

@Injectable()
export class DeviceService {


    private _orientation: Orientation = Orientation.Unknown;
    public get orientation(): Orientation {
        return this._orientation;
    }
    public set orientation(v: Orientation) {
        this._orientation = v;
    }

    public orientationChange = new EventEmitter<Orientation>();


    constructor(private deviceServiceDetector: DeviceDetectorService) {
        const that = this;
        window.addEventListener("orientationchange", function () {
            that.setOrientation();
        }, false);

        this.setOrientation();
    }

    private setOrientation() {
        const currentOrientation = this.orientation;

        console.log(screen);
        console.log(screen.orientation);
        const orientation = <string><unknown>screen.orientation.type;

        if (orientation === "landscape-primary" || orientation === "landscape-secondary") {
            this.orientation = Orientation.Landscape;
        } else if (orientation === "portrait-secondary" || orientation === "portrait-primary") {
            this.orientation = Orientation.Portrait;
        } else if (orientation === undefined) {
            this.orientation = Orientation.Unknown;
        }

        if (currentOrientation !== this.orientation) {
            this.orientationChange.emit(this.orientation);
        }

    }

    isMobile() {
        return this.deviceServiceDetector.isMobile();
    }
}
