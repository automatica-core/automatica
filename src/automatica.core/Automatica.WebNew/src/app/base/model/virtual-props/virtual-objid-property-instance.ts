import { VirtualPropertyInstance } from "./virtual-property-instance"
import { ITreeNode } from "../ITreeNode"

export class VirtualObjIdPropertyInstance extends VirtualPropertyInstance {

    /**
     *
     */
    constructor(private nodeInstance: ITreeNode) {
        super(nodeInstance);

        this.PropertyTemplate.Name = "COMMON.PROPERTY.OBJID.NAME";
        this.PropertyTemplate.Description = "COMMON.PROPERTY.OBJID.DESCRIPTION";
        this.PropertyTemplate.Key = "objid";
        this.PropertyTemplate.GroupOrder = 0;
        this.PropertyTemplate.Order = 1;
        this.PropertyTemplate.IsReadonly = true;
    }

    get Value(): any {
        return this.nodeInstance.Id;
    }
    set Value(value: any) {
        // do nothing
    }


}
