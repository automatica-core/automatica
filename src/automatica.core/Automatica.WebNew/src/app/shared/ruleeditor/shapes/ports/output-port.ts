declare var draw2d: any;
declare var $: any;

export class OutputPort {
    public static addOutputPort(logic) {
        logic.OutputPort = draw2d.OutputPort.extend({
            
        });

    }
}