import { L10nTranslationService } from "angular-l10n";
import { RulePage } from "../base/model/rule-page";
import { RuleInstance } from "../base/model/rule-instance";
import { RuleTemplate } from "../base/model/rule-template";
import { RuleInterfaceTemplate, RuleInterfaceParameterDataType } from "../base/model/rule-interface-template";
import { RuleInterfaceInstance } from "../base/model/rule-interface-instance";
import { NodeInstance } from "../base/model/node-instance";
import { NodeTemplate } from "../base/model/node-template";
import { NodeDataTypeEnum } from "../base/model/node-data-type";
import { InterfaceTypeEnum } from "../base/model/interface-type";
import { NodeInstance2RulePage } from "../base/model/node-instance-2-rule-page";
import { Link } from "../base/model/link";

/**
 * Builders for logic pages: real model classes (the editor relies on instanceof), fixed ids so failures are readable.
 */

let sequence = 0;
export function nextId(prefix: string) {
    sequence++;
    return `${prefix}-${sequence}`;
}

export interface PortSpec {
    name: string;
    /** Template.MaxLinks, 0 = unlimited */
    maxLinks?: number;
}

function interfaceTemplate(spec: PortSpec | string, dir: "I" | "O" | "P", sortOrder: number) {
    const s: PortSpec = typeof spec === "string" ? { name: spec } : spec;
    const t = new RuleInterfaceTemplate();
    t.ObjId = nextId("tpl-port");
    t.Name = s.name;
    t.Key = s.name;
    t.This2RuleInterfaceDirection = dir === "I" ? 1 : dir === "O" ? 2 : 3;
    t.InterfaceDirection = <any>{ Key: dir };
    t.MaxLinks = s.maxLinks ?? 0;
    t.SortOrder = sortOrder;
    t.ParameterDataType = RuleInterfaceParameterDataType.NoParameter;
    t.IsLinkableParameter = false;
    return t;
}

export function makeRuleTemplate(name: string, inputs: (PortSpec | string)[] = [], outputs: (PortSpec | string)[] = []) {
    const template = new RuleTemplate();
    template.ObjId = nextId("tpl");
    template.Name = name;
    template.Description = name + ".DESCRIPTION";
    let order = 0;
    template.Interfaces = [
        ...inputs.map(a => interfaceTemplate(a, "I", order++)),
        ...outputs.map(a => interfaceTemplate(a, "O", order++))
    ];
    return template;
}

/** a rule block; stored X/Y are in the old draw2d units (the editor spreads them by 1.5 / 1.2) */
export function makeRule(translate: L10nTranslationService, name: string,
    inputs: (PortSpec | string)[] = [], outputs: (PortSpec | string)[] = [], x = 0, y = 0): RuleInstance {
    const rule = RuleInstance.fromRuleTemplate(makeRuleTemplate(name, inputs, outputs), translate);
    rule.ObjId = nextId("rule");
    for (const intf of rule.Interfaces) {
        intf.ObjId = nextId("port");
        intf.This2RuleInstance = rule.ObjId;
    }
    rule.X = x;
    rule.Y = y;
    rule.setSaved();
    return rule;
}

export function input(rule: RuleInstance, index = 0): RuleInterfaceInstance {
    return rule.Inputs[index];
}

export function output(rule: RuleInstance, index = 0): RuleInterfaceInstance {
    return rule.Outputs[index];
}

export function makeNodeInstance(name: string, type: NodeDataTypeEnum,
    opts: { readable?: boolean, writeable?: boolean, interfaceType?: string, parent?: NodeInstance } = {}): NodeInstance {
    const node = new NodeInstance(opts.parent);
    node.ObjId = nextId("node");
    node.Name = name;
    const template = new NodeTemplate();
    template.ObjId = nextId("node-tpl");
    template.IsReadable = opts.readable ?? true;
    template.IsWriteable = opts.writeable ?? true;
    template.NodeTypeEnum = type;
    template.ProvidesInterface = <any>{ Type: opts.interfaceType ?? InterfaceTypeEnum.Value };
    node.NodeTemplate = template;
    if (opts.parent) {
        opts.parent.Children.push(node);
    }
    return node;
}

/** a data point block on the page */
export function makeDataPoint(page: RulePage, nodeInstance: NodeInstance, x = 0, y = 0): NodeInstance2RulePage {
    const dp = NodeInstance2RulePage.createFromNodeInstance(nodeInstance, page);
    dp.ObjId = nextId("dp");
    // createFromNodeInstance built the ports from the random id, rebuild them from the fixed one
    dp.Inputs = [];
    dp.Outputs = [];
    dp.afterFromJson();
    dp.X = x;
    dp.Y = y;
    dp.setSaved();
    return dp;
}

export function makePage(name = "Page 1"): RulePage {
    const page = new RulePage();
    page.ObjId = nextId("page");
    page.Name = name;
    page.Description = name;
    return page;
}

export function linkRules(page: RulePage, from: RuleInterfaceInstance, to: RuleInterfaceInstance): Link {
    const link = new Link();
    link.ObjId = nextId("link");
    link.This2RulePage = page.ObjId;
    link.FromRuleInstance = from;
    link.This2RuleInterfaceInstanceOutput = from.ObjId;
    link.ToRuleInstance = to;
    link.This2RuleInterfaceInstanceInput = to.ObjId;
    return link;
}

export function linkDataPointToRule(page: RulePage, from: NodeInstance2RulePage, to: RuleInterfaceInstance): Link {
    const link = new Link();
    link.ObjId = nextId("link");
    link.This2RulePage = page.ObjId;
    link.FromNodeInstance = from;
    link.This2NodeInstance2RulePageOutput = from.ObjId;
    link.ToRuleInstance = to;
    link.This2RuleInterfaceInstanceInput = to.ObjId;
    return link;
}

export function linkRuleToDataPoint(page: RulePage, from: RuleInterfaceInstance, to: NodeInstance2RulePage): Link {
    const link = new Link();
    link.ObjId = nextId("link");
    link.This2RulePage = page.ObjId;
    link.FromRuleInstance = from;
    link.This2RuleInterfaceInstanceOutput = from.ObjId;
    link.ToNodeInstance = to;
    link.This2NodeInstance2RulePageInput = to.ObjId;
    return link;
}
