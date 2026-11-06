// Commissions a Matter device with matter.js and exercises reads, a subscription and commands.
//   node commission.mjs <pairing-code>      (fresh controller storage each run)
import { Environment, ServerNode, Logger, Seconds } from "@matter/main";
import { ControllerBehavior } from "@matter/node";
import { rmSync } from "node:fs";

const pairingCode = process.argv[2];
const storage = new URL("./ctrl-storage", import.meta.url).pathname.replace(/^\/([A-Z]:)/, "$1");
if (process.argv[3] !== "keep") rmSync(storage, { recursive: true, force: true });
Environment.default.vars.set("storage.path", storage);
Logger.level = process.env.MJS_LOG ?? "notice";

const controller = await ServerNode.create(ServerNode.RootEndpoint.with(ControllerBehavior), {
    id: "controller",
    network: { port: 5590 },
    controller: { adminFabricLabel: "matter.js test" },
});
await controller.start();
console.log("controller started");

let node = controller.peers.commissioned[0];
if (!node) {
    node = await controller.peers.commission({ pairingCode, timeout: Seconds(120) });
    console.log("COMMISSIONED", node.id);
} else {
    console.log("already commissioned", node.id);
}

await new Promise(r => setTimeout(r, 3000));

function dump(endpoint, depth = 0) {
    const state = endpoint.state;
    const pad = "  ".repeat(depth);
    const desc = state.descriptor;
    console.log(`${pad}endpoint ${endpoint.number} types=${JSON.stringify(desc?.deviceTypeList?.map(d => d.deviceType))} clusters=${JSON.stringify(desc?.serverList)} parts=${JSON.stringify(desc?.partsList)}`);
    if (state.basicInformation) console.log(`${pad}  basicInformation vendor=${state.basicInformation.vendorName} product=${state.basicInformation.productName} unique=${state.basicInformation.uniqueId}`);
    if (state.bridgedDeviceBasicInformation) console.log(`${pad}  bridged label=${state.bridgedDeviceBasicInformation.nodeLabel} reachable=${state.bridgedDeviceBasicInformation.reachable}`);
    if (state.onOff) console.log(`${pad}  onOff=${state.onOff.onOff}`);
    if (state.levelControl) console.log(`${pad}  level=${state.levelControl.currentLevel}`);
    if (state.windowCovering) console.log(`${pad}  covering pos=${state.windowCovering.currentPositionLiftPercent100ths} status=${JSON.stringify(state.windowCovering.operationalStatus)}`);
    for (const child of endpoint.parts) dump(child, depth + 1);
}
dump(node);

const find = (label) => {
    const walk = (ep) => {
        if (ep.state.bridgedDeviceBasicInformation?.nodeLabel === label) return ep;
        for (const c of ep.parts) { const f = walk(c); if (f) return f; }
    };
    return walk(node);
};

const light = find("Demo Light");
if (light) {
    console.log("toggle light, before:", light.state.onOff.onOff);
    await light.commandsOf("onOff").toggle();
    await new Promise(r => setTimeout(r, 1500));
    console.log("light after toggle:", light.state.onOff.onOff);
}
const dimmer = find("Demo Dimmer");
if (dimmer) {
    await dimmer.commandsOf("levelControl").moveToLevelWithOnOff({ level: 127, transitionTime: null, optionsMask: {}, optionsOverride: {} });
    await new Promise(r => setTimeout(r, 1500));
    console.log("dimmer level after moveToLevel(127):", dimmer.state.levelControl.currentLevel, "onOff", dimmer.state.onOff.onOff);
}
const blind = find("Demo Blind");
if (blind) {
    await blind.commandsOf("windowCovering").goToLiftPercentage({ liftPercent100thsValue: 5000 });
    await new Promise(r => setTimeout(r, 3000));
    console.log("blind after goTo 50%:", blind.state.windowCovering.currentPositionLiftPercent100ths);
}

if (process.argv[4] === "wait") {
    console.log("waiting for reports, ctrl+c to stop");
    node.eventsOf?.("onOff");
    await new Promise(r => setTimeout(r, Number(process.argv[5] ?? 30) * 1000));
    if (light) console.log("light state now:", light.state.onOff.onOff);
}

await controller.close();
console.log("DONE");
process.exit(0);
