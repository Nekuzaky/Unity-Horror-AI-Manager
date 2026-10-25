function scene(w, h) {
  var s = w / 460;
  var S = function (v) { return (v * s).toFixed(1); };
  var cone = function (x, y, a, spread, r, op) {
    var p = ['M' + S(x) + ' ' + S(y)];
    for (var i = 0; i <= 24; i++) {
      var t = (a - spread / 2 + spread * i / 24) * Math.PI / 180;
      p.push('L' + S(x + Math.cos(t) * r) + ' ' + S(y + Math.sin(t) * r));
    }
    return '<path d="' + p.join(' ') + 'Z" fill="url(#cone)" opacity="' + op + '"/>';
  };
  var wall = function (x, y, ww, hh) {
    return '<rect x="' + S(x) + '" y="' + S(y) + '" width="' + S(ww) + '" height="' + S(hh) + '" rx="' + S(2) + '" fill="#3a3640"/>';
  };
  var ring = function (r, op, width) {
    return '<circle cx="' + S(286) + '" cy="' + S(84) + '" r="' + S(r) + '" fill="none" stroke="#ff9926" stroke-opacity="' + op + '" stroke-width="' + S(width) + '"/>';
  };
  var grid = '';
  for (var x = 23; x < 460; x += 23) grid += '<line x1="' + S(x) + '" y1="0" x2="' + S(x) + '" y2="' + S(250) + '" stroke="rgba(255,255,255,.03)"/>';
  for (var y = 25; y < 250; y += 25) grid += '<line x1="0" y1="' + S(y) + '" x2="' + S(460) + '" y2="' + S(y) + '" stroke="rgba(255,255,255,.03)"/>';
  return '<svg width="' + w + '" height="' + h + '" viewBox="0 0 ' + w + ' ' + h + '">' +
    '<defs>' +
    '<linearGradient id="bg" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#1b191e"/><stop offset="1" stop-color="#141316"/></linearGradient>' +
    '<radialGradient id="lamp"><stop offset="0" stop-color="#ffd27a" stop-opacity=".55"/><stop offset=".55" stop-color="#ffb84d" stop-opacity=".16"/><stop offset="1" stop-color="#ffb84d" stop-opacity="0"/></radialGradient>' +
    '<radialGradient id="cone" gradientUnits="userSpaceOnUse" cx="' + S(108) + '" cy="' + S(124) + '" r="' + S(320) + '"><stop offset="0" stop-color="#ff4848" stop-opacity=".55"/><stop offset="1" stop-color="#ff4848" stop-opacity="0"/></radialGradient>' +
    '<filter id="glow" x="-1" y="-1" width="3" height="3"><feGaussianBlur stdDeviation="' + S(5) + '" result="b"/><feMerge><feMergeNode in="b"/><feMergeNode in="SourceGraphic"/></feMerge></filter>' +
    '</defs>' +
    '<rect x="0" y="0" width="' + w + '" height="' + h + '" rx="' + S(13) + '" fill="url(#bg)"/>' + grid +
    '<circle cx="' + S(372) + '" cy="' + S(122) + '" r="' + S(100) + '" fill="url(#lamp)"/>' +
    cone(108, 124, -24, 84, 300, .22) + cone(108, 124, 24, 84, 300, .22) + cone(108, 124, 6, 84, 320, 1) +
    wall(0, 52, 196, 10) + wall(244, 52, 216, 10) + wall(0, 188, 186, 10) + wall(236, 188, 224, 10) + wall(186, 198, 8, 52) + wall(228, 198, 8, 52) +
    ring(10, .75, 1.6) + ring(20, .45, 1.4) + ring(31, .22, 1.2) +
    '<circle cx="' + S(286) + '" cy="' + S(84) + '" r="' + S(3.5) + '" fill="#ff9926"/>' +
    '<circle cx="' + S(372) + '" cy="' + S(118) + '" r="' + S(5) + '" fill="#ffe2a0" filter="url(#glow)"/>' +
    '<line x1="' + S(122) + '" y1="' + S(126) + '" x2="' + S(340) + '" y2="' + S(139) + '" stroke="#ff4848" stroke-opacity=".7" stroke-width="' + S(1.4) + '" stroke-dasharray="' + S(4) + ' ' + S(4) + '"/>' +
    '<circle cx="' + S(352) + '" cy="' + S(140) + '" r="' + S(10) + '" fill="#5c9fd6" stroke="#fff" stroke-width="' + S(2.4) + '" filter="url(#glow)"/>' +
    '<circle cx="' + S(211) + '" cy="' + S(226) + '" r="' + S(8) + '" fill="#5c9fd6" fill-opacity=".3" stroke="#5c9fd6" stroke-opacity=".7" stroke-width="' + S(1.4) + '" stroke-dasharray="' + S(3) + ' ' + S(2) + '"/>' +
    '<circle cx="' + S(108) + '" cy="' + S(124) + '" r="' + S(22) + '" fill="none" stroke="#ff4848" stroke-opacity=".35" stroke-width="' + S(1.4) + '"/>' +
    '<circle cx="' + S(108) + '" cy="' + S(124) + '" r="' + S(12) + '" fill="#d84650" stroke="#fff" stroke-width="' + S(2.6) + '" filter="url(#glow)"/>' +
    '<rect x="' + S(86) + '" y="' + S(94) + '" width="' + S(44) + '" height="' + S(5) + '" rx="' + S(2.5) + '" fill="#ff4848"/>' +
    '<rect x="0.5" y="0.5" width="' + (w - 1) + '" height="' + (h - 1) + '" rx="' + S(13) + '" fill="none" stroke="#322f36"/>' +
    '</svg>';
}

function timeline(container, count) {
  var phases = [['Patrol', 0, .30, 'patrol', .1], ['Alert', .30, .38, 'alert', .42], ['Investigate', .38, .55, 'investigate', .56],
    ['Search', .55, .72, 'search', .48], ['Chase', .72, .9, 'chase', .97], ['Lost Track', .9, 1.0001, 'lost', .62]];
  var bars = container.querySelector('.bars');
  var seed = 7;
  var rnd = function () { seed = (seed * 16807) % 2147483647; return seed / 2147483647; };
  for (var i = 0; i < count; i++) {
    var t = (i + .5) / count;
    var p = phases.filter(function (q) { return t >= q[1] && t < q[2]; })[0];
    var v = Math.max(.05, Math.min(1, p[4] + (rnd() - .5) * .12 + (p[0] === 'Patrol' ? .06 * Math.sin(i * .7) : 0)));
    var bar = document.createElement('i');
    bar.style.height = (v * 100).toFixed(0) + '%';
    bar.style.background = 'var(--' + p[3] + ')';
    bars.appendChild(bar);
  }
  phases.forEach(function (p) {
    var label = document.createElement('span');
    label.className = 'phase';
    label.style.left = (11 + (container.clientWidth - 22) * p[1]) + 'px';
    label.textContent = p[0];
    container.appendChild(label);
  });
}
