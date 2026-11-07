import { T } from "./automation-describer";

/** an entry of a select box, the text is the translation of key */
export interface OptionItem<V = any> {
    value: V;
    key: string;
}

const cache = new Map<string, OptionItem[]>();

/**
 * Stable option lists for the select boxes (the same array on every change detection), the text of each value is
 * UI.AUTOMATIONS.<group>.<VALUE>.
 */
export function options<V extends string | number | boolean>(group: string, values: V[], keyOf?: (v: V) => string): OptionItem<V>[] {
    const id = group + ":" + values.join(",");
    let list = cache.get(id);
    if (!list) {
        list = values.map(v => ({ value: v, key: `${T}${group}.${keyOf ? keyOf(v) : String(v).toUpperCase()}` }));
        cache.set(id, list);
    }
    return list as OptionItem<V>[];
}
