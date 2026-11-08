import { RuleInterfaceInstance } from "./rule-interface-instance";
import { RuleInterfaceParameterDataType, RuleInterfaceTemplate } from "./rule-interface-template";
import { ControlConfiguration } from "./control-configuration";
import { PropertyTemplateType } from "./property-template";
import { RuleInterfaceParamProperty } from "./virtual-props/rule-instance/rule-interface-param-property";

function controlsParam(): RuleInterfaceInstance {
  const template = new RuleInterfaceTemplate();
  template.ObjId = "a05d4062-8eb1-4a7f-8259-61cdbea05f21";
  template.Name = "LIGHTING_CENTRAL.PARAM.LIGHTS.NAME";
  template.ParameterDataType = RuleInterfaceParameterDataType.Controls;

  const instance = new RuleInterfaceInstance(void 0);
  instance.Template = template;
  return instance;
}

describe("RuleInterfaceInstance (controls parameter)", () => {

  it("is edited with the controls picker", () => {
    const property = new RuleInterfaceParamProperty(controlsParam());

    expect(property.PropertyTemplate.PropertyType.Type).toBe(PropertyTemplateType.Controls);
  });

  it("nothing picked yet gives an empty configuration", () => {
    const value = controlsParam().Value;

    expect(value instanceof ControlConfiguration).toBeTrue();
    expect(value.Controls).toEqual([]);
  });

  it("stores the picked controls as json and reads them back", () => {
    const param = controlsParam();
    const picked = new ControlConfiguration();
    picked.Controls = ["11111111-1111-1111-1111-111111111111", "22222222-2222-2222-2222-222222222222"];

    param.Value = picked;

    expect(JSON.parse(param.ValueString).Controls).toEqual(picked.Controls);
    expect(param.Value.Controls).toEqual(picked.Controls);
  });

  it("clears the value for anything else", () => {
    const param = controlsParam();
    param.ValueString = "{\"Controls\":[]}";

    param.Value = 5;

    expect(param.ValueString).toBeUndefined();
  });
});
