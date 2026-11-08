import { Injectable } from "@angular/core";
import { LogicEditorInstanceService } from "src/app/services/logic-editor-instance.service";
import { ControlsService } from "src/app/services/controls.service";
import { AreaService } from "src/app/services/areas.service";
import { CategoryService } from "src/app/services/categories.service";
import { DataHubService } from "src/app/base/communication/hubs/data-hub.service";
import { AutomationDto } from "src/app/base/model/automations/automation-dto";
import { AutomationLookups } from "./automation-lookups";
import { OperatingModesService } from "src/app/services/operating-modes.service";
import { operatingModeName } from "src/app/base/model/operating-modes/operating-modes";
import { L10nTranslationService } from "angular-l10n";

/**
 * Loads the datapoints (with live values, through LogicEditorInstanceService), controls, areas and categories the
 * automation pages show by name. Call release() when the page closes.
 */
@Injectable()
export class AutomationLookupLoader {

    constructor(
        private nodeInstanceService: LogicEditorInstanceService,
        private controlsService: ControlsService,
        private areaService: AreaService,
        private categoryService: CategoryService,
        private dataHub: DataHubService,
        private operatingModesService: OperatingModesService,
        private translate: L10nTranslationService) {
    }

    async load(lookups: AutomationLookups, automations: AutomationDto[] = []): Promise<void> {
        const failures: any[] = [];
        const safe = async <R>(p: Promise<R>): Promise<R | undefined> => {
            try {
                return await p;
            } catch (e) {
                failures.push(e);
                return void 0;
            }
        };

        const [, controls, areas, categories, modes] = await Promise.all([
            safe(this.nodeInstanceService.load()),
            safe(this.controlsService.getAll()),
            safe(this.areaService.getAllAreaInstances()),
            safe(this.categoryService.getAllCategoryInstances()),
            safe(this.operatingModesService.getState())
        ]);

        lookups.setNodeResolver(id => this.nodeInstanceService.hasNodeInstance(id) ? this.nodeInstanceService.getNodeInstance(id) : void 0);
        lookups.setControls(controls || []);
        lookups.setAreas(areas || []);
        lookups.setCategories(categories || []);
        lookups.setAutomations(automations);
        lookups.setOperatingModes((modes?.modes || []).map(m => ({ id: m.key, name: operatingModeName(m, m.key, k => this.translate.translate(k)) })));

        if (failures.length) {
            // names are a convenience, the page still works with ids
            console.warn("automations: some names could not be loaded", failures);
        }
    }

    async release() {
        try {
            await this.dataHub.unSubscribeForAll();
        } catch (e) {
            console.warn(e);
        }
    }
}
