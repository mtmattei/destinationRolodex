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
