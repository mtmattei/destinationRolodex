#!/usr/bin/env python3
"""14 structural/source-consistency checks, not a Uno build or semantic proof.
--require-photos additionally rejects missing full-size originals. Standard library only.
"""
import argparse,ast,json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def load(p):return json.loads((ROOT/p).read_text(encoding='utf-8'))
def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--require-photos',action='store_true');args=p.parse_args()
 g=load('design/design.graph.json');t=load(g['tokens']);s=load(g['states']);m=load(g['assets']);d=load('assets/destinations.json')
 nodes=g['nodes'];ids=[n['id'] for n in nodes];edges=g['edges'];results=[]
 def check(ok,name):
  results.append({'name':name,'passed':bool(ok)});print(('PASS ' if ok else 'FAIL ')+name)
 check(g['schemaVersion']=='1.0','01 graph version')
 check(g['approvedTransitions']==['morph','liquid'],'02 approved transitions')
 check(len(ids)==len(set(ids)),'03 unique node IDs')
 check(len(edges)==len({e['id'] for e in edges}),'04 unique edge IDs')
 check(all(e['from'] in ids and e['to'] in ids for e in edges),'05 edge endpoints resolve')
 check(all(n.get('parent') is None or n['parent'] in ids for n in nodes),'06 parent references resolve')
 parents={n['id']:n.get('parent') for n in nodes};acyclic=True
 for node in ids:
  seen=set();cur=node
  while cur is not None:
   if cur in seen:acyclic=False;break
   seen.add(cur);cur=parents.get(cur)
 check(acyclic,'07 containment is acyclic')
 check(all(n['provenance']['evidence'] and all((ROOT/p).is_file() for p in n['provenance']['evidence']) for n in nodes),'08 evidence files exist')
 check(all(n['provenance']['status'] in ['observed','proposed'] and 0<=n['provenance']['confidence']<=1 for n in nodes),'09 provenance marked')
 check(all(v in ids for v in g['ownership'].values()),'10 ownership references resolve')
 check(all(k in s['regions'] for k in ['view','playback','media','render','motionPolicy','lifecycle','debug']) and len(s['invariants'])>=7,'11 state regions and invariants')
 check([x['id'] for x in d]==['ph','jp','it','no','is','nz'] and {a['id'] for a in m['photographs']}=={x['id'] for x in d},'12 matching six asset IDs')
 check(t['motion']['allowedModes']==g['approvedTransitions'] and t['motion']['durationMs']==1200 and t['motion']['strength']==.6,'13 default tokens match')
 html=(ROOT/'prototype/approved.html').read_text(encoding='utf-8');engine=(ROOT/'prototype/source/transition-engine.js').read_text(encoding='utf-8')
 check(re.findall(r'data-effect="([^"]+)"',html)==['morph','liquid'] and 'vec4 orbit(' not in engine and 'vec4 slices(' not in engine,'14 approved HTML and shader modes')
 missing=[p for p in ['README.md','AGENT-KICKOFF.md','START-HERE.html','reference/original-user-reference.png','assets/images/philippines-preview.jpg','uno-reference/shaders/DestinationTransitions.sksl'] if not (ROOT/p).is_file()]
 syntax=[]
 for p in (ROOT/'tools').glob('*.py'):
  try:ast.parse(p.read_text(encoding='utf-8'))
  except SyntaxError as e:syntax.append(f'{p.name}: {e}')
 photos=[a['expectedPath'] for a in m['photographs'] if not (ROOT/a['expectedPath']).is_file()]
 result={'checks':results,'passed':sum(r['passed'] for r in results),'total':len(results),'requiredFilesMissing':missing,'pythonSyntaxErrors':syntax,'fullSizePhotosMissing':photos,'scope':'Structural/source consistency only; no Uno build, device test or full semantic-proof claim.'}
 (ROOT/'qa/package-validation.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
 if photos:print(f'KNOWN ASSET GAP: {len(photos)} full-size photos. See asset manifest and fetch tool.')
 if missing:print('MISSING REQUIRED FILES:',missing)
 if not all(r['passed'] for r in results) or missing or syntax:raise SystemExit(1)
 if args.require_photos and photos:raise SystemExit(2)
 print('14 consistency checks passed. App build and full-size media remain separate gates.')
if __name__=='__main__':main()
