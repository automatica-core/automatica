export interface RadioStationItem {
  name: string;
  /** TuneIn id (s8007) or stream url */
  id: string;
}

/** the station list of the Sonos Control logic ("radioStations" of its visu data); accepts camelCase and PascalCase */
export function radioStations(list: any): RadioStationItem[] {
  if (!Array.isArray(list)) {
    return [];
  }
  return list
    .map(a => ({
      name: (a?.name ?? a?.Name ?? "").toString().trim(),
      id: (a?.id ?? a?.Id ?? "").toString().trim()
    }))
    .filter(a => a.id.length > 0)
    .map(a => ({ name: a.name || a.id, id: a.id }));
}

/** the station of the list that is played: by id, or by name when a name was sent to the logic */
export function currentRadioStation(list: RadioStationItem[], value: any): RadioStationItem | undefined {
  const text = (value ?? "").toString().trim().toLowerCase();
  if (!text) {
    return undefined;
  }
  return list.find(a => a.id.toLowerCase() === text) ?? list.find(a => a.name.toLowerCase() === text);
}
