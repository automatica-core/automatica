import { NodeInstance } from "./node-instance";
import { LABEL_VISU_TEMPLATE, NodeTemplate } from "./node-template";

const CAMERA_VISU_TEMPLATE = "3733855a-667d-4cdd-bd43-6a7246046e4d";

function nodeWith(dataType: number, visuTemplate: string): NodeInstance {
  const template = new NodeTemplate();
  template.This2NodeDataType = dataType;
  template.IsWriteableFixed = true;
  template.This2DefaultMobileVisuTemplate = visuTemplate;

  const node = new NodeInstance(void 0);
  node.NodeTemplate = template;
  (node as any).addVirtualProperties();
  return node;
}

function keys(node: NodeInstance): string[] {
  return node.Properties.map(p => p.PropertyTemplate.Key);
}

describe("NodeInstance visu properties", () => {

  it("a node with a value gets the visu and the value properties", () => {
    const node = nodeWith(1, LABEL_VISU_TEMPLATE);

    expect(keys(node)).toContain("use_in_visu");
    expect(keys(node)).toContain("isReadable");
    expect(keys(node)).toContain("trending");
  });

  it("a node without a value and with the label tile gets no visu properties", () => {
    const node = nodeWith(0, LABEL_VISU_TEMPLATE);

    expect(keys(node)).not.toContain("use_in_visu");
  });

  it("a camera (no value, own tile) gets the visu properties but none of the value properties", () => {
    const node = nodeWith(0, CAMERA_VISU_TEMPLATE);

    expect(node.NodeTemplate.HasOwnVisuTile).toBeTrue();
    expect(keys(node)).toContain("use_in_visu");
    expect(keys(node)).not.toContain("isReadable");
    expect(keys(node)).not.toContain("trending");
  });

  it("compares the label tile id case insensitively", () => {
    const template = new NodeTemplate();
    template.This2DefaultMobileVisuTemplate = LABEL_VISU_TEMPLATE.toUpperCase();

    expect(template.HasOwnVisuTile).toBeFalse();
  });
});
