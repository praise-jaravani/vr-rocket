import json, os, sys, urllib.request, time
root=sys.argv[1]; kind=sys.argv[2]; res=sys.argv[3]; ids=sys.argv[4:]
def get(url):
    req=urllib.request.Request(url, headers={"User-Agent":"vr-rocket-env/1.0"})
    with urllib.request.urlopen(req, timeout=180) as r: return r.read()
for aid in ids:
    files=json.loads(get(f"https://api.polyhaven.com/files/{aid}"))
    d=os.path.join(root,aid); os.makedirs(d,exist_ok=True); total=0; got=[]
    if kind=="hdri":
        u=files["hdri"][res]["hdr"]["url"]; p=os.path.join(d,os.path.basename(u))
        if not os.path.exists(p): open(p,"wb").write(get(u))
        total=os.path.getsize(p); got.append(os.path.basename(p))
    else:
        for key in ["Diffuse","nor_gl","Rough","AO","arm","Displacement"]:
            e=files.get(key,{}).get(res)
            if not e: continue
            fmt="jpg" if "jpg" in e else ("png" if "png" in e else list(e.keys())[0])
            if key in ("nor_gl","Displacement") and "png" in e: fmt="png"
            u=e[fmt]["url"]; p=os.path.join(d,os.path.basename(u))
            if not os.path.exists(p): open(p,"wb").write(get(u)); time.sleep(0.2)
            total+=os.path.getsize(p); got.append(os.path.basename(p))
    print(f"{aid}: {total//1024} KB {got}")
