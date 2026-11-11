"""Verify active settings/status strings are covered by the bundled English catalog."""
import json
import pathlib
import re

root = pathlib.Path(__file__).resolve().parents[1]
src = root / 'src'
catalog = json.loads((src / 'PovPlus/Translations/en.json').read_text(encoding='utf-8-sig'))
missing = []
fixed = {'Language / 语言###language', '自动 / Auto\0简体中文\0English\0'}
paths = [*(src / 'PovPlus.Windows').glob('*.cs'), *(src / 'PovPlus').glob('*.cs')]
for path in paths:
    for number, line in enumerate(path.read_text(encoding='utf-8-sig').splitlines(), 1):
        if line.lstrip().startswith('//'):
            continue
        for raw in re.findall(r'"((?:[^"\\]|\\.)*)"', line):
            if not re.search('[\u4e00-\u9fff]', raw):
                continue
            value = raw.replace(r'\0', '\0').replace(r'\n', '\n').replace(r'\"', '"')
            if value not in catalog and value not in fixed:
                missing.append(f'{path.relative_to(root)}:{number}: {value}')
assert not missing, 'Missing translations:\n' + '\n'.join(missing)
assert all(isinstance(value, str) and value.strip() for value in catalog.values())
print(f'PASS: {len(catalog)} English translations; active Chinese strings fully covered.')
