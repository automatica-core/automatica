import { Injectable, EventEmitter } from "@angular/core";
import { DeviceDetectorService, OrientationType } from "ngx-device-detector";

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
        try {
            console.log(this.deviceServiceDetector.orientation);
            const orientation = this.deviceServiceDetector.orientation;

            if (orientation === OrientationType.Landscape) {
                this.orientation = Orientation.Landscape;
            } else if (orientation ===  OrientationType.Portrait) {
                this.orientation = Orientation.Portrait;
            } else if (orientation === undefined) {
                this.orientation = Orientation.Unknown;
            }

            if (currentOrientation !== this.orientation) {
                this.orientationChange.emit(this.orientation);
            }
        }
        catch (error) {
            this.orientation = Orientation.Unknown;
        }

    }

    isMobile() {
        return this.deviceServiceDetector.isMobile() || this.deviceServiceDetector.isTablet();
    }
}
