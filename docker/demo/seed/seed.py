#!/usr/bin/env python3
"""
Seeds a fresh Automatica.Core instance with the demo configuration, through the real Web API.

    python seed.py --url http://localhost:5011 --user sa --password sa

Creates driver nodes (only drivers that run without hardware), areas and categories, the visualization flags of the
nodes, logic pages with an instance of every logic type wired to the driver data points, and automations.
Only the Python standard library is used. All values (names, addresses, e-mail) are made up for the demo.

`python seed.py --prepare-db <file>` is the one offline step: a fresh database has the initial administrator 'sa'
with "must change password" set, the demo keeps sa/sa (server:allow_default_passwords). See run-seed.ps1.
"""
import argparse
import json
import sqlite3
import sys
import time
import urllib.error
import urllib.request
import uuid

# interface types (Automatica.Core.Base/Templates/InterfaceTypesUid.cs)
VIRTUAL = "177a9144-3f07-4fd2-a71d-51db61c51ad5"
ETHERNET = "c45eda96-7246-4fa0-9239-9ebb52e7ed66"

# area templates (Automatica.Core.Internals/Areas/Areas.cs)
AREA_BUILDING = "a2ec8a7b-1b8e-4dd7-9d4e-9567c1760af2"
AREA_BUILDINGPART = "8592c6ac-1093-4242-ba53-7d4456cbd22e"
AREA_ROOM = "492e20ce-7d6a-44d4-aa07-454851180c7f"
AREA_HALLWAY = "60cc7657-a3cc-4956-af1f-f0d24581e9b9"

# PropertyTemplateType -> PropertyInstance value field (Automatica.Core.EF/PropertyInstance.cs)
PROPERTY_FIELDS = {
    0: "valueString", 5: "valueString", 6: "valueString", 8: "valueString", 14: "valueString", 17: "valueString",
    19: "valueString", 25: "valueString", 100: "valueString", 300: "valueString",
    1: "valueInt", 3: "valueInt", 7: "valueInt", 9: "valueInt", 12: "valueInt", 13: "valueInt",
    2: "valueDouble", 10: "valueDouble",
    4: "valueBool",
    18: "valueLong",
    15: "valueNodeInstance",
}

# RuleInterfaceParameterDataType -> RuleInterfaceInstance value field (Automatica.Core.EF/Models/RuleInterfaceTemplate.cs)
PARAMETER_FIELDS = {1: "valueInteger", 2: "valueDouble", 8: "valueBool", 3: "valueString", 4: "valueString",
                    5: "valueString", 6: "valueString", 7: "valueString", 9: "valueString", 10: "valueString"}

UPDATE_SCOPE_GENERIC = 1
UPDATE_SCOPE_SPECIFIC = 2

WEEKDAYS = ["monday", "tuesday", "wednesday", "thursday", "friday"]


def new_id():
    return str(uuid.uuid4())


class Api:
    def __init__(self, url):
        self.base = url.rstrip("/") + "/webapi/"
        self.token = None

    def call(self, method, path, body=None, allow_error=False):
        data = None if body is None else json.dumps(body).encode("utf-8")
        request = urllib.request.Request(self.base + path, data=data, method=method,
                                         headers={"Content-Type": "application/json"})
        if self.token:
            request.add_header("Authorization", "Bearer " + self.token)
        try:
            with urllib.request.urlopen(request, timeout=120) as response:
                text = response.read()
                return json.loads(text) if text else None
        except urllib.error.HTTPError as e:
            detail = e.read().decode("utf-8", "replace")[:1000]
            if allow_error:
                print(f"  ! {method} {path}: HTTP {e.code} {detail}")
                return None
            raise RuntimeError(f"{method} {path}: HTTP {e.code} {detail}") from None

    def get(self, path):
        return self.call("GET", path)

    def post(self, path, body=None, allow_error=False):
        return self.call("POST", path, body, allow_error)

    def put(self, path, body=None):
        return self.call("PUT", path, body)

    def patch(self, path, body=None):
        return self.call("PATCH", path, body)

    def login(self, user, password):
        for _ in range(90):
            try:
                result = self.call("POST", "auth/login", {"username": user, "password": password})
                break
            except (urllib.error.URLError, ConnectionError, RuntimeError) as e:
                if isinstance(e, RuntimeError) and "HTTP 401" in str(e):
                    raise
                time.sleep(2)
        else:
            raise RuntimeError("server not reachable")
        if result.get("mustChangePassword"):
            raise RuntimeError("the user must change the password first (run seed.py --prepare-db on the database)")
        self.token = result["token"]

    def wait_started(self):
        for _ in range(120):
            state = self.get("server/state")
            if state and state.get("status") == 4:
                return
            time.sleep(1)
        raise RuntimeError("server did not start")


class Seeder:
    def __init__(self, api):
        self.api = api
        self.node_templates = {t["objId"]: t for t in api.get("nodeTemplates")}
        self.nodes = {}        # demo name -> node instance
        self.areas = {}        # name -> area id
        self.categories = {}   # short name -> category id
        self.logic_templates = api.get("logics/templates")
        self.logics = {}       # demo name -> (rule instance id, {interface key: interface instance id})

    # ------------------------------------------------------------------ nodes

    def template(self, key, parent):
        parent_template = self.node_templates[parent["this2NodeTemplate"]]
        provides = parent_template["providesInterface2InterfaceType"]
        candidates = [t for t in self.node_templates.values()
                      if t["key"] == key and t["needsInterface2InterfacesType"] == provides]
        if not candidates:
            raise RuntimeError(f"no node template {key} below {parent.get('name')}")
        return candidates[0]

    @staticmethod
    def clean_node(node):
        result = {k: v for k, v in node.items() if not k.endswith("Navigation")}
        result["inverseThis2ParentNodeInstanceNavigation"] = []
        props = []
        for prop in node.get("propertyInstance") or []:
            clean = {k: v for k, v in prop.items() if not k.endswith("Navigation") and k != "value"}
            props.append(clean)
        result["propertyInstance"] = props
        return result

    def set_properties(self, node, props):
        by_key = {}
        for prop in node.get("propertyInstance") or []:
            template = prop.get("this2PropertyTemplateNavigation") or {}
            by_key[template.get("key")] = prop
        for key, value in props.items():
            prop = by_key.get(key)
            if prop is None:
                raise RuntimeError(f"node {node['name']} has no property {key} ({list(by_key)})")
            template = prop["this2PropertyTemplateNavigation"]
            prop_type = template.get("this2PropertyType")
            if prop_type is None:
                prop_type = (template.get("this2PropertyTypeNavigation") or {}).get("type")
            field = PROPERTY_FIELDS[prop_type]
            prop[field] = value
            if field == "valueBool":
                prop["valueInt"] = 1 if value else 0

    def update(self, node, scope):
        self.api.post(f"nodeInstancesV2/update?updateScope={scope}", self.clean_node(node))

    def create(self, name, parent, key, props=None, visu=None, disabled=False, writeable=None):
        """creates the node with the server side factory (properties and default children) and saves the changes"""
        parent_node = self.nodes[parent] if isinstance(parent, str) else parent
        template = self.template(key, parent_node)
        node = self.api.post(f"nodeInstancesV2/create/en/{parent_node['objId']}/{template['objId']}")
        node["name"] = name
        if props:
            self.set_properties(node, props)
        node["isDisabled"] = disabled
        if writeable is not None:
            node["isWriteable"] = writeable
        if visu:
            self.apply_visu(node, visu)
        self.update(node, UPDATE_SCOPE_SPECIFIC if props or disabled else UPDATE_SCOPE_GENERIC)
        self.nodes[name] = node
        for child in node.get("inverseThis2ParentNodeInstanceNavigation") or []:
            child_template = self.node_templates.get(child["this2NodeTemplate"], {})
            self.nodes.setdefault(f"{name}/{child_template.get('key')}", child)
        print(f"  node {name} ({key})")
        return node

    def apply_visu(self, node, visu):
        area, category = visu.get("area"), visu.get("category")
        node["useInVisu"] = True
        node["this2AreaInstance"] = self.areas[area] if area else None
        node["this2CategoryInstance"] = self.categories[category] if category else None
        node["isFavorite"] = bool(visu.get("favorite"))
        if visu.get("name"):
            node["visuName"] = visu["name"]

    def visu(self, node_name, **visu):
        """shows an existing (e.g. default created) node in the visualization"""
        node = self.nodes[node_name]
        self.apply_visu(node, visu)
        self.update(node, UPDATE_SCOPE_GENERIC)

    def node_id(self, name):
        return self.nodes[name]["objId"]

    def keep_children(self, name, keep):
        """deletes the default created descendants of a node whose template key is not in 'keep' (children first)"""
        def prune(node):
            kept = []
            for child in node.get("inverseThis2ParentNodeInstanceNavigation") or []:
                key = self.node_templates[child["this2NodeTemplate"]]["key"]
                prune(child)
                if key in keep:
                    kept.append(child)
                else:
                    self.api.post("nodeInstancesV2/delete", {"objId": child["objId"]})
                    self.nodes.pop(f"{name}/{key}", None)
            node["inverseThis2ParentNodeInstanceNavigation"] = kept
        prune(self.nodes[name])

    # ------------------------------------------------------------------ areas / categories

    def seed_areas(self):
        project = [a for a in self.api.get("areas/all") if a.get("this2Parent") is None][0]
        pending = []

        def add(name, template, parent, icon):
            area = {"objId": new_id(), "name": name, "description": "", "icon": icon,
                    "this2AreaTemplate": template, "this2Parent": parent}
            pending.append(area)
            self.areas[name] = area["objId"]
            return area["objId"]

        house = add("Demo House", AREA_BUILDING, project["objId"], "home")
        ground = add("Ground floor", AREA_BUILDINGPART, house, "box")
        first = add("First floor", AREA_BUILDINGPART, house, "box")
        outside = add("Outside", AREA_BUILDINGPART, house, "tree")
        add("Living room", AREA_ROOM, ground, "couch")
        add("Kitchen", AREA_ROOM, ground, "utensils")
        add("Hallway", AREA_HALLWAY, ground, "door-open")
        add("Bedroom", AREA_ROOM, first, "bed")
        add("Office", AREA_ROOM, first, "laptop")
        add("Bathroom", AREA_ROOM, first, "bath")
        add("Garden", AREA_ROOM, outside, "seedling")
        add("Garage", AREA_ROOM, outside, "car")
        # one 'add' call: it reloads the area cache. A PUT per area does not work for a new area: the node update
        # fails with KeyNotFoundException in AreaCache.Get (AreaCache.cs:37, Add/Update never fill _areaInstances)
        self.api.post("areas/add", pending)
        print(f"  {len(self.areas)} areas")

    def seed_categories(self):
        # the default categories of the database init (COMMON.CATEGORY_INSTANCE.<NAME>.NAME)
        for category in self.api.get("categories/all"):
            short = category["name"].replace("COMMON.CATEGORY_INSTANCE.", "").replace(".NAME", "")
            self.categories[short] = category["objId"]
        print(f"  categories: {', '.join(sorted(self.categories))}")

    def seed_settings(self):
        settings = self.api.get("settings")
        changed = []
        # a made-up location (Vienna city centre) for the sun driver and the sun triggers
        for setting in settings:
            if setting["valueKey"] == "Latitude":
                setting["valueDouble"] = 48.2082
                setting["value"] = 48.2082
                changed.append(setting)
            elif setting["valueKey"] == "Longitude":
                setting["valueDouble"] = 16.3738
                setting["value"] = 16.3738
                changed.append(setting)
            elif setting["valueKey"] == "projectName":
                setting["valueText"] = "Automatica Demo House"
                setting["value"] = "Automatica Demo House"
                changed.append(setting)
        self.api.post("settings", changed)

    # ------------------------------------------------------------------ drivers

    def seed_drivers(self, root):
        children = root.get("inverseThis2ParentNodeInstanceNavigation") or []

        def interface(provides):
            for child in children:
                template = self.node_templates.get(child["this2NodeTemplate"])
                if template and template["providesInterface2InterfaceType"] == provides:
                    return child
            raise RuntimeError(f"no interface node {provides}")

        self.nodes["Virtual"] = interface(VIRTUAL)
        self.nodes["Ethernet"] = interface(ETHERNET)

        # without a license the core runs at most 100 data points (LicenseContext.cs:188): the demo stays below

        # --- constants
        self.create("Constants", "Virtual", "consts")
        self.create("Pi", "Constants", "const_pi")
        self.create("Zero", "Constants", "const", {"const_value": 0})
        self.create("Two", "Constants", "const", {"const_value": 2})
        self.create("Ten", "Constants", "const", {"const_value": 10})
        self.create("Thirty", "Constants", "const", {"const_value": 30})
        self.create("Thousand", "Constants", "const", {"const_value": 1000})
        self.create("Welcome text", "Constants", "const_string", {"const_value": "Welcome to the Automatica demo house"},
                    visu={"area": "Hallway", "category": "TECHNOLOGY", "favorite": True})

        # --- machine flags (virtual, writable data points)
        self.create("Flags", "Virtual", "machine-flags")
        binary = [
            ("Living room light", {"area": "Living room", "category": "LIGHTNING", "favorite": True}),
            ("Kitchen light", {"area": "Kitchen", "category": "LIGHTNING", "favorite": True}),
            ("Hallway light", {"area": "Hallway", "category": "LIGHTNING"}),
            ("Garden light", {"area": "Garden", "category": "LIGHTNING"}),
            ("Bedroom light", {"area": "Bedroom", "category": "LIGHTNING"}),
            ("Presence living room", {"area": "Living room", "category": "ALARM"}),
            ("Hallway motion", {"area": "Hallway", "category": "ALARM"}),
            ("Bedroom window open", {"area": "Bedroom", "category": "WINDOW"}),
            ("Holiday mode", {"area": "Demo House", "category": "TECHNOLOGY", "favorite": True}),
            ("Alarm armed", {"area": "Demo House", "category": "ALARM", "favorite": True}),
            ("Alarm light", {"area": "Garden", "category": "ALARM"}),
            ("Heating on", {"area": "Living room", "category": "HEATING"}),
            ("Any light on", {"area": "Demo House", "category": "LIGHTNING"}),
            ("Second half of minute", {"area": "Office", "category": "TECHNOLOGY"}),
            ("Second is even", {"area": "Office", "category": "TECHNOLOGY"}),
            ("Presence confirmed", None),
            ("Morning routine", {"area": "Bedroom", "category": "TECHNOLOGY"}),
            ("Garage door impulse", {"area": "Garage", "category": "TECHNOLOGY"}),
            ("Pump", {"area": "Garage", "category": "TECHNOLOGY"}),
        ]
        for name, visu in binary:
            self.create(name, "Flags", "machine-flag-binary", visu=visu)
        numbers = [
            ("Living room temperature", {"area": "Living room", "category": "TEMPERATURE", "favorite": True}),
            ("Living room setpoint", {"area": "Living room", "category": "HEATING"}),
            ("Heating demand", {"area": "Living room", "category": "HEATING"}),
            ("Dimmer value", {"area": "Living room", "category": "LIGHTNING"}),
            ("Blinds position", {"area": "Bedroom", "category": "SHADING"}),
            ("Minute counter", {"area": "Office", "category": "TECHNOLOGY"}),
            ("Bitcoin in thousand EUR", {"area": "Office", "category": "ENERGY"}),
            ("Alarm state value", None),
        ]
        for name, visu in numbers:
            self.create(name, "Flags", "machine-flag-number", visu=visu)
        self.create("Status text", "Flags", "machine-flag-string",
                    visu={"area": "Demo House", "category": "TECHNOLOGY", "favorite": True})
        self.create("Last automation", "Flags", "machine-flag-string", visu={"area": "Office", "category": "TECHNOLOGY"})
        self.create("Greeting", "Flags", "machine-flag-string", visu={"area": "Hallway", "category": "TECHNOLOGY"})

        # --- times and sun (the default children are created by the server)
        self.create("Date/Time", "Virtual", "times")
        self.keep_children("Date/Time", {"times-time", "times-date", "times-seconds", "times-minutes", "times-hours",
                                         "times-day-of-week"})
        for key, label, area in [("times-time", "Time", "Office"), ("times-date", "Date", "Office"),
                                 ("times-seconds", "Second", "Office"), ("times-day-of-week", "Day of week", "Office")]:
            self.visu(f"Date/Time/{key}", area=area, category="TECHNOLOGY", favorite=key == "times-time", name=label)
        self.create("Sun", "Virtual", "sun")
        self.keep_children("Sun", {"sunrise", "sunset", "is-daylight"})
        for key, label in [("sunrise", "Sunrise"), ("sunset", "Sunset"), ("is-daylight", "Daylight")]:
            self.visu(f"Sun/{key}", area="Garden", category="WEATHER", name=label)

        # --- crypto ticker (needs internet)
        self.create("Crypto ticker", "Virtual", "blockchain_ticker", {"poll": 120})
        self.keep_children("Crypto ticker", {"blockchain-btc", "blockchain-btc-eur", "blockchain-btc-eur-with-symbol",
                                             "blockchain-eth", "blockchain-eth-eur-with-symbol"})
        for key, label in [("blockchain-btc-eur-with-symbol", "Bitcoin"), ("blockchain-eth-eur-with-symbol", "Ethereum")]:
            matches = [n for n in self.all_children("Crypto ticker") if self.node_templates[n["this2NodeTemplate"]]["key"] == key]
            self.nodes[label + " price"] = matches[0]
            self.visu(label + " price", area="Office", category="ENERGY", name=label)
        self.nodes["Bitcoin EUR"] = [n for n in self.all_children("Crypto ticker")
                                     if self.node_templates[n["this2NodeTemplate"]]["key"] == "blockchain-btc-eur"][0]

        # --- Hue bridge simulator: the core acts as a Hue bridge for Hue apps
        self.create("Hue bridge", "Virtual", "hue-bridge-simulator", {"hue-port": 5005})
        self.create("Hue living room lamp", "Hue bridge", "hue-onoff")

        # --- HomeKit and Matter bridges (accessory servers, pairing with a phone in the same network)
        self.create("HomeKit bridge", "Virtual", "apple-homekit-server")
        self.create("Matter bridge", "Virtual", "matter-bridge")

        # --- OpenWeatherMap needs an API key: added as an unconfigured, disabled example
        self.create("OpenWeatherMap (needs API key)", "Virtual", "openweathermap", disabled=True)

        # --- ModBus TCP: a slave (simulated device) and a master that polls it over loopback
        self.create("ModBus slave (simulated meter)", "Ethernet", "modbus-slave-tcp",
                    {"modbus-port": 5020, "modbus-bind-address": "127.0.0.1", "modbus-allowed-clients": "127.0.0.1/32"})
        self.create("Simulated meter", "ModBus slave (simulated meter)", "modbus-device", {"modbus-device-id": 1})
        self.create("Slave: second counter", "Simulated meter", "modbus-+2byte", {"modbus-register": 0, "modbus-table": 0})
        self.create("Slave: temperature", "Simulated meter", "modbus-4float", {"modbus-register": 10, "modbus-table": 0})
        self.create("Slave: pump coil", "Simulated meter", "modbus-binary", {"modbus-register": 0, "modbus-table": 2})

        self.create("ModBus master (loopback)", "Ethernet", "modbus-master-tcp",
                    {"modbus-master-ip": "127.0.0.1", "modbus-master-port": 5020})
        self.create("Meter (polled)", "ModBus master (loopback)", "modbus-device",
                    {"modbus-device-id": 1, "modbus-poll-interval": 1000})
        self.create("Master: second counter", "Meter (polled)", "modbus-+2byte", {"modbus-register": 0, "modbus-table": 0},
                    visu={"area": "Garage", "category": "ENERGY", "name": "ModBus second counter"})
        self.create("Master: temperature", "Meter (polled)", "modbus-4float", {"modbus-register": 10, "modbus-table": 0},
                    visu={"area": "Garage", "category": "TEMPERATURE", "name": "ModBus temperature"})
        self.create("Master: pump coil", "Meter (polled)", "modbus-binary", {"modbus-register": 0, "modbus-table": 2},
                    visu={"area": "Garage", "category": "TECHNOLOGY", "name": "ModBus pump"})

        # --- Wake on LAN and ping
        self.create("Wake on LAN", "Ethernet", "wol")
        wol = self.create("NAS", "Wake on LAN", "wol", self.wol_props("Wake on LAN"),
                          visu={"area": "Office", "category": "TECHNOLOGY", "name": "Wake up NAS"})
        self.create("Ping", "Ethernet", "PING")
        self.create("Localhost reachable", "Ping", "ping-device", {"ip": "127.0.0.1", "interval": 10},
                    visu={"area": "Office", "category": "TECHNOLOGY"})
        self.create("Internet reachable", "Ping", "ping-device", {"ip": "1.1.1.1", "interval": 30},
                    visu={"area": "Office", "category": "TECHNOLOGY", "favorite": True})
        return wol

    def wol_props(self, parent):
        # the MAC address property of a WoL device (key differs between versions)
        template = self.template("wol", self.nodes[parent])
        keys = [p["key"] for p in template.get("propertyTemplate") or []]
        mac = [k for k in keys if "mac" in k.lower()]
        return {mac[0]: "02:00:5E:10:00:01"} if mac else {}

    def all_children(self, name):
        result = []

        def walk(node):
            for child in node.get("inverseThis2ParentNodeInstanceNavigation") or []:
                result.append(child)
                walk(child)
        walk(self.nodes[name])
        return result

    # ------------------------------------------------------------------ logics

    def logic_template(self, name):
        """by template name (translation key, unique) or key"""
        for template in self.logic_templates:
            if template["name"] == name:
                return template
        matches = [t for t in self.logic_templates if t["key"] == name]
        if len(matches) != 1:
            raise RuntimeError(f"logic template {name}: {len(matches)} matches")
        return matches[0]

    def page(self, name, description=""):
        page = {"objId": new_id(), "name": name, "description": description, "this2RulePageType": 1}
        self.api.post("logics/page/add", page)
        print(f"  logic page {name}")
        return page["objId"]

    def logic(self, page, template_name, name, x, y, params=None, visu=None):
        template = self.logic_template(template_name)
        rule_id = new_id()
        interfaces = []
        by_key = {}
        params = params or {}
        for it in template["ruleInterfaceTemplate"]:
            instance = {"objId": new_id(), "this2RuleInstance": rule_id, "this2RuleInterfaceTemplate": it["objId"],
                        "inverted": False}
            if it["this2RuleInterfaceDirection"] == 3:
                field = PARAMETER_FIELDS.get(it.get("parameterDataType"))
                value = params.get(it["key"], it.get("defaultValue"))
                if field and value is not None:
                    if field == "valueInteger":
                        value = int(float(value))
                    elif field == "valueDouble":
                        value = float(value)
                    elif field == "valueBool":
                        value = str(value).lower() == "true" if isinstance(value, str) else bool(value)
                    elif not isinstance(value, str):
                        value = json.dumps(value)
                    instance[field] = value
            interfaces.append(instance)
            by_key[it["key"]] = instance["objId"]
        unknown = set(params) - set(by_key)
        if unknown:
            raise RuntimeError(f"{template_name} has no parameters {unknown}")
        rule = {"objId": rule_id, "this2RuleTemplate": template["objId"], "this2RulePage": page, "name": name,
                "description": "", "x": x, "y": y, "ruleInterfaceInstance": interfaces, "useInVisu": False}
        if visu:
            rule["useInVisu"] = True
            rule["this2AreaInstance"] = self.areas.get(visu.get("area"))
            rule["this2CategoryInstance"] = self.categories.get(visu.get("category"))
            rule["isFavorite"] = bool(visu.get("favorite"))
            rule["visuName"] = visu.get("name", name)
        self.api.post(f"logics/item/logicInstance/{page}", rule)
        self.logics[name] = (rule_id, by_key)
        return by_key

    def place(self, page, node_name, x, y):
        n2p = {"objId": new_id(), "this2RulePage": page, "this2NodeInstance": self.node_id(node_name),
               "x": x, "y": y, "inverted": False}
        self.api.post(f"logics/item/nodeInstance/{page}", n2p)
        return n2p["objId"]

    def link(self, page, source, target):
        """source/target: ('node', n2p id) or ('logic', interface instance id); output -> input"""
        link = {"objId": new_id(), "this2RulePage": page}
        if source[0] == "node":
            link["this2NodeInstance2RulePageOutput"] = source[1]
        else:
            link["this2RuleInterfaceInstanceOutput"] = source[1]
        if target[0] == "node":
            link["this2NodeInstance2RulePageInput"] = target[1]
        else:
            link["this2RuleInterfaceInstanceInput"] = target[1]
        self.api.post("logics/link", link)

    def wire(self, page, layout):
        """helper: nodes placed once per page, links given as (source, target) pairs of
        'node:<name>' or '<logic name>.<interface key>'"""
        placed = {}

        def endpoint(ref, x):
            if ref.startswith("node:"):
                name = ref[5:]
                if name not in placed:
                    placed[name] = self.place(page, name, x, 60 + 70 * len(placed))
                return ("node", placed[name])
            # interface keys may contain dots (translation keys): match the longest logic name
            for candidate in sorted(self.logics, key=len, reverse=True):
                if ref.startswith(candidate + "."):
                    return ("logic", self.logics[candidate][1][ref[len(candidate) + 1:]])
            raise RuntimeError(f"unknown link endpoint {ref}")

        for source, target in layout:
            self.link(page, endpoint(source, 40), endpoint(target, 1100))

    def seed_logics(self):
        # 1 -------------------------------------------------------------- lighting
        p = self.page("1 Lighting", "switch, dimmer, push button, monoflop, flashing lights, timer")
        self.logic(p, "OPERATIONS.SWITCH.NAME", "Living room light switch", 400, 60,
                   visu={"area": "Living room", "category": "LIGHTNING", "favorite": True, "name": "Living room light"})
        self.logic(p, "OPERATIONS.DIMMER.NAME", "Living room dimmer", 400, 260,
                   visu={"area": "Living room", "category": "LIGHTNING", "name": "Dimmer"})
        self.logic(p, "OPERATIONS.PUSH.NAME", "Garage door opener", 400, 480, {"OPERATIONS.PUSH.DURATION.NAME": 500},
                   visu={"area": "Garage", "category": "TECHNOLOGY", "name": "Garage door"})
        self.logic(p, "TIME.MONOFLOP.NAME", "Hallway motion light", 700, 60, {"TIME.DELAY.NAME": 20})
        self.logic(p, "LIGHTNING.FLASHING_LIGHTS.NAME", "Alarm flashing", 700, 260,
                   {"LIGHTNING.FLASHING_LIGHTS.DELAY.NAME": 700, "LIGHTNING.FLASHING_LIGHTS.REPEAT_COUNT.NAME": 5})
        self.logic(p, "TIME.TIMER.NAME", "Garden light schedule", 700, 480,
                   {"TIME.TIMER.NAME": {"StartTime": "2026-01-01T18:00:00", "StopTime": "2026-01-01T23:30:00",
                                        "EnabledDays": [0, 1, 2, 3, 4, 5, 6], "TypeInfo": "TimerPropertyData"}})
        self.wire(p, [
            ("node:Living room light", "Living room light switch.OPERATIONS.SWITCH.STATE.NAME"),
            ("Living room light switch.OPERATIONS.SWITCH.OUTPUT.NAME", "node:Living room light"),
            ("node:Living room light", "Living room dimmer.OPERATIONS.DIMMER.INPUT.STATE.NAME"),
            ("Living room dimmer.outputValue", "node:Dimmer value"),
            ("Garage door opener.OPERATIONS.PUSH.OUTPUT.NAME", "node:Garage door impulse"),
            ("node:Hallway motion", "Hallway motion light.T"),
            ("Hallway motion light.O", "node:Hallway light"),
            ("node:Alarm armed", "Alarm flashing.LIGHTNING.FLASHING_LIGHTS.TRIGGER.NAME"),
            ("Alarm flashing.LIGHTNING.FLASHING_LIGHTS.OUTPUT.NAME", "node:Alarm light"),
            ("Garden light schedule.O", "node:Garden light"),
            # a Hue app switching the simulated lamp switches the kitchen light, and the lamp shows its state
            ("node:Hue living room lamp/hue-onoff-switch", "node:Kitchen light"),
            ("node:Kitchen light", "node:Hue living room lamp/hue-onoff-state"),
        ])

        # 2 -------------------------------------------------------------- time
        p = self.page("2 Time and presence", "delays, calendar timer, compare, binary decoder, impulses")
        calendar = {"Value": [{"AllDay": False, "Text": "Morning routine", "Description": "workdays 06:30 - 07:30",
                               "Disabled": False, "StartDate": "2026-01-05T06:30:00", "EndDate": "2026-01-05T07:30:00",
                               "RecurrenceRule": "FREQ=WEEKLY;BYDAY=MO,TU,WE,TH,FR;WKST=MO", "RecurrenceException": "",
                               "TypeInfo": "CalendarPropertyDataEntry"}],
                    "TypeInfo": "CalendarPropertyData"}
        self.logic(p, "TIME.ADVANCED_TIMER.NAME", "Workday calendar", 400, 40, {"TIME.ADVANCED_TIMER.NAME": calendar})
        self.logic(p, "TIME.DELAYON.NAME", "Presence delay on", 400, 200, {"delay": 5})
        self.logic(p, "TIME.DELAYOFF.NAME", "Presence delay off", 400, 360, {"delay": 30})
        self.logic(p, "COMPARE.BIGGER.NAME", "Second > 30", 700, 40)
        self.logic(p, "LOGIC.BINARY_DECODER.NAME", "Minute bits", 700, 200)
        self.logic(p, "LOGIC.BINARY_DECODER.NAME", "Second bits", 700, 480)
        self.logic(p, "DIGITAL_TO_ANALOG.STATE_TO_IMPULS.NAME", "Holiday impulse", 1000, 40,
                   {"DIGITAL_TO_ANALOG.STATE_TO_IMPULS.DELAY.NAME": 1000})
        self.logic(p, "DIGITAL_TO_ANALOG.TRIGGER.NAME", "Presence greeting", 1000, 240,
                   {"DIGITAL_TO_ANALOG.TRIGGER.VALUE_PARAM.NAME": "Welcome home!"})
        self.wire(p, [
            ("Workday calendar.O", "node:Morning routine"),
            ("node:Presence living room", "Presence delay on.T"),
            ("Presence delay on.O", "node:Presence confirmed"),
            ("node:Presence living room", "Presence delay off.T"),
            ("node:Date/Time/times-seconds", "Second > 30.I1"),
            ("node:Thirty", "Second > 30.I2"),
            ("Second > 30.O", "node:Second half of minute"),
            ("node:Date/Time/times-minutes", "Minute bits.AI"),
            ("Minute bits.Q1", "node:Presence living room"),
            ("node:Date/Time/times-seconds", "Second bits.AI"),
            ("Second bits.Q3", "node:Hallway motion"),
            ("node:Holiday mode", "Holiday impulse.I"),
            ("node:Presence living room", "Presence greeting.I"),
            ("Presence greeting.O", "node:Greeting"),
        ])

        # 3 -------------------------------------------------------------- math and logic
        p = self.page("3 Math, compare and logic", "random, add, subtract, multiply, divide, modulo, floor, ceiling, "
                                                    "and, or, not, xor, if, compare, string concat")
        self.logic(p, "MATH.RANDOM.NAME", "Room temperature simulator", 300, 40, {"Min": 18, "Max": 27})
        self.logic(p, "MATH.SUBTRACT.NAME", "Setpoint - temperature", 300, 200)
        self.logic(p, "MATH.MULTIPLY.NAME", "Delta x 10", 300, 340)
        self.logic(p, "MATH.ADD.NAME", "Hour + minute + second", 300, 480)
        self.logic(p, "MATH.DIVISION.NAME", "Bitcoin / 1000", 300, 640)
        self.logic(p, "MATH.FLOOR.NAME", "Bitcoin floor", 560, 600)
        self.logic(p, "MATH.CEILING.NAME", "Bitcoin ceiling", 560, 700)
        self.logic(p, "MATH.MODULO.NAME", "Second mod 2", 560, 40)
        self.logic(p, "COMPARE.EQUAL.NAME", "Is even", 800, 40)
        self.logic(p, "COMPARE.SMALLER.NAME", "Too cold", 560, 200)
        self.logic(p, "COMPARE.SMALLER_OR_EQUAL.NAME", "Cold or equal", 560, 300)
        self.logic(p, "COMPARE.BIGGER_OR_EQUAL.NAME", "Warm or equal", 560, 400)
        self.logic(p, "COMPARE.UNEQUAL.NAME", "Lights differ", 800, 300)
        self.logic(p, "LOGIC.NOT.NAME", "Not holiday", 560, 820)
        self.logic(p, "LOGIC.AND.NAME", "Presence and not holiday", 800, 820)
        self.logic(p, "LOGIC.OR.NAME", "Any light", 800, 460)
        self.logic(p, "LOGIC.EXCLUSIVE_OR.NAME", "Exactly one light", 800, 560)
        self.logic(p, "LOGIC.IF.NAME", "Alarm state as number", 800, 680,
                   {"LOGIC.IF.PARAM.TRUE.NAME": 100, "LOGIC.IF.PARAM.FALSE.NAME": 0})
        self.logic(p, "STRING_OPERATIONS.CONCAT.NAME", "Status text builder", 1000, 40)
        self.wire(p, [
            ("node:Date/Time/times-seconds", "Room temperature simulator.Tr"),
            ("Room temperature simulator.O", "node:Living room temperature"),
            ("node:Living room setpoint", "Setpoint - temperature.I1"),
            ("node:Living room temperature", "Setpoint - temperature.I2"),
            ("Setpoint - temperature.O", "Delta x 10.I1"),
            ("node:Ten", "Delta x 10.I2"),
            ("Delta x 10.O", "node:Heating demand"),
            ("node:Date/Time/times-hours", "Hour + minute + second.I1"),
            ("node:Date/Time/times-minutes", "Hour + minute + second.I2"),
            ("node:Date/Time/times-seconds", "Hour + minute + second.I3"),
            ("node:Bitcoin EUR", "Bitcoin / 1000.I1"),
            ("node:Thousand", "Bitcoin / 1000.I2"),
            ("Bitcoin / 1000.O", "node:Bitcoin in thousand EUR"),
            ("Bitcoin / 1000.O", "Bitcoin floor.I1"),
            ("Bitcoin / 1000.O", "Bitcoin ceiling.I1"),
            ("node:Date/Time/times-seconds", "Second mod 2.I1"),
            ("node:Two", "Second mod 2.I2"),
            ("Second mod 2.O1", "Is even.I1"),
            ("node:Zero", "Is even.I2"),
            ("Is even.O", "node:Second is even"),
            ("node:Living room temperature", "Too cold.I1"),
            ("node:Living room setpoint", "Too cold.I2"),
            ("Too cold.O", "node:Heating on"),
            ("node:Living room temperature", "Cold or equal.I1"),
            ("node:Living room setpoint", "Cold or equal.I2"),
            ("node:Living room temperature", "Warm or equal.I1"),
            ("node:Living room setpoint", "Warm or equal.I2"),
            ("node:Living room light", "Lights differ.I1"),
            ("node:Kitchen light", "Lights differ.I2"),
            ("node:Living room light", "Any light.I1"),
            ("node:Kitchen light", "Any light.I2"),
            ("Any light.O", "node:Any light on"),
            ("node:Living room light", "Exactly one light.I1"),
            ("node:Kitchen light", "Exactly one light.I2"),
            ("node:Holiday mode", "Not holiday.I1"),
            ("node:Presence living room", "Presence and not holiday.I1"),
            ("Not holiday.O", "Presence and not holiday.I2"),
            ("node:Alarm armed", "Alarm state as number.I1"),
            ("Alarm state as number.O", "node:Alarm state value"),
            ("node:Welcome text", "Status text builder.I1"),
            ("node:Date/Time/times-time", "Status text builder.I2"),
            ("Status text builder.O", "node:Status text"),
        ])

        # 4 -------------------------------------------------------------- modbus loopback
        p = self.page("4 ModBus loopback", "values written to the ModBus slave are polled back by the ModBus master")
        self.wire(p, [
            ("node:Date/Time/times-seconds", "node:Slave: second counter"),
            ("node:Living room temperature", "node:Slave: temperature"),
            ("node:Second is even", "node:Slave: pump coil"),
            ("node:Master: pump coil", "node:Pump"),
        ])

        # 5 -------------------------------------------------------------- visualization controls
        p = self.page("5 Visualization controls", "logics with their own visualization: slider, gauges, shutter, "
                                                   "media player, window monitor, metering, messenger, text to speech")
        self.logic(p, "OPERATIONS.SLIDER.NAME", "Setpoint slider", 400, 40,
                   {"OPERATIONS.SLIDER.INPUT.VALUE_MIN.NAME": 16, "OPERATIONS.SLIDER.INPUT.VALUE_MAX.NAME": 28},
                   visu={"area": "Living room", "category": "HEATING", "name": "Setpoint"})
        self.logic(p, "OPERATIONS.GAUGE.EASY_CIRCULAR.NAME", "Seconds gauge", 400, 220,
                   {"scale_start": 0, "scale_end": 60, "ticks": 10},
                   visu={"area": "Garage", "category": "ENERGY", "name": "ModBus seconds"})
        self.logic(p, "OPERATIONS.GAUGE.EASY_LINEAR.NAME", "Temperature gauge", 400, 360,
                   {"scale_start": 10, "scale_end": 35, "ticks": 5},
                   visu={"area": "Living room", "category": "TEMPERATURE", "favorite": True, "name": "Temperature"})
        self.logic(p, "OPERATIONS.GAUGE.RANGE_CIRCULAR.NAME", "Heating demand gauge", 400, 500,
                   {"first_scale_start": -100, "first_scale_end": -1, "second_scale_start": 0, "second_scale_end": 30,
                    "third_scale_start": 31, "third_scale_end": 100},
                   visu={"area": "Living room", "category": "HEATING", "name": "Heating demand"})
        self.logic(p, "OPERATIONS.SHUTTER.NAME", "Bedroom blinds", 700, 40,
                   visu={"area": "Bedroom", "category": "SHADING", "favorite": True, "name": "Blinds"})
        self.logic(p, "SONOS_CONTROL.NAME", "Living room speaker", 700, 360,
                   visu={"area": "Living room", "category": "MULTIMEDIA", "name": "Speaker (no device)"})
        self.logic(p, "SURVEILLANCE.WINDOW_MONITOR.NAME", "Window monitor", 1000, 40,
                   visu={"area": "Bedroom", "category": "WINDOW", "name": "Windows"})
        self.logic(p, "METERING.NAME", "Energy meter", 1000, 240)
        self.logic(p, "MESSENGER.CLOUD_EMAIL.NAME", "Alarm e-mail", 1000, 360,
                   {"MESSENGER.CLOUD_EMAIL.TO.NAME": "demo@example.com",
                    "MESSENGER.CLOUD_EMAIL.SUBJECT.NAME": "Automatica demo: alarm armed"})
        self.logic(p, "TEXT_TO_SPEECH.SYNTHESIZE.NAME", "Doorbell announcement", 1000, 500,
                   {"text": "Someone is at the garage door"})
        self.wire(p, [
            ("Setpoint slider.OPERATIONS.SLIDER.OUTPUT.VALUE.NAME", "node:Living room setpoint"),
            ("node:Master: second counter", "Seconds gauge.value"),
            ("node:Master: temperature", "Temperature gauge.value"),
            ("node:Heating demand", "Heating demand gauge.value"),
            ("Bedroom blinds.absolutePercentageOutput", "node:Blinds position"),
            ("node:Bedroom window open", "Window monitor.Ct"),
            ("node:Alarm armed", "Alarm e-mail.T"),
            ("node:Garage door impulse", "Doorbell announcement.trigger"),
        ])

    # ------------------------------------------------------------------ automations

    def automation(self, name, definition, description="", mode="single", enabled=True, area=None, category=None):
        body = {"name": name, "description": description, "isEnabled": enabled, "mode": mode, "maxRuns": 10,
                "this2AreaInstance": self.areas.get(area), "this2CategoryInstance": self.categories.get(category),
                "definition": dict({"schema": 1}, **definition)}
        result = self.api.post("automations", body)
        problems = [p for p in result.get("problems") or [] if p.get("severity") != "warning"]
        print(f"  automation {name}{' (problems: ' + json.dumps(problems) + ')' if problems else ''}")
        return result["objId"]

    def seed_automations(self):
        n = self.node_id
        group = lambda *conditions, operator="all": {"type": "group", "operator": operator, "conditions": list(conditions)}
        switch_control = self.logics["Living room light switch"][0]

        self.automation("Minute heartbeat", {
            "triggers": [{"type": "datapoint", "id": "fullMinute", "node": n("Date/Time/times-seconds"),
                          "change": "to", "value": 0}],
            "conditions": group(),
            "actions": [
                {"type": "setValue", "node": n("Minute counter"), "mode": "increment", "step": 1},
                {"type": "setValue", "node": n("Last automation"), "mode": "copy", "source": n("Date/Time/times-time")},
            ]}, "counts the full minutes and stores the time of the last run", area="Office", category="TECHNOLOGY")

        self.automation("Arrive home", {
            "triggers": [{"type": "datapoint", "id": "presence", "node": n("Presence living room"), "change": "to",
                          "value": True}],
            "conditions": group({"type": "datapoint", "node": n("Holiday mode"), "operator": "eq", "value": False}),
            "actions": [
                {"type": "setValue", "node": n("Living room light"), "mode": "value", "value": True},
                {"type": "if", "condition": {"type": "sun", "after": "sunset", "before": "sunrise"},
                 "then": [{"type": "setValue", "node": n("Garden light"), "mode": "value", "value": True}],
                 "else": [{"type": "setValue", "node": n("Blinds position"), "mode": "value", "value": 100}]},
                {"type": "setValue", "node": n("Last automation"), "mode": "value", "value": "Arrive home"},
            ]}, "presence turns the living room light on (garden light after sunset)", mode="restart",
            area="Living room", category="LIGHTNING")

        self.automation("Leave home", {
            "triggers": [{"type": "datapoint", "id": "absent", "node": n("Presence living room"), "change": "to",
                          "value": False, "forSeconds": 10}],
            "conditions": group(),
            "actions": [
                {"type": "groupControl", "area": self.areas["Demo House"], "includeSubAreas": True,
                 "category": self.categories["LIGHTNING"], "command": "off", "continueOnError": True},
                {"type": "setValue", "node": n("Living room light"), "mode": "value", "value": False},
                {"type": "setValue", "node": n("Kitchen light"), "mode": "value", "value": False},
                {"type": "delay", "seconds": 5, "randomToSeconds": 10},
                {"type": "setValue", "node": n("Last automation"), "mode": "value", "value": "Leave home"},
            ]}, "10 s without presence: all lights off", area="Demo House", category="LIGHTNING")

        self.automation("Too warm", {
            "triggers": [{"type": "datapoint", "id": "warm", "node": n("Living room temperature"), "change": "above",
                          "value": 25}],
            "conditions": group({"type": "time", "after": "06:00", "before": "23:00"}),
            "actions": [
                {"type": "setValue", "node": n("Bedroom window open"), "mode": "toggle"},
                {"type": "notify", "subject": "{automation}", "severity": "warning",
                 "body": "Living room temperature is {value:" + n("Living room temperature") + "} C"},
            ]}, "temperature above 25 C: toggles the window flag and creates a notification",
            area="Living room", category="TEMPERATURE")

        self.automation("Kitchen follows living room", {
            "triggers": [{"type": "control", "id": "switch", "control": switch_control, "to": "any"}],
            "conditions": group(),
            "actions": [{"type": "setValue", "node": n("Kitchen light"), "mode": "copy", "source": n("Living room light")}],
        }, "the living room switch (a logic control) also switches the kitchen light", area="Kitchen",
            category="LIGHTNING")

        self.automation("Morning blinds", {
            "triggers": [{"type": "time", "id": "morning", "at": "07:00", "days": WEEKDAYS},
                         {"type": "sun", "id": "sunrise", "event": "sunrise", "offsetMinutes": 15,
                          "days": ["saturday", "sunday"]}],
            "conditions": group({"type": "datapoint", "node": n("Holiday mode"), "operator": "ne", "value": True}),
            "actions": [{"type": "setValue", "node": n("Blinds position"), "mode": "value", "value": 0},
                        {"type": "setValue", "node": n("Bedroom light"), "mode": "value", "value": True},
                        {"type": "delay", "seconds": 600},
                        {"type": "setValue", "node": n("Bedroom light"), "mode": "value", "value": False}]},
            "07:00 on workdays, 15 min after sunrise at the weekend", area="Bedroom", category="SHADING")

        evening = self.automation("Evening garden light", {
            "triggers": [{"type": "sun", "id": "sunset", "event": "sunset", "offsetMinutes": -15}],
            "conditions": group(),
            "actions": [{"type": "setValue", "node": n("Garden light"), "mode": "value", "value": True},
                        {"type": "waitUntil", "condition": {"type": "time", "after": "23:00", "before": "05:00"},
                         "timeoutSeconds": 0},
                        {"type": "setValue", "node": n("Garden light"), "mode": "value", "value": False}]},
            "15 min before sunset until 23:00", area="Garden", category="LIGHTNING")

        self.automation("Server started", {
            "triggers": [{"type": "startup", "id": "startup"}],
            "conditions": group(),
            "actions": [{"type": "setValue", "node": n("Last automation"), "mode": "value", "value": "Server started"},
                        {"type": "notify", "subject": "Demo started", "body": "The Automatica demo house is running",
                         "severity": "info"}]}, "runs once after every start")

        self.automation("Holiday scene (manual)", {
            "triggers": [],
            "conditions": group(),
            "actions": [{"type": "setValue", "node": n("Holiday mode"), "mode": "value", "value": True},
                        {"type": "setValue", "node": n("Alarm armed"), "mode": "value", "value": True},
                        {"type": "enableAutomation", "automation": evening, "enable": True},
                        {"type": "runAutomation", "automation": evening, "skipConditions": True}]},
            "no trigger: started with 'Run' in the automation list", area="Demo House", category="ALARM")

        self.automation("Alarm while away (draft)", {
            "triggers": [{"type": "datapoint", "id": "window", "node": n("Bedroom window open"), "change": "to",
                          "value": True}],
            "conditions": group({"type": "datapoint", "node": n("Alarm armed"), "operator": "eq", "value": True},
                                {"type": "trigger", "triggerId": "window"}, operator="all"),
            "actions": [{"type": "setValue", "node": n("Alarm light"), "mode": "value", "value": True},
                        {"type": "notify", "subject": "Window opened while the alarm is armed", "severity": "error",
                         "email": ["demo@example.com"], "continueOnError": True},
                        {"type": "stop", "reason": "alarm handled"}]},
            "a disabled draft", enabled=False, area="Bedroom", category="ALARM")

    def run(self):
        print("settings")
        self.seed_settings()
        print("areas and categories")
        self.seed_areas()
        self.seed_categories()
        print("drivers")
        root = self.api.get("nodeInstances")[0]
        self.seed_drivers(root)
        print("logics")
        self.seed_logics()
        print("automations")
        self.seed_automations()


def prepare_db(path):
    """sa keeps the demo password (sa), so it must not be forced to change it"""
    db = sqlite3.connect(path)
    try:
        db.execute("PRAGMA wal_checkpoint(TRUNCATE)")
        db.execute("UPDATE Users SET MustChangePassword = 0 WHERE UserName = 'sa'")
        db.commit()
    finally:
        db.close()


def finish_db(path):
    """merges the WAL into the database file, so a single file can be shipped"""
    db = sqlite3.connect(path)
    try:
        db.execute("PRAGMA wal_checkpoint(TRUNCATE)")
        db.execute("PRAGMA journal_mode=DELETE")
        db.execute("VACUUM")
    finally:
        db.close()


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--url", default="http://localhost:5011")
    parser.add_argument("--user", default="sa")
    parser.add_argument("--password", default="sa")
    parser.add_argument("--prepare-db", metavar="FILE", help="offline: clear the 'must change password' flag of sa")
    parser.add_argument("--finish-db", metavar="FILE", help="offline: checkpoint the WAL and compact the database")
    args = parser.parse_args()

    if args.prepare_db:
        prepare_db(args.prepare_db)
        return
    if args.finish_db:
        finish_db(args.finish_db)
        return

    api = Api(args.url)
    api.login(args.user, args.password)
    api.wait_started()
    Seeder(api).run()
    print("done")


if __name__ == "__main__":
    sys.exit(main())
