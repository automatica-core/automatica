export interface MediaMode {
  /** 1..8, the value of the mode input */
  mode: number;
  /** the name from the logic's parameter, empty when it has none */
  name: string;
}

/** the modes the visu offers: the named ones, or all eight by number when none has a name */
export function mediaModes(names: any): MediaMode[] {
  const list: string[] = Array.isArray(names) ? names.map(a => (a ?? "").toString().trim()) : [];
  while (list.length < 8) {
    list.push("");
  }
  const all = list.slice(0, 8).map((name, i) => ({ mode: i + 1, name }));
  const named = all.filter(a => a.name.length > 0);
  return named.length > 0 ? named : all;
}

/** command groups of the popup; a group is shown when one of its outputs is linked, or when nothing is linked yet */
export type MediaGroup = "volume" | "channel" | "navigation" | "playback";

export const MEDIA_GROUP_OUTPUTS: { [group in MediaGroup]: string[] } = {
  volume: ["volumeUpOutput", "volumeDownOutput", "volumeOutput", "muteOutput"],
  channel: ["channelUpOutput", "channelDownOutput", "channelOutput"],
  navigation: ["navUpOutput", "navDownOutput", "navLeftOutput", "navRightOutput", "navOkOutput", "backOutput", "menuOutput", "homeOutput"],
  playback: ["playPauseOutput", "nextOutput", "previousOutput"]
};

export function visibleMediaGroups(linkedKeys: Set<string>): Set<MediaGroup> {
  const groups = Object.keys(MEDIA_GROUP_OUTPUTS) as MediaGroup[];
  const linked = groups.filter(g => MEDIA_GROUP_OUTPUTS[g].some(key => linkedKeys.has(key)));
  return new Set(linked.length > 0 ? linked : groups);
}
