import { Injectable } from "@angular/core";
import { L10nTranslationService } from "angular-l10n";
import { AutomationLookups } from "../automations/automation-lookups";

/** an entry of a select box, the text is the translation of key */
export interface ShadingOption<V = any> {
    value: V;
    key: string;
}

export const T = "UI.SHADING.";

const cache = new Map<string, ShadingOption[]>();

/**
 * Stable option lists for the select boxes, the text of each value is UI.SHADING.<group>.<VALUE>
 */
export function shadingOptions<V extends string | number | boolean>(group: string, values: V[], keyOf?: (v: V) => string): ShadingOption<V>[] {
    const id = group + ":" + values.join(",");
    let list = cache.get(id);
    if (!list) {
        list = values.map(v => ({ value: v, key: `${T}${group}.${keyOf ? keyOf(v) : String(v).toUpperCase()}` }));
        cache.set(id, list);
    }
    return list as ShadingOption<V>[];
}

/**
 * State shared by the shading page and its editors: datapoint names and the datapoint picker. Provided by
 * ShadingPageComponent.
 */
@Injectable()
export class ShadingContext {

    readonly lookups = new AutomationLookups();

    /** set by the page, opens the datapoint picker */
    nodePicker: (current: string) => Promise<string | undefined> = () => Promise.resolve(void 0);

    readonly optionText = (item: ShadingOption) => item ? this.translate.translate(item.key) : "";

    constructor(private translate: L10nTranslationService) {
    }

    t(key: string, params?: any): string {
        return this.translate.translate(T + key, params);
    }

    pickNode(current: string): Promise<string | undefined> {
        return this.nodePicker(current);
    }
}
