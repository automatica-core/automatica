import { VirtualPropertyInstance } from "./virtual-property-instance"
import { ITreeNode } from "../ITreeNode"
import { RuleInstance } from "../rule-instance";
import { NodeInstance } from "../node-instance";

export class VirtualObjIdPropertyInstance extends VirtualPropertyInstance {

    /**
     *
     */
    constructor(private instance: NodeInstance | RuleInstance) {
        super(instance);

        this.PropertyTemplate.Name = "COMMON.PROPERTY.OBJID.NAME";
        this.PropertyTemplate.Description = "COMMON.PROPERTY.OBJID.DESCRIPTION";
        this.PropertyTemplate.Key = "objid";
        this.PropertyTemplate.GroupOrder = 0;
        this.PropertyTemplate.Order = 3;
        this.PropertyTemplate.IsReadonly = true;
    }

    get Value(): any {
        return this.instance.ObjId;
    }
    set Value(value: any) {
        // do nothing
    }


}
