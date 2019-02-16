import { Component, OnInit } from "@angular/core";
import { navigation } from "../app-navigation";
import { HubConnectionService } from "../base/communication/hubs/hub-connection.service";
import { ServerStateService } from "../services/server-state.service";

@Component({
  selector: "app-admin",
  templateUrl: "./admin.component.html",
  styleUrls: ["./admin.component.scss"]
})
export class AdminComponent implements OnInit {

  menuItems = navigation;

  constructor(private hubService: HubConnectionService, private serverStateService: ServerStateService) { }

  async ngOnInit() {

    if (!await this.serverStateService.isStarted()) {
      this.serverStateService.init(false);

    }
    this.serverStateService.start();
    await this.hubService.init();
  }

}
