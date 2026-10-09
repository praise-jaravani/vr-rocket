import sys, os, time, urllib.request
from playwright.sync_api import sync_playwright
out_root = sys.argv[1]; slugs = sys.argv[2:]
with sync_playwright() as p:
    browser = p.chromium.connect_over_cdp("http://127.0.0.1:9222")
    ctx = browser.contexts[0]
    for slug in slugs:
        page = ctx.new_page()
        try:
            page.goto(f"https://kenney.nl/assets/{slug}", wait_until="networkidle", timeout=60000)
            zips = page.eval_on_selector_all("a", "els => els.map(e => e.getAttribute('href')).filter(h => h && h.endsWith('.zip'))")
            body = page.inner_text("body")
            lic = "CC0" if ("CC0" in body or "Creative Commons Zero" in body) else "unknown"
            if not zips: print(f"{slug}: no zip link"); continue
            url = zips[0]
            d = os.path.join(out_root, slug); os.makedirs(d, exist_ok=True)
            path = os.path.join(d, os.path.basename(url))
            req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
            with urllib.request.urlopen(req, timeout=300) as r, open(path, "wb") as f: f.write(r.read())
            print(f"{slug}: {os.path.basename(url)} {os.path.getsize(path)//1024} KB licence={lic}")
        except Exception as e:
            print(f"{slug}: FAILED {type(e).__name__}: {str(e)[:160]}")
        finally:
            page.close()
        time.sleep(2)
