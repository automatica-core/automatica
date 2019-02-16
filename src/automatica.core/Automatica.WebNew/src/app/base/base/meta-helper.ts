export class MetaHelper {
    public static checkMatches(inputValue: string): string[] {
        const regex = new RegExp(/\{([A-Za-z0-9+:\t_-]+)\}/g);
        let m;
        const matches = [];


        while ((m = regex.exec(inputValue)) !== null) {
            if (m.index === regex.lastIndex) {
                regex.lastIndex++;
            }

            m.forEach((match, groupIndex) => {
                if (groupIndex === 0) {
                    matches.push(match);
                }
            });
        }
        return matches;
    }
}
