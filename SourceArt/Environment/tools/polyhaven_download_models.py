import json, os, sys, urllib.request, time
ids = sys.argv[2:]
root = sys.argv[1]
def get(url):
    req = urllib.request.Request(url, headers={"User-Agent": "vr-rocket-env/1.0"})
    with urllib.request.urlopen(req, timeout=120) as r: return r.read()
for aid in ids:
    files = json.loads(get(f"https://api.polyhaven.com/files/{aid}"))
    fbx = files["fbx"]["1k"]["fbx"]
    d = os.path.join(root, aid); os.makedirs(d, exist_ok=True)
    total = 0
    items = [("fbx", fbx["url"])] + [(k, v["url"]) for k, v in fbx.get("include", {}).items()]
    for rel, url in items:
        name = os.path.basename(url) if rel == "fbx" else rel
        path = os.path.join(d, os.path.basename(name))
        if os.path.exists(path): total += os.path.getsize(path); continue
        data = get(url); open(path, "wb").write(data); total += len(data); time.sleep(0.2)
    print(f"{aid}: {len(items)} files, {total//1024} KB -> {[os.path.basename(u) for _,u in items]}")
