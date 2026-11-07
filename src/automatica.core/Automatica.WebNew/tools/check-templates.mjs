#!/usr/bin/env node
// Static checks of the templates that the karma specs cannot see (npm run test:i18n):
//  1. every translation key a template uses with a literal ('KEY' | translateAsync, "KEY" | translate, (a ? 'A' : 'B') | translateAsync)
//     exists in the English and the German locale files under src/assets/locale
//  2. the keys the code passes literally (translate("KEY"), setAppTitle, setError incl. its _HINT, notify*, the navigation texts) exist too
//  3. English and German have the same key set, file by file
//  4. every <as-split> stacks on phones: it binds [direction]="viewport.splitDirection"
// The locale files are only read, never written.

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const appRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const KEY = "[A-Z][A-Z0-9_]*(?:\\.[A-Z0-9_]+)+";

function walk(dir, out = []) {
    for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
        const full = path.join(dir, entry.name);
        if (entry.isDirectory()) {
            walk(full, out);
        } else {
            out.push(full);
        }
    }
    return out;
}

/** paths in the messages are relative to the checked app */
let reportRoot = appRoot;

function rel(file) {
    return path.relative(reportRoot, file).split(path.sep).join("/");
}

function lineOf(source, index) {
    return source.slice(0, index).split("\n").length;
}

/** the template parts of a file: the whole .html, or the inline `template:` strings of a .ts */
function templatesOf(file) {
    const source = fs.readFileSync(file, "utf8");
    if (file.endsWith(".html")) {
        return [{ source, offset: 0 }];
    }
    const parts = [];
    for (const m of source.matchAll(/template\s*:\s*`([\s\S]*?)`/g)) {
        parts.push({ source: m[1], offset: m.index + m[0].indexOf("`") + 1, whole: source });
    }
    return parts;
}

function add(map, key, where) {
    if (!map.has(key)) {
        map.set(key, new Set());
    }
    map.get(key).add(where);
}

/** keys used with a translate pipe in the templates */
export function templateKeys(files) {
    const keys = new Map();
    const literal = new RegExp(`(['"])(${KEY})\\1\\s*\\|\\s*translate(?:Async)?\\b`, "g");
    const grouped = /\(([^()]*?)\)\s*\|\s*translate(?:Async)?\b/g;
    const quoted = new RegExp(`(['"])(${KEY})\\1`, "g");

    for (const file of files) {
        for (const part of templatesOf(file)) {
            const whole = part.whole ?? part.source;
            for (const m of part.source.matchAll(literal)) {
                add(keys, m[2], `${rel(file)}:${lineOf(whole, part.offset + m.index)}`);
            }
            for (const m of part.source.matchAll(grouped)) {
                for (const q of m[1].matchAll(quoted)) {
                    add(keys, q[2], `${rel(file)}:${lineOf(whole, part.offset + m.index)}`);
                }
            }
        }
    }
    return keys;
}

/** keys the TypeScript code passes as literals */
export function codeKeys(files) {
    const keys = new Map();
    const calls = new RegExp(`\\b(translate|setAppTitle|requiredMessage|notifySuccess|notifyInfo|notifyWarning|notifyError)\\(\\s*(['"])(${KEY})\\2`, "g");
    // setError(key) shows key + "_HINT" too, unless the second argument is false
    const setError = new RegExp(`\\bsetError\\(\\s*(['"])(${KEY})\\1\\s*(,\\s*false\\s*)?\\)`, "g");
    const navText = new RegExp(`\\btext\\s*:\\s*(['"])(${KEY})\\1`, "g");

    for (const file of files) {
        const source = fs.readFileSync(file, "utf8");
        for (const m of source.matchAll(calls)) {
            add(keys, m[3], `${rel(file)}:${lineOf(source, m.index)}`);
        }
        for (const m of source.matchAll(setError)) {
            const where = `${rel(file)}:${lineOf(source, m.index)}`;
            add(keys, m[2], where);
            if (!m[3]) {
                add(keys, m[2] + "_HINT", where);
            }
        }
        if (file.endsWith("app-navigation.ts")) {
            for (const m of source.matchAll(navText)) {
                add(keys, m[2], `${rel(file)}:${lineOf(source, m.index)}`);
            }
        }
    }
    return keys;
}

/** "A": { "B": "text" } -> A.B (angular-l10n uses "." as the key separator) */
export function flatten(json, prefix = "", out = new Map()) {
    for (const [name, value] of Object.entries(json)) {
        const key = prefix ? `${prefix}.${name}` : name;
        if (value !== null && typeof value === "object") {
            flatten(value, key, out);
        } else {
            out.set(key, value);
        }
    }
    return out;
}

/** all locale files of one language, merged like angular-l10n does, plus the keys of every single file */
export function loadLocales(localeDir) {
    const result = { en: new Map(), de: new Map(), files: [] };
    for (const file of walk(localeDir).filter(f => f.endsWith(".json")).sort()) {
        const lang = /-(en|de)\.json$/.exec(file)?.[1];
        if (!lang) {
            continue;
        }
        const keys = flatten(JSON.parse(fs.readFileSync(file, "utf8")));
        for (const [k, v] of keys) {
            result[lang].set(k, v);
        }
        result.files.push({ file, lang, keys });
    }
    return result;
}

export function splitDirectionProblems(files) {
    const problems = [];
    for (const file of files) {
        for (const part of templatesOf(file)) {
            const whole = part.whole ?? part.source;
            // <as-split ...>, not <as-split-area>
            for (const m of part.source.matchAll(/<as-split(?=[\s>])[^>]*>/g)) {
                if (!/\[direction\]\s*=\s*"viewport\.splitDirection"/.test(m[0])) {
                    problems.push(`${rel(file)}:${lineOf(whole, part.offset + m.index)}: <as-split> without [direction]="viewport.splitDirection"`);
                }
            }
        }
    }
    return problems;
}

export function check(root = appRoot) {
    reportRoot = root;
    const appDir = path.join(root, "src", "app");
    const sources = walk(appDir).filter(f => !f.endsWith(".spec.ts") && !f.split(path.sep).includes("testing"));
    const templates = sources.filter(f => f.endsWith(".html") || f.endsWith(".ts"));
    const code = sources.filter(f => f.endsWith(".ts"));
    const locales = loadLocales(path.join(root, "src", "assets", "locale"));
    const errors = [];

    const used = templateKeys(templates);
    for (const [key, places] of codeKeys(code)) {
        for (const p of places) {
            add(used, key, p);
        }
    }

    for (const [key, places] of [...used].sort((a, b) => a[0].localeCompare(b[0]))) {
        for (const lang of ["en", "de"]) {
            if (!locales[lang].has(key)) {
                errors.push(`missing in locale-${lang}: ${key} (used at ${[...places].join(", ")})`);
            }
        }
    }

    // same keys in both languages, compared per file pair (locale-en.json <-> locale-de.json in the same folder)
    const byPair = new Map();
    for (const f of locales.files) {
        const pair = f.file.replace(/-(en|de)\.json$/, "");
        byPair.set(pair, { ...byPair.get(pair), [f.lang]: f.keys });
    }
    for (const [pair, langs] of byPair) {
        if (!langs.en || !langs.de) {
            errors.push(`${rel(pair)}: only ${langs.en ? "en" : "de"} exists`);
            continue;
        }
        for (const k of langs.en.keys()) {
            if (!langs.de.has(k)) {
                errors.push(`${rel(pair)}-de.json lacks ${k}`);
            }
        }
        for (const k of langs.de.keys()) {
            if (!langs.en.has(k)) {
                errors.push(`${rel(pair)}-en.json lacks ${k}`);
            }
        }
    }

    errors.push(...splitDirectionProblems(templates));

    return { errors, usedKeys: used.size, enKeys: locales.en.size, deKeys: locales.de.size };
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
    const result = check();
    if (result.errors.length > 0) {
        console.error(result.errors.join("\n"));
        console.error(`\n${result.errors.length} problem(s)`);
        process.exit(1);
    }
    console.log(`translation keys ok: ${result.usedKeys} used, ${result.enKeys} en / ${result.deKeys} de; as-split directions ok`);
}
