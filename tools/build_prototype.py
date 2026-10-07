#!/usr/bin/env python3
"""Build a self-contained HTML reference from approved source. Standard library only.
Connected build preserves original remote photos. --offline requires every local photo.
"""
from __future__ import annotations
import argparse,base64,copy,json,mimetypes,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def data_uri(path:Path)->str:
    if not path.is_file(): raise FileNotFoundError(f'Missing asset: {path.relative_to(ROOT)}')
    mime=mimetypes.guess_type(path.name)[0] or 'application/octet-stream'
    return f'data:{mime};base64,'+base64.b64encode(path.read_bytes()).decode('ascii')
def main()->None:
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--offline',action='store_true');args=parser.parse_args()
    source=ROOT/'prototype/source'
    data=json.loads((ROOT/'assets/destinations.json').read_text(encoding='utf-8'))
    manifest=json.loads((ROOT/'assets/manifest.json').read_text(encoding='utf-8'))
    assets={a['id']:a for a in manifest['photographs']}
    if args.offline:
        missing=[a['expectedPath'] for a in assets.values() if not (ROOT/a['expectedPath']).is_file()]
        if missing:raise SystemExit('Offline build stopped. Download the missing originals first:\n'+'\n'.join(missing)+'\nRun: python tools/fetch_assets.py')
    linked=copy.deepcopy(data)
    for d in linked:
        media=d['media']
        if args.offline:media['src']='../../'+assets[d['id']]['expectedPath']
        if media.get('preview'):media['preview']='../../'+media['preview']
    (source/'destinations.js').write_text('window.DEMO_DESTINATIONS = '+json.dumps(linked,ensure_ascii=False,indent=2)+';\n',encoding='utf-8')
    for d in data:
        media=d['media']
        if args.offline:media['src']=data_uri(ROOT/assets[d['id']]['expectedPath'])
        if media.get('preview'):media['preview']=data_uri(ROOT/media['preview'])
    html=(source/'index.html').read_text(encoding='utf-8')
    html=html.replace('<link rel="stylesheet" href="demo.css">','<style>\n'+(source/'demo.css').read_text(encoding='utf-8')+'\n</style>')
    for name in ['transition-engine.js','rolodex.js','destinations.js','demo.js','lab.js']:
        script=('window.DEMO_DESTINATIONS = '+json.dumps(data,ensure_ascii=False).replace('</','<\\/')+';') if name=='destinations.js' else (source/name).read_text(encoding='utf-8')
        script=re.sub(r'</script',r'<\\/script',script,flags=re.I)
        html=html.replace(f'<script src="{name}"></script>','<script>\n'+script+'\n</script>')
    if args.offline:
        html=html.replace('This connected prototype uses the original online photos. A local loading preview is bundled. Run tools/fetch_assets.py, then tools/build_prototype.py --offline from the handoff root to embed all six photos.','All six photographs and all component code are embedded in this offline build. Credits link to the original sources.')
    name='approved-offline.html' if args.offline else 'approved.html'
    path=ROOT/'prototype'/name;path.write_text(html,encoding='utf-8');print(f'Built {path.relative_to(ROOT)} ({path.stat().st_size:,} bytes)')
if __name__=='__main__':main()
