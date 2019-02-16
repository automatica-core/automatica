import { Injectable, ChangeDetectorRef, EventEmitter } from "@angular/core";

@Injectable()
export class AppService {

    private _isLoading: boolean = false;
    private _isStarting: boolean;

    public title = "COMMON.HOME";

    public isLoadingChanged: EventEmitter<boolean> = new EventEmitter<boolean>();
    public isStartingChanged: EventEmitter<boolean> = new EventEmitter<boolean>();

    constructor() {

    }

    public setAppTitle(title: string) {
        this.title = title;
    }



    public get isStarting(): boolean {
        return this._isStarting;
    }
    public set isStarting(v: boolean) {
        if (v !== this._isStarting) {
            this._isStarting = v;
            this.isStartingChanged.emit(v);
        }
    }


    public get isLoading(): boolean {
        return this._isLoading;
    }
    public set isLoading(v: boolean) {
        if (v !== this._isLoading) {
            this.isLoadingChanged.emit(v);
            this._isLoading = v;
        }
    }

}
