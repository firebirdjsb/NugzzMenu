"""Stage the pinned IL2CPP S1API package and refresh local build references."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import urllib.request
import zipfile


def main():
    root = Path(__file__).resolve().parents[1]
    lock = json.loads((root / "S1API.lock.json").read_text(encoding="utf-8"))
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--archive", type=Path, help="Use an already downloaded release ZIP.")
    parser.add_argument("--reference-dir", type=Path, default=root / "net6.0")
    parser.add_argument("--package-dir", type=Path,
                        default=root / "trialbuild" / "s1api" / lock["version"])
    args = parser.parse_args()
    args.package_dir.mkdir(parents=True, exist_ok=True)
    archive = args.archive or args.package_dir / "S1API.zip"
    if args.archive is None and not archive.exists():
        request = urllib.request.Request(lock["downloadUrl"], headers={"User-Agent": "NugzzMenu-DependencyUpdate"})
        with urllib.request.urlopen(request, timeout=60) as response:
            payload = response.read()
        if hashlib.sha256(payload).hexdigest() != lock["sha256"]:
            raise ValueError("S1API download checksum does not match S1API.lock.json")
        archive.write_bytes(payload)

    actual_hash = hashlib.sha256(archive.read_bytes()).hexdigest()
    if actual_hash != lock["sha256"]:
        raise ValueError("S1API archive checksum does not match S1API.lock.json")

    with zipfile.ZipFile(archive) as bundle:
        # Extract only the two explicitly pinned entries, never ZIP-controlled paths.
        contents = {entry: bundle.read(entry)
                    for entry in (lock["referenceAssembly"], lock["loaderAssembly"])}
    for entry, data in contents.items():
        target = args.package_dir / entry
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(data)

    args.reference_dir.mkdir(parents=True, exist_ok=True)
    reference = args.reference_dir / Path(lock["referenceAssembly"]).name
    replacement = contents[lock["referenceAssembly"]]
    if reference.exists() and reference.read_bytes() != replacement:
        old_hash = hashlib.sha256(reference.read_bytes()).hexdigest()[:12]
        backup = args.package_dir / "backups" / (reference.stem + "-" + old_hash + ".dll")
        backup.parent.mkdir(parents=True, exist_ok=True)
        if not backup.exists():
            shutil.copy2(reference, backup)
        print("Previous reference preserved:", backup)
    reference.write_bytes(replacement)
    print("S1API version:", lock["version"])
    print("SHA-256 verified:", actual_hash)
    print("Build reference:", reference)
    print("IL2CPP install package:", args.package_dir)
    print("Game installation was not modified.")


if __name__ == "__main__":
    main()
