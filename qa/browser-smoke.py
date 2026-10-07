#!/usr/bin/env python3
"""Requires Playwright and Pillow. Real browser execution with synthetic test fixtures only."""
from pathlib import Path
from playwright.sync_api import sync_playwright
from PIL import Image, ImageOps, ImageEnhance
import io,json,os,shutil
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'qa'/'artifacts';OUT.mkdir(exist_ok=True)
# Explicit synthetic grid fixtures, not recolored destination photographs.
from PIL import ImageDraw
fixtures=[]
FIX=ROOT/'qa'/'fixtures';FIX.mkdir(exist_ok=True)
for i in range(6):
 im=Image.new('RGB',(960,800))
 draw=ImageDraw.Draw(im)
 for y in range(0,800,20):
  for x in range(0,960,20):
   draw.rectangle((x,y,x+20,y+20),fill=((x*255//960+i*31)%256,(y*255//800+i*53)%256,(i*59+x//4)%256))
 for x in range(0,960,80):draw.line((x,0,x,800),fill='white',width=2)
 for y in range(0,800,80):draw.line((0,y,960,y),fill='white',width=2)
 draw.rectangle((15,15,720,55),fill='black');draw.text((25,29),f'SYNTHETIC QA FIXTURE {i+1} - NOT DESTINATION PHOTOGRAPHY',fill='white')
 buf=io.BytesIO();im.save(buf,format='JPEG',quality=90);fixtures.append(buf.getvalue())
 if i<2:(FIX/f'fixture-{i+1}.jpg').write_bytes(buf.getvalue())
keys=['1758782551890','1635861105962','1741893041975','1710762635726','1532586733327','Milford']
results=[]
def check(value,label):
 if not value:raise AssertionError(label)
 results.append(label);print('PASS',label,flush=True)
with sync_playwright() as p:
 browser=p.chromium.launch(executable_path=os.environ.get('CHROMIUM_PATH') or shutil.which('chromium') or p.chromium.executable_path,headless=True,args=['--no-sandbox','--disable-dev-shm-usage','--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader'])
 # Layout captures use the actual bundled loading crop, not a generated mockup.
 ref=browser.new_page(viewport={'width':1440,'height':1040},device_scale_factor=1)
 preview=(ROOT/'assets/images/philippines-preview.jpg').read_bytes()
 def reference_route(route):
  if '1758782551890' in route.request.url:route.fulfill(status=200,content_type='image/jpeg',headers={'Access-Control-Allow-Origin':'*'},body=preview)
  else:route.abort()
 ref.route('https://**/*',reference_route)
 ref.set_content((ROOT/'prototype/approved.html').read_text(),wait_until='load')
 ref.wait_for_function('window.destinationCard && destinationCard.selectedIndex===0');ref.evaluate('destinationCard.pause()');ref.wait_for_timeout(900)
 ref.screenshot(path=str(ROOT/'reference/layout-desktop-preview-only.png'),full_page=True)
 ref.locator('destination-rolodex').screenshot(path=str(ROOT/'reference/card-preview-only.png'))
 ref.set_viewport_size({'width':390,'height':844});ref.wait_for_timeout(200)
 ref.screenshot(path=str(ROOT/'reference/layout-mobile-preview-only.png'),full_page=True)
 ref.close()
 (ROOT/'reference/capture-metadata.json').write_text(json.dumps({'source':'prototype/approved.html','desktopViewport':[1440,1040],'mobileViewport':[390,844],'dpr':1,'mediaOverride':'For layout capture only, the Philippines remote URL is fulfilled with the bundled preview. Other remote images are blocked. Production prototype URLs are unchanged.','fontNote':'Browser fallback may differ from the target app; no font files are included.'},indent=2))
 page=browser.new_page(viewport={'width':1440,'height':1040},device_scale_factor=1)
 errors=[];logs=[]
 page.on('pageerror',lambda e: errors.append(str(e)))
 page.on('console',lambda e: logs.append(e.type+': '+e.text))
 def fixture(route):
  i=next((i for i,k in enumerate(keys) if k in route.request.url),0)
  route.fulfill(status=200,content_type='image/jpeg',headers={'Access-Control-Allow-Origin':'*'},body=fixtures[i])
 page.route('https://**/*',fixture)
 page.set_content((ROOT/'prototype'/'approved.html').read_text(),wait_until='load')
 page.wait_for_function('window.destinationCard && destinationCard.selectedIndex===0')
 page.evaluate('destinationCard.pause()')
 page.wait_for_timeout(1200)
 print('ENGINE',page.evaluate('({available:destinationCard._fx.available,error:destinationCard._fx.error})'))
 print('ERRORS',errors,'CONSOLE',logs[-8:])
 check(page.evaluate('destinationCard._fx.available'), 'WebGL shader compiles')
 check(page.locator('destination-rolodex .country-row').count()==9, 'Original infinite picker retained')
 check(page.locator('.effect-option').count()==2,'Two approved live mode selectors')
 check(page.locator('#baseline').count()==0,'No public crossfade comparison')
 for rejected in ['orbit','slices','fade']:
  check(page.evaluate('(m)=>{try{destinationCard.transition=m;return false}catch(e){return e instanceof TypeError}}',rejected),'Reject public '+rejected)
 page.screenshot(path=str(OUT/'desktop-fixture.png'),full_page=True)
 # Deterministic replay and frame inspection.
 for mode in ['morph','liquid']:
  page.evaluate('(mode)=>{destinationCard.transition=mode;}',mode)
  check(page.evaluate('destinationCard.seekTransition(.5)'),mode+' scrubs')
  page.wait_for_timeout(70)
  check(page.evaluate('destinationCard._fx.active && Math.abs(destinationCard._fx.progress-.5)<.001'),mode+' active at midpoint')
  page.screenshot(path=str(OUT/f'{mode}-midpoint-fixture.png'),full_page=True)
  import base64
  for progress in [0,.25,.5,.75,1]:
   page.evaluate('(p)=>destinationCard.seekTransition(p)',progress)
   raw=page.evaluate('destinationCard._fx.canvas.toDataURL("image/png").split(",")[1]')
   (OUT/f'{mode}-{int(progress*100):03}-fixture.png').write_bytes(base64.b64decode(raw))
  for value in [0,1]:
   page.evaluate('(v)=>destinationCard.seekTransition(v)',value)
   page.wait_for_timeout(40)
   check(page.evaluate('destinationCard.selectedIndex')==(0 if value==0 else 1),mode+f' selection at {value}')
  # Capturing GL error after drawing ensures actual render-path validity.
  check(page.evaluate('destinationCard._fx.gl.getError()')==0, mode+' renders without GL errors')
 check((OUT/'morph-050-fixture.png').read_bytes() != (OUT/'liquid-050-fixture.png').read_bytes(),'Distinct intermediate effect output')
 for phase in ['000','100']:
  check((OUT/f'morph-{phase}-fixture.png').read_bytes() == (OUT/f'liquid-{phase}-fixture.png').read_bytes(),'Identical mode endpoints '+phase)
 page.evaluate('destinationCard.transition="morph";destinationCard.transitionDuration=650;')
 check(page.evaluate('destinationCard.replayTransition()'),'Replay starts')
 page.wait_for_function('!destinationCard._previewing && !destinationCard._fx.active',timeout=10000)
 check(page.evaluate('!destinationCard._previewing && !destinationCard._fx.active'),'Replay releases control cleanly')
 check(page.evaluate('destinationCard.selectedIndex===1 && destinationCard._displayedId==="jp"'),'Replay commits matching destination and photo')
 # Normal picker interactions after a scrub.
 page.evaluate('destinationCard.seekTransition(.3)')
 page.locator('destination-rolodex .next').click()
 page.wait_for_timeout(1450)
 check(page.evaluate('!destinationCard._previewing'),'Navigation exits scrub mode')
 check(page.evaluate('destinationCard.selectedIndex===1 && destinationCard._displayedId==="jp"'),'Navigation after scrub matches selected photo')
 page.evaluate('destinationCard.goTo(0)')
 page.wait_for_timeout(1300)
 page.locator('destination-rolodex .picker').focus()
 page.keyboard.press('ArrowUp');page.wait_for_timeout(1400)
 check(page.evaluate('destinationCard.selectedIndex===5 && destinationCard._displayedId==="nz"'),'Backward loop works with transitions')
 page.keyboard.press('ArrowDown');page.wait_for_timeout(1400)
 check(page.evaluate('destinationCard.selectedIndex===0 && destinationCard._displayedId==="ph"'),'Forward loop works with transitions')
 page.locator('destination-rolodex .look-toggle').click()
 check(page.evaluate('destinationCard.view')=='look','Look-around retained')
 page.locator('destination-rolodex .zoom-in').click()
 check(page.evaluate('destinationCard._targetZoom>1'),'Zoom retained')
 page.locator('destination-rolodex .scene').focus();page.keyboard.press('Escape')
 check(page.evaluate('destinationCard.view')=='browse','Escape restores browsing')
 page.evaluate('destinationCard.goTo(2);destinationCard.goTo(4);destinationCard.goTo(3)')
 page.wait_for_timeout(1600)
 check(page.evaluate('destinationCard.selectedIndex===3 && destinationCard._displayedId==="no"'),'Rapid navigation settles correctly')
 check(not errors,'No browser JavaScript errors')
 # Mobile layout / input.
 page.set_viewport_size({'width':390,'height':844})
 page.wait_for_timeout(250)
 check(page.evaluate('document.documentElement.scrollWidth<=innerWidth'),'Mobile has no horizontal overflow')
 page.screenshot(path=str(OUT/'mobile.png'),full_page=True)
 # Reduced-motion behavior.
 page.emulate_media(reduced_motion='reduce');page.wait_for_timeout(200)
 page.evaluate('destinationCard.replayTransition()')
 page.wait_for_timeout(300)
 check(page.evaluate('!destinationCard._fx.active && !destinationCard._auto'),'Reduced motion disables the shader animation')
 page.close()
 # Context denied: base component survives.
 fallback=browser.new_page(viewport={'width':1200,'height':850})
 deny_webgl = "<script>(()=>{const old=HTMLCanvasElement.prototype.getContext;HTMLCanvasElement.prototype.getContext=function(t,...a){return String(t).includes('webgl')?null:old.call(this,t,...a);};})()</script>"
 fallback.route('https://**/*',fixture)
 fallback.set_content((ROOT/'prototype'/'approved.html').read_text().replace('<head>', '<head>'+deny_webgl, 1),wait_until='load')
 fallback.wait_for_function('window.destinationCard && destinationCard.selectedIndex===0')
 fallback.evaluate('destinationCard.pause();destinationCard.next()');fallback.wait_for_function('destinationCard.selectedIndex===1', timeout=8000)
 print('FALLBACK', fallback.evaluate('({renderer:destinationCard.transitionRenderer,index:destinationCard.selectedIndex})'),flush=True)
 check(fallback.evaluate('destinationCard.transitionRenderer==="crossfade" && destinationCard.selectedIndex===1'),'No-WebGL fallback keeps navigation working')
 fallback.close();browser.close()
(ROOT/'qa/browser-results.json').write_text(json.dumps({'passed':results,'errors':errors,'fixture_note':'Browser tests use synthetic grid images, not production photography. They do not verify external hosts, Uno/C# compilation, SkSL execution or device performance.'},indent=2))
print(json.dumps({'passed':len(results),'errors':errors}))
