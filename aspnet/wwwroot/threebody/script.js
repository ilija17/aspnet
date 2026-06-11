const API_BASE = "/api/threebody";
const LOGIN_URL = "/Account/Login?returnUrl=/threebody/index.html";
const PROFILE_URL = "/Account/Profile";

const CHIP_AMOUNTS = [25, 50, 100, 200];
const PLANET_NAMES = ["A", "B", "C"];
const FRAME_MS = 20; // server simulation frames are 50 fps
const WORLD_VIEW_RADIUS = 880; // world units mapped inside the canvas
const TRAIL_LENGTH = 110;

const FALLBACK_PLANETS = [
  { name: "A", color: "#ff6b6b", mass: 400, radius: 32 },
  { name: "B", color: "#4ecdc4", mass: 250, radius: 24 },
  { name: "C", color: "#ffe66d", mass: 150, radius: 28 },
];

let serverState = null;
let lastRenderedVersion = -1;
let selectedChip = 25; // client-side: chip used for the next bet
let phase = "betting"; // betting | playing | over (purely client-side)
let busy = false; // a POST is in flight
let showResultBanner = false; // only show WIN/LOSE right after a round
let audioContext;

// Playback state (the whole round is played back locally).
let round = null; // round payload from POST start
let heldState = null; // full state from POST start, revealed after playback
let playbackStart = 0;
let playbackPos = 0;
let nextElimIndex = 0;
const deadPlanets = new Set();

const ui = {
  table: document.getElementById("table"),
  gate: document.getElementById("gate"),
  gateMessage: document.getElementById("gate-message"),
  gateLink: document.getElementById("gate-link"),
  playerName: document.getElementById("player-name"),
  balance: document.getElementById("balance"),
  wins: document.getElementById("wins"),
  losses: document.getElementById("losses"),
  betDisplay: document.getElementById("bet-display"),
  chips: Array.from(document.querySelectorAll(".chip")),
  planetCards: Array.from(document.querySelectorAll(".planet-card")),
  status: document.getElementById("status"),
  roundResult: document.getElementById("round-result"),
  startBtn: document.getElementById("start-btn"),
  skipBtn: document.getElementById("skip-btn"),
  againBtn: document.getElementById("again-btn"),
  vfxLayer: document.getElementById("vfx-layer"),
  simOverlay: document.getElementById("sim-overlay"),
};

// ============================================
// SERVER API LAYER (authenticated, singleplayer)
// ============================================

// Thrown when a response was already handled (gate shown / status set).
class HandledApiError extends Error {}

function showGate(message, linkHref, linkText) {
  ui.gateMessage.textContent = message;
  ui.gateLink.href = linkHref;
  ui.gateLink.textContent = linkText;
  ui.gate.classList.remove("hidden");
  ui.table.classList.add("hidden");
}

function hideGate() {
  ui.gate.classList.add("hidden");
  ui.table.classList.remove("hidden");
}

async function readErrorMessage(res, fallback) {
  try {
    const body = await res.json();
    if (body && typeof body.error === "string" && body.error.length > 0) {
      return body.error;
    }
  } catch {
    /* non-JSON body */
  }
  return fallback;
}

async function api(path, options) {
  let res;
  try {
    res = await fetch(`${API_BASE}/${path}`, options);
  } catch (error) {
    setStatus("Connection lost. Check your network and try again.", true);
    throw new HandledApiError(String(error));
  }

  if (res.status === 401) {
    showGate("Za igru se moraš prijaviti.", LOGIN_URL, "Prijavi se");
    throw new HandledApiError("401");
  }

  if (res.status === 409) {
    const message = await readErrorMessage(
      res,
      "Tvoj račun nema zapis igrača. Spremi svoj profil (Moj profil) pa pokušaj ponovno."
    );
    showGate(message, PROFILE_URL, "Moj profil");
    throw new HandledApiError("409");
  }

  if (!res.ok) {
    const message = await readErrorMessage(res, `Request failed (${res.status}). Try again.`);
    setStatus(message, true);
    throw new HandledApiError(String(res.status));
  }

  hideGate();
  return res.json();
}

function apiGetState() {
  return api("state", { headers: { Accept: "application/json" } });
}

function apiPost(action, body = {}) {
  return api(action, {
    method: "POST",
    headers: { "Content-Type": "application/json", Accept: "application/json" },
    body: JSON.stringify(body),
  });
}

function applyServerState(state) {
  serverState = state;
  render();
}

async function refreshState() {
  if (busy || phase === "playing") {
    return;
  }
  try {
    applyServerState(await apiGetState());
  } catch (error) {
    if (!(error instanceof HandledApiError)) {
      console.error("Three body error:", error);
      setStatus("Something went wrong. Try again.", true);
    }
  }
}

// ============================================
// PLAYER ACTIONS
// ============================================

function getPlanet(name) {
  const fromServer = (serverState?.planets || []).find((p) => p.name === name);
  return fromServer || FALLBACK_PLANETS.find((p) => p.name === name);
}

function currentBetAmount() {
  const fromServer = Number(serverState?.selectedBet);
  return CHIP_AMOUNTS.includes(fromServer) ? fromServer : selectedChip;
}

async function placeBet(planet, amount) {
  if (busy || phase !== "betting" || !serverState) {
    return;
  }
  busy = true;
  playSound("bet");
  triggerActionFx("bet");
  try {
    applyServerState(await apiPost("bet", { amount, planet }));
  } catch (error) {
    if (!(error instanceof HandledApiError)) {
      console.error("Three body error:", error);
      setStatus("Something went wrong. Try again.", true);
    }
  } finally {
    busy = false;
    updateControls();
  }
}

async function start() {
  if (busy || phase !== "betting" || !serverState || !serverState.canStart) {
    return;
  }
  busy = true;
  updateControls();
  playSound("start");
  triggerActionFx("start");
  setStatus("Gravity engaged — simulating…");

  let result;
  try {
    result = await apiPost("start", {});
  } catch (error) {
    busy = false;
    updateControls();
    render(true);
    if (!(error instanceof HandledApiError)) {
      console.error("Three body error:", error);
      setStatus("Something went wrong. Try again.", true);
    }
    return;
  }

  busy = false;
  heldState = result;
  round = result.round;

  if (!round || !Array.isArray(round.frames) || round.frames.length === 0) {
    // Degenerate round: reveal immediately.
    phase = "playing";
    finishPlayback();
    return;
  }

  beginPlayback();
}

function beginPlayback() {
  clearTrails();
  particles.length = 0;
  deadPlanets.clear();
  hideOverlay();
  nextElimIndex = 0;
  playbackPos = 0;
  playbackStart = performance.now();
  phase = "playing";
  ui.roundResult.textContent = "";
  ui.roundResult.className = "round-result hidden";
  setStatus("Simulation running — last planet standing wins…");
  updateControls();
}

function skipPlayback() {
  if (phase !== "playing" || !round) {
    return;
  }
  playSound("button");
  const last = round.frames.length - 1;
  playbackPos = last;
  playbackStart = performance.now() - last * FRAME_MS;
  processEliminations();
  finishPlayback();
}

function finishPlayback() {
  if (phase !== "playing") {
    return;
  }
  phase = "over";
  const finishedRound = round;
  showResultBanner = true;

  if (heldState) {
    serverState = heldState;
    heldState = null;
    render(true);
  } else {
    render(true);
  }

  const winner = finishedRound ? finishedRound.winnerPlanet : null;
  showOverlay(
    winner ? `Planet ${winner} survived!` : "All planets destroyed!",
    finishedRound && finishedRound.result === "win" ? "win" : "loss"
  );

  if (finishedRound && finishedRound.result === "win") {
    playSound("win");
    triggerActionFx("win");
  } else {
    playSound("lose");
  }
}

function playAgain() {
  if (phase !== "over") {
    return;
  }
  playSound("button");
  round = null;
  deadPlanets.clear();
  clearTrails();
  particles.length = 0;
  hideOverlay();
  showResultBanner = false;
  phase = "betting";
  ui.roundResult.textContent = "";
  ui.roundResult.className = "round-result hidden";
  render(true);
  setStatus("Pick a planet, place your bet, then start the simulation.");
  refreshState();
}

// ============================================
// RENDERING (version-gated, same as roulette)
// ============================================

function formatMoney(value) {
  const amount = Number(value ?? 0);
  return `$${amount.toFixed(2)}`;
}

function setStatus(text, danger = false) {
  ui.status.textContent = text;
  ui.status.classList.toggle("danger", danger);
  ui.status.classList.remove("pulse");
  animateStatusPulse(ui.status);
  void ui.status.offsetWidth;
  ui.status.classList.add("pulse");
}

function renderChips() {
  ui.chips.forEach((chip) => {
    const chipValue = Number(chip.dataset.bet);
    chip.classList.toggle("active", selectedChip === chipValue);
    chip.disabled = busy || phase !== "betting";
  });
}

function selectedPlanetName() {
  if (phase === "betting") {
    return serverState?.betOnPlanet || null;
  }
  return round?.betPlanet || null;
}

function renderPlanetCards() {
  const selected = selectedPlanetName();
  ui.planetCards.forEach((card) => {
    const name = card.dataset.planet;
    const planet = getPlanet(name);
    if (planet) {
      card.style.setProperty("--planet-color", planet.color);
      const stats = card.querySelector(".planet-stats");
      if (stats) {
        stats.textContent = `Mass ${planet.mass} · Radius ${planet.radius}`;
      }
    }
    const dead = deadPlanets.has(name);
    card.classList.toggle("selected", selected === name);
    card.classList.toggle("dead", dead);
    const pill = card.querySelector(".planet-state");
    if (pill) {
      pill.textContent = dead ? "Destroyed" : "Active";
      pill.className = `planet-state ${dead ? "destroyed" : "active"}`;
    }
    card.disabled = busy || phase !== "betting";
  });
}

function renderBetDisplay() {
  if (serverState?.betOnPlanet) {
    ui.betDisplay.textContent = `Your Bet: $${serverState.selectedBet} on Planet ${serverState.betOnPlanet}`;
  } else if (phase !== "betting" && round) {
    ui.betDisplay.textContent = `Your Bet: $${round.betAmount} on Planet ${round.betPlanet}`;
  } else {
    ui.betDisplay.textContent = `Your Bet: $${selectedChip} — select a planet`;
  }
}

function updateControls() {
  renderChips();
  renderPlanetCards();
  ui.startBtn.disabled = busy || phase !== "betting" || !serverState || !serverState.canStart;
  ui.skipBtn.classList.toggle("hidden", phase !== "playing");
  ui.skipBtn.disabled = phase !== "playing";
  ui.againBtn.classList.toggle("hidden", phase !== "over");
  ui.againBtn.disabled = phase !== "over";
}

function render(force = false) {
  if (!serverState) {
    return;
  }
  // Only re-render when the server state actually changed, so focus
  // re-fetches never replay animations.
  if (!force && serverState.version === lastRenderedVersion) {
    return;
  }
  lastRenderedVersion = serverState.version;

  // Header: real player identity and real casino balance (server-authoritative).
  ui.playerName.textContent = serverState.playerName || "—";
  ui.balance.textContent = `Balance: ${formatMoney(serverState.balance)}`;
  ui.wins.textContent = `Wins: ${serverState.wins ?? 0}`;
  ui.losses.textContent = `Losses: ${serverState.losses ?? 0}`;
  animateBalanceChange(ui.balance);

  // Keep the local chip in sync with the server's selected bet.
  if (CHIP_AMOUNTS.includes(Number(serverState.selectedBet))) {
    selectedChip = Number(serverState.selectedBet);
  }

  renderBetDisplay();
  updateControls();

  // WIN/LOSE banner (only right after a finished round).
  if (showResultBanner) {
    showResultBanner = false;
    if (serverState.lastResult === "win") {
      ui.roundResult.textContent = `WIN ${formatMoney(serverState.lastPayout)}`;
      ui.roundResult.className = "round-result win";
    } else {
      ui.roundResult.textContent = "YOU LOSE";
      ui.roundResult.className = "round-result loss";
    }
    animateRoundResult(ui.roundResult);
  } else if (phase === "betting") {
    ui.roundResult.textContent = "";
    ui.roundResult.className = "round-result hidden";
  }

  if (serverState.status && phase !== "playing") {
    setStatus(serverState.status);
  }
}

// ============================================
// CANVAS SIMULATION VIEW
// ============================================

const canvas = document.getElementById("sim-canvas");
const ctx = canvas.getContext("2d");
let cw = 0;
let ch = 0;
let stars = [];
const trails = new Map(); // planet name -> [{x, y}, ...] canvas coords
const particles = [];

function buildStars() {
  const count = Math.min(220, Math.round((cw * ch) / 6000));
  stars = [];
  for (let i = 0; i < count; i += 1) {
    stars.push({
      x: Math.random(),
      y: Math.random(),
      r: 0.5 + Math.random() * 1.4,
      phase: Math.random() * Math.PI * 2,
      speed: 0.6 + Math.random() * 2.2,
    });
  }
}

function clearTrails() {
  trails.clear();
}

function resizeCanvas() {
  const rect = canvas.getBoundingClientRect();
  if (rect.width === 0 || rect.height === 0) {
    return;
  }
  const dpr = window.devicePixelRatio || 1;
  cw = rect.width;
  ch = rect.height;
  canvas.width = Math.round(cw * dpr);
  canvas.height = Math.round(ch * dpr);
  ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
  buildStars();
  clearTrails();
}

window.addEventListener("resize", resizeCanvas);

function viewScale() {
  return (Math.min(cw, ch) / 2 - 16) / WORLD_VIEW_RADIUS;
}

function worldToCanvas(x, y) {
  const s = viewScale();
  return [cw / 2 + x * s, ch / 2 + y * s];
}

function planetDrawRadius(planet) {
  return Math.max(6, planet.radius * viewScale() * 1.5);
}

function positionsAt(pos) {
  const frames = round.frames;
  const last = frames.length - 1;
  const clamped = Math.max(0, Math.min(pos, last));
  const i0 = Math.floor(clamped);
  const i1 = Math.min(i0 + 1, last);
  const t = clamped - i0;
  const f0 = frames[i0];
  const f1 = frames[i1];
  const out = {};
  PLANET_NAMES.forEach((name, idx) => {
    const x = f0[idx * 2] + (f1[idx * 2] - f0[idx * 2]) * t;
    const y = f0[idx * 2 + 1] + (f1[idx * 2 + 1] - f0[idx * 2 + 1]) * t;
    out[name] = worldToCanvas(x, y);
  });
  return out;
}

function idlePositions(now) {
  const t = now / 1000;
  const base = Math.min(cw, ch) / 2;
  const orbits = {
    A: { r: 0.46, s: 0.22, p: 0.4 },
    B: { r: 0.66, s: -0.15, p: 2.3 },
    C: { r: 0.3, s: 0.34, p: 4.4 },
  };
  const out = {};
  PLANET_NAMES.forEach((name) => {
    const o = orbits[name];
    out[name] = [
      cw / 2 + Math.cos(t * o.s + o.p) * base * o.r,
      ch / 2 + Math.sin(t * o.s + o.p) * base * o.r * 0.78,
    ];
  });
  return out;
}

function paintBackground(now) {
  ctx.fillStyle = "#04050d";
  ctx.fillRect(0, 0, cw, ch);

  let nebula = ctx.createRadialGradient(cw * 0.26, ch * 0.3, 0, cw * 0.26, ch * 0.3, Math.max(cw, ch) * 0.5);
  nebula.addColorStop(0, "rgba(83, 28, 127, 0.34)");
  nebula.addColorStop(1, "rgba(83, 28, 127, 0)");
  ctx.fillStyle = nebula;
  ctx.fillRect(0, 0, cw, ch);

  nebula = ctx.createRadialGradient(cw * 0.76, ch * 0.72, 0, cw * 0.76, ch * 0.72, Math.max(cw, ch) * 0.45);
  nebula.addColorStop(0, "rgba(34, 80, 24, 0.24)");
  nebula.addColorStop(1, "rgba(34, 80, 24, 0)");
  ctx.fillStyle = nebula;
  ctx.fillRect(0, 0, cw, ch);

  const t = now / 1000;
  stars.forEach((star) => {
    const a = 0.18 + 0.55 * (0.5 + 0.5 * Math.sin(t * star.speed + star.phase));
    ctx.globalAlpha = a;
    ctx.fillStyle = "#d6deff";
    ctx.beginPath();
    ctx.arc(star.x * cw, star.y * ch, star.r, 0, Math.PI * 2);
    ctx.fill();
  });
  ctx.globalAlpha = 1;
}

function pushTrail(name, x, y) {
  let trail = trails.get(name);
  if (!trail) {
    trail = [];
    trails.set(name, trail);
  }
  trail.push({ x, y });
  if (trail.length > TRAIL_LENGTH) {
    trail.shift();
  }
}

function drawTrail(name, color) {
  const trail = trails.get(name);
  if (!trail || trail.length < 2) {
    return;
  }
  for (let i = 1; i < trail.length; i += 1) {
    const a = i / trail.length;
    ctx.globalAlpha = a * 0.45;
    ctx.strokeStyle = color;
    ctx.lineWidth = 1 + a * 2.4;
    ctx.beginPath();
    ctx.moveTo(trail[i - 1].x, trail[i - 1].y);
    ctx.lineTo(trail[i].x, trail[i].y);
    ctx.stroke();
  }
  ctx.globalAlpha = 1;
}

function drawPlanetBody(name, x, y) {
  const planet = getPlanet(name);
  const r = planetDrawRadius(planet);
  const grad = ctx.createRadialGradient(x - r * 0.35, y - r * 0.4, r * 0.12, x, y, r);
  grad.addColorStop(0, "rgba(255, 255, 255, 0.95)");
  grad.addColorStop(0.32, planet.color);
  grad.addColorStop(1, "#0c0c18");
  ctx.shadowColor = planet.color;
  ctx.shadowBlur = 20;
  ctx.fillStyle = grad;
  ctx.beginPath();
  ctx.arc(x, y, r, 0, Math.PI * 2);
  ctx.fill();
  ctx.shadowBlur = 0;

  ctx.font = "700 11px 'Trebuchet MS', 'Segoe UI', sans-serif";
  ctx.textAlign = "center";
  ctx.globalAlpha = 0.9;
  ctx.fillStyle = planet.color;
  ctx.fillText(name, x, y - r - 8);
  ctx.globalAlpha = 1;
}

function drawPlanets(positions, now, { bob = false } = {}) {
  PLANET_NAMES.forEach((name, idx) => {
    if (deadPlanets.has(name)) {
      return;
    }
    let [x, y] = positions[name];
    if (bob) {
      y += Math.sin(now / 700 + idx * 2.1) * 3;
    }
    pushTrail(name, x, y);
  });
  PLANET_NAMES.forEach((name) => {
    if (!deadPlanets.has(name)) {
      drawTrail(name, getPlanet(name).color);
    }
  });
  PLANET_NAMES.forEach((name, idx) => {
    if (deadPlanets.has(name)) {
      return;
    }
    let [x, y] = positions[name];
    if (bob) {
      y += Math.sin(now / 700 + idx * 2.1) * 3;
    }
    drawPlanetBody(name, x, y);
  });
}

function drawProgress(t) {
  ctx.fillStyle = "rgba(192, 255, 90, 0.16)";
  ctx.fillRect(0, ch - 5, cw, 5);
  ctx.shadowColor = "#c0ff5a";
  ctx.shadowBlur = 10;
  ctx.fillStyle = "rgba(192, 255, 90, 0.95)";
  ctx.fillRect(0, ch - 5, cw * Math.max(0, Math.min(t, 1)), 5);
  ctx.shadowBlur = 0;
}

function drawCaption(now) {
  const a = 0.5 + 0.3 * Math.sin(now / 600);
  ctx.globalAlpha = a;
  ctx.font = "700 13px 'Trebuchet MS', 'Segoe UI', sans-serif";
  ctx.textAlign = "center";
  ctx.fillStyle = "#9ba3c6";
  ctx.fillText("SELECT A PLANET AND PLACE YOUR BET", cw / 2, ch - 16);
  ctx.globalAlpha = 1;
}

// Canvas explosion particles (separate from the DOM vfx layer).
function spawnExplosion(x, y, color) {
  for (let i = 0; i < 70; i += 1) {
    const angle = Math.random() * Math.PI * 2;
    const speed = 0.8 + Math.random() * 6.5;
    particles.push({
      type: "dot",
      x,
      y,
      vx: Math.cos(angle) * speed,
      vy: Math.sin(angle) * speed,
      size: 1 + Math.random() * 3.2,
      life: 28 + Math.random() * 34,
      maxLife: 62,
      color,
    });
  }
  particles.push({ type: "ring", x, y, r: 3, vr: 5.5, life: 26, maxLife: 26, color });
}

function updateParticles() {
  for (let i = particles.length - 1; i >= 0; i -= 1) {
    const p = particles[i];
    p.life -= 1;
    if (p.life <= 0) {
      particles.splice(i, 1);
      continue;
    }
    const a = p.life / p.maxLife;
    if (p.type === "ring") {
      p.r += p.vr;
      ctx.globalAlpha = a;
      ctx.strokeStyle = p.color;
      ctx.lineWidth = 2.5;
      ctx.shadowColor = p.color;
      ctx.shadowBlur = 14;
      ctx.beginPath();
      ctx.arc(p.x, p.y, p.r, 0, Math.PI * 2);
      ctx.stroke();
      ctx.shadowBlur = 0;
    } else {
      p.x += p.vx;
      p.y += p.vy;
      p.vx *= 0.96;
      p.vy *= 0.96;
      ctx.globalAlpha = Math.min(1, a * 1.4);
      ctx.fillStyle = p.color;
      ctx.shadowColor = p.color;
      ctx.shadowBlur = 8;
      ctx.beginPath();
      ctx.arc(p.x, p.y, p.size, 0, Math.PI * 2);
      ctx.fill();
      ctx.shadowBlur = 0;
    }
  }
  ctx.globalAlpha = 1;
}

function processEliminations() {
  if (!round) {
    return;
  }
  const elims = round.eliminations || [];
  while (nextElimIndex < elims.length && elims[nextElimIndex].frame <= playbackPos) {
    const elim = elims[nextElimIndex];
    nextElimIndex += 1;
    const pos = positionsAt(elim.frame)[elim.planet];
    const planet = getPlanet(elim.planet);
    deadPlanets.add(elim.planet);
    spawnExplosion(pos[0], pos[1], planet.color);
    markPlanetDestroyed(elim.planet);
    playSound("boom");
    triggerActionFx("boom");
  }
}

function markPlanetDestroyed(name) {
  const card = ui.planetCards.find((c) => c.dataset.planet === name);
  if (!card) {
    return;
  }
  card.classList.add("dead");
  const pill = card.querySelector(".planet-state");
  if (pill) {
    pill.textContent = "Destroyed";
    pill.className = "planet-state destroyed";
  }
}

function showOverlay(text, kind) {
  ui.simOverlay.textContent = text;
  ui.simOverlay.className = `sim-overlay ${kind}`;
}

function hideOverlay() {
  ui.simOverlay.textContent = "";
  ui.simOverlay.className = "sim-overlay hidden";
}

function drawScene(now) {
  // The table can start hidden (auth gate), so pick up late layout changes.
  if (canvas.clientWidth && Math.abs(canvas.clientWidth - cw) > 1) {
    resizeCanvas();
  }
  if (cw === 0 || ch === 0) {
    return;
  }

  paintBackground(now);

  if (phase === "playing" && round) {
    const last = round.frames.length - 1;
    playbackPos = (now - playbackStart) / FRAME_MS;
    processEliminations();
    drawPlanets(positionsAt(playbackPos), now);
    drawProgress(playbackPos / last);
    updateParticles();
    if (playbackPos >= last) {
      finishPlayback();
    }
    return;
  }

  if (phase === "over" && round) {
    drawPlanets(positionsAt(round.frames.length - 1), now, { bob: true });
    updateParticles();
    return;
  }

  drawPlanets(idlePositions(now), now);
  drawCaption(now);
  updateParticles();
}

function frameLoop(now) {
  requestAnimationFrame(frameLoop);
  drawScene(now);
}

resizeCanvas();
requestAnimationFrame(frameLoop);

// ============================================
// VFX (same flair as blackjack / roulette)
// ============================================

function triggerActionFx(kind) {
  if (!ui.vfxLayer) {
    return;
  }

  const config = {
    bet: { count: 60, color: "#b4ff66", spread: 260, flash: "flash-bet", waves: 1, words: ["BET", "LOCKED IN", "CHIP DOWN"] },
    start: { count: 140, color: "#b06bff", spread: 460, flash: "flash-start", waves: 2, words: ["IGNITION", "CHAOS", "GRAVITY WELL"] },
    boom: { count: 110, color: "#ff952a", spread: 380, flash: "flash-boom", waves: 2, words: ["DESTROYED", "COLLAPSE", "OBLITERATED"] },
    win: { count: 200, color: "#70ff3a", spread: 540, flash: "flash-bet", waves: 3, words: ["WINNER", "SURVIVOR", "PAYOUT"] },
  }[kind];

  if (!config) {
    return;
  }

  const rect = ui.vfxLayer.getBoundingClientRect();
  const centerX = rect.width * (0.35 + Math.random() * 0.3);
  const centerY = rect.height * (0.35 + Math.random() * 0.3);

  const spawnBurst = (wave = 1) => {
    const waveScale = 1 + wave * 0.2;
    for (let i = 0; i < config.count; i += 1) {
      const piece = document.createElement("span");
      const angle = Math.random() * Math.PI * 2;
      const speed = (95 + Math.random() * config.spread) * waveScale;
      const size = 4 + Math.random() * 14;
      piece.className = "fx-particle";
      piece.style.setProperty("--fx-x", `${centerX}px`);
      piece.style.setProperty("--fx-y", `${centerY}px`);
      piece.style.setProperty("--fx-dx", `${Math.cos(angle) * speed}`);
      piece.style.setProperty("--fx-dy", `${Math.sin(angle) * speed}`);
      piece.style.setProperty("--fx-color", config.color);
      piece.style.width = `${size}px`;
      piece.style.height = `${size}px`;
      piece.style.opacity = `${0.5 + Math.random() * 0.5}`;
      ui.vfxLayer.appendChild(piece);
      setTimeout(() => piece.remove(), 900);
    }

    const ring = document.createElement("span");
    ring.className = "fx-ring";
    ring.style.setProperty("--fx-x", `${centerX}px`);
    ring.style.setProperty("--fx-y", `${centerY}px`);
    ring.style.setProperty("--fx-color", config.color);
    ui.vfxLayer.appendChild(ring);
    setTimeout(() => ring.remove(), 780);

    const shardCount = 8 + wave * 4;
    for (let i = 0; i < shardCount; i += 1) {
      const shard = document.createElement("span");
      const driftX = (Math.random() - 0.5) * 340;
      const driftY = (Math.random() - 0.5) * 200;
      shard.className = "fx-shard";
      shard.style.setProperty("--fx-x", `${centerX}px`);
      shard.style.setProperty("--fx-y", `${centerY}px`);
      shard.style.setProperty("--fx-dx", `${driftX}`);
      shard.style.setProperty("--fx-dy", `${driftY}`);
      shard.style.setProperty("--fx-color", config.color);
      shard.style.transform = `translate(-50%, -50%) rotate(${Math.random() * 360}deg)`;
      ui.vfxLayer.appendChild(shard);
      setTimeout(() => shard.remove(), 860);
    }
  };

  for (let wave = 0; wave < config.waves; wave += 1) {
    setTimeout(() => spawnBurst(wave), wave * 90);
  }

  const word = document.createElement("span");
  word.className = "fx-banner";
  word.textContent = config.words[Math.floor(Math.random() * config.words.length)];
  word.style.setProperty("--fx-color", config.color);
  word.style.left = `${Math.max(16, centerX - 120)}px`;
  word.style.top = `${Math.max(16, centerY - 28)}px`;
  ui.vfxLayer.appendChild(word);
  setTimeout(() => word.remove(), 720);

  document.body.classList.remove("action-flash", "flash-bet", "flash-start", "flash-boom");
  document.body.classList.remove("action-shake", "action-glitch", "action-overdrive", "action-strobe", "action-tilt");
  void document.body.offsetWidth;
  const heavy = kind === "start" || kind === "win" || kind === "boom";
  const classes = ["action-flash", config.flash, "action-glitch", "action-overdrive"];
  if (heavy) {
    classes.push("action-shake", "action-strobe", "action-tilt");
  }
  document.body.classList.add(...classes);
  setTimeout(() => {
    document.body.classList.remove(...classes);
  }, heavy ? 980 : 680);
}

// ============================================
// AUDIO
// ============================================

function ensureAudioContext() {
  if (!window.AudioContext && !window.webkitAudioContext) {
    return null;
  }
  if (!audioContext) {
    const AudioCtor = window.AudioContext || window.webkitAudioContext;
    audioContext = new AudioCtor();
  }
  if (audioContext.state === "suspended") {
    audioContext.resume();
  }
  return audioContext;
}

function playTone(freq, duration, type = "sine", volume = 0.05, delay = 0) {
  const ctx = ensureAudioContext();
  if (!ctx) {
    return;
  }

  const osc = ctx.createOscillator();
  const gain = ctx.createGain();
  const startAt = ctx.currentTime + delay;
  const endAt = startAt + duration;
  osc.type = type;
  osc.frequency.setValueAtTime(freq, startAt);
  gain.gain.setValueAtTime(0.0001, startAt);
  gain.gain.exponentialRampToValueAtTime(volume, startAt + 0.01);
  gain.gain.exponentialRampToValueAtTime(0.0001, endAt);
  osc.connect(gain);
  gain.connect(ctx.destination);
  osc.start(startAt);
  osc.stop(endAt + 0.01);
}

function playSound(kind) {
  if (kind === "bet") {
    playTone(430, 0.05, "triangle", 0.04);
    playTone(560, 0.06, "triangle", 0.03, 0.05);
  } else if (kind === "start") {
    playTone(120, 0.08, "sawtooth", 0.07);
    playTone(240, 0.08, "square", 0.06, 0.06);
    playTone(360, 0.09, "triangle", 0.05, 0.12);
    playTone(520, 0.1, "sawtooth", 0.05, 0.18);
  } else if (kind === "boom") {
    playTone(72, 0.26, "sawtooth", 0.09);
    playTone(46, 0.36, "triangle", 0.08, 0.04);
    playTone(210, 0.07, "square", 0.05, 0.02);
  } else if (kind === "win") {
    playTone(392, 0.12, "triangle", 0.06);
    playTone(494, 0.12, "triangle", 0.06, 0.1);
    playTone(587, 0.14, "triangle", 0.06, 0.2);
    playTone(784, 0.22, "triangle", 0.06, 0.3);
  } else if (kind === "lose") {
    playTone(220, 0.14, "sine", 0.05);
    playTone(160, 0.2, "triangle", 0.04, 0.12);
  } else if (kind === "button") {
    playTone(430, 0.05, "triangle", 0.03);
  }
}

// ============================================
// EVENT WIRING
// ============================================

ui.chips.forEach((chip) => {
  chip.addEventListener("click", () => {
    if (chip.disabled) {
      return;
    }
    selectedChip = Number(chip.dataset.bet);
    playSound("button");
    animateChipSelect(chip);
    renderChips();
    if (serverState?.betOnPlanet) {
      // Planet already picked: re-bet with the new amount.
      placeBet(serverState.betOnPlanet, selectedChip);
    } else {
      renderBetDisplay();
    }
  });
});

ui.planetCards.forEach((card) => {
  card.addEventListener("click", () => {
    if (card.disabled) {
      return;
    }
    animatePlanetSelect(card);
    placeBet(card.dataset.planet, currentBetAmount());
  });
});

ui.startBtn.addEventListener("click", () => {
  animateButtonPress(ui.startBtn);
  animateButtonGlow(ui.startBtn);
  start();
});

ui.skipBtn.addEventListener("click", () => {
  animateButtonPress(ui.skipBtn);
  skipPlayback();
});

ui.againBtn.addEventListener("click", () => {
  animateButtonPress(ui.againBtn);
  playAgain();
});

if (!CHIP_AMOUNTS.includes(selectedChip)) {
  selectedChip = CHIP_AMOUNTS[0];
}
renderChips();

// Singleplayer: fetch once on load and after each action; re-sync on focus
// in case the balance changed elsewhere (no polling needed).
refreshState();
window.addEventListener("focus", refreshState);

// ============================================
// ANIME.JS ANIMATION FUNCTIONS
// ============================================

function animateButtonPress(buttonElement) {
  if (!window.anime) return;
  anime({
    targets: buttonElement,
    scale: [1, 0.95, 1],
    duration: 200,
    easing: "easeOutQuad",
  });
}

function animatePlanetSelect(cardElement) {
  if (!window.anime) return;
  anime({
    targets: cardElement,
    scale: [1, 0.93, 1.05, 1],
    duration: 280,
    easing: "easeOutBack",
  });
}

function animateStatusPulse(statusElement) {
  if (!window.anime) return;
  anime({
    targets: statusElement,
    opacity: [1, 0.5, 1],
    scale: [1, 1.05, 1],
    duration: 600,
    easing: "easeInOutQuad",
  });
}

function animateRoundResult(resultElement) {
  if (!window.anime) return;
  anime.set(resultElement, {
    scale: 0.5,
    opacity: 0,
    rotate: -180,
  });
  anime({
    targets: resultElement,
    scale: [0.5, 1.15, 1],
    opacity: 1,
    rotate: 0,
    duration: 700,
    easing: "easeOutElastic(1, .7)",
  });
}

function animateChipSelect(chipElement) {
  if (!window.anime) return;
  anime({
    targets: chipElement,
    scale: [1, 1.2, 1],
    duration: 300,
    easing: "easeOutBack",
  });
}

function animateBalanceChange(balanceElement) {
  if (!window.anime) return;
  anime({
    targets: balanceElement,
    scale: [1, 1.15, 1],
    duration: 400,
    easing: "easeOutQuad",
  });
}

function animateButtonGlow(buttonElement) {
  if (!window.anime) return;
  anime({
    targets: buttonElement,
    boxShadow: [
      "0 0 16px rgba(112, 255, 58, 0.24)",
      "0 0 32px rgba(112, 255, 58, 0.6)",
      "0 0 16px rgba(112, 255, 58, 0.24)",
    ],
    duration: 800,
    easing: "easeInOutQuad",
  });
}
