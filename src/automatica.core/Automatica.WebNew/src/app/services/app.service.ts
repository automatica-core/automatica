import { Injectable, EventEmitter } from "@angular/core";

@Injectable()
export class AppService {

    private _isStarting: boolean;

    // every running operation holds one reference, the spinner is shown while any reference is held
    private loadingCount = 0;
    // bumped by resetLoading, releases of older references are ignored afterwards
    private loadingGeneration = 0;
    // reference held by the legacy `isLoading = true/false` setter
    private legacyLoadingRelease: () => void;
    private emittedLoading = false;
    private loadingEmitTimer: any;

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
        return this.loadingCount > 0;
    }
    /**
     * Legacy switch, shares one reference between all callers. Prefer beginLoading()/whileLoading()
     * so concurrent operations don't hide each other's spinner.
     */
    public set isLoading(v: boolean) {
        if (v && !this.legacyLoadingRelease) {
            this.legacyLoadingRelease = this.beginLoading();
        } else if (!v && this.legacyLoadingRelease) {
            const release = this.legacyLoadingRelease;
            this.legacyLoadingRelease = undefined;
            release();
        }
    }

    /**
     * Shows the loading spinner until the returned function is called (call it in a finally block).
     * Calling the returned function more than once has no effect.
     */
    public beginLoading(): () => void {
        const generation = this.loadingGeneration;
        let released = false;

        this.loadingCount++;
        this.loadingStateChanged();

        return () => {
            if (released) {
                return;
            }
            released = true;
            if (generation !== this.loadingGeneration) {
                return;
            }
            this.loadingCount = Math.max(0, this.loadingCount - 1);
            this.loadingStateChanged();
        };
    }

    public async whileLoading<T>(work: () => Promise<T>): Promise<T> {
        const done = this.beginLoading();
        try {
            return await work();
        } finally {
            done();
        }
    }

    /**
     * Drops all references, e.g. when the login page is shown and nothing that was running matters anymore.
     */
    public resetLoading() {
        this.loadingGeneration++;
        this.loadingCount = 0;
        this.legacyLoadingRelease = undefined;
        this.loadingStateChanged();
    }

    private loadingStateChanged() {
        clearTimeout(this.loadingEmitTimer);

        // only emit if the state is still the same after 50ms, short operations never flash the spinner
        this.loadingEmitTimer = setTimeout(() => {
            this.loadingEmitTimer = undefined;
            const loading = this.isLoading;
            if (loading !== this.emittedLoading) {
                this.emittedLoading = loading;
                this.isLoadingChanged.emit(loading);
            }
        }, 50);
    }

}
