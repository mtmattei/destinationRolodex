attribute vec2 aPosition;
    varying vec2 vUv;
    void main(){ vUv=aPosition*.5+.5; gl_Position=vec4(aPosition,0.,1.); }
