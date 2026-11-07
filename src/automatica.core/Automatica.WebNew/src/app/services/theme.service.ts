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
        const storageKey = "automaticaTheme";
        theme = theme === "light" || theme === "dark" ? theme : (window.localStorage[storageKey] || "dark");

        // the theme bundles are named generic.light / generic.dark (plus the output hash).
        // The <link> elements are switched, not document.styleSheets: at startup the sheets are not loaded yet,
        // they would be missing from that list and both themes would stay active.
        for (const link of Array.from(document.querySelectorAll<HTMLLinkElement>("link[rel=stylesheet]"))) {
            const match = link.href?.match(/generic[.](light|dark)/);
            if (match) {
                link.disabled = match[1] !== theme;
            }
        }

        window.localStorage[storageKey] = theme;
        // our own components read the tokens in tokens.scss, keyed by this attribute
        document.documentElement.dataset.theme = theme;
        currentTheme("generic." + theme);
        refreshTheme();
        this.currentTheme = theme;
        this.themeChanged.emit(theme);
    }

    constructor() { }

}