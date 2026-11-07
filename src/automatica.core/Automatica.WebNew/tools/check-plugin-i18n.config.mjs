// Configuration of tools/check-plugin-i18n.mjs (npm run test:i18n-plugins).
// Every entry says why it is here. Keep the list short: a key that the UI shows belongs in the resource files.

import fs from "node:fs";
import path from "node:path";

/** literals that look like keys but are no translation keys; { key: string | RegExp, plugin?: folder name, why } */
export const allow = [
    // rows of core lookup tables (NodeDataTypes, PropertyTemplateTypes, AreaTypes, Privileges): the frontend uses the
    // enum values and never shows these names, so they stay untranslated on purpose
    { key: /^NODE_DATA_TYPE\./, plugin: "automatica.core", why: "NodeDataType table names, not shown in the UI" },
    { key: /^PROPERTY_TEMPLATE_TYPE\./, plugin: "automatica.core", why: "PropertyTemplateType table names, not shown in the UI" },
    { key: /^AREA_TYPE\./, plugin: "automatica.core", why: "AreaType table names, not shown in the UI" },
    { key: /^PRIVILEDGE\./, plugin: "automatica.core", why: "Privilege table names, not shown in the UI" },
    // settings group: settings-property-instance.ts shows COMMON.CATEGORY.MISC for it
    { key: "SERVER.COMMON", plugin: "automatica.core", why: "mapped to COMMON.CATEGORY.MISC by the frontend" },
    // DNS TSIG algorithm name in the mDNS code
    { key: "HMAC-MD5.SIG-ALG.REG.INT", why: "TSIG algorithm name (RFC 2845), not a translation key" }
];

/** path fragments (relative to the repo root) whose .cs files are not scanned */
export const ignorePaths = [
    // code generator for EnOceanRorg*Data.cs: its templates hold "{0}" placeholders, the generated files are scanned
    "automatica.driver.enocean/EEPParser/"
];

const read = (dir, file) => fs.readFileSync(path.join(dir, file), "utf8");
const upper = key => key.replace(/-/g, "_").toUpperCase();

/**
 * Keys built at runtime from data the scanner cannot follow. keys(pluginDir) returns the full keys,
 * prefixes(pluginDir) the literals that are only key prefixes (they are not reported as keys themselves).
 */
export const dynamic = [
    {
        plugin: "automatica.driver.knx",
        why: "KnxDptCatalog entries: {NamePrefix}.NAME/.DESCRIPTION and {NamePrefix}.{FIELD}.NAME per child field",
        parse(dir) {
            const src = read(dir, "P3.Driver.Knx.DriverFactory/Dpt/KnxDptCatalog.cs");
            const entries = [];
            // new("knx-dpt15", 15, "KNX.DPT15", "guid", ... children: new[] { C("code", "guid"), ... })
            const re = /new\(\s*"knx-[^"]+"\s*,\s*\d+\s*,\s*"([A-Z0-9_.]+)"/g;
            const starts = [...src.matchAll(re)];
            starts.forEach((m, i) => {
                const body = src.slice(m.index, i + 1 < starts.length ? starts[i + 1].index : src.length);
                const fields = [...body.matchAll(/\bC\(\s*"([^"]+)"/g)].map(c => c[1]);
                entries.push({ prefix: m[1], fields });
            });
            return entries;
        },
        keys(dir) {
            return this.parse(dir).flatMap(e => [`${e.prefix}.NAME`, `${e.prefix}.DESCRIPTION`, ...e.fields.map(f => `${e.prefix}.${upper(f)}.NAME`)]);
        },
        prefixes(dir) {
            return this.parse(dir).map(e => e.prefix);
        }
    },
    {
        plugin: "automatica.driver.matter",
        why: "MatterDeviceTemplates: MATTER_BRIDGE.DEVICE.{kind}.NAME/.DESCRIPTION and MATTER_BRIDGE.POINT.{point}.NAME/.DESCRIPTION",
        keys(dir) {
            const src = read(dir, "P3.Driver.Matter/MatterDeviceKinds.cs");
            const kinds = [...src.matchAll(/MatterDeviceKind\s+\w+\s*=\s*new\(\s*"([^"]+)"/g)].map(m => m[1]);
            const points = new Set([
                ...[...src.matchAll(/new MatterDataPoint\(\s*"([^"]+)"/g)].map(m => m[1]),
                ...[...src.matchAll(/MatterDataPoint\s+\w+\(\)\s*=>\s*new\(\s*"([^"]+)"/g)].map(m => m[1]),
                ...[...src.matchAll(/const string \w+Point\s*=\s*"([^"]+)"/g)].map(m => m[1])
            ]);
            return [
                ...kinds.flatMap(k => [`MATTER_BRIDGE.DEVICE.${upper(k)}.NAME`, `MATTER_BRIDGE.DEVICE.${upper(k)}.DESCRIPTION`]),
                ...[...points].flatMap(p => [`MATTER_BRIDGE.POINT.${upper(p)}.NAME`, `MATTER_BRIDGE.POINT.${upper(p)}.DESCRIPTION`])
            ];
        }
    }
];
