/** a station of the server's radio station catalog (webapi/radio-stations, camelCase) */
export interface RadioStation {
  /** stable key, e.g. "at-oe3": stored in logic parameters, the device drivers translate it */
  key: string;
  name: string;
  /** ISO 3166 country code, e.g. "AT" */
  country?: string;
  genre?: string;
  streamUrl: string;
  logo?: string;
  codec?: string;
  /** optional TuneIn id (s8007) */
  tuneInId?: string;
  /** added by the administrator */
  isCustom?: boolean;
}

/** "at-oe3;de-1live" (also "," or one per line), trimmed, without empty entries and duplicates */
export function parseStationKeys(value: any): string[] {
  const keys: string[] = [];
  for (const part of (value ?? "").toString().split(/[;,\r\n]+/)) {
    const key = part.trim();
    if (key && !keys.some(a => a.toLowerCase() === key.toLowerCase())) {
      keys.push(key);
    }
  }
  return keys;
}

export function joinStationKeys(keys: string[]): string {
  return (keys ?? []).map(a => (a ?? "").toString().trim()).filter(a => a.length > 0).join(";");
}

/** a key of a custom station: lower case letters, digits and '-' (as the server checks it) */
export function isValidStationKey(key: string): boolean {
  return /^[a-z0-9][a-z0-9-]{0,63}$/.test(key ?? "");
}

export function isValidStreamUrl(url: string): boolean {
  return /^https?:\/\/[^\s/]+/i.test((url ?? "").trim());
}

/** "lounge fm" -> "lounge-fm" (a key proposal for a new custom station) */
export function stationKeyOf(name: string): string {
  const key = (name ?? "")
    .toLowerCase()
    .normalize("NFD").replace(/[̀-ͯ]/g, "")
    .replace(/ß/g, "ss")
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .substring(0, 64);
  return key;
}
