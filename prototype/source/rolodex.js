/**
 * Destination Rolodex. Dependency-free custom element.
 * Copyright 2026. Component source licensed under MIT; photographs retain their own licenses.
 */
(() => {
  'use strict';
  if (customElements.get('destination-rolodex')) return;
  const mod = (n, d) => ((n % d) + d) % d;
  const clamp = (n, a, b) => Math.max(a, Math.min(b, n));
  const svg = (body, size = 18) => `<svg width="${size}" height="${size}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.45" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${body}</svg>`;
  const icons = {
    globe: svg('<circle cx="12" cy="12" r="9"/><ellipse cx="12" cy="12" rx="4.3" ry="9"/><path d="M3 12h18M4.8 6.8h14.4M4.8 17.2h14.4"/>'),
    orbit: svg('<circle cx="12" cy="12" r="3"/><ellipse cx="12" cy="12" rx="10" ry="4.5" transform="rotate(-35 12 12)"/><path d="m18 3 3 1-1 3"/>'),
    pin: svg('<path d="M18.5 10c0 5-6.5 10-6.5 10S5.5 15 5.5 10a6.5 6.5 0 0 1 13 0Z"/><circle cx="12" cy="10" r="2"/>', 15),
    up: svg('<path d="m6 14 6-6 6 6"/>'),
    down: svg('<path d="m6 10 6 6 6-6"/>'),
    play: svg('<path d="m9 5 10 7-10 7V5Z" fill="currentColor" stroke="none"/>', 16),
    pause: svg('<path d="M8.5 6v12M15.5 6v12" stroke-width="2.7"/>', 16),
    back: svg('<path d="m10 6-6 6 6 6M4 12h16"/>', 16),
    reset: svg('<path d="M3 10a9 9 0 1 1 2 8M3 4v6h6"/>', 16),
    minus: svg('<path d="M6 12h12"/>', 16),
    plus: svg('<path d="M6 12h12M12 6v12"/>', 16),
    info: svg('<circle cx="12" cy="12" r="9"/><path d="M12 11v6M12 7v.1"/>', 15),
    close: svg('<path d="m6 6 12 12M18 6 6 18"/>'),
    scroll: svg('<path d="M12 3v18m-3-3 3 3 3-3M9 6l3-3 3 3"/>', 15),
    expand: svg('<path d="M8 3H3v5m13-5h5v5M3 16v5h5m13-5v5h-5"/>', 16),
  };
  let serial = 0;
  const stylesheet = `
    :host{display:block;width:100%;max-width:var(--rolodex-width,480px);container-type:inline-size;color:#263633;font-family:Inter,"Helvetica Neue",Arial,sans-serif;-webkit-font-smoothing:antialiased;--accent:#e5f5b6;--radius:26px;--scene-height:clamp(365px,58svh,530px)}
    *,*::before,*::after{box-sizing:border-box}
    button,a,input{-webkit-tap-highlight-color:transparent}
    button{font:inherit;cursor:pointer;color:inherit;border:0;background:none;padding:0}
    button:disabled{cursor:default;opacity:.45}
    button:focus-visible,a:focus-visible,[tabindex]:focus-visible{outline:2px solid #bce589;outline-offset:4px}
    svg{display:block;flex-shrink:0}
    .sr{position:absolute!important;width:1px;height:1px;padding:0;margin:-1px;overflow:hidden;clip:rect(0,0,0,0);white-space:nowrap;border:0}
    .card{position:relative;background:#fff;border-radius:var(--rolodex-radius,var(--radius));isolation:isolate;box-shadow:0 0 0 1px #172e2012,0 4px 8px #16312303,0 24px 64px -22px #223e3336;overflow:hidden}
    .scene{height:var(--rolodex-scene-height,var(--scene-height));min-height:340px;position:relative;isolation:isolate;background:#295c65;color:#fff;overflow:hidden;touch-action:pan-x pinch-zoom;user-select:none;-webkit-user-select:none;cursor:grab;contain:layout paint}
    .scene.dragging{cursor:grabbing}
    .layers{position:absolute;inset:-13%;pointer-events:none;will-change:transform;transform-origin:50% 50%}
    .layer{position:absolute;inset:0;opacity:0;transition:opacity 650ms cubic-bezier(.2,.7,.2,1);background-size:cover;background-position:center;overflow:hidden}
    .layer.active{opacity:1}
    .transition-canvas{position:absolute;inset:0;width:100%;height:100%;z-index:1;opacity:0;pointer-events:none}
    .layers.fx-mounted .layer{transition:none}
    .motion-badge{position:absolute;left:26px;top:70px;z-index:3;font-size:8px;letter-spacing:1.25px;text-transform:uppercase;opacity:.7;pointer-events:none}
    .scene[data-view="look"] .motion-badge{display:none}
    .layer img,.layer video{display:block;width:100%;height:100%;object-fit:cover;object-position:var(--focal,50% 50%)}
    .wash{position:absolute;inset:0;pointer-events:none;background:linear-gradient(180deg,#051d2430 0%,#021e2412 18%,#00171c18 35%,#00161e29 54%,#00161c08 76%,#061c305c 100%),linear-gradient(90deg,#00131920,transparent 45%,#0013190b)}
    .readability{position:absolute;inset:17% 0 12%;pointer-events:none;background:radial-gradient(ellipse at center,#00171f2e 0%,#00171f12 49%,transparent 74%);transition:opacity 400ms}
    .topbar{position:absolute;z-index:4;left:26px;right:20px;top:20px;display:flex;align-items:center;justify-content:space-between;gap:12px;pointer-events:none}
    .eyebrow{display:flex;align-items:center;gap:9px;font-size:10px;letter-spacing:2.2px;font-weight:600;text-shadow:0 1px 8px #00242955}
    .top-actions{display:flex;align-items:center;gap:12px;pointer-events:auto}
    .counter{font-size:10px;letter-spacing:1px;font-variant-numeric:tabular-nums;opacity:.88}
    .counter span{opacity:.5;margin:0 5px}
    .icon-button{width:38px;height:38px;display:grid;place-items:center;border:1px solid #ffffff42;border-radius:50%;background:#0c333224;backdrop-filter:blur(10px);transition:background 180ms,transform 180ms}
    .icon-button:hover{background:#ffffff25;transform:scale(1.05)}
    .icon-button:active{transform:scale(.96)}
    .play-progress{position:absolute;width:42px;height:42px;pointer-events:none;transform:rotate(-90deg)}
    .play-progress circle{stroke:var(--accent);stroke-width:1;fill:none;stroke-dasharray:113.1;stroke-dashoffset:113.1}
    .autoplay-button{position:relative}
    .picker{position:absolute;z-index:2;left:0;right:0;top:49.5%;height:300px;transform:translateY(-50%);outline:0!important;perspective:900px;transition:opacity 360ms,transform 600ms cubic-bezier(.2,.7,.2,1);mask-image:linear-gradient(transparent,#000 10%,#000 90%,transparent);-webkit-mask-image:linear-gradient(transparent,#000 10%,#000 90%,transparent)}
    .picker:focus-visible::after{content:"";position:absolute;inset:106px 13px;border:1px solid #e5f5b6a6;border-radius:8px;pointer-events:none}
    .rows{position:absolute;inset:0;transform-style:preserve-3d;pointer-events:none}
    .country-row{position:absolute;top:50%;left:24px;right:24px;height:70px;margin-top:-35px;display:flex;justify-content:center;align-items:center;white-space:nowrap;font-size:clamp(36px,12.45cqw,64px);font-weight:650;letter-spacing:-.057em;line-height:1.12;text-shadow:0 3px 20px #001c2324;transform-origin:center;will-change:transform,opacity;pointer-events:auto;cursor:pointer;backface-visibility:hidden}
    .country-row.long{font-size:clamp(34px,11.3cqw,59px)}
    .selection-marker{position:absolute;top:50%;left:20px;width:7px;height:7px;margin-top:-3.5px;border-radius:50%;background:var(--accent);box-shadow:0 0 0 5px #e5f5b619;pointer-events:none}
    .selection-marker-right{position:absolute;right:19px;top:50%;width:12px;height:1px;background:#ffffff60;pointer-events:none}
    .scene-footer{position:absolute;z-index:5;left:24px;right:21px;bottom:21px;display:flex;align-items:center;justify-content:space-between;gap:8px;pointer-events:none}
    .gesture-hint{display:flex;align-items:center;gap:7px;font-size:9px;letter-spacing:1.3px;font-weight:500;opacity:.82;white-space:nowrap;text-shadow:0 2px 6px #00202940}
    .look-toggle{display:flex;align-items:center;gap:7px;min-height:40px;padding:0 13px;border-radius:30px;border:1px solid #ffffff52;box-shadow:0 2px 8px #001b2210;background:#13373225;backdrop-filter:blur(12px);font-size:11px;font-weight:500;pointer-events:auto;transition:background 180ms,transform 180ms}
    .look-toggle:hover{background:#ffffff22}
    .look-toggle:active{transform:scale(.96)}
    .detail{padding:23px 25px 15px}
    .detail-head{display:flex;align-items:center;justify-content:space-between;gap:12px;margin-bottom:10px}
    .location{display:flex;align-items:center;gap:6px;font-size:12px;font-weight:600;min-width:0;letter-spacing:.06px}
    .location svg{color:#788581}
    .region{font-size:9px;font-weight:500;letter-spacing:1.45px;color:#7e8984;white-space:nowrap}
    .description{font-size:13px;line-height:1.65;margin:0;color:#707a75;min-height:43px;max-width:385px;letter-spacing:.035px}
    .detail-bottom{display:flex;align-items:center;justify-content:space-between;margin-top:13px;gap:12px}
    .pagination{display:flex;align-items:center;gap:3px;max-width:calc(100% - 116px);flex-wrap:wrap}
    .page-dot{height:30px;width:24px;display:flex;align-items:center;justify-content:center}
    .page-dot::after{content:"";width:16px;height:3px;border-radius:4px;background:#dee4df;transition:background 200ms,width 250ms}
    .page-dot[aria-pressed="true"]::after{background:#3d5e4e;width:24px}
    .page-dot:hover::after{background:#8da392}
    .navigation{display:flex;align-items:center;gap:5px;flex-shrink:0}
    .nav-button{width:40px;height:36px;display:grid;place-items:center;border:1px solid #e5eae5;border-radius:11px;color:#52655b;transition:background 180ms,transform 180ms}
    .nav-button:hover{background:#f3f6f2}
    .nav-button:active{transform:scale(.93)}
    .look-title{position:absolute;left:27px;right:27px;top:91px;z-index:3;opacity:0;pointer-events:none;transform:translateY(6px);transition:opacity 350ms,transform 400ms}
    .look-country{font-size:34px;letter-spacing:-1.3px;font-weight:600}
    .look-subtitle{font-size:11px;opacity:.77;line-height:1.5;margin-top:7px}
    .camera-controls{display:none;align-items:center;gap:2px;background:#092b333b;backdrop-filter:blur(12px);border:1px solid #ffffff45;border-radius:24px;padding:2px;pointer-events:auto}
    .camera-controls button{height:34px;width:33px;display:grid;place-items:center;border-radius:50%}
    .camera-controls button:hover{background:#ffffff1f}
    .camera-controls output{min-width:32px;text-align:center;font-size:9px;font-variant-numeric:tabular-nums}
    .camera-controls .reset{border-left:1px solid #ffffff29;border-radius:0;margin-left:3px;padding-left:3px}
    .photo-credit{position:absolute;z-index:3;bottom:78px;left:26px;color:#ffffffa8;text-decoration:none;font-size:9px;display:none;align-items:center;gap:5px;text-shadow:0 1px 4px #0005}
    .photo-credit:hover{color:white}
    .scene[data-view="look"]{touch-action:none}
    .scene[data-view="look"] .picker{opacity:0;transform:translateY(-46%);pointer-events:none}
    .scene[data-view="look"] .picker *{pointer-events:none}
    .scene[data-view="look"] .look-title{opacity:1;transform:translateY(0)}
    .scene[data-view="look"] .readability{opacity:0}
    .scene[data-view="look"] .gesture-hint{display:none}
    .scene[data-view="look"] .camera-controls{display:flex}
    .scene[data-view="look"] .photo-credit{display:flex}
    .scene[data-view="look"] .autoplay-button{visibility:hidden}
    .media-status{position:absolute;z-index:7;left:26px;top:72px;font-size:10px;background:#10322f66;backdrop-filter:blur(8px);border:1px solid #ffffff28;border-radius:20px;padding:6px 10px;color:white;display:none;align-items:center;gap:7px;max-width:calc(100% - 52px)}
    .media-status.visible{display:flex}
    .media-status button{font-weight:600;text-decoration:underline;text-underline-offset:3px}
    .empty{padding:50px 25px;text-align:center;font-size:14px;color:#61756c}
    @container (max-width:360px){.topbar{left:20px;right:16px}.detail{padding:20px 20px 13px}.scene-footer{left:19px;right:16px}.selection-marker{left:15px;width:6px;height:6px}.selection-marker-right{right:14px;width:9px}.region{font-size:8px;letter-spacing:1px}.location{font-size:11px}.eyebrow{font-size:9px;letter-spacing:1.5px}.description{font-size:12px}.counter{display:none}.look-toggle{padding:0 11px}.gesture-hint{font-size:8px;letter-spacing:.8px}.picker{height:266px}.nav-button{width:37px}.detail-bottom{margin-top:9px}}
    @media(prefers-reduced-motion:reduce){*,*::before,*::after{animation:none!important;transition:none!important;scroll-behavior:auto!important}.layers{will-change:auto}.country-row{will-change:auto}}
  `;

  class DestinationRolodex extends HTMLElement {
    constructor() {
      super();
      this.attachShadow({ mode: 'open' });
      this._id = `rolodex-${++serial}`;
      this._data = [];
      this._cache = new Map();
      this._loaded = new Set();
      this._position = this._target = 0;
      this._velocity = 0;
      this._index = -1;
      this._front = 0;
      this._mediaToken = 0;
      this._view = 'browse';
      this._pan = { x: 0, y: 0, targetX: 0, targetY: 0 };
      this._zoom = this._targetZoom = 1;
      this._reducedQuery = matchMedia('(prefers-reduced-motion: reduce)');
      this._reduced = this._reducedQuery.matches;
      this._auto = false;
      this._hover = false;
      this._focused = false;
      this._visible = true;
      this._progress = 0;
      this._frame = 0;
      this._last = 0;
      this._phase = 0;
      this._drag = null;
      this._rendered = false;
      this._onFrame = t => this._tick(t);
      this._transitionMode = 'morph';
      this._transitionDuration = 1200;
      this._transitionStrength = .6;
      this._previewing = false;
      this._mountSerial = 0;
      this._previewTicket = 0;
    }

    get destinations() { return this._data.map(d => ({ ...d, media: { ...d.media } })); }
    set destinations(value) {
      if (!Array.isArray(value) || !value.length) throw new TypeError('destinations must be a non-empty array.');
      const ids = new Set();
      const safeURL = (value, label) => {
        if (typeof value !== 'string' || !value.trim()) throw new TypeError(`${label} must be a non-empty URL or path.`);
        const url = new URL(value, document.baseURI);
        if (!['http:', 'https:', 'file:', 'data:', 'blob:'].includes(url.protocol)) throw new TypeError(`${label} uses an unsupported URL protocol.`);
        return value;
      };
      this._data = value.map((d, i) => {
        if (!d || typeof d.country !== 'string' || !d.country.trim()) throw new TypeError(`Destination ${i} needs a country.`);
        const id = String(d.id || i);
        if (ids.has(id)) throw new TypeError(`Duplicate destination id: ${id}`);
        ids.add(id);
        if (!d.media || !['image', 'video'].includes(d.media.type || 'image')) throw new TypeError(`Destination ${i} needs image or video media.`);
        return {
          id, country: d.country.trim(), location: String(d.location || d.country),
          region: String(d.region || ''), description: String(d.description || ''),
          color: /^#[\da-f]{3,8}$/i.test(d.color) ? d.color : '#244e51',
          media: {
            type: d.media.type || 'image', src: safeURL(d.media.src, `Destination ${i} media.src`),
            poster: d.media.poster ? safeURL(d.media.poster, 'poster') : '',
            preview: d.media.preview ? safeURL(d.media.preview, 'preview') : '',
            position: typeof d.media.position === 'string' ? d.media.position : '50% 50%',
            alt: String(d.media.alt || `${d.location || d.country}, ${d.country}`),
            credit: String(d.media.credit || ''),
            source: d.media.source ? safeURL(d.media.source, 'source') : '',
          },
        };
      });
      this._mediaToken++;
      this._fx?.cancel();this._previewing=false;this._lastTransitionPair=null;this._displayedFrame=null;this._displayedId=null;
      this._cache.clear();
      this._loaded.clear();
      const start = clamp(parseInt(this.getAttribute('initial-index') || '0', 10) || 0, 0, this._data.length - 1);
      this._position = this._target = start;
      this._velocity = 0;
      this._index = -1;
      if (this.isConnected) this._initializeData();
    }
    get selectedIndex() { return this._index; }
    get selectedDestination() { return this._index < 0 ? null : this.destinations[this._index]; }
    get view() { return this._view; }
    get playing() { return this._auto; }
    get transition() { return this._transitionMode; }
    set transition(value) {
      if (!['morph','liquid'].includes(value)) throw new TypeError('Unknown transition.');
      this._transitionMode = value;
      this._syncTransitionBadge();
      this.dispatchEvent(new CustomEvent('transitionmodechange', {detail:{mode:value}}));
    }
    get transitionDuration() { return this._transitionDuration; }
    set transitionDuration(value) {
      if (!Number.isFinite(Number(value))) throw new TypeError('Duration must be a number of milliseconds.');
      this._transitionDuration = clamp(Number(value), 350, 3000);
    }
    get transitionStrength() { return this._transitionStrength; }
    set transitionStrength(value) {
      if (!Number.isFinite(Number(value))) throw new TypeError('Strength must be a number between zero and one.');
      this._transitionStrength = clamp(Number(value), 0, 1);
    }
    get transitionRenderer() { return this._fx?.available ? 'webgl' : 'crossfade'; }
    _syncTransitionBadge() {
      const badge = this.shadowRoot.querySelector('.motion-badge');
      if (badge) badge.textContent = {morph:'Morph blend',liquid:'Liquid warp'}[this._transitionMode];
    }
    _fxOptions(preview = false) {
      return { mode:this._transitionMode, duration:this._reduced ? 1 : this._transitionDuration,
        strength:this._transitionStrength, direction:this._lastDirection || 1, preview };
    }
    _emitMotion(name, detail) {
      this.dispatchEvent(new CustomEvent(name, {bubbles:true, composed:true, detail}));
    }
    _onTransitionFrame(detail) {
      if (detail.preview && this._previewing) this._syncPreviewPosition(detail.progress);
      this._emitMotion('transitionprogress', detail);
    }
    async _onTransitionEnd(detail) {
      if (detail.preview && this._previewing) {
        this._syncPreviewPosition(1);
        const destination = this._data.find(d=>d.id===detail.to.id);
        const token = ++this._mediaToken;
        this._suppressTransition = true;
        try { await this._mountMedia(destination, false, token); }
        finally { this._suppressTransition = false; this._previewing = false; }
      }
      this._emitMotion('transitionend', detail);
    }
    _syncPreviewPosition(progress) {
      const pair = this._previewPair;
      if (!pair) return;
      const n = this._data.length, from = this._data.findIndex(d=>d.id===pair.from.id), to = this._data.findIndex(d=>d.id===pair.to.id);
      let delta = mod(to-from,n); if (delta>n/2) delta-=n;
      const p = progress*progress*(3-2*progress);
      this._position = this._target = from + delta*p;
      this._velocity = 0;
      this._userChange = true;
      this._renderRows();
    }
    async _preparePreview() {
      if (!this._data.length || this._data.length<2) return null;
      this.pause();
      if (this._view==='look') this.setView('browse');
      const pair=this._lastTransitionPair || {
        from:this._data[Math.max(0,this._index)],
        to:this._data[mod(Math.max(0,this._index)+1,this._data.length)]
      };
      if (!this._fx?.available || this._reduced) {
        this.goTo(this._data.findIndex(d=>d.id===pair.to.id));
        this._emitMotion('transitionfallback',{message:this._reduced?'Reduced motion is enabled.':'WebGL is unavailable. Using the original crossfade.'});
        return null;
      }
      const ticket=++this._previewTicket;
      ++this._mediaToken;
      clearTimeout(this._statusTimer);
      this._status('',false);
      this._scene.setAttribute('aria-busy','false');
      this._previewing=true;
      this._previewPair=pair;
      this._lastTransitionPair=pair;
      let delta=mod(this._data.findIndex(d=>d.id===pair.to.id)-this._data.findIndex(d=>d.id===pair.from.id),this._data.length);
      if(delta>this._data.length/2)delta-=this._data.length;
      this._lastDirection=Math.sign(delta)||1;
      this._target=this._position;this._velocity=0;
      const media=d=>({...d.media,id:d.id,country:d.country});
      try {
        await Promise.all([this._preload(pair.from.media),this._preload(pair.to.media)]);
        if(ticket!==this._previewTicket)return null;
        return {from:media(pair.from),to:media(pair.to)};
      } catch(error) {
        if(ticket===this._previewTicket){this._previewing=false;this._emitMotion('transitionfallback',{message:'Photos need to finish loading before replay.'});}
        return null;
      }
    }
    async replayTransition() {
      this._fx?.cancel();
      const pair=await this._preparePreview();if(!pair)return false;
      this._pan.targetX=this._pan.targetY=0;this._targetZoom=1;
      const ok=await this._fx.play(pair.from,pair.to,this._fxOptions(true));
      if(!ok)this._exitPreview();
      return ok;
    }
    async seekTransition(progress) {
      if(!Number.isFinite(Number(progress)))throw new TypeError('Progress must be a number.');
      const pair=await this._preparePreview();if(!pair)return false;
      const ok=await this._fx.seek(pair.from,pair.to,clamp(Number(progress),0,1),this._fxOptions(true));
      if(!ok)this._exitPreview();
      return ok;
    }
    _exitPreview() {
      if(!this._previewing)return;
      ++this._previewTicket;this._fx?.cancel();
      this._previewing=false;
      this._position=this._target=Math.round(this._position);this._velocity=0;
      const d=this._data[mod(this._position,this._data.length)];
      const token=++this._mediaToken;
      this._suppressTransition=true;
      this._mountMedia(d,false,token).finally(()=>{this._suppressTransition=false;});
      this._renderRows();
    }

    connectedCallback() {
      if (!this._rendered) this._build();
      if (!this._fx) this._buildTransitions();
      this._abort = new AbortController();
      const on = (node, type, handler, options = {}) => node.addEventListener(type, handler, { ...options, signal: this._abort.signal });
      on(this._scene, 'wheel', e => this._wheel(e), { passive: false });
      on(this._scene, 'pointerdown', e => this._pointerDown(e));
      on(this._scene, 'pointermove', e => this._pointerMove(e));
      on(this._scene, 'pointerup', e => this._pointerUp(e));
      on(this._scene, 'pointercancel', e => this._pointerUp(e, true));
      on(this._scene, 'lostpointercapture', e => { if (this._drag?.id === e.pointerId) this._pointerUp(e, true); });
      on(this._scene, 'pointerenter', e => { if (e.pointerType === 'mouse') { this._hover = true; this._wake(); } });
      on(this._scene, 'pointerleave', () => { this._hover = false; if (!this._drag && this._view === 'browse') this._pan.targetX = this._pan.targetY = 0; this._wake(); });
      on(this.shadowRoot, 'focusin', () => { this._focused = true; this._wake(); });
      on(this.shadowRoot, 'focusout', () => { queueMicrotask(() => { this._focused = !!this.shadowRoot.activeElement; this._wake(); }); });
      on(this.shadowRoot, 'keydown', e => this._key(e));
      on(this.shadowRoot.querySelector('.prev'), 'click', () => this.previous());
      on(this.shadowRoot.querySelector('.next'), 'click', () => this.next());
      on(this._playButton, 'click', () => this._auto ? this.pause() : this.play());
      on(this._lookButton, 'click', () => this.setView(this._view === 'browse' ? 'look' : 'browse'));
      on(this.shadowRoot.querySelector('.zoom-in'), 'click', () => this._setZoom(this._targetZoom + .15));
      on(this.shadowRoot.querySelector('.zoom-out'), 'click', () => this._setZoom(this._targetZoom - .15));
      on(this.shadowRoot.querySelector('.reset'), 'click', () => this._resetCamera());
      on(this.shadowRoot.querySelector('.retry'), 'click', () => {
        const d = this._data[this._index];
        this._cache.delete(d.media.type === 'video' ? d.media.poster : d.media.src);
        this._showMedia(d, true);
      });
      on(this._pagination, 'click', e => { const b = e.target.closest('button'); if (b) this.goTo(Number(b.dataset.index)); });
      on(document, 'visibilitychange', () => { this._syncVideos(); this._wake(); });
      on(this._reducedQuery, 'change', e => {
        this._reduced = e.matches;
        if (e.matches) { this._exitPreview(); this._fx?.cancel(); this.pause(); this._resetCamera(); this._position = Math.round(this._target); this._velocity = 0; }
        if (this._index >= 0) this._showMedia(this._data[this._index], true);
        this._renderRows(); this._wake();
      });
      this._resizeObserver = new ResizeObserver(() => { this._rowHeight = clamp(this.clientWidth * .17, 58, 80); this._renderRows(); });
      this._resizeObserver.observe(this);
      this._intersection = new IntersectionObserver(([entry]) => {
        this._visible = entry.isIntersecting;
        this._syncVideos();
        this._wake();
      }, { threshold: .08 });
      this._intersection.observe(this);
      if (this._data.length) this._initializeData();
      else this._empty.hidden = false;
      this._wake();
    }

    disconnectedCallback() {
      this._abort?.abort();
      this._resizeObserver?.disconnect();
      this._intersection?.disconnect();
      cancelAnimationFrame(this._frame);
      this._frame = 0;
      this._last = 0;
      this._mediaToken++;
      clearTimeout(this._wheelTimer);
      clearTimeout(this._statusTimer);
      clearTimeout(this._announceTimer);
      this.shadowRoot.querySelectorAll('video').forEach(v => v.pause());
      this._fx?.destroy(); this._fx=null;
      ++this._previewTicket;this._previewing=false;
    }

    _build() {
      this.shadowRoot.innerHTML = `<style>${stylesheet}</style>
      <article class="card" aria-label="Destination explorer">
        <div class="empty" hidden>Add destinations to start exploring.</div>
        <div class="content" hidden>
          <div class="scene" data-view="browse">
            <div class="layers" aria-hidden="true"><div class="layer active"></div><div class="layer"></div></div>
            <div class="wash"></div><div class="readability"></div>
            <div class="topbar">
              <span class="eyebrow">${icons.globe} DESTINATIONS</span>
              <div class="top-actions"><div class="counter" aria-hidden="true"><b class="current-number">01</b><span>/</span><b class="total-number">06</b></div>
                <button class="icon-button autoplay-button" type="button" aria-label="Play destination tour" aria-pressed="false" title="Play destination tour">
                  <svg class="play-progress" viewBox="0 0 42 42" aria-hidden="true"><circle cx="21" cy="21" r="18"/></svg><span class="play-icon">${icons.play}</span>
                </button>
              </div>
            </div>
            <span class="motion-badge" aria-hidden="true">Morph blend</span>
            <div class="media-status" role="status"><span class="status-text"></span><button class="retry" type="button" hidden>Retry</button></div>
            <div class="picker" tabindex="0" role="listbox" aria-label="Choose a country" aria-describedby="${this._id}-hint">
              <div class="rows" aria-hidden="true">${Array.from({ length: 9 }, () => '<div class="country-row"></div>').join('')}</div>
              <div class="selection-marker"></div><div class="selection-marker-right"></div>
              <div class="sr options"></div>
            </div>
            <div class="look-title"><div class="look-country"></div><div class="look-subtitle">Drag to explore the photo.<br>Scroll or use + / − to zoom.</div></div>
            <a class="photo-credit" target="_blank" rel="noopener noreferrer">${icons.expand}<span></span></a>
            <div class="scene-footer">
              <span class="gesture-hint">${icons.scroll}<span>SCROLL TO EXPLORE</span></span>
              <div class="camera-controls" aria-label="Photo view controls">
                <button class="zoom-out" type="button" aria-label="Zoom out">${icons.minus}</button><output class="zoom-value" aria-live="off">1.0×</output><button class="zoom-in" type="button" aria-label="Zoom in">${icons.plus}</button><button class="reset" type="button" aria-label="Reset photo view">${icons.reset}</button>
              </div>
              <button class="look-toggle" type="button" aria-pressed="false" title="Explore this photo">${icons.orbit}<span>Look around</span></button>
            </div>
          </div>
          <div class="detail">
            <div class="detail-copy"><div class="detail-head"><div class="location">${icons.pin}<span></span></div><span class="region"></span></div><p class="description"></p></div>
            <div class="detail-bottom"><div class="pagination" role="group" aria-label="Jump to a country"></div><div class="navigation"><button class="nav-button prev" type="button" aria-label="Previous country" title="Previous country">${icons.up}</button><button class="nav-button next" type="button" aria-label="Next country" title="Next country">${icons.down}</button></div></div>
          </div>
        </div>
      </article>
      <span class="sr" id="${this._id}-hint">Scroll, drag vertically, or use the up and down arrow keys to browse countries. The list loops in both directions. Home selects the first country; End selects the last. Look around lets you pan and zoom the selected photo.</span>
      <span class="sr announcement" role="status" aria-live="polite" aria-atomic="true"></span>`;
      const $ = s => this.shadowRoot.querySelector(s);
      this._scene = $('.scene');
      this._picker = $('.picker');
      this._layersContainer = $('.layers');
      this._layers = [...this.shadowRoot.querySelectorAll('.layer')];
      this._rows = [...this.shadowRoot.querySelectorAll('.country-row')];
      this._playButton = $('.autoplay-button');
      this._lookButton = $('.look-toggle');
      this._pagination = $('.pagination');
      this._empty = $('.empty');
      this._detailCopy = $('.detail-copy');
      this._rowHeight = 76;
      this._rendered = true;
    }

    _buildTransitions() {
      if (!window.SceneTransitions) return;
      this._fx = new window.SceneTransitions(this._layersContainer, {
        onStart: detail => {
          this._scene.dataset.renderer='webgl';
          this._emitMotion('transitionstart', detail);
        },
        onFrame: detail => this._onTransitionFrame(detail),
        onEnd: detail => this._onTransitionEnd(detail),
        onFallback: message => { this._scene.dataset.renderer='crossfade';this._layersContainer.classList.remove('fx-mounted');this._emitMotion('transitionfallback',{message}); }
      });
      this._syncTransitionBadge();
    }

    _initializeData() {
      if (!this._rendered) return;
      const $ = s => this.shadowRoot.querySelector(s);
      this._empty.hidden = true;
      $('.content').hidden = false;
      $('.total-number').textContent = String(this._data.length).padStart(2, '0');
      this._pagination.replaceChildren();
      $('.options').replaceChildren();
      this._data.forEach((d, i) => {
        const b = document.createElement('button');
        b.type = 'button'; b.className = 'page-dot'; b.dataset.index = i;
        b.setAttribute('aria-label', `Show ${d.country}`); b.setAttribute('aria-pressed', 'false'); b.title = d.country;
        this._pagination.append(b);
        const option = document.createElement('div');
        option.id = `${this._id}-option-${i}`; option.setAttribute('role', 'option'); option.setAttribute('aria-selected', 'false'); option.textContent = d.country;
        $('.options').append(option);
      });
      this.shadowRoot.querySelector('.gesture-hint span').textContent = matchMedia('(pointer: coarse)').matches ? 'SWIPE TO EXPLORE' : 'SCROLL TO EXPLORE';
      this.shadowRoot.querySelector('.prev').disabled = this._data.length < 2;
      this.shadowRoot.querySelector('.next').disabled = this._data.length < 2;
      this._playButton.disabled = this._data.length < 2;
      this._index = -1;
      this._autoExplicit = false;
      this._auto = this.hasAttribute('autoplay') && !this._reduced && this._data.length > 1;
      this._progress = 0;
      this._syncPlayButton();
      this._select(mod(Math.round(this._position), this._data.length), false);
      this._renderRows();
      this._wake();
    }

    next() { this._step(1); }
    previous() { this._step(-1); }
    _step(step, user = true) {
      if (!this._data.length) return;
      if (user) { this._exitPreview(); this.pause(); }
      this._lastDirection = Math.sign(step) || this._lastDirection || 1;
      this._target = Math.round(this._target) + step;
      this._progress = 0;
      this._userChange = user;
      this._normalize();
      if (this._reduced) { this._position = this._target; this._velocity = 0; this._renderRows(); }
      this._wake();
    }
    goTo(index) {
      if (!Number.isInteger(index) || index < 0 || index >= this._data.length) throw new RangeError('goTo requires a valid destination index.');
      const n = this._data.length, current = mod(Math.round(this._target), n);
      let delta = mod(index - current, n);
      if (delta > n / 2) delta -= n;
      this._step(delta);
    }
    play() {
      this._exitPreview();
      if (this._data.length < 2) return;
      if (this._view === 'look') this.setView('browse');
      this._auto = true; this._progress = 0;
      // A deliberate Play press may run while the play button retains focus.
      this._autoExplicit = true;
      this._syncPlayButton(); this._wake();
    }
    pause() { this._auto = false; this._autoExplicit = false; this._progress = 0; this._syncPlayButton(); }
    _syncPlayButton() {
      if (!this._rendered) return;
      const label = this._auto ? 'Pause destination tour' : 'Play destination tour';
      this._playButton.setAttribute('aria-label', label); this._playButton.setAttribute('aria-pressed', String(this._auto)); this._playButton.title = label;
      this.shadowRoot.querySelector('.play-icon').innerHTML = this._auto ? icons.pause : icons.play;
      this.shadowRoot.querySelector('.play-progress circle').style.strokeDashoffset = '113.1';
    }
    setView(view) {
      if (!['browse', 'look'].includes(view)) throw new TypeError('view must be browse or look.');
      if (this._view === view) return;
      this._exitPreview();
      this.pause();
      this._view = view;
      this._scene.dataset.view = view;
      this._picker.inert = view === 'look';
      this._scene.tabIndex = view === 'look' ? 0 : -1;
      this._scene.setAttribute('aria-label', view === 'look' ? 'Explore destination photo. Arrow keys pan; plus and minus zoom; Escape returns to countries.' : '');
      this._lookButton.setAttribute('aria-pressed', String(view === 'look'));
      this._lookButton.innerHTML = view === 'look' ? `${icons.back}<span>Back to list</span>` : `${icons.orbit}<span>Look around</span>`;
      this._resetCamera();
      this._wake();
      this.dispatchEvent(new CustomEvent('viewchange', { bubbles: true, composed: true, detail: { view } }));
    }
    _setZoom(z) { this._targetZoom = clamp(z, 1, 1.65); this.shadowRoot.querySelector('.zoom-value').textContent = `${this._targetZoom.toFixed(1)}×`; this._wake(); }
    _resetCamera() { this._pan.targetX = this._pan.targetY = 0; this._targetZoom = 1; if (this._reduced) { this._pan.x = this._pan.y = 0; this._zoom = 1; } this.shadowRoot.querySelector('.zoom-value').textContent = '1.0×'; this._wake(); }

    _normalize() {
      const n = this._data.length;
      if (Math.abs(this._target) > n * 1000) {
        const shift = Math.trunc(this._target / n) * n;
        this._target -= shift; this._position -= shift;
        if (this._drag) this._drag.position -= shift;
      }
    }
    _renderRows() {
      if (!this._data.length || !this._rendered) return;
      const n = this._data.length, base = Math.floor(this._position);
      this._rows.forEach((row, i) => {
        const integer = base + i - 4, d = integer - this._position, abs = Math.abs(d);
        const destination = this._data[mod(integer, n)];
        if (row.textContent !== destination.country) row.textContent = destination.country;
        row.classList.toggle('long', destination.country.length > 11);
        row.dataset.step = integer;
        const y = d * this._rowHeight * (1 - Math.min(abs, 3) * .035);
        row.style.transform = `translate3d(0,${y.toFixed(2)}px,${(-abs * 13).toFixed(2)}px) rotateX(${(-d * 12).toFixed(2)}deg) scale(${Math.max(.65, 1 - abs * .044).toFixed(3)})`;
        row.style.opacity = String(clamp(abs <= 1 ? 1 - abs * .5 : abs <= 2 ? .5 - (abs - 1) * .34 : .16 * (3 - abs), 0, 1));
        row.style.filter = abs > 1.3 ? `blur(${Math.min((abs - 1.3) * .45, 1).toFixed(2)}px)` : 'none';
        row.style.visibility = abs >= 3 || (n === 1 && Math.abs(d) > .5) ? 'hidden' : 'visible';
      });
      const motionDirection = Math.sign(this._position - (this._previousPosition ?? this._position));
      if (motionDirection) this._lastDirection = motionDirection;
      this._previousPosition = this._position;
      const index = mod(Math.round(this._position), n);
      if (index !== this._index) this._select(index, this._userChange);
    }
    _select(index, announce) {
      this._index = index;
      const d = this._data[index], $ = s => this.shadowRoot.querySelector(s);
      $('.current-number').textContent = String(index + 1).padStart(2, '0');
      $('.location span').textContent = d.location;
      $('.region').textContent = d.region.toUpperCase();
      $('.description').textContent = d.description;
      $('.look-country').textContent = d.country;
      const credit = $('.photo-credit');
      credit.querySelector('span').textContent = d.media.credit ? `Photo: ${d.media.credit}` : 'View photo source';
      if (d.media.source) credit.href = d.media.source;
      else credit.removeAttribute('href');
      credit.style.visibility = d.media.source ? 'visible' : 'hidden';
      this._picker.setAttribute('aria-activedescendant', `${this._id}-option-${index}`);
      this._pagination.querySelectorAll('button').forEach((b, i) => b.setAttribute('aria-pressed', String(i === index)));
      this.shadowRoot.querySelectorAll('[role="option"]').forEach((b, i) => b.setAttribute('aria-selected', String(i === index)));
      this._scene.style.backgroundColor = d.color;
      if (!this._reduced) {
        this._copyAnimation?.cancel();
        this._copyAnimation = this._detailCopy.animate([{ opacity: .22, transform: 'translateY(4px)' }, { opacity: 1, transform: 'translateY(0)' }], { duration: 320, easing: 'cubic-bezier(.2,.7,.2,1)' });
      }
      if (announce) {
        clearTimeout(this._announceTimer);
        this._announceTimer = setTimeout(() => { $('.announcement').textContent = `${d.country}. ${d.description}`; }, 320);
      }
      this._showMedia(d);
      [-1, 1].forEach(offset => this._preload(this._data[mod(index + offset, this._data.length)].media).catch(() => {}));
      this.dispatchEvent(new CustomEvent('destinationchange', { bubbles: true, composed: true, detail: { index, id: d.id, country: d.country, destination: { ...d, media: { ...d.media } } } }));
    }

    _preload(media) {
      const src = media.type === 'video' ? media.poster : media.src;
      if (!src) return Promise.resolve();
      if (this._cache.has(src)) return this._cache.get(src);
      const promise = new Promise((resolve, reject) => {
        const img = new Image();
        const timeout = setTimeout(() => { img.onload = img.onerror = null; reject(new Error('Image request timed out.')); }, 18000);
        img.onload = async () => { clearTimeout(timeout); try { await img.decode(); } catch (_) {} this._loaded.add(src); this._fx?.warm({src}).catch(()=>{}); resolve(src); };
        img.onerror = () => { clearTimeout(timeout); reject(new Error('Photo unavailable.')); };
        img.src = src;
      });
      this._cache.set(src, promise);
      return promise;
    }
    async _showMedia(d, force = false) {
      if (this._previewing) return;
      const token = ++this._mediaToken;
      clearTimeout(this._statusTimer);
      this._status('', false);
      this._scene.setAttribute('aria-busy', 'true');
      // A cached destination crossfades directly. A preview is only used for an uncached photo.
      const imageSource = d.media.type === 'video' ? d.media.poster : d.media.src;
      if (!this._displayedId || (!this._loaded.has(imageSource) && d.media.preview)) await this._mountMedia(d, true, token);
      this._statusTimer = setTimeout(() => { if (token === this._mediaToken) this._status('Loading photo…', false); }, 1100);
      try {
        await this._preload(d.media);
        if (token !== this._mediaToken || !this.isConnected) return;
        await this._mountMedia(d, false, token);
        if (token !== this._mediaToken) return;
        this._status('', false);
      } catch (_) {
        if (token !== this._mediaToken || !this.isConnected) return;
        if (this._displayedId !== d.id) this._mountMedia(d, true, token);
        this._status(d.media.preview ? 'Preview · photo needs a connection' : 'Photo unavailable · check connection', true);
      } finally {
        if (token === this._mediaToken) { clearTimeout(this._statusTimer); this._scene.setAttribute('aria-busy', 'false'); }
      }
    }
    async _mountMedia(d, preview, token) {
      if (!d) return;
      const serial=++this._mountSerial;
      const source=preview ? d.media.preview : (d.media.type==='video' && this._reduced ? d.media.poster : d.media.src);
      const oldFrame=this._displayedFrame;
      const frame={...d.media,src:source,id:d.id,country:d.country,preview};
      const canTransition=oldFrame?.src && source && oldFrame.id!==d.id && !preview &&
        !oldFrame.preview && !this._reduced && !this._suppressTransition && !this._previewing &&
        d.media.type==='image' && oldFrame.type==='image';
      let animated=false;
      if (canTransition) {
        this._lastTransitionPair={from:this._data.find(v=>v.id===oldFrame.id),to:d};
        if(this._fx?.available) animated=await this._fx.play(oldFrame,frame,this._fxOptions());
      }
      if(token!==this._mediaToken || serial!==this._mountSerial || !this.isConnected)return;
      this._displayedId=d.id;this._displayedFrame=frame;
      const layer=this._layers[1-this._front],old=this._layers[this._front];
      layer.querySelectorAll('video').forEach(v=>v.pause());layer.replaceChildren();
      layer.style.setProperty('--focal',d.media.position);layer.style.background=d.color;
      let node;
      if(source){
        if(!preview && d.media.type==='video' && !this._reduced){
          node=document.createElement('video');node.muted=true;node.loop=true;node.playsInline=true;node.preload='metadata';
          if(d.media.poster)node.poster=d.media.poster;
          node.src=source;
          node.addEventListener('error',()=>{if(token===this._mediaToken)this._status('Video unavailable',true);},{once:true});
        }else{node=document.createElement('img');node.alt=d.media.alt;node.draggable=false;node.src=source;node.decoding='async';}
        layer.append(node);
      }
      this._layersContainer.classList.toggle('fx-mounted',animated || this._suppressTransition || this._previewing);
      layer.classList.add('active');old.classList.remove('active');
      old.querySelectorAll('video').forEach(v=>v.pause());
      this._front=1-this._front;this._syncVideos();
    }
    _status(message, retry) {
      const node = this.shadowRoot.querySelector('.media-status');
      node.classList.toggle('visible', !!message);
      node.querySelector('.status-text').textContent = message;
      node.querySelector('.retry').hidden = !retry;
    }
    _syncVideos() {
      if (!this._rendered) return;
      this._layers.forEach((layer, i) => layer.querySelectorAll('video').forEach(v => {
        if (i === this._front && this._visible && !document.hidden && !this._reduced && this.isConnected) v.play().catch(() => {});
        else v.pause();
      }));
    }

    _wheel(e) {
      if (e.ctrlKey || !this._data.length) return;
      if (Math.abs(e.deltaX) > Math.abs(e.deltaY)) return;
      e.preventDefault();
      this._exitPreview();
      const delta = e.deltaY * (e.deltaMode === 1 ? 16 : e.deltaMode === 2 ? this._scene.clientHeight : 1);
      if (this._view === 'look') { this._setZoom(this._targetZoom - delta * .0012); return; }
      this.pause(); this._userChange = true;
      this._target += clamp(delta, -180, 180) / 115;
      clearTimeout(this._wheelTimer);
      this._wheelTimer = setTimeout(() => { this._target = Math.round(this._target); this._normalize(); this._wake(); }, 110);
      if (this._reduced) { this._position = Math.round(this._target); this._velocity = 0; this._renderRows(); }
      this._wake();
    }
    _pointerDown(e) {
      if ((e.pointerType === 'mouse' && e.button !== 0) || !this._data.length || this._drag) return;
      if (e.target.closest('button,a,input')) return;
      this._exitPreview();
      this.pause();
      clearTimeout(this._wheelTimer);
      this._drag = { id: e.pointerId, x: e.clientX, y: e.clientY, position: this._position, lastY: e.clientY, lastT: e.timeStamp, speed: 0, moved: false, panX: this._pan.targetX, panY: this._pan.targetY, row: e.target.closest('.country-row')?.dataset.step };
      this._velocity = 0; this._target = this._position;
      this._scene.setPointerCapture(e.pointerId);
      this._scene.classList.add('dragging');
      if (e.pointerType === 'mouse' && this._view === 'browse') this._picker.focus({ preventScroll: true });
      this._wake();
    }
    _pointerMove(e) {
      if (this._drag && e.pointerId === this._drag.id) {
        const d = this._drag, dy = e.clientY - d.y, dx = e.clientX - d.x;
        if (Math.hypot(dx, dy) > 5) d.moved = true;
        if (this._view === 'look') {
          this._pan.targetX = clamp(d.panX + dx / 145, -1, 1);
          this._pan.targetY = clamp(d.panY + dy / 145, -1, 1);
        } else {
          const dt = Math.max((e.timeStamp - d.lastT) / 1000, .008);
          d.speed = d.speed * .55 + (-(e.clientY - d.lastY) / this._rowHeight / dt) * .45;
          this._position = d.position - dy / this._rowHeight;
          this._target = this._position; this._userChange = true;
          this._renderRows();
        }
        d.lastY = e.clientY; d.lastT = e.timeStamp;
      } else if (e.pointerType === 'mouse' && this._view === 'browse' && !this._reduced) {
        const r = this._scene.getBoundingClientRect();
        this._pan.targetX = ((e.clientX - r.left) / r.width - .5) * 2;
        this._pan.targetY = ((e.clientY - r.top) / r.height - .5) * 2;
      }
      this._wake();
    }
    _pointerUp(e, cancelled = false) {
      if (!this._drag || e.pointerId !== this._drag.id) return;
      const d = this._drag; this._drag = null;
      this._scene.classList.remove('dragging');
      if (this._scene.hasPointerCapture(e.pointerId)) this._scene.releasePointerCapture(e.pointerId);
      if (this._view === 'browse') {
        const speed = !cancelled && e.timeStamp - d.lastT < 95 ? clamp(d.speed, -10, 10) : 0;
        this._target = !cancelled && !d.moved && d.row !== undefined ? Number(d.row) : Math.round(this._position + clamp(speed * .15, -1.6, 1.6));
        this._velocity = cancelled ? 0 : speed * .3;
        if (this._reduced) { this._position = this._target; this._velocity = 0; this._renderRows(); }
      }
      this._normalize(); this._wake();
    }
    _key(e) {
      if (e.key === 'Escape' && this._view === 'look') { e.preventDefault(); this.setView('browse'); this._picker.focus(); return; }
      if (e.target !== this._picker && e.target !== this._scene) return;
      if (e.ctrlKey || e.metaKey || e.altKey) return;
      if (this._view === 'look') {
        const adjustments = { ArrowLeft: [-.18, 0], ArrowRight: [.18, 0], ArrowUp: [0, -.18], ArrowDown: [0, .18] };
        if (adjustments[e.key]) {
          e.preventDefault(); const [x, y] = adjustments[e.key]; this._pan.targetX = clamp(this._pan.targetX + x, -1, 1); this._pan.targetY = clamp(this._pan.targetY + y, -1, 1); this._wake();
        } else if (['+', '=', '-'].includes(e.key)) { e.preventDefault(); this._setZoom(this._targetZoom + (e.key === '-' ? -.15 : .15)); }
        return;
      }
      const steps = { ArrowDown: 1, ArrowUp: -1, PageDown: 3, PageUp: -3 };
      if (steps[e.key]) { e.preventDefault(); this._step(steps[e.key]); }
      else if (e.key === 'Home') { e.preventDefault(); this.goTo(0); }
      else if (e.key === 'End') { e.preventDefault(); this.goTo(this._data.length - 1); }
      else if (/^[a-z]$/i.test(e.key)) {
        const time = performance.now();
        this._typeAhead = time - (this._typeTime || 0) < 650 ? (this._typeAhead || '') + e.key.toLowerCase() : e.key.toLowerCase();
        this._typeTime = time;
        const i = this._data.findIndex(d => d.country.toLowerCase().startsWith(this._typeAhead));
        if (i >= 0) { e.preventDefault(); this.goTo(i); }
      }
    }
    _wake() {
      if (!this._frame && this.isConnected && this._visible && !document.hidden && this._data.length) this._frame = requestAnimationFrame(this._onFrame);
    }
    _tick(t) {
      this._frame = 0;
      if (!this.isConnected || !this._visible || document.hidden || !this._data.length) { this._last = 0; return; }
      const dt = this._last ? Math.min((t - this._last) / 1000, .032) : 1 / 60;
      this._last = t;
      let moving = false;
      if (!this._previewing && !this._drag && (Math.abs(this._target - this._position) > .0008 || Math.abs(this._velocity) > .003)) {
        if (this._reduced) { this._position = Math.round(this._target); this._velocity = 0; }
        else {
          const acceleration = (this._target - this._position) * 200 - this._velocity * 27;
          this._velocity += acceleration * dt; this._position += this._velocity * dt;
        }
        this._renderRows(); moving = true;
      } else if (!this._previewing && !this._drag && this._position !== this._target && !this._reduced) { this._position = this._target; this._velocity = 0; this._renderRows(); }
      const canAuto = this._auto && this._view === 'browse' && !this._drag && (this._autoExplicit || (!this._hover && !this._focused));
      if (canAuto) {
        const interval = Math.max(2500, Number(this.getAttribute('interval')) || 5500);
        this._progress += dt * 1000 / interval;
        if (this._progress >= 1) { this._progress = 0; this._step(1, false); }
      }
      this.shadowRoot.querySelector('.play-progress circle').style.strokeDashoffset = String(113.1 * (1 - this._progress));
      const pan = this._pan, damp = 1 - Math.exp(-dt * 6);
      pan.x += (pan.targetX - pan.x) * damp; pan.y += (pan.targetY - pan.y) * damp;
      this._zoom += (this._targetZoom - this._zoom) * damp;
      const cameraMoving = Math.abs(pan.x - pan.targetX) + Math.abs(pan.y - pan.targetY) + Math.abs(this._zoom - this._targetZoom) > .0008;
      // A constrained flat-image camera: overscan prevents exposed edges. It does not invent hidden geometry.
      const looking = this._view === 'look';
      this._phase += dt;
      const drifting = this._auto && !this._reduced && !looking;
      const driftX = drifting ? Math.sin(this._phase * .16) * 3 : 0;
      const driftY = drifting ? Math.cos(this._phase * .13) * 3 : 0;
      const amplitude = looking ? 37 : 12;
      const x = (this._reduced && !looking ? 0 : pan.x * amplitude) + driftX;
      const y = (this._reduced && !looking ? 0 : pan.y * amplitude) + driftY;
      const rx = this._reduced ? 0 : -pan.y * (looking ? 2.8 : 1.25);
      const ry = this._reduced ? 0 : pan.x * (looking ? 3.8 : 1.6);
      this._layersContainer.style.transform = `perspective(1000px) translate3d(${x.toFixed(2)}px,${y.toFixed(2)}px,0) rotateX(${rx.toFixed(2)}deg) rotateY(${ry.toFixed(2)}deg) scale(${this._zoom.toFixed(4)})`;
      if (moving || cameraMoving || canAuto || drifting) this._wake();
      else this._last = 0;
    }
  }
  customElements.define('destination-rolodex', DestinationRolodex);
})();
