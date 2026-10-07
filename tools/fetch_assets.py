#!/usr/bin/env python3
"""Retrieve the six exact photo sources. Requires Pillow: python -m pip install Pillow.
No search or silent substitution. Existing valid files are preserved unless --force is used.
Normalizes orientation/encoding to JPEG and records dimensions, hashes and retrieval results.
"""
from __future__ import annotations
import argparse,concurrent.futures,hashlib,io,json,os,time
from datetime import datetime,timezone
from pathlib import Path
from urllib.parse import urlparse,parse_qsl,urlencode,urlunparse
from urllib.request import Request,urlopen
ROOT=Path(__file__).resolve().parents[1]
LIMIT=20_000_000

def retrieve(entry:dict,force:bool)->dict:
    from PIL import Image,ImageOps
    dest=ROOT/entry['expectedPath'];dest.parent.mkdir(parents=True,exist_ok=True)
    result={'id':entry['id'],'path':entry['expectedPath'],'sourceUrl':entry['remoteUrl']}
    try:
        if dest.is_file() and not force:
            with Image.open(dest) as im:im.verify()
            result['status']='existing'
        else:
            url=entry['remoteUrl']
            # Force JPEG encoding without changing which photograph is requested.
            parts=urlparse(url)
            if parts.hostname=='images.unsplash.com':
                query=dict(parse_qsl(parts.query));query.pop('auto',None);query['fm']='jpg'
                url=urlunparse(parts._replace(query=urlencode(query)))
            error=None
            for attempt in range(3):
                try:
                    req=Request(url,headers={'User-Agent':'DestinationRolodex-LocalAssetBuild/1.0','Accept':'image/jpeg,image/*;q=0.9'})
                    with urlopen(req,timeout=30) as res:
                        if not res.headers.get('Content-Type','').lower().startswith('image/'):
                            raise ValueError('Remote source did not return an image.')
                        raw=res.read(LIMIT+1)
                        if len(raw)>LIMIT:raise ValueError('Photo exceeds 20 MB download limit.')
                    break
                except Exception as exc:
                    error=exc
                    if attempt==2:raise
                    time.sleep(1+attempt)
            with Image.open(io.BytesIO(raw)) as im:
                im.load();im=ImageOps.exif_transpose(im).convert('RGB')
                if min(im.size)<200:raise ValueError('Image is unexpectedly small; verify the source manually.')
                tmp=dest.with_suffix('.jpg.part');im.save(tmp,format='JPEG',quality=92,optimize=True);os.replace(tmp,dest)
            result['status']='downloaded';result['retrievalUrl']=url
        with Image.open(dest) as im:result['width'],result['height']=im.size
        raw=dest.read_bytes();result.update(bytes=len(raw),sha256=hashlib.sha256(raw).hexdigest())
        entry.update(bundled=True,downloadStatus=result['status'],width=result['width'],height=result['height'],sha256=result['sha256'])
    except Exception as exc:
        result.update(status='failed',error=str(exc));entry['downloadStatus']='failed';entry['bundled']=False
    return result

def main()->None:
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--force',action='store_true');args=parser.parse_args()
    try:import PIL
    except ImportError:raise SystemExit('Pillow is required for image validation. Run: python -m pip install Pillow')
    path=ROOT/'assets/manifest.json';manifest=json.loads(path.read_text(encoding='utf-8'))
    with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:
        results=list(pool.map(lambda e:retrieve(e,args.force),manifest['photographs']))
    for r in results:print(f"{r['id']}: {r['status']}"+(f" — {r['error']}" if r.get('error') else ''))
    failed=[r for r in results if r['status']=='failed']
    manifest['status']='All six full-size originals are local. Source-page rights checks remain as recorded.' if not failed else f'{len(failed)} required photos are still missing; no substitutes were used.'
    manifest['lastFetchAt']=datetime.now(timezone.utc).isoformat()
    path.write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    (ROOT/'assets/download-report.json').write_text(json.dumps({'time':manifest['lastFetchAt'],'results':results},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    if failed:raise SystemExit('Asset preparation incomplete. See assets/download-report.json and the source pages. Do not call an offline build complete.')
    print('All originals ready. Next: python tools/build_prototype.py --offline')
if __name__=='__main__':main()
