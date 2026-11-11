"""Generates eep-vectors.json: one decode test vector per case of every EEP profile in eep.xml.

The expected values come straight from the EEP definition (bit offset/size, range/scale, enum segments), read
independently of P3.Driver.EepParser, so the driver templates and the decoding are both checked against the spec.

    python generate_eep_vectors.py <eep.xml> <eep-vectors.json>

Every vector holds the telegram user data (case constants and condition values set, LRN bit = data telegram) and
per data field the raw value and the value the driver has to dispatch:

    bool      1 bit field                                    -> raw == 1
    scaled    <range> + <scale>                              -> linear scale, double
    segment   enum item with <min>/<max> and a nested <scale> -> scaled within the segment, double
    boolenum  enum with exactly two value ranges             -> raw within the 2nd range -> true
    raw       everything else (enum values, counters, ...)   -> the raw value, int
    none      no value is dispatched (LRN bit, window handle, fields > 32 bit)
"""
import json
import re
import sys
import xml.etree.ElementTree as ET

RESERVED = re.compile(r"reserved|not used", re.I)
SEE = re.compile(r"see\s+([0-9A-F]{2})-([0-9A-F]{2})-([0-9A-F]{2})", re.I)
RORG_SIZE = {0xF6: 1, 0xD5: 1, 0xA5: 4}


def num(text):
    """12, 0x0C, 0b1100, +40; None for patterns like 0b11X0 or 1...5."""
    if text is None:
        return None
    t = text.strip()
    try:
        if t.lower().startswith("0x"):
            return int(t[2:], 16)
        if t.lower().startswith("0b"):
            return int(t[2:], 2)
        return int(t, 10)
    except ValueError:
        return None


def dec(text):
    if text is None:
        return None
    try:
        return float(text.strip())
    except ValueError:
        return None


def number_of(el):
    return num(el.findtext("number"))


class Field:
    def __init__(self, el):
        self.reserved = el.find("reserved") is not None
        # reserved fields sometimes have no position
        self.offset = num(el.findtext("bitoffs")) or 0
        self.size = num(el.findtext("bitsize")) or 0
        self.data = (el.findtext("data") or "").strip()
        self.shortcut = (el.findtext("shortcut") or "").strip()
        self.el = el

    @property
    def mask(self):
        return (1 << self.size) - 1


def resolve(profile, typ):
    """Types without cases reference another type (<ref> or a "see D2-01-00" title), possibly over several hops."""
    for _ in range(5):
        if typ.findall("case"):
            return typ
        ref = typ.find("ref")
        if ref is not None:
            r, f, t = (num(ref.findtext(x)) if ref.findtext(x).strip().lower().startswith("0x") else int(ref.findtext(x).strip(), 16)
                       for x in ("rorg", "func", "type"))
        else:
            m = SEE.search(typ.findtext("title") or "")
            if not m:
                return None
            r, f, t = (int(g, 16) for g in m.groups())
        target = None
        for rorg in profile.findall("rorg"):
            if number_of(rorg) != r:
                continue
            for func in rorg.findall("func"):
                if number_of(func) != f:
                    continue
                for ty in func.findall("type"):
                    if number_of(ty) == t:
                        target = ty
        if target is None or target is typ:
            return None
        typ = target
    return None


def co2_scale_max(rorg, func, typ, shortcut):
    # D2-04: the xml says "2000 (or 5000)", the family table assigns 0..5000 ppm to types 08..1E
    if rorg == 0xD2 and func == 0x04 and shortcut == "CO2":
        return 5000.0 if typ >= 0x08 else 2000.0
    return None


def pick_raw(field, seed):
    """A raw value that is not 0 and not all ones, so wrong offsets and lengths show up."""
    if field.size == 1:
        return 1
    v = (0x5A3C96E1 >> (seed % 7)) & field.mask
    if v in (0, field.mask):
        v = 1 + seed % max(1, field.mask - 1)
    return v


def inside(a, b, mask):
    """A raw value a third into the range, None if the range does not fit into the field."""
    lo, hi = max(min(a, b), 0), min(max(a, b), mask)
    if lo > hi:
        return None
    return lo + (hi - lo) // 3


def expectation(rorg, func, typ, field, seed):
    el = field.el
    if field.size > 32:
        return "none", pick_raw(field, seed) & 0xFFFFFFFF, None
    if field.shortcut in ("LRN", "LRNB") or field.data.upper() == "LRN BIT":
        return "none", 1, None
    if field.shortcut == "WIN":
        return "none", pick_raw(field, seed), None

    items = el.findall("enum/item")
    # a single fixed value (D2-01 CMD, D2-20 MT): the case constant
    if len(items) == 1 and num(items[0].findtext("value")) is not None:
        v = num(items[0].findtext("value"))
        return ("bool", v, v == 1) if field.size == 1 else ("raw", v, v)

    if field.size == 1:
        return "bool", 1, True

    # enum segments with their own scale (D2-14 temperature 0..250 => -40..60 °C)
    for item in items:
        lo, hi = num(item.findtext("min")), num(item.findtext("max"))
        s = item.find("scale")
        if lo is not None and hi is not None and s is not None:
            smin, smax = dec(s.findtext("min")), dec(s.findtext("max"))
            raw = inside(lo, hi, field.mask)
            if smin is None or smax is None or raw is None:
                continue
            expected = smin if hi == lo else smin + (raw - lo) * (smax - smin) / (hi - lo)
            return "segment", raw, expected

    if len(items) == 2 and all(num(i.findtext("min")) is not None and num(i.findtext("max")) is not None for i in items) \
            and not any(RESERVED.search(i.findtext("description") or "") for i in items):
        return "boolenum", num(items[1].findtext("min")), True

    rng, scl = el.find("range"), el.find("scale")
    if rng is not None and scl is not None:
        rmin, rmax = num(rng.findtext("min")), num(rng.findtext("max"))
        smin = dec(scl.findtext("min"))
        smax = co2_scale_max(rorg, func, typ, field.shortcut) or dec(scl.findtext("max"))
        if None not in (rmin, rmax, smin, smax) and rmin != rmax and smin != smax:
            if min(rmin, rmax) < 0:
                # signed values (D2-00-01, little endian two's complement) are not supported by the driver
                return "none", pick_raw(field, seed), None
            if max(rmin, rmax) > field.mask:
                # one part of a split value (A5-13-06 LAT(LSB), D2-01 MD_LSB/MD_MSB): the part is dispatched raw
                raw = pick_raw(field, seed)
                return "raw", raw, raw
            raw = inside(rmin, rmax, field.mask)
            return "scaled", raw, smin + (raw - rmin) * (smax - smin) / (rmax - rmin)

    values = [num(i.findtext("value")) for i in items]
    values = [v for v in values if v is not None and v <= field.mask]
    if len(values) > 1:
        return "raw", values[1], values[1]
    raw = pick_raw(field, seed)
    return "raw", raw, raw


def set_bits(data, offset, size, value):
    for i in range(size):
        bit = (value >> (size - 1 - i)) & 1
        pos = offset + i
        if bit:
            data[pos >> 3] |= 0x80 >> (pos & 7)
        else:
            data[pos >> 3] &= ~(0x80 >> (pos & 7)) & 0xFF


def main(eep_path, out_path):
    raw = open(eep_path, "rb").read()
    text = raw.decode("utf-16") if raw[:2] in (b"\xff\xfe", b"\xfe\xff") else raw.decode("utf-8-sig")
    text = re.sub(r"<\?xml[^>]*\?>", "", text)
    profile = ET.fromstring(text)
    if profile.tag != "profile":
        profile = profile.find("profile")

    vectors = []
    for rorg_el in profile.findall("rorg"):
        rorg = number_of(rorg_el)
        if rorg not in (0xF6, 0xD5, 0xA5, 0xD2):
            continue
        for func_el in rorg_el.findall("func"):
            func = number_of(func_el)
            for type_el in func_el.findall("type"):
                typ = number_of(type_el)
                source = resolve(profile, type_el)
                if source is None:
                    continue
                code = "%02X-%02X-%02X" % (rorg, func, typ)
                for case_index, case in enumerate(source.findall("case"), 1):
                    fields = [Field(d) for d in case.findall("datafield")]
                    if rorg in RORG_SIZE:
                        size = RORG_SIZE[rorg]
                    else:
                        size = max((f.offset + f.size + 7) // 8 for f in fields) if fields else 1
                    data = bytearray(size)
                    # 1BS/4BS: LRN bit (DB0.3) = 1, data telegram - profiles that define it as a field set it again
                    if rorg == 0xA5:
                        set_bits(data, 28, 1, 1)
                    elif rorg == 0xD5:
                        set_bits(data, 4, 1, 1)

                    out_fields = []
                    for seed, f in enumerate(fields):
                        if f.reserved:
                            continue
                        if f.offset + f.size > size * 8:
                            continue
                        kind, raw_value, expected = expectation(rorg, func, typ, f, seed)
                        set_bits(data, f.offset, f.size, raw_value)
                        out_fields.append({"name": f.shortcut or f.data, "offset": f.offset, "length": f.size,
                                           "kind": kind, "raw": raw_value, "expected": expected})

                    # explicit data field conditions win over the chosen values
                    for cond in case.findall("condition/datafield"):
                        o, s, v = int(cond.findtext("bitoffs")), int(cond.findtext("bitsize")), num(cond.findtext("value"))
                        if v is None:
                            continue
                        set_bits(data, o, s, v)
                        for of in out_fields:
                            if of["offset"] == o and of["length"] == s:
                                of["raw"] = v
                                if of["kind"] in ("raw",):
                                    of["expected"] = v
                                elif of["kind"] == "bool":
                                    of["expected"] = v == 1

                    if not out_fields:
                        continue
                    vectors.append({"code": code, "case": case_index, "data": data.hex().upper(), "fields": out_fields})

    with open(out_path, "w", encoding="utf-8", newline="\r\n") as f:
        f.write("[\n")
        f.write(",\n".join(json.dumps(v, ensure_ascii=False, separators=(",", ":")) for v in vectors))
        f.write("\n]\n")
    print(f"{len(vectors)} vectors, {sum(len(v['fields']) for v in vectors)} fields, "
          f"{len({v['code'] for v in vectors})} profiles")


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
