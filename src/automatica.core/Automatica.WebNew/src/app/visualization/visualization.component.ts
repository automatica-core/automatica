import { Component, OnInit, ChangeDetectionStrategy } from "@angular/core";
import { HubConnectionService } from "../base/communication/hubs/hub-connection.service";
import { AreaService } from "../services/areas.service";
import { VisuService } from "../services/visu.service";
import { CategoryService } from "../services/categories.service";
import { AppService } from "../services/app.service";
import { ServerStateService } from "../services/server-state.service";
import { AreaInstance } from "../base/model/areas";
import { VisuPage } from "../base/model/visu-page";
import { CategoryInstance } from "../base/model/categories";
import { DataService } from "../services/data.service";
import { AutomationsService } from "../services/automations.service";

@Component({
    selector: "app-visualization",
    templateUrl: "./visualization.component.html",
    styleUrls: ["./visualization.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class VisualizationComponent implements OnInit {

  menuItems = [];
  areas: AreaInstance[] = [];
  categories: CategoryInstance[] = [];
  values: any;

  constructor(private hubService: HubConnectionService,
    private areaService: AreaService,
    private catService: CategoryService,
    appService: AppService,
    private dataService: DataService,
    private automationsService: AutomationsService,
    private serverStateService: ServerStateService) {
    appService.setAppTitle("Automatica.Core");
  }

  async ngOnInit() {

    if (!await this.serverStateService.isStarted()) {
      this.serverStateService.init(false);
    }

    this.serverStateService.start();
    await this.hubService.init();

    const [areas, categories, automations] = await Promise.all(
      [
        this.areaService.getAreaInstances(),
        this.catService.getCategoryInstances(),
        // the menu entry only shows when the admins released automations for the visualization
        this.automationsService.getVisu().catch(() => [])
      ]);

    this.areas = areas;
    this.categories = categories;


    const menu = [];
    menu.push({
      text: "UI.NAV.HOME",
      icon: "house",
      path: "/visualization/page/home",
    });

    if (automations.length > 0) {
      menu.push({
        text: "UI.NAV.AUTOMATIONS",
        icon: "bolt",
        path: "/visualization/automations",
      });
    }

    const categoriesItem = {
      text: "CATEGORIES.NAME",
      group: true,
      items: []
    };

    for (const x of this.categories) {
      categoriesItem.items.push({
        text: x.DisplayName,
        path: "/visualization/page/category/" + x.ObjId,
        icon: x.Icon.replace("fas", "").trim()
      });
    }
    menu.push(categoriesItem);

    const areasItem = {
      text: "AREAS.NAME",
      group: true,
      items: []
    };


    var map = new Map<string, AreaInstance>();
    var menuMap = new Map<string, any>();
    var root = this.areas.filter(a => a.This2Parent == null)[0];

    this.areas.forEach(x => {
      map.set(x.ObjId, x);
      const item = {
        text: x.DisplayName,
        path: "/visualization/page/area/" + x.ObjId,
        icon: x.Icon?.replace("fas", "").trim(),
        expanded: false,
        items: []
      };
      menuMap.set(x.ObjId, item);

      if(x.This2Parent == root.ObjId) {
        areasItem.items.push(item);
      }
    });

    for (const x of areas) {
      if (!x.This2Parent) {
        continue;
      }
      const parent = menuMap.get(x.This2Parent);
      parent.items.push(menuMap.get(x.ObjId));
    }
    
    menu.push(areasItem);

    this.menuItems = menu;
  }

}
