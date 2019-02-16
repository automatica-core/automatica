import { Injectable, EventEmitter } from "@angular/core";
import { BaseHub, SignalRMethod } from "../base-hub";

@Injectable()
export class TelegramHubService extends BaseHub {

    @SignalRMethod
    public onTelegram: EventEmitter<any> = new EventEmitter<any>();

    constructor() {
        super("telegramHub");
    }

    public subscribe(busId: string) {
        super.callHubProxyWithParam("subscribe", busId);
    }
    public unsubscribe(busId: string) {
        super.callHubProxyWithParam("unsubscribe", busId);
    }

}
