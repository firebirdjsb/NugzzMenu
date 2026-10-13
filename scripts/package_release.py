"""Build the verified v1.0.1 player package from a clean committed checkout.

Run after the Release build and checks have written trialbuild/release-v1.0.1.
This script does not install, publish, or write game/save/config files.
"""
from pathlib import Path
from datetime import datetime, timezone
import hashlib
import json
import subprocess
import zipfile

ROOT=Path(__file__).resolve().parents[1]
VERSION='1.0.1'
PROOF=ROOT/'trialbuild'/('release-v'+VERSION)
OUTPUT=ROOT/'trialbuild/releases'/('NugzzMenu-v'+VERSION+'.zip')

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def git(*args): return subprocess.check_output(['git',*args],cwd=ROOT,text=True).strip()

def main():
    assert not OUTPUT.exists(),'Preserve existing packages'
    assert not git('status','--porcelain','--untracked-files=all'),'Commit scoped source before packaging'
    subprocess.run(['git','diff','--exit-code','v1.0.0','--','Services/VehicleService.cs'],cwd=ROOT,check=True)
    lock=json.loads((ROOT/'S1API.lock.json').read_text())
    dependency=ROOT/'trialbuild/s1api'/lock['version']
    assert lock['version']=='3.2.1' and sha(dependency/'S1API.zip')==lock['sha256']
    release=json.loads((PROOF/'release-audit.json').read_text())
    assert release['version']==VERSION and release['assemblyVersion']=='1.0.1.0'
    assert release['menuSha256']==sha(ROOT/'bin/Release/net6.0/NugzzMenu.dll') and release['rejectedRecordedResources']==0
    build=(PROOF/'build.txt').read_text();assert '0 Warning(s)' in build and '0 Error(s)' in build
    api=json.loads((PROOF/'api-audit.json').read_text())
    assert not api['errors'] and api['hasSpecialCustomersUpdate'] and api['s1ApiVersion']=='3.2.1'
    checks={
        'animationAndMotorcycle':('checks.txt','124 local animation and motorcycle checks passed',124),
        'stateAndPolicy':('native-state-checks.txt','27 state/policy checks passed',27),
        'interactionPrompts':('prompt-checks.txt','11 prompt frame checks passed',11),
        'clothingPicker':('color-checks.txt','6 picker checks passed',6),
        'startupConfig':('config-checks.txt','9 config checks passed',9),
        'accessoryFailure':('accessory-checks.txt','9 accessory failure checks passed',9)}
    for name,(file,phrase,count) in checks.items():
        report=(PROOF/file).read_text();assert phrase in report and 'FAIL:' not in report,name
    assert (PROOF/'unity-call-audit.txt').read_text().startswith('1321 Unity call sites')
    assert 'GETGROUNDHIT_REFERENCES bin/Release/net6.0/NugzzMenu.dll: 0' in (PROOF/'bindings-after.txt').read_text()
    audio=json.loads((ROOT/'Assets/Audio/design.json').read_text())
    assert audio['externalRecordingsIncluded'] is False and audio['gameAudioAssetsIncluded'] is False
    assert sha(ROOT/audio['generator'])==audio['generatorSha256']
    for bank in audio['loops']:assert sha(ROOT/bank['asset'])==bank['sha256']
    sources={
        'Mods/NugzzMenu.dll':ROOT/'bin/Release/net6.0/NugzzMenu.dll',
        lock['referenceAssembly']:dependency/lock['referenceAssembly'],
        lock['loaderAssembly']:dependency/lock['loaderAssembly'],
        'CHANGELOG.md':ROOT/'RELEASE_NOTES_v1.0.1.md',
        'S1API.lock.json':ROOT/'S1API.lock.json',
        'examples/nuggzmenu-config.json':ROOT/'examples/nuggzmenu-config.json',
        'licenses/S1API-MIT.txt':ROOT/'trialbuild/special-customers/s1api-source/LICENSE',
        'audio/AUDIO-NOTES.md':ROOT/'Assets/Audio/AUDIO-NOTES.md',
        'audio/design.json':ROOT/'Assets/Audio/design.json',
        'validation/RELEASE_VALIDATION_v1.0.1.md':ROOT/'docs/RELEASE_VALIDATION_v1.0.1.md'}
    payload={name:path.read_bytes() for name,path in sources.items()}
    with zipfile.ZipFile(dependency/'S1API.zip') as upstream:
        for name in [lock['referenceAssembly'],lock['loaderAssembly']]:assert payload[name]==upstream.read(name)
    payload['README-INSTALL.txt']=b'''NugzzMenu v1.0.1
For Schedule I 0.4.7f12 with MelonLoader IL2CPP. S1API 3.2.1 is included.

Read CHANGELOG.md for every change since v1.0.0 and the known notes.

INSTALL / UPDATE
1. Close the game.
2. Back up the DLLs you replace somewhere OUTSIDE Mods and Plugins:
   Mods/NugzzMenu.dll
   Mods/S1API.Il2Cpp.MelonLoader.dll
   Plugins/S1APILoader.MelonLoader.dll
3. Copy this ZIP's Mods and Plugins folders into your Schedule I game folder.
   Keep only one NugzzMenu DLL and replace the included S1API files together.
4. Keep your existing Mods/nuggzmenu-config.json. The example is reference only.
   If there is no config yet, the menu creates it beside its DLL on startup.
5. Start the game. Default menu key: F8. Xbox: LB + RB + D-pad Up.
   PlayStation: L1 + R1 + D-pad Up. Your saved custom key takes precedence.
6. For new motorcycles/animations, host and clients should use v1.0.1 and the
   host must approve clients. Bike spawning, weather and visitor scheduling
   are solo/host controls. Never Wanted also supports approved clients.

Motorcycles last for the current session and have no storage. Restart your
session and spawn a fresh bike after updating. Previous save progress needs
no conversion. Existing controller, keyboard and mouse controls remain.

ROLLBACK
Close the game, restore the DLLs you backed up, then restart all players.
Preserve your config. Do not restore the older experimental stability/client-
wanted test that caused the motorcycle spawning crash.

The package passed 186 automated checks, build and game-compatibility checks.
Final live checks of multiplayer, new animations/audio, the shelf trash crown
and the separate intermittent dark-screen report remain: see CHANGELOG.md.
No game files, saves or existing config are changed by this packaging script.
'''
    counts={name:value[2] for name,value in checks.items()};counts['total']=sum(counts.values());assert counts['total']==186
    public_api={k:api[k] for k in ['hasSpecialCustomersUpdate','s1ApiVersion','checkedTypes','checkedMembers','checkedPatches','checkedDynamicPatches','errors']}
    validation={'checks':counts,'api':public_api,'unityCallSites':1321,'unsafeGroundHitReferences':0,
                'versionAudit':release,'nativePlaytest':'Final in-game and two-player QA pending'}
    payload['validation/checks.json']=(json.dumps(validation,indent=2)+'\n').encode()
    manifest={'mod':'NugzzMenu','version':VERSION,'gameVersion':'0.4.7f12','s1apiVersion':'3.2.1',
              'sourceCommit':git('rev-parse','HEAD'),'baselineCommit':git('rev-parse','v1.0.0^{commit}'),
              'createdUtc':datetime.now(timezone.utc).isoformat(),
              'files':{name:hashlib.sha256(data).hexdigest() for name,data in payload.items()}}
    payload['manifest.json']=(json.dumps(manifest,indent=2)+'\n').encode()
    OUTPUT.parent.mkdir(parents=True,exist_ok=True)
    with zipfile.ZipFile(OUTPUT,'x',compression=zipfile.ZIP_DEFLATED) as archive:
        for name,data in payload.items():archive.writestr(name,data)
    with zipfile.ZipFile(OUTPUT) as archive:
        assert archive.testzip() is None and set(archive.namelist())==set(payload)
        assert sum(name.endswith('.dll') for name in archive.namelist())==3
        assert 'Mods/nuggzmenu-config.json' not in archive.namelist()
        for name,expected in manifest['files'].items():assert hashlib.sha256(archive.read(name)).hexdigest()==expected
    sums=f'{sha(OUTPUT)}  {OUTPUT.name}\n{release["menuSha256"]}  NugzzMenu.dll\n'
    (OUTPUT.parent/'SHA256SUMS-v1.0.1.txt').write_text(sums,encoding='utf-8')
    print('Verified release ZIP:',OUTPUT)
    print('Source commit:',manifest['sourceCommit'])
    print('Bytes:',OUTPUT.stat().st_size)
    print(sums,end='')

if __name__=='__main__':main()
