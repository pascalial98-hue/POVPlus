"""Check the standard DalamudPackager output, using only the Python standard library."""
import argparse
import hashlib
import json
from pathlib import Path
import struct
import zipfile

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('output', type=Path, help='dotnet build output directory')
parser.add_argument('--baseline', type=Path, help='optional tested installer ZIP to compare DLL bytes')
args = parser.parse_args()
root = Path(__file__).resolve().parents[1]
expected = json.loads((root / 'PovPlus.json').read_text(encoding='utf-8'))
archive = args.output / 'PovPlus/latest.zip'
allowed = {'PovPlus.dll', 'PovPlus.json', 'LICENSE.md', 'NOTICE.md'}

with zipfile.ZipFile(archive) as package:
    assert package.testzip() is None, 'Corrupt package'
    assert len(package.namelist()) == len(allowed), 'Duplicate or unexpected ZIP entries'
    assert set(package.namelist()) == allowed, 'Unexpected package contents / bundled dependencies'
    assert json.loads(package.read('PovPlus.json')) == expected, 'Packaged manifest differs from source'
    dll = package.read('PovPlus.dll')
    assert dll == (args.output / 'PovPlus.dll').read_bytes(), 'Packaged DLL differs from build'
    for name in ('LICENSE.md', 'NOTICE.md'):
        assert package.read(name) == (root / name).read_bytes(), f'{name} differs from source'

assert json.loads((args.output / 'PovPlus/PovPlus.json').read_text(encoding='utf-8')) == expected
assert {p.name for p in args.output.rglob('*.dll')} == {'PovPlus.dll'}, 'Runtime dependencies leaked into output'
assert expected['InternalName'] == 'PovPlus'
assert expected['Name'] == 'POV+ Enhanced'
assert expected['DalamudApiLevel'] == 15

images = {}
for name in ('povplus-enhanced-icon-v1.png', 'povplus-enhanced-store-preview-v2.png'):
    data = (root / 'docs/images' / name).read_bytes()
    assert data[:8] == b'\x89PNG\r\n\x1a\n', 'Image is not PNG'
    dimensions = struct.unpack('>II', data[16:24])
    images[name] = dimensions
    if 'icon' in name:
        assert 64 <= dimensions[0] <= 512 and dimensions[0] == dimensions[1]
    else:
        assert dimensions[0] <= 730 and dimensions[1] <= 380

report = {
    'version': expected['AssemblyVersion'],
    'files': sorted(allowed),
    'dll_sha256': hashlib.sha256(dll).hexdigest(),
    'images': images,
}
if args.baseline:
    with zipfile.ZipFile(args.baseline) as baseline:
        assert dll == baseline.read('PovPlus.dll'), 'DLL differs from tested baseline'
    report['baseline_dll_identical'] = True
print(json.dumps(report, ensure_ascii=False, indent=2))
