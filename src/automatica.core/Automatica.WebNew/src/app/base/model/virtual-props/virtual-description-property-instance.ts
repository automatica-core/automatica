import { PropertyInstance } from "../property-instance"
import { PropertyTemplate, PropertyTemplateType } from "../property-template"
import { NodeInstance } from "../node-instance"
import { VirtualPropertyInstance } from "./virtual-property-instance"
import { IDescriptionModel } from "../IDescriptionModel";

export class VirtualDescriptionPropertyInstance extends VirtualPropertyInstance {

    /**
     *
     */

    
    private _valueOverride : string;
    public get valueOverride() : string {
        return this._valueOverride;
    }
    public set valueOverride(v : string) {
        this._valueOverride = v;
    }
    
    constructor(private nodeInstance: IDescriptionModel, isReadonly: boolean = false) {
        super(nodeInstance);

        this.PropertyTemplate.Name = "COMMON.PROPERTY.DESCRIPTION.NAME";
        this.PropertyTemplate.Description = "COMMON.PROPERTY.DESCRIPTION.DESCRIPTION";
        this.PropertyTemplate.Key = "description";
        this.PropertyTemplate.GroupOrder = 0;
        this.PropertyTemplate.Order = 2;
        this.PropertyTemplate.IsReadonly = isReadonly;
    }

    get Value(): any {
        if(this.valueOverride === "") {
            return this.valueOverride;
        }
        return this.nodeInstance.DisplayDescription;
    }
    set Value(value: any) {
        this.valueOverride = value;
        this.nodeInstance.DisplayDescription = value;
    }


}
