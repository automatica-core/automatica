import { RuleInstance } from "src/app/base/model/rule-instance";
import { RuleInterfaceInstance } from "src/app/base/model/rule-interface-instance";
import { NodeInstance2RulePage } from "src/app/base/model/node-instance-2-rule-page";
import { LinkService } from "../link.service";
import { LogicEngineService, LogicUpdateScope } from "src/app/services/logicengine.service";
import { ILogicErrorHandler } from "../ilogicErrorHandler";
import { LogicShapeValueLocator } from "./logic-shape-value-locator";
import { ILogicInfoHandler } from "../ilogicInfoHandler";
import { LogicInterfaceDirection } from "src/app/base/model/rule-interface-direction";
import { NodeDataType, NodeDataTypeEnum } from "src/app/base/model/node-data-type";
declare let draw2d: any;
declare let $: any;

class ValueHandler {
    static handleValue(value: any) {
        if (value) {
            let strValue: string = value.toString();
            if (strValue.length > 10) {
                return strValue.substring(0, 10) + "...";
            }
        }
        return value;
    }
}

export class LogicShapes {
    public static addShape(logic, ruleEngineService: LogicEngineService, errorHandler: ILogicErrorHandler, infoHandler: ILogicInfoHandler) {

        logic.LogicInfoHeader = draw2d.shape.icon.Talkq.extend({
            init: function (attr, setter, getter) {
                this._super($.extend({}, attr), setter, getter);

                this.on("click", () => {
                    infoHandler.showInfoForLogic(logic);
                });
            }


        });

        logic.LogicHeader = draw2d.shape.layout.FlexGridLayout.extend({
            text: void 0,

            init: function (attr, setter, getter) {
                this._super($.extend({
                    columns: "90px, 10px",
                    rows: "grow",
                    valign: "center",
                    bgColor: "#457987",
                    stroke: 0,
                    width: 100
                }, attr),
                    setter,
                    getter);

                const fontSize = this.text?.length <= 18 ? 10 : 8;

                this.classLabel = new logic.Label({
                    text: this.truncate(this.text, fontSize),
                    fontSize: fontSize,
                    stroke: 0,
                    fontColor: "#000000",
                    bgColor: "#457987",
                    radius: this.getRadius(),
                    padding: 5,
                    resizeable: false,
                    minWidth: 90,
                    width: 100,
                });

                this.add(this.classLabel,
                    {
                        row: 0, col: 0,
                        valign: "center"
                    });
            },

            setText: function setText(text) {
                const fontSize = text?.length <= 18 ? 10 : 8;
                this.classLabel.setFontSize(fontSize);
                this.classLabel.setText(this.truncate(text, fontSize));
            },

            truncate: function (input, fontSize) {
                if (!input) {
                    return input;
                }
                if (fontSize == 8) {
                    if (input.length > 22) {
                        return input.substring(0, 18) + '...';
                    }
                }
                else if (fontSize == 10) {
                    if (input.length > 18) {
                        return input.substring(0, 15) + '...';
                    }
                }
                return input;
            }
        });


        logic.LogicShape = draw2d.shape.layout.VerticalLayout.extend({
            init: function (attr, element: RuleInstance, linkService: LinkService) {
                if (element.Y < 0) {
                    element.Y *= -1;
                }
                if (element.X < 0) {
                    element.X *= -1;
                }
                this._super($.extend(
                    {
                        id: element.key,
                        bgColor: "#d7d7d7",
                        alpha: 1,
                        color: "#325862",
                        stroke: 0,
                        radius: 0,
                        x: element.X,
                        y: element.Y,
                        keepAspectRatio: false,
                        resizeable: false,
                        height: 25
                    }, attr));

                this.setUserData(element);


                this.headerLayout = new logic.LogicHeader();
                this.headerLayout.setText(element.Name);


                element.onNameChanged.subscribe((v) => {
                    this.headerLayout.setText(v);
                });

                const translatedName = linkService.translate.translate(element.RuleTemplateName);
                this.logicName = new logic.Label({
                    text: translatedName,
                    stroke: 0,
                    fontColor: "#000000",
                    bgColor: "#d7d7d7",
                    radius: this.getRadius(),
                    padding: 5,
                    resizeable: true,
                    minWidth: 100,
                    fontSize: 8,
                    height: 20
                });

                this.on("move", (context) => {
                    element.X = context.x;
                    element.Y = context.y;
                });


                this.on("dragEnd", async (context, data) => {
                    try {
                        if (element.itemMoved) {
                            await ruleEngineService.updateItem(element, LogicUpdateScope.Drag);
                        }
                    }
                    catch (error) {
                        errorHandler.notifyError(error);
                    }
                });

                this.add(this.headerLayout);
                this.add(this.logicName);

                this.add(new logic.PortShape({}, element, this, linkService));
            },
            getMinWidth() {
                if (this.classLabel) {
                    if (this.classLabel.cachedMinWidth > 150) {
                        return this.classLabel.cachedMinWidth;
                    }
                }
                return 100;
            },
            onDragEnd(x, y, shiftKey, ctrlKey) {
                this._super();

                this.fireEvent("dragEnd", { x: x, y: y });

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
                        stroke: 0,
                        radius: 0,
                        padding: 0,
                        x: element.X,
                        y: element.Y,
                        keepAspectRatio: false,
                        width: realParent.width,
                        id: element.key,
                        minWidth: realParent.minWidth
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
                    this.createPortInstances(element, x.items, linkService);
                }
            },


            createPortInstances: function (parent: RuleInstance, portInstances: RuleInterfaceInstance[], linkService: LinkService) {
                const data = [];
                for (const portInstance of portInstances) {
                    const isInput = portInstance.Template.InterfaceDirection.Key === "I" || portInstance.Template.InterfaceDirection.Key === "P";
                    const label = new logic.LogicPortText({
                        text: portInstance.Name,
                        stroke: 0,
                        radius: 0,
                        bgColor: null,
                        fontColor: portInstance.Inverted ? "red" : "#4a4a4a",
                        resizeable: false,
                        padding: { top: 5, right: isInput ? 0 : 10, bottom: 5, left: isInput ? 7 : 5 },
                        fontSize: 8
                    }, isInput ? 0 : 1, this.realParent, portInstance);

                    let port = void 0;

                    let dataLabel = void 0;

                    if (isInput) {
                        port = this.createPort("input", new logic.LogicInputPortLocator(this.realParent, label));
                        port.setName(portInstance.PortId);
                        port.setConnectionDirection(3);
                        port.setDiameter(8);

                        if (portInstance.Template.InterfaceDirection.ObjId === LogicInterfaceDirection.Param) {
                            port.setColor("orange");
                            port.setBackgroundColor("orange");
                        }
                        else {
                            port.setColor("red");
                            port.setBackgroundColor("red");
                        }

                    } else {
                        port = this.createPort("output", new logic.LogicOutputPortLocator(this.realParent, label));
                        port.setName(portInstance.PortId);
                        port.setConnectionDirection(1);
                        port.setDiameter(8);
                        port.setColor("green");
                        port.setBackgroundColor("green");

                        dataLabel = new draw2d.shape.basic.Label({
                            text: portInstance.PortValue,
                            textLength: "100%",
                            stroke: 0,
                            radius: 0,
                            bgColor: null,
                            padding: 5,
                            paddingLeft: 10,
                            paddingRight: 10,
                            fontColor: "lightgray",
                            fontSize: 9,
                            resizeable: true,

                        });

                        port.add(dataLabel, new LogicShapeValueLocator({ marginBottom: 12, marginRight: 10 }));
                        dataLabel.toBack();

                    }

                    portInstance.notifyChangeEvent.subscribe(async (v) => {
                        if (v.propertyName === "Inverted") {
                            label.setFontColor((<any>v.object).Inverted ? "red" : "#4a4a4a");

                            try {
                                await ruleEngineService.updateItem(parent, LogicUpdateScope.InvertedValueUpdated);
                            }
                            catch (error) {
                                errorHandler.notifyError(error);
                            }
                        }
                        else if (!isInput && v.propertyName === "PortValue" && dataLabel) {
                            let value = ValueHandler.handleValue((<any>v.object).PortValue);

                            dataLabel.setText(value);
                        }
                    });

                    port.setMaxFanOut(portInstance.FromMaxLinks);
                    port.setUserData(portInstance);

                    port.on("connect", async function (emitterPort, connection) {
                        try {
                            await LinkService.handleOnConnection(linkService, port, connection, isInput, portInstance, ruleEngineService);
                        }
                        catch (error) {
                            errorHandler.notifyError(error);
                        }

                    });

                    port.on("disconnect", async function (emitterPort, connection) {
                        try {
                            await LinkService.handleOnDisconnection(linkService, connection, isInput);
                        }
                        catch (error) {

                            errorHandler.notifyError(error);
                        }
                    });

                    data.push(label);
                }

                this.addRow(...data);
            }
        });



        logic.NodeInstanceShape = draw2d.shape.layout.HorizontalLayout.extend({


            init: function (attr, element: NodeInstance2RulePage, linkService: LinkService) {
                this.tooltip = null;
                this.element = element;

                if (element.Y < 0) {
                    element.Y *= -1;
                } if (element.X < 0) {
                    element.X *= -1;
                }
                this._super($.extend({ id: element.key, bgColor: "#EEEEEE", alpha: 1, color: "#000000", stroke: 1, radius: 0, x: element.X, y: element.Y, keepAspectRatio: false, minWidth: 80 }, attr));

                this.setUserData(element);

                let dataType = "b";

                switch (element.NodeInstance.NodeTemplate.NodeTypeEnum) {
                    case NodeDataTypeEnum.Boolean:
                        dataType = "b";
                        break;
                    case NodeDataTypeEnum.Date:
                        dataType = "d";
                        break;
                    case NodeDataTypeEnum.DateTime:
                        dataType = "dt";
                        break;
                    case NodeDataTypeEnum.Double:
                        dataType = "db";
                        break;
                    case NodeDataTypeEnum.Integer:
                        dataType = "i";
                        break;
                    case NodeDataTypeEnum.String:
                        dataType = "s";
                        break;
                    case NodeDataTypeEnum.Time:
                        dataType = "t";
                        break;
                    case NodeDataTypeEnum.WindowState:
                        dataType = "ws";
                        break;
                    case NodeDataTypeEnum.NoAttribute:
                        dataType = "error";
                        break;

                }

                this.dataTypeLabel = new logic.NodeInstanceText({
                    text: dataType,
                    padding: { top: 2.5, right: 5, bottom: 2, left: 5 },
                    paddingLeft: 10,
                    paddingRight: 10,
                    fontColor: element.Inverted ? "red" : "#4a4a4a",
                    stroke: 0,
                    radius: 0,
                    resizeable: false,
                    fontSize: 8,
                }, this.realParent, element);

                this.label = new draw2d.shape.basic.Label({
                    text: element.Name,
                    textLength: "100%",
                    stroke: 0,
                    radius: 0,
                    bgColor: null,
                    padding: { top: 2.5, right: 5, bottom: 2, left: 5 },
                    paddingLeft: 10,
                    paddingRight: 10,
                    fontColor: "#4a4a4a",
                    resizeable: false,
                    fontSize: 8,
                    minWidth: 80
                });

                if (element.NodeInstance) {
                    element.NodeInstance.notifyChangeEvent.subscribe((v) => {
                        if (v.propertyName === "Name") {
                            this.label.setText((<any>v.object).Name);
                        }
                    });
                }

                element.notifyChangeEvent.subscribe(async (v) => {
                    if (v.propertyName === "Inverted") {
                        this.dataTypeLabel.setFontColor((<any>v.object).Inverted ? "red" : "#4a4a4a");

                        try {

                            if (element.itemMoved) {
                                await ruleEngineService.updateItem(element, LogicUpdateScope.InvertedValueUpdated);
                            }
                        } catch (error) {
                            errorHandler.notifyError(error);
                        }
                    }
                });

                if (element.Inputs.length > 0) {
                    const input = this.createPort("input");
                    input.setName(element.Inputs[0].PortId);
                    input.setId(element.Inputs[0].PortId);


                    input.setColor("red");
                    input.setBackgroundColor("red");

                    let dataLabel = new draw2d.shape.basic.Label({
                        text: ValueHandler.handleValue((element.NodeInstance).WriteValue),
                        textLength: "100%",
                        stroke: 0,
                        radius: 0,
                        bgColor: null,
                        padding: 5,
                        paddingLeft: 10,
                        paddingRight: 10,
                        fontColor: "lightgray",
                        fontSize: 9,
                        resizeable: false,
                        width: 100
                    });

                    if (element.NodeInstance) {
                        element.NodeInstance.notifyChangeEvent.subscribe((v) => {
                            if (v.propertyName === "WriteValue") {
                                let value = ValueHandler.handleValue((<any>v.object).WriteValue);

                                dataLabel.setText(value);
                            }

                        });
                    }
                    input.add(dataLabel, new LogicShapeValueLocator({ marginBottom: 12, marginLeft: -30 }));
                    dataLabel.toBack();

                    input.on("connect", async function (emitterPort, connection) {
                        try {
                            await LinkService.handleOnConnection(linkService, input, connection, true, element, ruleEngineService);
                        }
                        catch (error) {
                            errorHandler.notifyError(error);
                        }

                    });

                    input.on("disconnect", async function (emitterPort, connection) {
                        try {
                            await LinkService.handleOnDisconnection(linkService, connection, true);
                        }
                        catch (error) {

                            errorHandler.notifyError(error);
                        }
                    });
                }
                if (element.Outputs.length > 0) {

                    const output = this.createPort("output");
                    output.setName(element.Outputs[0].PortId);
                    output.setId(element.Outputs[0].PortId);

                    let dataLabel = new draw2d.shape.basic.Label({
                        text: ValueHandler.handleValue((element.NodeInstance).ReadValue),
                        textLength: "100%",
                        stroke: 0,
                        radius: 0,
                        bgColor: null,
                        padding: 5,
                        paddingLeft: 10,
                        paddingRight: 10,
                        fontColor: "lightgray",
                        fontSize: 9,
                        resizeable: false,
                        width: 100
                    });

                    if (element.NodeInstance) {
                        element.NodeInstance.notifyChangeEvent.subscribe((v) => {
                            if (v.propertyName === "ReadValue") {
                                let value = ValueHandler.handleValue((<any>v.object).ReadValue);
                                dataLabel.setText(value);
                            }
                        });
                    }
                    output.add(dataLabel, new LogicShapeValueLocator({ marginBottom: 12, marginRight: 10 }));
                    dataLabel.toBack();

                    output.on("connect", async function (emitterPort, connection) {
                        try {
                            await LinkService.handleOnConnection(linkService, output, connection, false, element, ruleEngineService);
                        }
                        catch (error) {

                            errorHandler.notifyError(error);
                        }
                    });

                    output.on("disconnect", async function (emitterPort, connection) {
                        try {
                            await LinkService.handleOnDisconnection(linkService, connection, false);
                        }
                        catch (error) {

                            errorHandler.notifyError(error);
                        }
                    });
                }

                this.on("mouseenter", () => {
                    this.showTooltip();
                });
                this.on("mouseleave", () => {
                    this.hideTooltip()
                });
                this.on("dragstart", () => {
                    this.hideTooltip()
                });

                this.label.on("mouseenter", () => {
                    this.showTooltip();
                });
                this.label.on("mouseleave", () => {
                    this.hideTooltip()
                });
                this.label.on("dragstart", () => {
                    this.hideTooltip()
                });

                this.on("move", (context) => {
                    element.X = context.x;
                    element.Y = context.y;
                    this.hideTooltip();
                });

                this.on("dragEnd", async (context, data) => {
                    try {

                        if (element.itemMoved) {
                            await ruleEngineService.updateItem(element, LogicUpdateScope.Drag);
                        }
                    } catch (error) {
                        errorHandler.notifyError(error);
                    }
                });


                if (element.Inputs.length > 0) {
                    this.add(this.dataTypeLabel);

                }

                this.add(this.label);
            },
            getMinWidth() {
                if (this.label) {
                    return Math.max(this.label.cachedMinWidth, 80);
                }
                return 80;
            },
            onDragEnd(x, y, shiftKey, ctrlKey) {
                this._super();

                this.fireEvent("dragEnd", { x: x, y: y });

            },
            showTooltip: function () {
                this.tooltip = $('<div class="tooltip">' + this.element.FullName + '</div>')
                    .appendTo('body');
                this.positionTooltip();
            },

            positionTooltip: function () {
                if (this.tooltip === null) {
                    return
                }

                var width = this.tooltip.outerWidth(true)
                var pos = this.canvas.fromCanvasToDocumentCoordinate(
                    this.getAbsoluteX() + this.getWidth() / 2 - width / 2 + 8,
                    this.getAbsoluteY() + this.getHeight() + 10)

                // remove the scrolling part from the tooltip because the tooltip is placed
                // inside the scrolling container
                pos.x += this.canvas.getScrollLeft()
                pos.y += this.canvas.getScrollTop()

                this.tooltip.css({ 'top': pos.y, 'left': pos.x })
            },
            hideTooltip: function () {
                if (this.tooltip !== null) {
                    this.tooltip.remove();
                    this.tooltip = null;
                }
            }
        });

    }
}
