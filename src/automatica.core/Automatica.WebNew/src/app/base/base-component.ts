import { EventEmitter } from "@angular/core";
import { Observable, Subscription } from "rxjs";
import { TranslationService } from "angular-l10n";
import { WebApiException, ExceptionSeverity } from "./model/web-api-exception";
import { NotifyService } from "../services/notify.service";

export class BaseComponent {

    private subscriptions: Subscription[] = [];

    // gutterH = `url("data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAUAAAAeCAYAAADkftS9AAAAIklEQVQoU2M4c+bMfxAGAgYYmwGrIIiDjrELjpo5aiZeMwF+yNnOs5KSvgAAAABJRU5ErkJggg==");`;

    constructor(protected notifyService: NotifyService, protected translate: TranslationService) {


    }

    protected baseOnInit() {

    }

    protected handleError(error) {
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

        } else {
            this.notifyService.notifyError(error);
        }

        console.log(error);
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
    }
}
