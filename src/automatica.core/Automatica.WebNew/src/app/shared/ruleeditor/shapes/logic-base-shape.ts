import { RuleInstance } from "src/app/base/model/rule-instance";
import { RuleInterfaceInstance } from "src/app/base/model/rule-interface-instance";
import { LinkService } from "../link.service";

declare var draw2d: any;
declare var $: any;


export class LogicBaseShape {
    public static addLogicBaseShapes(logic) {

        logic.LogicBaseShape = draw2d.shape.layout.TableLayout.extend({

            init: function (attr, ruleInstance: RuleInstance, realParent, linkService: LinkService) {
                this._super(attr);

                this.realParent = realParent;
            },

            getMinWidth() {
                if (this.realParent) {
                    return this.realParent.getMinWidth();
                }
                return 100;
            },
            createPort: function (type, locator, portInstance: RuleInterfaceInstance) {
                let newPort = null
                let count = 0

                switch (type) {
                    case "input":
                        newPort = new logic.InputPort(this);
                        count = this.inputPorts.getSize()
                        break
                    case "output":
                        newPort = new logic.OutputPort(this)
                        count = this.outputPorts.getSize()
                        break
                    case "hybrid":
                        newPort = draw2d.Configuration.factory.createHybridPort(this)
                        count = this.hybridPorts.getSize()
                        break
                    default:
                        throw "Unknown type [" + type + "] of port requested"
                }

                newPort.setName(type + count)

                this.addPort(newPort, locator)
                // relayout the ports
                this.setDimension(this.width, this.height)

                return newPort
            }

        });
    }
}
