import { Injectable, EventEmitter } from "@angular/core";
import { BaseHub, SignalRMethod } from "../base-hub";


@Injectable()
export class LogHubService extends BaseHub {

    @SignalRMethod
    pushEventLog: EventEmitter<any> = new EventEmitter<any>();

    constructor() {
        super("loggingHub");
    }
}
