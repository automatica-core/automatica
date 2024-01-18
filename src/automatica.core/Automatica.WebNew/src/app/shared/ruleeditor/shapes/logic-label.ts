import { NodeInstance2RulePage } from "src/app/base/model/node-instance-2-rule-page";
import { RuleInterfaceInstance } from "src/app/base/model/rule-interface-instance";

declare var draw2d: any;
declare var $: any;


export class LogicLables {
    public static addLables(logic) {

        logic.NodeInstanceText = draw2d.shape.basic.Label.extend({
            init: function (attr, realParent, instance: NodeInstance2RulePage) {
                this._super(attr);
                this.instance = instance;

                this.on("mouseenter", () => {
                    this.showTooltip();
                });
                this.on("mouseleave", () => {
                    this.hideTooltip()
                });
                this.on("dragstart", () => {
                    this.hideTooltip()
                });
                this.on("click", () => {
                    this.instance.Inverted = !this.instance.Inverted;
                });
            },
            showTooltip: function () {
                this.tooltip = $('<div class="invert_tooltip">°</div>')
                    .appendTo('body');
                this.positionTooltip();
            },

            positionTooltip: function () {
                if (this.tooltip === null) {
                    return
                }

                var pos = this.canvas.fromCanvasToDocumentCoordinate(
                    this.getAbsoluteX() + 8,
                    this.getAbsoluteY() - this.getHeight() + 5)

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
            },
            getX() {
                return this.x + 2;
            },
        });

        logic.LogicPortText = draw2d.shape.basic.Label.extend({

            NAME: "LogicLabel",

            direction: 0,
            fontSize: 10,
            realParent: void 0,

            init: function (attr, direction, realParent, portInstance: RuleInterfaceInstance) {
                this._super(attr);

                this.direction = direction;
                this.realParent = realParent;
                this.portInstance = portInstance;


                if (this.direction === 1) {
                    this.setPadding({ top: 0, right: -20, bottom: 0, left: 20 });
                }

                if (this.direction === 1) {
                    this.setTextAlign("end")
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
                this.on("click", () => {
                    this.portInstance.Inverted = !this.portInstance.Inverted;
                });
            },
            getMinWidth() {
                if (this.direction === 0) {
                    return 30;
                }

                return 50;
            },

            getX() {
                if (this.direction === 1) {
                    return this.realParent.width - 30;
                }
                return this.x;
            },
            showTooltip: function () {
                this.tooltip = $('<div class="invert_tooltip">°</div>')
                    .appendTo('body');
                this.positionTooltip();
            },

            positionTooltip: function () {
                if (this.tooltip === null) {
                    return
                }

                var width = this.tooltip.outerWidth(true)
                var pos = this.canvas.fromCanvasToDocumentCoordinate(
                    this.getAbsoluteX(),
                    this.getAbsoluteY() - this.getHeight() + 5)

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

        logic.Label = draw2d.shape.basic.Label.extend({
            NAME: "Label",
            fontSize: 10,

            init: function (attr) {
                this._super(attr);
            },

            getWidth() {
                return Math.max(this.width, this.minWidth);
            }

        });


        logic.MinWidthLabel = draw2d.shape.basic.Label.extend({

            getWidth() {
                return Math.max(this.width, this.minWidth);
            }
        });
    }
}
