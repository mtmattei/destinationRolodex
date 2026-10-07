(() => {
  'use strict';
  const card = document.getElementById('destinations');
  card.destinations = window.DEMO_DESTINATIONS;
  const dialog = document.getElementById('credits');
  document.querySelector('.country-count').lastChild.textContent = `${card.destinations.length} destinations`;
  document.getElementById('open-credits').addEventListener('click', () => { card.pause(); dialog.showModal(); });
  document.getElementById('close-credits').addEventListener('click', () => dialog.close());
  dialog.addEventListener('click', e => {
    const r = dialog.getBoundingClientRect();
    if (e.target === dialog && (e.clientX < r.left || e.clientX > r.right || e.clientY < r.top || e.clientY > r.bottom)) dialog.close();
  });
  const list = document.getElementById('photo-list');
  for (const d of card.destinations) {
    const li = document.createElement('li'), country = document.createElement('span'), link = document.createElement('a');
    country.textContent = d.country; link.textContent = `${d.media.credit} ↗`; link.href = d.media.source;
    link.target = '_blank'; link.rel = 'noopener noreferrer'; li.append(country, link); list.append(li);
  }
  // Expose a normal reference for the demo console; component state stays scoped to the element.
  window.destinationCard = card;
})();
