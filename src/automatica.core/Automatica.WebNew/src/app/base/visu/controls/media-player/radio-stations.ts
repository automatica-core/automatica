export interface RadioStationItem {
  name: string;
  /** station key of the server's catalog (at-oe3), or as configured before the catalog a TuneIn id (s8007) or stream url */
  id: string;
  /** https url of the station logo (stations of the catalog) */
  logo?: string;
}

/** the station list of the Sonos Control logic ("radioStations" of its visu data); accepts camelCase and PascalCase */
export function radioStations(list: any): RadioStationItem[] {
  if (!Array.isArray(list)) {
    return [];
  }
  return list
    .map(a => ({
      name: (a?.name ?? a?.Name ?? "").toString().trim(),
      id: (a?.id ?? a?.Id ?? "").toString().trim(),
      logo: (a?.logo ?? a?.Logo ?? "").toString().trim()
    }))
    .filter(a => a.id.length > 0)
    .map(a => {
      const item: RadioStationItem = { name: a.name || a.id, id: a.id };
      if (/^https?:\/\//i.test(a.logo)) {
        item.logo = a.logo;
      }
      return item;
    });
}

/** the station of the list that is played: by id, or by name when a name was sent to the logic */
export function currentRadioStation(list: RadioStationItem[], value: any): RadioStationItem | undefined {
  const text = (value ?? "").toString().trim().toLowerCase();
  if (!text) {
    return undefined;
  }
  return list.find(a => a.id.toLowerCase() === text) ?? list.find(a => a.name.toLowerCase() === text);
}
