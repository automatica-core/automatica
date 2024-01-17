import { EventEmitter, Input, Directive } from "@angular/core";
import { Observable, Subscription } from "rxjs";
import { L10nTranslationService } from "angular-l10n";
import { WebApiException, ExceptionSeverity } from "./model/web-api-exception";
import { NotifyService } from "../services/notify.service";
import { AppService } from "../services/app.service";
import { HttpErrorResponse } from "@angular/common/http";

@Directive()
export class BaseComponent {

    private subscriptions: Subscription[] = [];
    private intervals: NodeJS.Timeout[] = [];
    private lastState: boolean = false;
    // gutterH = `url("data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAUAAAAeCAYAAADkftS9AAAAIklEQVQoU2M4c+bMfxAGAgYYmwGrIIiDjrELjpo5aiZeMwF+yNnOs5KSvgAAAABJRU5ErkJggg==");`;


    private _isLoading: boolean;

    @Input()
    public get isLoading(): boolean {
        return this._isLoading;
    }
    public set isLoading(v: boolean) {
        this._isLoading = v;
        this.appService.isLoading = v;
    }

    constructor(protected notifyService: NotifyService, protected translate: L10nTranslationService, protected appService: AppService) {


    }

    protected baseOnInit() {
        this.registerEvent(this.appService.isStartingChanged, async (v) => {

            if (!v && this.lastState !== v) {
                if (this.isLoading) {
                    console.log("Cannot start reload, because we are not finished now...");
                    return;
                }
                await this.load();
            }
            this.lastState = v;
        });
    }

    protected load(): Promise<any> {
        return Promise.resolve();
    }

    protected parseErrorString(error) {
        if (error instanceof WebApiException) {
            const errorText = this.translate.translate("ERROR." + error.ErrorText);
            return errorText;
        } else if (error instanceof HttpErrorResponse) {
            return error.message;
        }
        else {
            return error;
        }
    }

    protected handleError(error) {
        console.log(error);
        if (error instanceof WebApiException) {
            const errorText = this.translate.translate("ERROR." + error.ErrorText);
            switch (error.Severity) {
                case ExceptionSeverity.Warning:
                    this.notifyService.notifyWarning(errorText, 5000);
                    break;
                case ExceptionSeverity.Error:
                case ExceptionSeverity.Dead:
                    this.notifyService.notifyError(errorText);
                    break;
                case ExceptionSeverity.Info:
                    this.notifyService.notifyInfo(errorText);
                    break;
            }

        } else if (error instanceof HttpErrorResponse) {
            this.notifyService.notifyError(error.message);
        }
        else {
            this.notifyService.notifyError(error);
        }

    }

    protected baseOnDestroy() {
        this.unregisterAll();
    }
    protected registerEvent(event: EventEmitter<any>, callback: (any)) {
        const subscriber = event.subscribe((data) => callback(data), (error) => console.error(error));
        this.subscriptions.push(subscriber);
        return subscriber;
    }

    protected unregisterEvent(subscriber: any) {
        subscriber.unsubscribe();

        this.subscriptions = this.subscriptions.filter(a => a !== subscriber);
    }

    protected registerObservable(event: Observable<any>, callback: (any)) {
        const subscriber = event.subscribe((data) => callback(data));
        this.subscriptions.push(subscriber);
    }

    protected unregisterAll() {
        this.subscriptions.forEach(sub => {
            sub.unsubscribe();
        });
        this.subscriptions = [];

        this.intervals.forEach(sub => {
            clearInterval(sub);
        });
        this.intervals = [];
    }

    protected registerInterval(callback: (any), intervalInMs) {
        const interval = setInterval(callback, intervalInMs);
        this.intervals.push(interval);
    }
}
