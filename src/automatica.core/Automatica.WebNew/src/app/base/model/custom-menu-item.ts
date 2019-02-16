import { EventEmitter } from "@angular/core";
import { NodeTemplate } from "./node-template";


export interface CustomMenuItem {
    id: any;
    nodeTemplate?: NodeTemplate;
    label?: string;
    icon?: string;
    command?: (event?: any) => void;
    url?: string;
    routerLink?: any;
    eventEmitter?: EventEmitter<any>;
    items?: CustomMenuItem[];
    expanded?: boolean;
    disabled?: boolean;
    source?: any;

    color?: any;
}
