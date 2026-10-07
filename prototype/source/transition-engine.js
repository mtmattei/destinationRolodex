/**
 * Destination photo transitions. WebGL 1, no third-party runtime.
 * The morph is a procedural displacement morph, not optical flow or reconstructed geometry.
 * All media remain ordinary DOM layers underneath the progressive-enhancement canvas.
 */
(() => {
  'use strict';
  const clamp = (n,a,b) => Math.max(a,Math.min(b,n));
  const modes = { morph:0, liquid:1 };
  const vertex = `
    attribute vec2 aPosition;
    varying vec2 vUv;
    void main(){ vUv=aPosition*.5+.5; gl_Position=vec4(aPosition,0.,1.); }
  `;
  const fragment = `
    precision highp float;
    varying vec2 vUv;
    uniform sampler2D uFrom;
    uniform sampler2D uTo;
    uniform float uProgress, uStrength, uDirection, uAspect, uAspectA, uAspectB;
    uniform vec2 uFocalA, uFocalB;
    uniform int uMode;
    const float PI=3.141592653589793;
    float hash(vec2 p){return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453);}
    float noise(vec2 p){
      vec2 i=floor(p),f=fract(p);f=f*f*(3.-2.*f);
      return mix(mix(hash(i),hash(i+vec2(1,0)),f.x),mix(hash(i+vec2(0,1)),hash(i+vec2(1,1)),f.x),f.y);
    }
    float fbm(vec2 p){return .58*noise(p)+.28*noise(p*2.03+5.7)+.14*noise(p*4.11+11.2);}
    vec2 crop(vec2 uv,float aspect,vec2 focal){
      vec2 scale=aspect>uAspect?vec2(uAspect/aspect,1.):vec2(1.,aspect/uAspect);
      return clamp(uv*scale+(1.-scale)*vec2(focal.x,1.-focal.y),vec2(.001),vec2(.999));
    }
    vec4 A(vec2 uv){return texture2D(uFrom,crop(uv,uAspectA,uFocalA));}
    vec4 B(vec2 uv){return texture2D(uTo,crop(uv,uAspectB,uFocalB));}
    float ease(float t){return t*t*(3.-2.*t);}
    float luma(vec4 c){return dot(c.rgb,vec3(.2126,.7152,.0722));}
    vec4 morph(vec2 uv,float p){
      float bell=sin(PI*p);
      vec2 q=uv*vec2(2.7*uAspect,2.7);
      // Both images are advected through one low-frequency field in opposing directions.
      float n=fbm(q+vec2(p*.32,-p*.38*uDirection));
      float m=fbm(q+vec2(7.1,3.8)+vec2(-p*.26,p*.4));
      float a=luma(A(uv)),b=luma(B(uv));
      vec2 field=vec2(n-.5,m-.5)+vec2((a-b)*.19,(a+b-1.)*.10);
      float power=(.18+uStrength*.48)*bell;
      vec2 drift=vec2(sin(uv.y*PI*2.+p*PI)*.028,uDirection*.045)*bell;
      vec2 va=uv+field*power*p+drift*p;
      vec2 vb=uv-field*power*(1.-p)-drift*(1.-p);
      float mask=clamp(p+(n-.5)*bell*(.55+.3*uStrength),0.,1.);
      return mix(A(va),B(vb),smoothstep(0.,1.,mask));
    }
    vec4 liquid(vec2 uv,float p){
      vec2 origin=vec2(.48,uDirection>0.?.38:.62);
      vec2 v=(uv-origin)*vec2(uAspect,1.);
      float r=length(v);
      vec2 normal=v/max(r,.0001)/vec2(uAspect,1.);
      float radius=p*1.35-.13;
      float wobble=(noise(uv*4.8)-.5)*.035*sin(PI*p);
      float sd=r-radius+wobble;
      float ring=exp(-pow(sd/(.095+.045*uStrength),2.));
      float wave=sin(sd*25.-p*5.5)*ring;
      float displacement=wave*(.025+.085*uStrength)*sin(PI*p);
      vec2 va=uv+normal*displacement;
      vec2 vb=origin+(uv-origin)*(1.-ring*.07*uStrength)-normal*displacement*.78;
      float mask=1.-smoothstep(-.065,.065,sd);
      vec4 ca=A(va),cb=B(vb);
      // Very small spectral separation at the moving liquid edge, not a full-screen blur.
      float fringe=ring*.0035*uStrength*sin(PI*p);
      ca.r=A(va+normal*fringe).r;ca.b=A(va-normal*fringe).b;
      cb.r=B(vb-normal*fringe).r;cb.b=B(vb+normal*fringe).b;
      vec4 c=mix(ca,cb,mask);
      c.rgb+=ring*.035*uStrength*sin(PI*p);
      return c;
    }
    void main(){
      float p=clamp(uProgress,0.,1.);
      if(p<=.00001){gl_FragColor=A(vUv);return;}
      if(p>=.99999){gl_FragColor=B(vUv);return;}
      vec4 color;
      if(uMode==0)color=morph(vUv,p);
      else if(uMode==1)color=liquid(vUv,p);
      else color=mix(A(vUv),B(vUv),ease(p));
      gl_FragColor=vec4(color.rgb,1.);
    }
  `;

  class SceneTransitions {
    constructor(container, callbacks={}) {
      this.container=container;this.callbacks=callbacks;this.cache=new Map();
      this.frame=0;this.generation=0;this.active=false;this.progress=0;this.mode='morph';
      this.canvas=document.createElement('canvas');this.canvas.className='transition-canvas';
      this.canvas.setAttribute('aria-hidden','true');container.append(this.canvas);
      try {
        const gl=this.canvas.getContext('webgl',{alpha:false,antialias:false,depth:false,stencil:false,preserveDrawingBuffer:true,powerPreference:'low-power'});
        if(!gl)throw new Error('WebGL is unavailable.');
        this.gl=gl;
        const compile=(type,source)=>{
          const s=gl.createShader(type);gl.shaderSource(s,source);gl.compileShader(s);
          if(!gl.getShaderParameter(s,gl.COMPILE_STATUS)){const log=gl.getShaderInfoLog(s);gl.deleteShader(s);throw new Error(log);}
          return s;
        };
        this.program=gl.createProgram();
        const vs=compile(gl.VERTEX_SHADER,vertex),fs=compile(gl.FRAGMENT_SHADER,fragment);
        gl.attachShader(this.program,vs);gl.attachShader(this.program,fs);gl.linkProgram(this.program);
        gl.deleteShader(vs);gl.deleteShader(fs);
        if(!gl.getProgramParameter(this.program,gl.LINK_STATUS))throw new Error(gl.getProgramInfoLog(this.program));
        gl.useProgram(this.program);
        this.buffer=gl.createBuffer();gl.bindBuffer(gl.ARRAY_BUFFER,this.buffer);
        gl.bufferData(gl.ARRAY_BUFFER,new Float32Array([-1,-1,1,-1,-1,1,-1,1,1,-1,1,1]),gl.STATIC_DRAW);
        const pos=gl.getAttribLocation(this.program,'aPosition');
        gl.enableVertexAttribArray(pos);gl.vertexAttribPointer(pos,2,gl.FLOAT,false,0,0);
        this.uniforms={};
        ['uFrom','uTo','uProgress','uStrength','uDirection','uAspect','uAspectA','uAspectB','uFocalA','uFocalB','uMode'].forEach(n=>this.uniforms[n]=gl.getUniformLocation(this.program,n));
        gl.uniform1i(this.uniforms.uFrom,0);gl.uniform1i(this.uniforms.uTo,1);
        this.available=true;
      } catch(error){ this.available=false;this.error=error.message; }
      this.canvas.addEventListener('webglcontextlost',e=>{
        e.preventDefault();this.cancel();this.available=false;this.cache.clear();
        this.callbacks.onFallback?.('Graphics context lost. Using the original crossfade.');
      });
      this._visibilityHandler=()=>{this.skipTime=true;};
      document.addEventListener('visibilitychange',this._visibilityHandler);
      this.resizeObserver=new ResizeObserver(()=>{if(this.active)this.draw(this.progress);});
      this.resizeObserver.observe(container);
    }

    warm(media){
      if(!this.available)return Promise.reject(new Error('WebGL unavailable.'));
      const src=media.src;
      if(this.cache.has(src))return this.cache.get(src);
      const promise=new Promise((resolve,reject)=>{
        const image=new Image();image.crossOrigin='anonymous';image.decoding='async';
        const timer=setTimeout(()=>{image.onload=image.onerror=null;reject(new Error('Photo texture timed out.'));},12000);
        image.onload=()=>{
          clearTimeout(timer);
          try {
            if(!this.available)throw new Error('WebGL unavailable.');
            const gl=this.gl,texture=gl.createTexture();
            gl.bindTexture(gl.TEXTURE_2D,texture);
            gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_S,gl.CLAMP_TO_EDGE);
            gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_T,gl.CLAMP_TO_EDGE);
            gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MIN_FILTER,gl.LINEAR);
            gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MAG_FILTER,gl.LINEAR);
            gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL,true);
            // Limit uploaded textures without changing the DOM photograph.
            const limit=Math.min(1600,gl.getParameter(gl.MAX_TEXTURE_SIZE));
            let source=image;
            if(Math.max(image.naturalWidth,image.naturalHeight)>limit){
              const down=document.createElement('canvas'),s=limit/Math.max(image.naturalWidth,image.naturalHeight);
              down.width=Math.round(image.naturalWidth*s);down.height=Math.round(image.naturalHeight*s);
              down.getContext('2d').drawImage(image,0,0,down.width,down.height);source=down;
            }
            gl.texImage2D(gl.TEXTURE_2D,0,gl.RGBA,gl.RGBA,gl.UNSIGNED_BYTE,source);
            if(gl.getError()!==gl.NO_ERROR){gl.deleteTexture(texture);throw new Error('Photo texture upload failed.');}
            resolve({texture,aspect:image.naturalWidth/image.naturalHeight});
          } catch(error){reject(error);}
        };
        image.onerror=()=>{clearTimeout(timer);reject(new Error('This photo could not be loaded with cross-origin permission.'));};
        image.src=src;
      });
      this.cache.set(src,promise);
      promise.catch(()=>{if(this.cache.get(src)===promise)this.cache.delete(src);});
      return promise;
    }

    async configure(from,to,options){
      const ticket=++this.generation;
      cancelAnimationFrame(this.frame);this.frame=0;
      try {
        const [a,b]=await Promise.all([this.warm(from),this.warm(to)]);
        if(ticket!==this.generation)return false;
        this.a=a;this.b=b;this.from=from;this.to=to;this.options=options;
        this.mode=options.mode in modes?options.mode:'morph';
        this.active=true;this.canvas.style.opacity='1';
        this.callbacks.onStart?.({from,to,mode:this.mode,duration:options.duration,preview:!!options.preview});
        return true;
      }catch(error){
        if(ticket===this.generation){this.cancel();this.callbacks.onFallback?.(error.message);}
        return false;
      }
    }

    async play(from,to,options={}){
      if(!await this.configure(from,to,options))return false;
      const ticket=this.generation;
      let last=0,elapsed=0;
      this.draw(0);
      const tick=now=>{
        if(ticket!==this.generation)return;
        // Resume at the same phase after a hidden tab, rather than jumping on return.
        const dt=last && !this.skipTime ? now-last : 0;last=now;this.skipTime=false;
        if(!document.hidden)elapsed+=dt;
        const p=clamp(elapsed/Math.max(1,options.duration||1200),0,1);
        this.draw(p);
        if(p<1)this.frame=requestAnimationFrame(tick);
        else {
          this.frame=0;
          Promise.resolve(this.callbacks.onEnd?.({from,to,mode:this.mode,preview:!!options.preview})).then(()=>{
            if(ticket===this.generation){this.active=false;this.canvas.style.opacity='0';}
          });
        }
      };
      this.frame=requestAnimationFrame(tick);
      return true;
    }

    async seek(from,to,p,options={}){
      cancelAnimationFrame(this.frame);this.frame=0;
      const same=this.active&&this.from?.src===from.src&&this.to?.src===to.src&&this.options?.preview;
      if(!same && !await this.configure(from,to,{...options,preview:true}))return false;
      this.options={...options,preview:true};this.mode=options.mode in modes?options.mode:'morph';
      this.draw(clamp(p,0,1));return true;
    }

    draw(p){
      if(!this.available||!this.a||!this.b)return;
      this.progress=p;
      const gl=this.gl,u=this.uniforms;
      const width=this.container.clientWidth,height=this.container.clientHeight;
      if(!width||!height)return;
      const dpr=Math.min(window.devicePixelRatio||1,1.6,Math.sqrt(1500000/(width*height)));
      const w=Math.round(width*dpr),h=Math.round(height*dpr);
      if(this.canvas.width!==w||this.canvas.height!==h){this.canvas.width=w;this.canvas.height=h;}
      gl.viewport(0,0,w,h);gl.useProgram(this.program);
      gl.activeTexture(gl.TEXTURE0);gl.bindTexture(gl.TEXTURE_2D,this.a.texture);
      gl.activeTexture(gl.TEXTURE1);gl.bindTexture(gl.TEXTURE_2D,this.b.texture);
      gl.uniform1f(u.uProgress,p);gl.uniform1f(u.uStrength,this.options.strength??.6);
      gl.uniform1f(u.uDirection,this.options.direction||1);gl.uniform1f(u.uAspect,width/height);
      gl.uniform1f(u.uAspectA,this.a.aspect);gl.uniform1f(u.uAspectB,this.b.aspect);
      const focal=value=>{const v=(value||'50% 50%').match(/[\d.]+/g)||[50,50];return v.map(n=>clamp(Number(n)/100,0,1));};
      const a=focal(this.from.position),b=focal(this.to.position);
      gl.uniform2f(u.uFocalA,a[0],a[1]??a[0]);gl.uniform2f(u.uFocalB,b[0],b[1]??b[0]);
      gl.uniform1i(u.uMode,modes[this.mode]);gl.drawArrays(gl.TRIANGLES,0,6);
      this.callbacks.onFrame?.({progress:p,mode:this.mode,from:this.from,to:this.to,preview:!!this.options.preview});
    }
    cancel(){++this.generation;cancelAnimationFrame(this.frame);this.frame=0;this.active=false;this.canvas.style.opacity='0';}
    destroy(){this.cancel();document.removeEventListener('visibilitychange',this._visibilityHandler);this.resizeObserver.disconnect();const gl=this.gl;
      if(gl){for(const p of this.cache.values())p.then(v=>gl.deleteTexture(v.texture)).catch(()=>{});gl.deleteBuffer(this.buffer);gl.deleteProgram(this.program);}
      this.cache.clear();this.canvas.remove();}
  }
  window.SceneTransitions=SceneTransitions;
})();
