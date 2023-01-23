import { EventEmitter, Injectable } from '@angular/core';
import { getPalette } from 'devextreme/viz/palette';
import { currentTheme, getTheme, refreshTheme } from 'devextreme/viz/themes';

@Injectable()
export class ThemeService {

    themeChanged = new EventEmitter<string>();
    currentTheme = "light";

    private criteria(category: string): Array<string> {
        let categoryItems: any = {
            'sector': ['Banking', 'Energy', 'Health', 'Insurance', 'Manufacturing', 'Telecom'],
            'product': ['Eco Max', 'Eco Supreme', 'EnviroCare', 'EnviroCare Max', 'SolarMax', 'SolarOne'],
            'channel': ['Consultants', 'Direct', 'Resellers', 'Retail', 'VARs']
        };
        return categoryItems[category.toLowerCase()];
    }

    private getCriteriaIndex(category: string, criteria: string) {
        return this.criteria(category).indexOf(criteria);
    }

    getColor(category: string, criteria: string): string {
        const palette: Array<string> = getPalette(this.getThemeItem('defaultPalette'))['simpleSet'];
        return palette[this.getCriteriaIndex(category, criteria)];
    }

    getAccentColor(): string {
        return getPalette(this.getThemeItem('defaultPalette')).accentColor;
    }
    getBackgroundColor(): string {
        const themeName = currentTheme();
        const theme = getTheme(themeName);
        return theme.backgroundColor;
    }
    getGridColor(): string {
        const themeName = currentTheme();
        const theme = getTheme(themeName);
        return theme.gridColor;
    }

    getLegendItems(category: string): Array<any> {
        return this.criteria(category).map(function (criteria) {
            return {
                color: this.getColor(category, criteria),
                name: criteria
            };
        }, this);
    }

    getThemeItem(...keys: Array<string>): any {
        const theme = getTheme(currentTheme());
        let item = theme;
        for (let key in keys)
            item = item[keys[key]];
        return item;
    }

    getCurrentTheme() {
        return this.currentTheme;
    }

    applyTheme(theme?: string) {
        let themeMarker = "generic.",
            storageKey = "automaticaTheme";
        theme = theme || window.localStorage[storageKey] || "dark";


        for (let index in document.styleSheets) {
            let styleSheet = document.styleSheets[index],
                href = styleSheet.href;
            if (href) {
                let themeMarkerPosition = href.indexOf(themeMarker);
                if (themeMarkerPosition >= 0) {
                    let startPosition = themeMarkerPosition + themeMarker.length,
                        endPosition = href.indexOf(".", startPosition),
                        fileNamePart = href.substring(startPosition, endPosition);
                    styleSheet.disabled = fileNamePart != theme;
                }
            }
        }

        window.localStorage[storageKey] = theme;
        currentTheme("generic." + theme + ".compact");
        refreshTheme();
        this.currentTheme = theme;
        this.themeChanged.emit(theme);
    }

    constructor() { }

}