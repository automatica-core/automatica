import { Injectable } from "@angular/core";
import { ConfigService } from "./config.service";
import { Router } from "@angular/router";
import { AppService } from "./app.service";
import { DataHubService } from "../base/communication/hubs/data-hub.service";

export enum RunState {
  Constructed,
  Loading,
  Configure,
  Starting,
  Started,
  Stopped
}

@Injectable()
export class ServerStateService {

  public serverState: RunState;

  constructor(private configService: ConfigService, private dataHub: DataHubService, private router: Router, private appService: AppService) {

  }

  async isStarted(): Promise<boolean> {
    try {
      const currentServerState = await this.configService.getServerState();
      const state: RunState = currentServerState.Status;
      this.serverState = state;

      return state === RunState.Started;
    } catch (error) {
      console.log(error);
      return false;
    }
  }


  async init(started) {
    if (!started) {
      this.appService.isStarting = true;
    } else if (this.appService.isStarting) {
      this.appService.isStarting = false;
    }
  }

  start() {
    this.dataHub.serverStateChanged.subscribe((data) => {
      this.init(data[0] === RunState.Started)
      this.serverState = data[0];
    });

    this.dataHub.connectionStateChanged.subscribe(async (connectionState) => {
      if (connectionState) {
        const isStarted = await this.isStarted();

        if (!isStarted) {
          this.init(false);
        } else {
          this.init(true);
        }
      }
      this.init(connectionState);
    });

  }
}
