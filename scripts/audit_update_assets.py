"""Read-only 0.4.7 serialized-content audit. Requires UnityPy on PYTHONPATH."""
import argparse
import hashlib
import json
from pathlib import Path
import struct
import UnityPy


class Fields:
    """Bounded Unity serialized fields; these offsets are file data, not memory."""
    def __init__(self, obj):
        self.raw = obj.get_raw_data()
        # Unity 2022 MonoBehaviour header: 12-byte GameObject PPtr, one enabled
        # byte padded to 4 bytes, 12-byte script PPtr, and an aligned name string.
        if len(self.raw) < 32:
            raise ValueError("Truncated MonoBehaviour header")
        length = struct.unpack_from('<i', self.raw, 28)[0]
        if not 0 <= length <= len(self.raw) - 32:
            raise ValueError("Invalid MonoBehaviour name")
        self.pos = (32 + length + 3) & ~3

    def take(self, length):
        if length < 0 or self.pos + length > len(self.raw):
            raise ValueError("Serialized field outside object")
        data = self.raw[self.pos:self.pos + length]
        self.pos += length
        return data

    def i32(self): return struct.unpack('<i', self.take(4))[0]
    def pptr(self): return struct.unpack('<iq', self.take(12))
    def align(self): self.pos = (self.pos + 3) & ~3
    def string(self):
        length = self.i32()
        value = self.take(length).decode('utf8')
        self.align()
        return value
    def count(self):
        count = self.i32()
        if not 0 <= count <= 1000: raise ValueError("Invalid collection count")
        return count


def identity(obj):
    f = Fields(obj)
    # NPCData._basicInfo is ValueOrReference<BasicInfo, BasicInfoPreset>:
    # 4-byte enum mode, inline strings/bool, followed by a 12-byte preset PPtr.
    mode = f.i32()
    first = f.string()
    has_last = f.take(1) != b'\0'
    f.align()
    last = f.string()
    npc_id = f.string()
    reference = f.pptr()
    if mode == 1:
        preset = UnityPy.classes.PPtr(m_FileID=reference[0], m_PathID=reference[1], assetsfile=obj.assets_file).deref()
        f = Fields(preset)
        first = f.string(); has_last = f.take(1) != b'\0'; f.align()
        last = f.string(); npc_id = f.string()
    elif mode != 0:
        raise ValueError("Unknown BasicInfo reference mode")
    native_name = (first + (' ' + last if has_last else '')).strip()
    return {"id": npc_id, "name": 'Vince Knuckles' if npc_id == 'vince_knuckles' else native_name,
        "nativeName": native_name}


def group(obj):
    f = Fields(obj)
    name, group_id = f.string(), f.string()
    for _ in range(3): f.string()  # description, arrival, incoming messages
    f.take(16 + 12)  # Color RGBA floats and three buy-quantity int32 values
    f.take(f.count() * 4)  # DrugPreference enum list
    f.take(f.count() * 12)  # Effect asset PPtrs
    refs = [f.pptr()]  # Leader.Data
    refs.extend(f.pptr() for _ in range(f.count()))  # Customers[].Data
    people = {}
    for file_id, path_id in refs:
        if path_id == 0: continue
        native = UnityPy.classes.PPtr(m_FileID=file_id, m_PathID=path_id, assetsfile=obj.assets_file).deref()
        person = identity(native)
        people[person['id']] = person
    return {"id": group_id, "name": name, "members": list(people.values())}


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--game-data', type=Path, required=True)
    p.add_argument('--report', type=Path, required=True)
    a = p.parse_args()
    root = Path(__file__).resolve().parents[1]
    expected = json.loads((root/'docs/0.4.7-content.json').read_text())
    found_groups, items, cosmetics, teleports, businesses, hashes = {}, {}, {}, set(), [], {}
    errors = []
    for filename in ['sharedassets0.assets', 'sharedassets1.assets', 'resources.assets', 'level1']:
        path = a.game_data/filename
        hashes[filename] = hashlib.sha256(path.read_bytes()).hexdigest()
        env = UnityPy.load(str(path))
        for obj in env.objects:
            if obj.type.name == 'GameObject' and filename == 'level1':
                name = (obj.peek_name() or '').lower()
                if name in expected['teleports']: teleports.add(name)
                continue
            if obj.type.name != 'MonoBehaviour': continue
            cls = None;
            try:
                head = obj.parse_monobehaviour_head()
                cls = head.m_Script.deref_parse_as_object().m_ClassName
                if cls == 'SpecialCustomerData':
                    data = group(obj); found_groups[data['id']] = data
                elif cls.endswith('ItemDefinition') or cls == 'ClothingDefinition':
                    f = Fields(obj)
                    name, item_id = f.string(), f.string()
                    if item_id in expected['items']: items[item_id] = {"name": name, "class": cls, "asset": filename, "pathId": obj.path_id}
                elif cls in ['AvatarObject', 'HairAvatarObject']:
                    f = Fields(obj)
                    name, object_id = f.string(), f.string()
                    if object_id in expected['cosmetics']: cosmetics[object_id] = name
                elif cls == 'Business':
                    import re
                    strings = re.findall(rb'[ -~]{4,}',obj.get_raw_data())
                    if any(b'Hyland Medical' in s for s in strings): businesses.append({"asset":filename,"pathId":obj.path_id})
                elif cls == 'Console' and filename == 'level1':
                    f = Fields(obj)
                    file_id, path_id = f.pptr()  # Console.TeleportPointsContainer
                    container = UnityPy.classes.PPtr(m_FileID=file_id,m_PathID=path_id,assetsfile=obj.assets_file).deref()
                    for ref in container.read().m_Children:
                        point = ref.deref_parse_as_object()
                        name = point.m_GameObject.deref_parse_as_object().m_Name.lower()
                        if name in expected['teleports']: teleports.add(name)
            except Exception as ex:
                if cls == 'SpecialCustomerData': errors.append(str(ex))
    for group_id, names in expected['groups'].items():
        actual = {m['name'] for m in found_groups.get(group_id, {}).get('members', [])}
        for name in names:
            if name not in actual: errors.append('Missing group member: '+group_id+'/'+name)
    for key in expected['items']:
        if key not in items: errors.append('Missing item: '+key)
    for key in expected['cosmetics']:
        if key not in cosmetics: errors.append('Missing cosmetic: '+key)
    for key in expected['teleports']:
        if key not in teleports: errors.append('Missing teleport: '+key)
    if not businesses: errors.append('Hyland Medical business not found')
    report = {"scope":"Read-only serialized game assets; not in-game menu QA", "assetHashes": hashes,
        "groups":found_groups,"items":items,"cosmetics":cosmetics,"teleports":sorted(teleports),
        "hylandMedical":businesses,"errors":errors}
    a.report.write_text(json.dumps(report,indent=2))
    print(json.dumps({"groups":len(found_groups),"visitors":sum(len(g['members']) for g in found_groups.values()),
        "items":len(items),"cosmetics":len(cosmetics),"teleports":len(teleports),"errors":errors},indent=2))
    return 0 if not errors else 2


if __name__ == '__main__':
    raise SystemExit(main())
