"""Package the current build and complete corresponding source; standard library only."""
import hashlib
import json
from pathlib import Path
import re
import shutil
import time
import zipfile

root = Path(__file__).resolve().parents[1]
manifest = json.loads((root / 'PovPlus.json').read_text(encoding='utf-8'))
version = manifest['AssemblyVersion']
assembly = (root / 'src/Properties/AssemblyInfo.cs').read_text(encoding='utf-8-sig')
assert re.search(r'AssemblyVersion\("' + re.escape(version) + r'"\)', assembly), 'Source/manifest version mismatch'
dll = root / 'src/bin/Release/net10.0-windows7.0/PovPlus.dll'
assert dll.is_file(), 'Build Release first'
out = root / 'artifacts'
out.mkdir(exist_ok=True)
prefix = f'PovPlus-Enhanced-{version}'
binary = out / f'{prefix}.zip'
with zipfile.ZipFile(binary, 'w', zipfile.ZIP_DEFLATED, compresslevel=9) as z:
    z.write(dll, 'PovPlus.dll')
    for name in ('PovPlus.json', 'LICENSE.md', 'NOTICE.md', 'README.md', 'README.en.md', 'CHANGELOG.md'):
        z.write(root / name, name)
shutil.copyfile(binary, root / 'PovPlus.Enhanced.zip')
url = 'https://raw.githubusercontent.com/pascalial98-hue/POVPlus/main/PovPlus.Enhanced.zip'
entry = dict(manifest, TestingAssemblyVersion=version, TestingDalamudApiLevel=manifest['DalamudApiLevel'],
             DownloadLinkInstall=url, DownloadLinkUpdate=url, DownloadLinkTesting=url,
             IsHide=False, IsTestingExclusive=False, LastUpdate=int(time.time()),
             Changelog=manifest.get('Changelog', f'{version}: See CHANGELOG.md / 请参阅更新记录。'))
(root / 'repo.json').write_text(json.dumps([entry], ensure_ascii=False, indent=2) + '\n', encoding='utf-8', newline='\n')
source = out / f'{prefix}-source.zip'
with zipfile.ZipFile(source, 'w', zipfile.ZIP_DEFLATED, compresslevel=9) as z:
    for folder in ('src', 'tests', 'scripts', 'docs'):
        for path in sorted((root / folder).rglob('*')):
            if path.is_file() and not {'bin', 'obj', '__pycache__'}.intersection(path.relative_to(root).parts):
                z.write(path, path.relative_to(root).as_posix())
    for name in ('PovPlus.json', 'repo.json', 'LICENSE.md', 'NOTICE.md', 'README.md', 'README.en.md', 'CHANGELOG.md', '.gitignore', '.gitattributes'):
        z.write(root / name, name)
checksums = ''.join(f'{hashlib.sha256(path.read_bytes()).hexdigest()}  {path.name}\n' for path in (binary, source))
(out / 'SHA256SUMS.txt').write_text(checksums, encoding='ascii')
with zipfile.ZipFile(binary) as z:
    assert z.testzip() is None
    assert json.loads(z.read('PovPlus.json')) == manifest
    assert z.read('PovPlus.dll') == dll.read_bytes()
print(checksums, end='')
