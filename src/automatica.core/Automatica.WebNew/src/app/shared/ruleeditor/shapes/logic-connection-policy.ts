
declare var draw2d: any;
declare var $: any;


export class LogicConnectionPolicy {
    public static addPolicy(logic) {
        logic.ConnectionPolicy = draw2d.policy.line.LineSelectionFeedbackPolicy.extend({
            NAME: "logic.ConnectionPolicy",
            init: function (attr, setter, getter) {
                this._super(attr, setter, getter)
            },

            onSelect: function (canvas, figure, isPrimarySelection) {
                this._super(canvas, figure, isPrimarySelection);
            },

            onUnselect: function (canvas, figure) {
                this._super(canvas, figure);
            }
        });

        logic.LogicSelectionPolicy = draw2d.policy.figure.AntSelectionFeedbackPolicy.extend({
            NAME: "logic.LogicSelectionPolicy",
            init: function (attr, setter, getter) {
                this._super(attr, setter, getter)
            },

            onSelect: function (canvas, figure, isPrimarySelection) {
                this._super(canvas, figure, isPrimarySelection);
                figure.getChildren().each((i, child) => {
                    let inputPorts = child.getInputPorts();
                    let outputPorts = child.getOutputPorts();

                    
                    this.setGlowOnPort(inputPorts, true);
                    this.setGlowOnPort(outputPorts, true);
                });
                this.setGlowOnPort(figure.getInputPorts(), true);
                this.setGlowOnPort(figure.getOutputPorts(), true);
            },
            onUnselect: function (canvas, figure) {
                this._super(canvas, figure);
                figure.getChildren().each((i, child) => {
                    let inputPorts = child.getInputPorts();
                    let outputPorts = child.getOutputPorts();

                    this.setGlowOnPort(inputPorts, false);
                    this.setGlowOnPort(outputPorts, false);
                });
                this.setGlowOnPort(figure.getInputPorts(), false);
                this.setGlowOnPort(figure.getOutputPorts(), false);
            },

            setGlowOnPort(ports, glow) {
                ports.each((i, port) => {
                    port.getConnections().each((i, connection) => {
                        connection.setGlow(glow);
                    });
                });
            }
        });
    }
}