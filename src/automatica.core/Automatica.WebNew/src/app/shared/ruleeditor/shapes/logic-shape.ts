import { RuleInstance } from "src/app/base/model/rule-instance";
import { RuleInterfaceInstance } from "src/app/base/model/rule-interface-instance";
import { NodeInstance2RulePage } from "src/app/base/model/node-instance-2-rule-page";
import { LinkService } from "../link.service";
import { Link } from "src/app/base/model/link";

declare var draw2d: any;
declare var $: any;

export class LogicShapes {
    public static addShape(logic) {

        logic.LogicShape = draw2d.shape.layout.VerticalLayout.extend({
            init: function (attr, element: RuleInstance, linkService: LinkService) {
                if (element.Y < 0) {
                    element.Y *= -1;
                } if (element.X < 0) {
                    element.X *= -1;
                }
                this._super($.extend({ id: element.key, bgColor: "#FFFFFF", alpha: 1, color: "#325862", stroke: 1, radius: 0, x: element.X, y: element.Y, keepAspectRatio: false, minWidth: 150 }, attr));

                this.setUserData(element);

                this.classLabel = new logic.Label({
                    text: element.Name,
                    stroke: 0,
                    fontColor: "#000000",
                    bgColor: "#457987",
                    radius: this.getRadius(),
                    padding: 10,
                    resizeable: true,
                    minWidth: 150
                });
                const translatedName = linkService.translate.translate(element.RuleTemplateName);
                const logicName = new logic.Label({
                    text: translatedName,
                    stroke: 0,
                    fontColor: "#000000",
                    bgColor: "#d7d7d7",
                    radius: this.getRadius(),
                    padding: 10,
                    resizeable: true,
                    minWidth: 150
                });


                this.on("move", (context) => {
                    element.X = context.x;
                    element.Y = context.y;
                });

                this.add(this.classLabel);
                this.add(logicName);

                this.add(new logic.PortShape({}, element, this, linkService));
            },
            getWidth() {
                return Math.max(this.width, this.minWidth);
            }
        });

        logic.PortShape = draw2d.shape.layout.TableLayout.extend({

            NAME: "LogicShape",
            realParent: void 0,
            linkService: void 0,

            init: function (attr, element: RuleInstance, realParent, linkService: LinkService) {
                this._super($.extend(
                    {
                        bgColor: "#dbddde",
                        color: "#d7d7d7",
                        stroke: 1,
                        radius: 0,
                        x: element.X,
                        y: element.Y,
                        keepAspectRatio: false,
                        width: realParent.width,
                        id: element.key,
                        minWidth: 150
                    }, attr));
                this.realParent = realParent;
                this.linkService = linkService;

                const totalRows = Math.max(element.Inputs.length, element.Outputs.length);
                const list = [];

                for (let i = 0; i < totalRows; i++) {
                    list.push({ items: [] });
                }

                for (let i = 0; i < element.Inputs.length; i++) {
                    list[i].items.push(element.Inputs[i]);
                }
                for (let i = 0; i < element.Outputs.length; i++) {
                    list[i].items.push(element.Outputs[i]);
                }

                for (const x of list) {
                    this.createPortInstances(x.items, linkService);
                }
            },


            createPortInstances: function (portInstances: RuleInterfaceInstance[], linkService: LinkService) {
                const data = [];
                for (const portInstance of portInstances) {
                    const isInput = portInstance.Template.InterfaceDirection.Key === "I" || portInstance.Template.InterfaceDirection.Key === "P";
                    const label = new logic.LogicText({
                        text: portInstance.Name,
                        stroke: 0,
                        radius: 0,
                        bgColor: null,
                        fontColor: "#4a4a4a",
                        resizeable: true,
                        padding: 5
                    }, isInput ? 0 : 1, this.realParent);

                    let port = void 0;

                    if (isInput) {
                        port = this.createPort("input", new logic.LogicInputPortLocator(this.realParent, label));
                        port.setName(portInstance.PortId);
                        port.setConnectionDirection(3);
                    } else {
                        port = this.createPort("output", new logic.LogicOutputPortLocator(this.realParent, label));
                        port.setName(portInstance.PortId);
                        port.setConnectionDirection(1);
                    }

                    port.setMaxFanOut(portInstance.FromMaxLinks);
                    port.setUserData(portInstance);

                    port.on("connect", function (emitterPort, connection) {
                        LinkService.handleOnConnection(linkService, port, connection, isInput, portInstance);

                    });

                    port.on("disconnect", function (emitterPort, connection) {
                        LinkService.handleOnDisconnection(linkService, connection);
                    });

                    data.push(label);
                }

                this.addRow(...data);
            },
            getWidth() {
                return Math.max(this.width, this.minWidth);
            }
        });



        logic.ItemShape = draw2d.shape.layout.VerticalLayout.extend({
            init: function (attr, element: NodeInstance2RulePage, linkService: LinkService) {
                if (element.Y < 0) {
                    element.Y *= -1;
                } if (element.X < 0) {
                    element.X *= -1;
                }
                this._super($.extend({ id: element.key, bgColor: "#EEEEEE", alpha: 1, color: "#000000", stroke: 1, radius: 0, x: element.X, y: element.Y, keepAspectRatio: false, minWidth: 150, width: 150 }, attr));

                this.setUserData(element);
                const label = new draw2d.shape.basic.Label({
                    text: element.Name,
                    stroke: 0,
                    radius: 0,
                    bgColor: null,
                    padding: 5,
                    fontColor: "#4a4a4a",
                    resizeable: true
                });


                if (element.Inputs.length > 0) {
                    const input = this.createPort("input");
                    input.setName(element.Inputs[0].PortId);
                    input.setId(element.Inputs[0].PortId);

                    input.on("connect", function (emitterPort, connection) {
                        LinkService.handleOnConnection(linkService, input, connection, true, element);

                    });

                    input.on("disconnect", function (emitterPort, connection) {
                        LinkService.handleOnDisconnection(linkService, connection);
                    });
                }
                if (element.Outputs.length > 0) {

                    const output = this.createPort("output");
                    output.setName(element.Outputs[0].PortId);
                    output.setId(element.Outputs[0].PortId);

                    output.on("connect", function (emitterPort, connection) {
                        LinkService.handleOnConnection(linkService, output, connection, false, element);
                    });

                    output.on("disconnect", function (emitterPort, connection) {
                        LinkService.handleOnDisconnection(linkService, connection);
                    });
                }

                this.on("move", (context) => {
                    element.X = context.x;
                    element.Y = context.y;
                });


                this.add(label);
            },
            getWidth() {
                return 130;
            }
        });

    }
}
