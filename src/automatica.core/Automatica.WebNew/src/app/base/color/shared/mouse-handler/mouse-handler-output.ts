import { Vector } from '../../vector';

export interface MouseHandlerOutput {
    s?: number;
    v?: number;
    rgX?: number;
    rgY?: number;
    rg?: number;
    realWorld: Vector;
}
