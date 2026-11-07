// Reads the license XML the server hands out (update/license) into something a page can show,
// and turns the server's license errors into a sentence with a fix.

export interface LicenseFeature {
    key: string;
    /** i18n key for known features, undefined for unknown ones (then key is shown) */
    labelKey?: string;
    value: string;
    /** true / false for on/off features, undefined for numbers and text */
    enabled?: boolean;
}

export interface LicenseInfo {
    id: string;
    type: string;
    expiration: Date | undefined;
    isExpired: boolean;
    quantity: string;
    customerName: string;
    customerEmail: string;
    maxDatapoints: number | undefined;
    maxUsers: number | undefined;
    features: LicenseFeature[];
}

export interface LicenseError {
    /** i18n keys for known errors, undefined for unknown ones */
    titleKey?: string;
    textKey?: string;
    /** the server's own words, shown when the error is not known */
    message: string;
    howToResolve: string;
    /** "expired" errors get the expiration date as {{ date }} */
    kind: "expired" | "signature" | "datapoints" | "users" | "other";
}

const FEATURE_LABELS: { [key: string]: string } = {
    "maxdatapoints": "UI.SYSTEM.FEATURE_DATAPOINTS",
    "maxusers": "UI.SYSTEM.FEATURE_USERS",
    "allowremotecontrol": "UI.SYSTEM.FEATURE_REMOTE",
    "knx-ets-import": "UI.SYSTEM.FEATURE_ETS_IMPORT",
    "maxsatellites": "UI.SYSTEM.FEATURE_SATELLITES",
    "maxrecordingdatapoints": "UI.SYSTEM.FEATURE_RECORDING"
};

function parseNumber(v: string): number | undefined {
    const n = parseInt(v, 10);
    return isNaN(n) ? undefined : n;
}

export function parseLicense(xml: string): LicenseInfo | undefined {
    if (!xml || typeof xml !== "string" || xml.indexOf("<") < 0) {
        return undefined;
    }
    let doc: Document;
    try {
        doc = new DOMParser().parseFromString(xml, "application/xml");
    } catch {
        return undefined;
    }
    if (doc.getElementsByTagName("parsererror").length > 0) {
        return undefined;
    }
    const root = doc.documentElement;
    const text = (parent: Element, name: string) => {
        const el = parent.getElementsByTagName(name)[0];
        return el ? (el.textContent || "").trim() : "";
    };

    const expirationText = text(root, "Expiration");
    const expiration = expirationText ? new Date(expirationText) : undefined;
    const validExpiration = expiration && !isNaN(expiration.getTime()) ? expiration : undefined;

    const customer = root.getElementsByTagName("Customer")[0];

    const features: LicenseFeature[] = [];
    let maxDatapoints: number | undefined;
    let maxUsers: number | undefined;
    const featureEls = root.getElementsByTagName("Feature");
    for (let i = 0; i < featureEls.length; i++) {
        const el = featureEls[i];
        const key = el.getAttribute("name") || "";
        const value = (el.textContent || "").trim();
        const lower = key.toLowerCase();
        const isBool = /^(true|false)$/i.test(value);
        if (lower === "maxdatapoints") {
            maxDatapoints = parseNumber(value);
        } else if (lower === "maxusers") {
            maxUsers = parseNumber(value);
        }
        features.push({
            key,
            labelKey: FEATURE_LABELS[lower],
            value,
            enabled: isBool ? value.toLowerCase() === "true" : undefined
        });
    }

    return {
        id: text(root, "Id"),
        type: text(root, "Type"),
        expiration: validExpiration,
        isExpired: !!validExpiration && validExpiration.getTime() < Date.now(),
        quantity: text(root, "Quantity"),
        customerName: customer ? text(customer, "Name") : "",
        customerEmail: customer ? text(customer, "Email") : "",
        maxDatapoints,
        maxUsers,
        features
    };
}

export function describeLicenseErrors(raw: any): LicenseError[] {
    const list: any[] = Array.isArray(raw) ? raw : (raw ? [raw] : []);
    return list.map(e => {
        const message = String(e?.message ?? e?.Message ?? e ?? "");
        const howToResolve = String(e?.howToResolve ?? e?.HowToResolve ?? "");
        const lower = message.toLowerCase();
        if (lower.includes("expired")) {
            return { kind: "expired", titleKey: "UI.SYSTEM.ERR_EXPIRED", textKey: "UI.SYSTEM.ERR_EXPIRED_FIX", message, howToResolve } as LicenseError;
        }
        if (lower.includes("signature")) {
            return { kind: "signature", titleKey: "UI.SYSTEM.ERR_SIGNATURE", textKey: "UI.SYSTEM.ERR_SIGNATURE_FIX", message, howToResolve } as LicenseError;
        }
        if (lower.includes("datapoint")) {
            return { kind: "datapoints", titleKey: "UI.SYSTEM.ERR_DATAPOINTS", textKey: "UI.SYSTEM.ERR_DATAPOINTS_FIX", message, howToResolve } as LicenseError;
        }
        if (lower.includes("user")) {
            return { kind: "users", titleKey: "UI.SYSTEM.ERR_USERS", textKey: "UI.SYSTEM.ERR_USERS_FIX", message, howToResolve } as LicenseError;
        }
        return { kind: "other", message, howToResolve } as LicenseError;
    });
}
