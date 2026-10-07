(() => {
  'use strict';
  const card=window.destinationCard;
  const $=s=>document.querySelector(s);
  const buttons=[...document.querySelectorAll('[data-effect]')];
  const duration=$('#duration'),strength=$('#strength'),timeline=$('#timeline');
  const replay=$('#replay'),renderer=$('#renderer-status');
  let replayTicket=0,seekTicket=0;
  const paint=input=>input.style.setProperty('--fill',`${100*(Number(input.value)-Number(input.min))/(Number(input.max)-Number(input.min))}%`);
  [duration,strength,timeline].forEach(paint);
  renderer.textContent=matchMedia('(prefers-reduced-motion: reduce)').matches?'Reduced motion enabled':card.transitionRenderer==='webgl'?'WebGL renderer ready':'Original crossfade · WebGL unavailable';
  if(card.transitionRenderer!=='webgl')$('#renderer-dot').classList.add('fallback');
  const setRoute=(from,to)=>{$('#route-from').textContent=from.country;$('#route-to').textContent=to.country;};
  const setProgress=p=>{timeline.value=String(Math.round(p*1000));$('#timeline-value').textContent=`${Math.round(p*100)}%`;paint(timeline);};
  async function run(){
    const ticket=++replayTicket;replay.setAttribute('aria-busy','true');
    try {await card.replayTransition();}
    catch(error){renderer.textContent=error.message;$('#renderer-dot').classList.add('fallback');}
    finally{if(ticket===replayTicket)replay.setAttribute('aria-busy','false');}
  }
  function choose(mode,play=true){
    card.transition=mode;
    buttons.forEach(b=>{const checked=b.dataset.effect===mode;b.setAttribute('aria-checked',String(checked));b.tabIndex=checked?0:-1;});
    if(play)run();
  }
  buttons.forEach((button,index)=>{
    button.addEventListener('click',()=>choose(button.dataset.effect));
    button.addEventListener('keydown',e=>{
      const directions={ArrowDown:1,ArrowRight:1,ArrowUp:-1,ArrowLeft:-1};
      if(e.key in directions){e.preventDefault();const next=buttons[(index+directions[e.key]+buttons.length)%buttons.length];next.focus();choose(next.dataset.effect);}
      else if(e.key==='Home'||e.key==='End'){e.preventDefault();const next=buttons[e.key==='Home'?0:buttons.length-1];next.focus();choose(next.dataset.effect);}
    });
  });
  replay.addEventListener('click',run);
  duration.addEventListener('input',()=>{card.transitionDuration=Number(duration.value);$('#duration-value').textContent=`${(Number(duration.value)/1000).toFixed(2)} s`;paint(duration);});
  strength.addEventListener('input',()=>{card.transitionStrength=Number(strength.value)/100;$('#strength-value').textContent=`${strength.value}%`;paint(strength);});
  duration.addEventListener('change',run);strength.addEventListener('change',run);
  timeline.addEventListener('input',async()=>{
    const ticket=++seekTicket;const value=Number(timeline.value)/1000;
    $('#timeline-value').textContent=`${Math.round(value*100)}%`;paint(timeline);
    try{await card.seekTransition(value);}catch(error){if(ticket===seekTicket)renderer.textContent=error.message;}
  });
  card.addEventListener('transitionmodechange',e=>{
    const mode=e.detail.mode;
    buttons.forEach(b=>{const active=b.dataset.effect===mode;b.setAttribute('aria-checked',String(active));b.tabIndex=active?0:-1;});
  });
  card.addEventListener('transitionstart',e=>{
    setRoute(e.detail.from,e.detail.to);
    renderer.textContent='WebGL · live';$('#renderer-dot').classList.remove('fallback');
  });
  card.addEventListener('transitionprogress',e=>setProgress(e.detail.progress));
  card.addEventListener('transitionend',()=>{renderer.textContent='WebGL renderer ready';});
  card.addEventListener('transitionfallback',e=>{renderer.textContent=e.detail.message;$('#renderer-dot').classList.add('fallback');});
  document.addEventListener('keydown',e=>{
    if(e.ctrlKey||e.metaKey||e.altKey)return;
    if(e.composedPath().some(node=>node instanceof HTMLElement && (node.matches('input,select,textarea,[role="listbox"],[role="radio"]')||node.isContentEditable)))return;
    if(e.key.toLowerCase()==='r'){e.preventDefault();run();}
    else if(/^[1-2]$/.test(e.key)){e.preventDefault();choose(buttons[Number(e.key)-1].dataset.effect);}
  });
  // Readable handles for console experiments, without a framework or build step.
  window.transitionLab={card,replay:run,choose};
})();
