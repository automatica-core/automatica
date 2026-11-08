import { Directive } from "@angular/core";
import { BaseMobileRuleComponent } from "../../base-mobile-rule-component";
import { RuleInterfaceInstance } from "src/app/base/model/rule-interface-instance";
import { ClimateValues } from "./climate.model";

/**
 * Base of the climate tiles: keeps the current value of every interface of the logic by its key
 * ("tt", "status", "h", ...), so the tiles can read the blocks' interfaces by name.
 */
@Directive()
export abstract class ClimateRuleComponent extends BaseMobileRuleComponent {

  values: ClimateValues = {};
  private keyById = new Map<string, string>();

  protected get keys(): string[] {
    return Array.from(this.keyById.values());
  }

  protected interfaceOf(key: string): RuleInterfaceInstance | undefined {
    return this.getInterfaceByKey(key);
  }

  protected async climateInit() {
    this.baseOnInit();
    super.mobileRuleInit();

    for (const interf of this.ruleInstance?.Interfaces ?? []) {
      if (interf.Template?.Key) {
        this.keyById.set(interf.ObjId, interf.Template.Key);
        const current = this.dataHub.getCurrentValue(interf.ObjId)?.value;
        if (current !== undefined) {
          this.values[interf.Template.Key] = current;
        }
      }
    }
    this.onValuesChanged();

    const data = await this.ruleInstanceVisuService.getRuleInstanceData(this.ruleInstance.ObjId);
    if (data) {
      for (const [id, value] of Object.entries(data)) {
        this.onRuleInstanceValueChanged(id, value);
      }
    }
  }

  onRuleInstanceValueChanged(interfaceId, value) {
    const key = this.keyById.get(interfaceId) ?? this.keyById.get(String(interfaceId).toLowerCase());
    if (key === undefined) {
      return;
    }
    this.values = { ...this.values, [key]: value };
    this.onValuesChanged();
  }

  /** a value changed: recompute what the tile shows */
  protected abstract onValuesChanged(): void;

  protected async write(key: string, value: any): Promise<boolean> {
    const interf = this.interfaceOf(key);
    if (!interf) {
      return false;
    }
    try {
      await this.dataHub.setValue(interf.ObjId, value);
      return true;
    } catch (error) {
      this.notifyService.notifyError(this.translate.translate("UI.VISU.CLIMATE_WRITE_FAILED"));
      return false;
    }
  }
}
