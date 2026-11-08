#!/usr/bin/env node
// Translation check for the driver and logic plugins (npm run test:i18n-plugins).
//
// Every plugin brings Resources/<Name>-en.json and <Name>-de.json as embedded resources; the core loads them through the
// LocalizationProvider (server side: node names, scan results, notifications) and serves them to the frontend
// (GET /webapi/localization/{lang}). A key that is missing shows up raw in the UI.
//
// For every plugin folder under src/automatica.drivers and src/automatica.logics this script
//  1. collects the translation keys the C# code uses: string literals that are keys as a whole ("TIMES.SUN.NAME",
//     "MBUS-OMS.NAME"), keys inside ENUM(...) meta strings, and interpolated keys ($"SONOS.DEVICE.{name.ToUpperInvariant()}.NAME")
//     whose holes are expanded from the literal arguments of the calls of the enclosing method or from for-loop bounds;
//  2. checks each key against the plugin's own en and de resource files, falling back to the core keys
//     (Automatica.Core.Runtime/Resources and the frontend locale files under src/assets/locale);
//  3. reports keys that exist in only one language of a plugin resource pair, keys that are a string in one place
//     and an object in another, and top-level keys two plugins share (the core keeps only the first plugin's subtree);
//  4. exits with 1 when anything is missing.
// Allowlist and dynamic-key expansions: tools/check-plugin-i18n.config.mjs. The resource files are only read.
//
//   node tools/check-plugin-i18n.mjs            report problems
//   node tools/check-plugin-i18n.mjs --verbose  also list unresolved dynamic keys and per-plugin counts
//   node tools/check-plugin-i18n.mjs --json     machine readable result

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

const toolsDir = path.dirname(fileURLToPath(import.meta.url));
const appRoot = path.resolve(toolsDir, "..");
const repoRoot = path.resolve(appRoot, "..", "..", "..");

const SEG = "[A-Z0-9][A-Z0-9_-]*";
const KEY_RE = new RegExp(`^[A-Z][A-Z0-9_-]*(?:\\.${SEG})+$`);
const HOLE = "\u0000";

// folders that never ship in a plugin package
const SKIP_DIR = /^(bin|obj|node_modules|\.git|\.vs|tests?|unittests?)$|(\.|^)(Tests?|UnitTests|Console|ConsoleApp|ConsoleTest)$|ConsoleTest$/i;

function walk(dir, filter, out = []) {
    for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
        const full = path.join(dir, entry.name);
        if (entry.isDirectory()) {
            if (!SKIP_DIR.test(entry.name)) {
                walk(full, filter, out);
            }
        } else if (filter(full)) {
            out.push(full);
        }
    }
    return out;
}

function rel(file) {
    return path.relative(repoRoot, file).split(path.sep).join("/");
}

const lineStarts = new Map();

function lineOf(source, index) {
    let starts = lineStarts.get(source);
    if (!starts) {
        starts = [0];
        for (let i = 0; i < source.length; i++) {
            if (source.charCodeAt(i) === 10) starts.push(i + 1);
        }
        lineStarts.set(source, starts);
    }
    let lo = 0;
    let hi = starts.length - 1;
    while (lo < hi) {
        const mid = (lo + hi + 1) >> 1;
        if (starts[mid] <= index) lo = mid;
        else hi = mid - 1;
    }
    return lo + 1;
}

// ---------------------------------------------------------------- C# string literals

/**
 * All string literals of a C# source with comments skipped: { value, index, interpolated, holes: [expr] }.
 * For interpolated strings the holes are replaced by HOLE in value; holes[] holds their expressions in order.
 * Raw string literals ("""...""") are returned as plain strings.
 */
export function csharpStrings(src) {
    const out = [];
    let i = 0;
    const n = src.length;
    while (i < n) {
        const c = src[i];
        const next = src[i + 1];
        if (c === "/" && next === "/") {
            while (i < n && src[i] !== "\n") i++;
            continue;
        }
        if (c === "/" && next === "*") {
            const end = src.indexOf("*/", i + 2);
            i = end < 0 ? n : end + 2;
            continue;
        }
        if (c === "'") {
            // char literal
            i++;
            while (i < n && src[i] !== "'") {
                if (src[i] === "\\") i++;
                i++;
            }
            i++;
            continue;
        }
        // prefix: $, @, $@, @$, $$...
        let j = i;
        let dollar = 0;
        let verbatim = false;
        while (src[j] === "$" || src[j] === "@") {
            if (src[j] === "$") dollar++;
            else verbatim = true;
            j++;
        }
        if (src[j] !== "\"" || (j > i && /[A-Za-z0-9_]/.test(src[i - 1] ?? ""))) {
            if (j > i) {
                i = j;
                continue;
            }
            i++;
            continue;
        }
        const start = i;
        // raw string literal
        if (src.startsWith("\"\"\"", j)) {
            let q = 0;
            while (src[j + q] === "\"") q++;
            const fence = "\"".repeat(q);
            const end = src.indexOf(fence, j + q);
            const body = src.slice(j + q, end < 0 ? n : end);
            out.push({ value: body, index: start, interpolated: false, holes: [] });
            i = end < 0 ? n : end + q;
            continue;
        }
        j++; // opening quote
        let value = "";
        const holes = [];
        while (j < n) {
            const ch = src[j];
            if (verbatim) {
                if (ch === "\"") {
                    if (src[j + 1] === "\"") {
                        value += "\"";
                        j += 2;
                        continue;
                    }
                    j++;
                    break;
                }
            } else {
                if (ch === "\\") {
                    value += src[j + 1];
                    j += 2;
                    continue;
                }
                if (ch === "\"" || ch === "\n") {
                    j++;
                    break;
                }
            }
            if (dollar > 0 && ch === "{") {
                if (src[j + 1] === "{" && dollar === 1) {
                    value += "{";
                    j += 2;
                    continue;
                }
                // hole: balanced braces, strings inside are skipped roughly
                let depth = 0;
                let k = j;
                for (; k < n; k++) {
                    const h = src[k];
                    if (h === "{") depth++;
                    else if (h === "}") {
                        depth--;
                        if (depth === 0) break;
                    } else if (h === "\"") {
                        k++;
                        while (k < n && src[k] !== "\"") {
                            if (src[k] === "\\") k++;
                            k++;
                        }
                    }
                }
                let expr = src.slice(j + 1, k);
                // format / alignment part
                expr = expr.replace(/^([^:"]*?)(?:,\s*-?\d+)?(?::[^"]*)?$/, "$1").trim();
                holes.push(expr);
                value += HOLE;
                j = k + 1;
                continue;
            }
            if (dollar > 0 && ch === "}" && src[j + 1] === "}") {
                value += "}";
                j += 2;
                continue;
            }
            value += ch;
            j++;
        }
        out.push({ value, index: start, interpolated: dollar > 0, holes });
        i = j;
    }
    return out;
}

/** "a, f(b, c), \"d,e\"" -> ["a", "f(b, c)", "\"d,e\""] */
export function splitArgs(text) {
    const args = [];
    let depth = 0;
    let cur = "";
    for (let i = 0; i < text.length; i++) {
        const ch = text[i];
        if (ch === "\"" || ch === "'") {
            let j = i + 1;
            while (j < text.length && text[j] !== ch) {
                if (text[j] === "\\") j++;
                j++;
            }
            cur += text.slice(i, j + 1);
            i = j;
            continue;
        }
        if ("([{<".includes(ch) && !(ch === "<" && !/\w/.test(text[i - 1] ?? ""))) depth++;
        if (")]}>".includes(ch) && !(ch === ">" && depth === 0)) depth--;
        if (ch === "," && depth === 0) {
            args.push(cur.trim());
            cur = "";
            continue;
        }
        cur += ch;
    }
    if (cur.trim()) args.push(cur.trim());
    return args;
}

/** the text between the parenthesis at open and its partner */
function balanced(src, open) {
    let depth = 0;
    for (let i = open; i < src.length; i++) {
        const ch = src[i];
        if (ch === "\"") {
            i++;
            while (i < src.length && src[i] !== "\"") {
                if (src[i] === "\\") i++;
                i++;
            }
            continue;
        }
        if (ch === "(") depth++;
        else if (ch === ")") {
            depth--;
            if (depth === 0) return src.slice(open + 1, i);
        }
    }
    return null;
}

const METHOD_DECL = /(?:^|[\s;{}\]])(?:(?:public|private|protected|internal|static|override|virtual|async|sealed|new|unsafe|extern|partial)\s+)*[\w<>\[\],.?]+\s+(\w+)\s*\(([^()]*(?:\([^()]*\)[^()]*)*)\)\s*(?:where[^{]*)?(?:\{|=>)/g;

/** the method declaration that encloses index: { name, params: [name] } */
export function enclosingMethod(src, index) {
    let found = null;
    METHOD_DECL.lastIndex = 0;
    for (const m of src.matchAll(METHOD_DECL)) {
        if (m.index > index) break;
        if (["if", "for", "foreach", "while", "switch", "using", "lock", "catch", "return", "new", "nameof"].includes(m[1])) continue;
        found = { name: m[1], params: splitArgs(m[2]).map(p => p.replace(/=.*$/, "").trim().split(/\s+/).pop()), index: m.index };
    }
    return found;
}

function stringLiteral(arg) {
    const m = /^@?"((?:[^"\\]|\\.)*)"$/.exec(arg.trim());
    return m ? m[1] : null;
}

/** literal values a parameter gets at the call sites of method in files */
export function argumentValues(files, method, paramIndex) {
    const values = new Set();
    let unresolved = false;
    // attributes are applied without their suffix: [AreaTemplate(...)] calls AreaTemplateAttribute(...)
    const names = method.endsWith("Attribute") && method !== "Attribute" ? `(?:${method}|${method.slice(0, -"Attribute".length)})` : method;
    const call = new RegExp(`\\b${names}\\s*\\(`, "g");
    for (const f of files) {
        for (const m of f.source.matchAll(call)) {
            // skip the declaration itself: "private void Name(" / "public Name(" at the start of a line, but not "new Name("
            const lineStart = f.source.lastIndexOf("\n", m.index) + 1;
            const prefix = f.source.slice(lineStart, m.index);
            if (/^\s*(?:(?:public|private|protected|internal|static|override|virtual|async|sealed|unsafe|extern|partial)\s+)*[\w<>\[\],.?]+\s+$/.test(prefix)
                && !/^\s*(?:new|return|await|yield|throw|else|case|in|out|ref)\s+$/.test(prefix)) {
                continue;
            }
            const inner = balanced(f.source, m.index + m[0].length - 1);
            if (inner == null) continue;
            const args = splitArgs(inner);
            const lit = args[paramIndex] != null ? stringLiteral(args[paramIndex]) : null;
            if (lit == null) unresolved = true;
            else values.add(lit);
        }
    }
    return { values: [...values], unresolved };
}

function intConstant(src, name) {
    if (/^\d+$/.test(name)) return Number(name);
    const m = new RegExp(`\\b${name}\\s*(?:=>|=)\\s*(\\d+)\\s*;`).exec(src);
    return m ? Number(m[1]) : null;
}

/** values of a for-loop variable declared before index: for (var i = 1; i < Max; i++) */
function loopValues(src, index, variable) {
    const re = new RegExp(`for\\s*\\(\\s*(?:var|int)\\s+${variable}\\s*=\\s*(\\w+)\\s*;\\s*${variable}\\s*(<=?)\\s*(\\w+)\\s*;\\s*${variable}\\s*\\+\\+\\s*\\)`, "g");
    let last = null;
    for (const m of src.matchAll(re)) {
        if (m.index < index) last = m;
    }
    if (!last) return null;
    const from = intConstant(src, last[1]);
    let to = intConstant(src, last[3]);
    if (from == null || to == null) return null;
    if (last[2] === "<") to--;
    const values = [];
    for (let v = from; v <= to; v++) values.push(String(v));
    return values;
}

/** possible values of an interpolation hole, or null */
function holeValues(hole, file, files, index) {
    let expr = hole.trim();
    let transform = v => v;
    let m;
    if ((m = /^(\w+)\.ToUpper(?:Invariant)?\(\)$/.exec(expr))) {
        expr = m[1];
        transform = v => v.toUpperCase();
    } else if ((m = /^(\w+)\.ToLower(?:Invariant)?\(\)$/.exec(expr))) {
        expr = m[1];
        transform = v => v.toLowerCase();
    } else if ((m = /^Upper\((\w+)\)$/.exec(expr))) {
        expr = m[1];
        transform = v => v.replace(/-/g, "_").toUpperCase();
    }
    if (!/^\w+$/.test(expr)) return null;

    const loop = loopValues(file.source, index, expr);
    if (loop) return loop.map(transform);

    const method = enclosingMethod(file.source, index);
    if (method) {
        const p = method.params.indexOf(expr);
        if (p >= 0) {
            // calls in the same file first: helpers like Param(...) exist with the same name in many factories
            const local = argumentValues([file], method.name, p);
            const { values } = local.values.length > 0 ? local : argumentValues(files, method.name, p);
            if (values.length > 0) return values.map(transform);
        }
    }

    // const string Prefix = "LIGHTNING.LIGHTING_CONTROLLER"; in the same file
    const constant = new RegExp(`\\bconst\\s+string\\s+${expr}\\s*=\\s*"([^"\\\\]*)"\\s*;`).exec(file.source);
    if (constant) return [transform(constant[1])];
    return null;
}

/**
 * values of a property that a constructor sets from one of its parameters (NamePrefix = namePrefix;), taken from the
 * literal arguments of the `new Type(...)` calls: { values, prefix } or null
 */
export function propertyValues(hole, files) {
    const m = /^(?:\w+\.)?([A-Z]\w*)$/.exec(hole.trim());
    if (!m) return null;
    const prop = m[1];
    // several classes may have a property of that name (HomeKit folders and data points): take them all
    const all = new Set();
    for (const f of files) {
        for (const a of f.source.matchAll(new RegExp(`\\b${prop}\\s*=\\s*(\\w+)\\s*;`, "g"))) {
            const ctor = enclosingMethod(f.source, a.index);
            if (!ctor) continue;
            const p = ctor.params.indexOf(a[1]);
            if (p < 0) continue;
            argumentValues(files, ctor.name, p).values.forEach(v => all.add(v));
        }
    }
    return all.size > 0 ? [...all] : null;
}

function expand(value, holeSets) {
    let results = [""];
    let h = 0;
    for (const part of value.split(HOLE)) {
        results = results.map(r => r + part);
        if (h < holeSets.length) {
            const set = holeSets[h++];
            results = results.flatMap(r => set.map(v => r + v));
        }
    }
    return results;
}

/** a key with holes: static parts must look like a key */
function looksLikeDynamicKey(value) {
    if (!value.includes(HOLE)) return false;
    const probe = value.split(HOLE).join("X");
    if (!KEY_RE.test(probe.startsWith("X") ? "X" + probe.slice(1) : probe)) return false;
    // at least one static upper-case segment
    return /[A-Z]{2,}/.test(value.split(HOLE).join(""));
}

// ---------------------------------------------------------------- resources

/**
 * The core reads the embedded resources as UTF-8: a file saved as Latin-1 shows "Ger�t" in the UI,
 * so invalid UTF-8 and replacement characters are errors (thrown like a parse error).
 */
function readJson(file) {
    let text;
    try {
        text = new TextDecoder("utf-8", { fatal: true }).decode(fs.readFileSync(file));
    } catch {
        throw new Error("not UTF-8 (save the file as UTF-8, the core reads it as UTF-8)");
    }
    if (text.includes("�")) {
        throw new Error("contains the replacement character � (a broken umlaut)");
    }
    return JSON.parse(text.replace(/^﻿/, ""));
}

/** "A": { "B": "text" } -> A.B ; object nodes are remembered too so string/object clashes can be reported */
export function flatten(json, prefix = "", out = new Map(), objects = new Set()) {
    for (const [name, value] of Object.entries(json)) {
        const key = prefix ? `${prefix}.${name}` : name;
        if (value !== null && typeof value === "object") {
            objects.add(key);
            flatten(value, key, out, objects);
        } else {
            out.set(key, value);
        }
    }
    return { keys: out, objects };
}

function loadPair(files) {
    const result = { en: new Map(), de: new Map(), objects: { en: new Set(), de: new Set() }, pairs: [], topLevel: new Set() };
    const byBase = new Map();
    for (const file of files) {
        const m = /^(.*)-(en|de)\.json$/i.exec(file);
        if (!m) continue;
        const lang = m[2].toLowerCase();
        let json;
        try {
            json = readJson(file);
        } catch (e) {
            result.parseErrors = [...(result.parseErrors ?? []), `${rel(file)}: ${e.message}`];
            continue;
        }
        const { keys, objects } = flatten(json);
        for (const [k, v] of keys) result[lang].set(k, { value: v, file });
        for (const o of objects) result.objects[lang].add(o);
        Object.keys(json).forEach(k => result.topLevel.add(k));
        byBase.set(m[1], { ...byBase.get(m[1]), [lang]: { file, keys } });
    }
    result.pairs = [...byBase.entries()];
    return result;
}

function loadCore() {
    const files = [
        ...walk(path.join(repoRoot, "src", "automatica.core", "Automatica.Core.Runtime", "Resources"), f => f.endsWith(".json")),
        ...walk(path.join(appRoot, "src", "assets", "locale"), f => f.endsWith(".json"))
    ];
    return loadPair(files);
}

// ---------------------------------------------------------------- plugins

function pluginFolders() {
    const roots = ["automatica.drivers", "automatica.logics"].map(r => path.join(repoRoot, "src", r));
    const plugins = [];
    for (const root of roots) {
        for (const entry of fs.readdirSync(root, { withFileTypes: true })) {
            if (entry.isDirectory() && !entry.name.startsWith(".")) {
                plugins.push(path.join(root, entry.name));
            }
        }
    }
    plugins.sort();
    // the core itself: its own templates (visualization objects, areas, settings) use keys from the same pool
    plugins.push(path.join(repoRoot, "src", "automatica.core"));
    return plugins;
}

/** files below dir; for the core the frontend app is left out (its keys are checked by check-templates.mjs) */
function pluginFiles(dir, filter) {
    if (path.basename(dir) !== "automatica.core") return walk(dir, filter);
    return fs.readdirSync(dir, { withFileTypes: true })
        .filter(e => e.isDirectory() && e.name !== "Automatica.WebNew" && !SKIP_DIR.test(e.name))
        .flatMap(e => walk(path.join(dir, e.name), filter));
}

/**
 * CreatePropertyTemplate(uid, name, description, key, type, objectRef, group, isVisible, ...) with isVisible = false:
 * the property editor never shows it, so its name and description need no translation (the generated EnOcean
 * bit offset / length / range properties). Returns the call spans and the name/description literals to skip.
 */
function hiddenPropertySpans(src) {
    const spans = [];
    for (const m of src.matchAll(/\bCreatePropertyTemplate\s*\(/g)) {
        const open = m.index + m[0].length - 1;
        const inner = balanced(src, open);
        if (inner == null) continue;
        const args = splitArgs(inner);
        if (args.length >= 8 && args[7].trim() === "false") {
            spans.push({ start: open, end: open + inner.length + 1, keys: [args[1], args[2]].map(stringLiteral).filter(Boolean) });
        }
    }
    return spans;
}

function addUse(map, key, where, extra = {}) {
    if (!map.has(key)) map.set(key, { places: new Set(), ...extra });
    map.get(key).places.add(where);
}

function isAllowed(config, key, pluginName) {
    return config.allow.some(a => {
        const scopeOk = !a.plugin || a.plugin === pluginName;
        const keyOk = a.key instanceof RegExp ? a.key.test(key) : a.key === key;
        return scopeOk && keyOk;
    });
}

export function scanPlugin(dir, config) {
    const name = path.basename(dir);
    const ignore = config.ignorePaths.map(p => p.replace(/\\/g, "/"));
    const csFiles = pluginFiles(dir, f => f.endsWith(".cs"))
        .filter(f => !ignore.some(p => rel(f).includes(p)))
        .map(f => ({ file: f, source: fs.readFileSync(f, "utf8") }));
    const resourceFiles = pluginFiles(dir, f => /[\\/]Resources[\\/][^\\/]+-(en|de)\.json$/i.test(f));

    const used = new Map();
    const unresolved = [];
    const prefixes = new Set();

    for (const f of csFiles) {
        const hidden = hiddenPropertySpans(f.source);
        let h = 0;
        for (const lit of csharpStrings(f.source)) {
            while (h < hidden.length && hidden[h].end < lit.index) h++;
            if (h < hidden.length && lit.index >= hidden[h].start && lit.index <= hidden[h].end && hidden[h].keys.includes(lit.value)) {
                continue;
            }
            const where = `${rel(f.file)}:${lineOf(f.source, lit.index)}`;
            if (!lit.interpolated || lit.holes.length === 0) {
                const v = lit.value;
                if (KEY_RE.test(v)) {
                    addUse(used, v, where);
                } else if (v.startsWith("ENUM(") && v.endsWith(")")) {
                    for (const item of v.slice(5, -1).split(";")) {
                        const label = item.split(",")[1]?.trim();
                        if (label && KEY_RE.test(label)) addUse(used, label, where);
                    }
                }
                continue;
            }
            if (!looksLikeDynamicKey(lit.value)) continue;
            const sets = lit.holes.map(h => holeValues(h, f, csFiles, lit.index));
            if (lit.holes.length === 1 && !sets[0]) {
                // $"{NamePrefix}.NAME": the prefixes passed to the constructor are no keys of their own
                sets[0] = propertyValues(lit.holes[0], csFiles);
            }
            if (sets[0] && lit.value.startsWith(HOLE)) {
                // $"{key}.NAME" with key = "MATTER_BRIDGE.QR_CODE": the passed literal is only a prefix
                sets[0].forEach(v => prefixes.add(v));
            }
            if (sets.every(s => s)) {
                for (const k of expand(lit.value, sets)) {
                    if (KEY_RE.test(k)) addUse(used, k, where, { dynamic: true });
                }
            } else {
                const pattern = lit.value.split(HOLE).map((part, idx) => idx < lit.holes.length ? `${part}{${lit.holes[idx]}}` : part).join("");
                unresolved.push({ pattern, where, regex: new RegExp("^" + lit.value.split(HOLE).map(p => p.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")).join("[A-Za-z0-9_.-]+") + "$") });
            }
        }
    }

    // configured expansions for keys the scanner cannot resolve (values computed at runtime)
    for (const d of config.dynamic.filter(d => d.plugin === name)) {
        for (const k of d.keys(dir)) addUse(used, k, `config: ${d.why}`, { dynamic: true });
        for (const p of d.prefixes?.(dir) ?? []) prefixes.add(p);
    }

    const resources = loadPair(resourceFiles);
    for (const p of prefixes) {
        if (used.has(p) && !used.get(p).dynamic && !resources.en.has(p) && !resources.de.has(p)) {
            used.delete(p);
        }
    }

    return { name, dir, used, unresolved, resources };
}

export async function loadConfig() {
    const file = path.join(toolsDir, "check-plugin-i18n.config.mjs");
    if (!fs.existsSync(file)) return { allow: [], ignorePaths: [], dynamic: [] };
    const mod = await import(pathToFileURL(file).href);
    return { allow: mod.allow ?? [], ignorePaths: mod.ignorePaths ?? [], dynamic: mod.dynamic ?? [] };
}

export async function check() {
    const config = await loadConfig();
    const core = loadCore();
    const errors = [];
    const warnings = [];
    const stats = [];
    const topLevelOwners = new Map();


    // every plugin's keys end up in one pool; the core's own copies of plugin code (src/automatica.core/Drivers) use it
    const pool = { en: new Set(), de: new Set() };

    for (const dir of pluginFolders()) {
        const plugin = scanPlugin(dir, config);
        const res = plugin.resources;
        const missing = { en: [], de: [] };
        const isCore = plugin.name === "automatica.core";
        if (res.parseErrors) errors.push(...res.parseErrors.map(e => `parse error: ${e}`));

        for (const [key, use] of [...plugin.used].sort((a, b) => a[0].localeCompare(b[0]))) {
            if (isAllowed(config, key, plugin.name)) continue;
            for (const lang of ["en", "de"]) {
                if (!res[lang].has(key) && !core[lang].has(key) && !(isCore && pool[lang].has(key))) {
                    missing[lang].push(key);
                    const clash = res.objects[lang].has(key) ? " (exists as an object, the key needs a string)" : "";
                    errors.push(`${plugin.name}: missing in ${lang}: ${key}${clash}  (${[...use.places].slice(0, 2).join(", ")}${use.places.size > 2 ? ", ..." : ""})`);
                }
            }
        }

        for (const [base, langs] of res.pairs) {
            if (!langs.en || !langs.de) {
                errors.push(`${rel(base)}: only ${langs.en ? "en" : "de"} exists`);
                continue;
            }
            for (const [k, v] of langs.en.keys) {
                if (!langs.de.keys.has(k) && !isAllowed(config, k, plugin.name)) errors.push(`${plugin.name}: only in en: ${k}  (${rel(base)}-de.json lacks it)`);
                // German text copied into the en file: umlauts never occur in the English texts
                if (typeof v === "string" && /[äöüÄÖÜß]/.test(v) && !isAllowed(config, k, plugin.name)) {
                    errors.push(`${plugin.name}: German text in en: ${k} = "${v}"  (${rel(base)}-en.json)`);
                }
            }
            for (const k of langs.de.keys.keys()) {
                if (!langs.en.keys.has(k) && !isAllowed(config, k, plugin.name)) errors.push(`${plugin.name}: only in de: ${k}  (${rel(base)}-en.json lacks it)`);
            }
        }

        for (const u of plugin.unresolved) {
            const hit = ["en", "de"].map(lang => [...res[lang].keys(), ...core[lang].keys()].some(k => u.regex.test(k)));
            if (!hit[0] || !hit[1]) {
                if (!isAllowed(config, u.pattern, plugin.name)) {
                    errors.push(`${plugin.name}: dynamic key ${u.pattern} matches no ${!hit[0] ? "en" : "de"} key  (${u.where})`);
                }
            } else if (!config.dynamic.some(d => d.plugin === plugin.name)) {
                warnings.push(`${plugin.name}: dynamic key not expanded, only checked by pattern: ${u.pattern}  (${u.where})`);
            }
        }

        for (const lang of ["en", "de"]) {
            for (const k of res[lang].keys()) pool[lang].add(k);
        }

        for (const t of res.topLevel) {
            topLevelOwners.set(t, [...(topLevelOwners.get(t) ?? []), plugin.name]);
        }

        stats.push({ plugin: plugin.name, used: plugin.used.size, missingEn: missing.en.length, missingDe: missing.de.length, unresolved: plugin.unresolved.length });
    }

    for (const [top, owners] of topLevelOwners) {
        if (owners.length > 1 && !isAllowed(config, `top-level:${top}`, null)) {
            errors.push(`top-level key ${top} is defined by ${owners.join(" and ")}: the core keeps only the subtree loaded first, the other plugin's keys stay raw`);
        }
    }

    return { errors, warnings, stats };
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
    const verbose = process.argv.includes("--verbose");
    const json = process.argv.includes("--json");
    const result = await check();
    if (json) {
        console.log(JSON.stringify(result, null, 2));
    } else {
        if (verbose) {
            for (const s of result.stats) {
                console.log(`${s.plugin.padEnd(48)} ${String(s.used).padStart(5)} keys  missing en ${s.missingEn}, de ${s.missingDe}${s.unresolved ? `, ${s.unresolved} dynamic by pattern` : ""}`);
            }
            if (result.warnings.length) console.log("\n" + result.warnings.join("\n"));
            console.log("");
        }
        if (result.errors.length > 0) {
            console.error(result.errors.join("\n"));
        }
    }
    const used = result.stats.reduce((a, s) => a + s.used, 0);
    if (result.errors.length > 0) {
        console.error(`\n${result.errors.length} plugin translation problem(s) (${used} keys checked in ${result.stats.length} plugins)`);
        process.exit(1);
    }
    console.log(`plugin translation keys ok: ${used} keys in ${result.stats.length} plugins, en and de complete`);
}
