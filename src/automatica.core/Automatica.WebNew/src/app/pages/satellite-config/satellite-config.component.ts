import { Component, OnInit, OnDestroy, ChangeDetectionStrategy } from "@angular/core";
import { L10nTranslationService } from "angular-l10n";
import { Guid } from "src/app/base/utils/Guid";
import { NotifyService } from "src/app/services/notify.service";
import { AppService } from "src/app/services/app.service";
import { BaseComponent } from "src/app/base/base-component";
import { Satellite } from "src/app/base/model/satellites/satellite";
import { SatelliteService } from "src/app/services/satellite.services";
import { confirm } from "devextreme/ui/dialog";

@Component({
    selector: "app-satellite-config",
    templateUrl: "./satellite-config.component.html",
    styleUrls: ["./satellite-config.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class SatelliteConfigComponent extends BaseComponent implements OnInit, OnDestroy {
  satellites: Satellite[] = [];
  selectedSatellite: Satellite = void 0;

  search = "";
  loaded = false;
  /** the list is saved as a whole, adds and deletes wait for "Save" */
  isDirty = false;

  createVisible = false;
  newName = "";
  createError: string = void 0;

  constructor(
    private catService: SatelliteService,
    translate: L10nTranslationService,
    private notify: NotifyService,
    appService: AppService) {
    super(notify, translate, appService);

    appService.setAppTitle("COMMON.SATELLITES");
  }

  async ngOnInit() {
    this.load();
  }

  ngOnDestroy() {
    super.baseOnDestroy();
  }

  get onlineCount() {
    return this.satellites.filter(a => a.Connected).length;
  }

  get filteredSatellites(): Satellite[] {
    const q = (this.search || "").trim().toLowerCase();
    return this.satellites
      .filter(s => !q || [s.Name, s.Description, s.ClientId].some(v => (v || "").toLowerCase().includes(q)))
      .sort((a, b) => (a.Name || "").localeCompare(b.Name || ""));
  }

  async load() {
    const loadingDone = this.appService.beginLoading();

    try {
      const selected = this.selectedSatellite?.ObjId;
      this.satellites = await this.catService.getAll();
      this.selectedSatellite = selected ? this.satellites.find(a => a.ObjId === selected) : void 0;
      this.isDirty = false;
    } catch (error) {
      this.handleError(error);
    } finally {
      this.loaded = true;
      loadingDone();
    }
  }

  async save() {
    const loadingDone = this.appService.beginLoading();

    try {
      // only report "saved" once the server confirmed it
      await this.catService.save(this.satellites);
      this.isDirty = false;
      this.notify.notifySuccess("COMMON.SAVED");
    } catch (error) {
      super.handleError(error);
    } finally {
      loadingDone();
    }
  }

  select(satellite: Satellite) {
    this.selectedSatellite = satellite;
  }

  closeEditor() {
    this.selectedSatellite = void 0;
  }

  onChanged($event) {
    // edits in the inspector change the object, the list is saved with "Save"
    this.isDirty = true;
  }

  async deleteSelected() {
    const satellite = this.selectedSatellite;
    if (!satellite) {
      return;
    }
    const message = this.translate.translate("UI.SATELLITES.DELETE_CONFIRM", { name: satellite.Name || "" });
    const ok = await confirm(message, this.translate.translate("UI.SATELLITES.DELETE_TITLE"));
    if (!ok) {
      return;
    }
    this.satellites = this.satellites.filter(a => a !== satellite);
    this.selectedSatellite = void 0;
    this.isDirty = true;
  }

  openCreate() {
    this.newName = "";
    this.createError = void 0;
    this.createVisible = true;
  }

  create() {
    const name = (this.newName || "").trim();
    if (!name) {
      this.createError = this.translate.translate("UI.SATELLITES.NAME_REQUIRED");
      return;
    }
    const newObject = new Satellite();
    newObject.ObjId = Guid.MakeNew().ToString();
    newObject.Name = name;
    newObject.addVirtualProperties();

    this.satellites = [...this.satellites, newObject];
    this.selectedSatellite = newObject;
    this.isDirty = true;
    this.createVisible = false;
  }
}
