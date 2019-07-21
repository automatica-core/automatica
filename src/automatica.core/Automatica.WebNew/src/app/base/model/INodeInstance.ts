import { NodeTemplate } from "./node-template";

export interface INodeInstance {

    IsWriteable: boolean;
    NodeTemplate: NodeTemplate;
    This2Slave: string;
}
