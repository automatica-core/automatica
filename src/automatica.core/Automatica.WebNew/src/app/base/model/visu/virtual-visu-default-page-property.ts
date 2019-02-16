
import { PropertyTemplateType } from "../property-template";
import { EventEmitter } from "@angular/core";
import { BaseModel } from "../base-model";
import { IVisuPage } from "./visu-page-interface";
import { VirtualPropertyInstance } from "../virtual-props/virtual-property-instance";

export class VirtualVisuDefaultPageProperty extends VirtualPropertyInstance {

    public defaultPageChange = new EventEmitter<boolean>();

    constructor(private visupage: IVisuPage) {
        super(visupage);

        this.PropertyTemplate.PropertyType.Type = PropertyTemplateType.Bool;
        this.PropertyTemplate.Name = "VISU.PAGE.DEFAULT.NAME";
        this.PropertyTemplate.Description = "VISU.PAGE.DEFAULT.DESCRIPTION";
        this.PropertyTemplate.Key = "default-page";

        this.propertyChanged.subscribe(() => {
            this.defaultPageChange.emit(this.ValueBool);
        });
        this.PropertyTemplate.GroupOrder = 12;
    }


    get Value(): any {
        return this.Parent.DefaultPage;
    }
    set Value(value: any) {
        this.Parent.DefaultPage = value;
        this.propertyChanged.emit(this);
    }
}
