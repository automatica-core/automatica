import { Injectable } from "@angular/core";
import { ConfigService } from "./config.service";
import { Router } from "@angular/router";
import { AppService } from "./app.service";
import { DataHubService } from "../base/communication/hubs/data-hub.service";
import { NotifyService } from "./notify.service";
import { DeviceService } from "./device/device.service";

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
  private hubEventsSubscribed = false;

  constructor(private configService: ConfigService,
    private dataHub: DataHubService,
    private router: Router,
    private appService: AppService,
    private notifyService: NotifyService,
    private deviceService: DeviceService) {

  }

  async isStarted(): Promise<boolean> {
    try {
      const currentServerState = await this.configService.getServerState();
      const state: RunState = currentServerState.status;
      this.serverState = state;

      return state === RunState.Started;
    } catch (error) {
      console.log(error);
      if (error?.status === 401 || error?.status === 404) {
        await this.router.navigate(["/login"]);
      }
      else if (error?.status === 0 && this.deviceService.isMobileApp) {
        await this.router.navigate(["/login"]);
        await this.notifyService.notifyError("ERROR.SERVER_NOT_AVAILABLE");
      }
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
    // called on every admin/visualization init, the hub events only need to be subscribed once
    if (this.hubEventsSubscribed) {
      return;
    }
    this.hubEventsSubscribed = true;

    this.dataHub.serverStateChanged.subscribe((data) => {
      this.init(data[0] === RunState.Started)
      this.serverState = data[0];
    });

    this.dataHub.connectionStateChanged.subscribe(async (connectionState) => {
      if (connectionState) {
        // connected is not started: keep the overlay until the server reports Started (serverStateChanged)
        this.init(await this.isStarted());
      } else {
        this.init(false);
      }
    });

  }
}
