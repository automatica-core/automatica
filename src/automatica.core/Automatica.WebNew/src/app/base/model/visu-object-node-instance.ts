import { VisuObjectInstance } from "./visu-object-instance";
import { Model } from "./base-model";
import { NodeInstance } from "./node-instance";
import { VisuObjectTemplate } from "./visu-object-template";


export class VisuObjectNodeInstance extends VisuObjectInstance {

    public static CreateFromTemplateForNode(template: VisuObjectTemplate, nodeInstance: NodeInstance): VisuObjectNodeInstance {
        const instance = new VisuObjectNodeInstance(nodeInstance);

        return this.FillNewInstance(instance, template);
    }

    constructor(public nodeInstance: NodeInstance) {
        super();
    }

}

