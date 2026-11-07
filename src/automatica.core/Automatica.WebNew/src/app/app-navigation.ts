export interface NavItem {
    text: string;
    path?: string;
    icon?: string;
    /** section header: always open, rendered as a label above its items */
    group?: boolean;
    expanded?: boolean;
    items?: NavItem[];
}

// admin navigation, grouped by what you do there
export const navigation: NavItem[] = [
    {
        text: "UI.NAV.OVERVIEW",
        path: "/admin/home",
        icon: "gauge"
    },
    {
        text: "UI.NAV.OPERATE",
        group: true,
        items: [
            { text: "UI.NAV.VISUALIZATION", path: "/visualization", icon: "table-cells-large" },
            { text: "UI.NAV.LOGIC", path: "/admin/logic-editor", icon: "diagram-project" }
        ]
    },
    {
        text: "UI.NAV.CONFIGURE",
        group: true,
        items: [
            { text: "UI.NAV.DEVICES", path: "/admin/config", icon: "sitemap" },
            { text: "UI.NAV.AREAS", path: "/admin/area-config", icon: "layer-group" },
            { text: "UI.NAV.CATEGORIES", path: "/admin/category-config", icon: "tags" },
            { text: "UI.NAV.VISU_EDITOR", path: "/admin/visualisation", icon: "pen-ruler" },
            { text: "UI.NAV.TELEGRAM_MONITOR", path: "/admin/telegram-monitor", icon: "wave-square" }
        ]
    },
    {
        text: "UI.NAV.SYSTEM",
        group: true,
        items: [
            { text: "UI.NAV.USERS", path: "/admin/user-config", icon: "user" },
            { text: "UI.NAV.GROUPS", path: "/admin/usergroup-config", icon: "users" },
            { text: "UI.NAV.PLUGINS", path: "/admin/plugins", icon: "plug" },
            { text: "UI.NAV.SATELLITES", path: "/admin/satellites", icon: "satellite-dish" },
            { text: "UI.NAV.LOGS", path: "/admin/logs", icon: "file-lines" },
            { text: "UI.NAV.SYSTEM_PAGE", path: "/admin/system", icon: "server" }
        ]
    }
];
