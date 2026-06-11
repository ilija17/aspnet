const API_BASE = "/api/threebody";
const LOGIN_URL = "/Account/Login?returnUrl=/threebody/index.html";
const PROFILE_URL = "/Account/Profile";

let serverState = null;
let lastRenderedVersion = -1;
let audioContext;
let animFrameId = null;
let trails = [[], [], []]; // trail points for A, B, C
let particles = [];
let stars = [];
let canvasWidth = 0;
let canvasHeight = 0;

const ui = {
  table: document.getElementById("table"),
  gate: document.getElementById("gate"),
  gateMessage: document.getElementById("gate-message"),
  gateLink: document.getElementById("gate-link"),
  playerName: document.getElementById("player-name"),
  balance: document.getElementById("balance"),
  status: document.getElementById("status"),
  roundResult: document.getElementById("round-result"),
  betDisplay: document.getElementById("bet-display"),
  chips: Array.from(document.querySelectorAll(".chip")),
  planetCards: Array.from(document.querySelectorAll(".planet-card")),
  startBtn: document.getElementById("start-btn"),
  skipBtn: document.getElementById("skip-btn"),
  resetBtn: document.getElementById("reset-btn"),
  wins: document.getElementById("wins"),
  losses: document.getElementById("losses"),
  vfxLayer: document.getElementById("vfx-layer"),
  canvas: document.getElementById("space-canvas"),
  canvasStatus: document.getElementById("canvas-status"),
  planetStatusA: document.getElementById("planet-a-status"),
  planetStatusB: document.getElementById("planet-b-status"),
  planetStatusC: document.getElementById("planet-c-status"),
};

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

async function sendAction(action, body, sound) {
  if (sound) playSound(sound);
  try {
    applyServerState(await apiPost(action, body));
  } catch (error) {
    if (!(error instanceof HandledApiError)) {
      console.error("Three body error:", error);
      setStatus("Something went wrong. Try again.", true);
    }
  }
}

async function refreshState() {
  try {
    applyServerState(await apiGetState());
  } catch (error) {
    if (!(error instanceof HandledApiError)) {
      console.error("Three body error:", error);
      setStatus("Something went wrong. Try again.", true);
    }
  }
}

// ─── RENDERING ─────────────────────────────────────────────────────────────────

function formatMoney(value) {
  return `$${Number(value ?? 0).toFixed(2)}`;
}

function setStatus(text, danger = false) {
  ui.status.textContent = text;
  ui.status.classList.toggle("danger", danger);
  ui.status.classList.remove("pulse");
  void ui.status.offsetWidth;
  ui.status.classList.add("pulse");
}

function render() {
  if (!serverState) return;
  if (serverState.version === lastRenderedVersion) return;
  lastRenderedVersion = serverState.version;

  ui.playerName.textContent = serverState.playerName || "—";
  ui.balance.textContent = `Balance: ${formatMoney(serverState.balance)}`;
  animateBalanceChange(ui.balance);
  ui.wins.textContent = `Wins: ${serverState.wins ?? 0}`;
  ui.losses.textContent = `Losses: ${serverState.losses ?? 0}`;
  ui.betDisplay.textContent = `Your Bet: ${formatMoney(serverState.selectedBet)}`;

  // Planet cards
  ui.planetCards.forEach((card) => {
    const planet = card.dataset.planet;
    const statusEl = card.querySelector(".planet-status");
    if (serverState.alivePlanets && serverState.alivePlanets.includes(planet)) {
      statusEl.textContent = "Active";
      statusEl.className = "planet-status alive";
      card.classList.remove("dead");
    } else {
      statusEl.textContent = "Destroyed";
      statusEl.className = "planet-status dead";
      card.classList.add("dead");
    }
    card.classList.toggle("selected", serverState.betOnPlanet === planet);
  });

  // Chips
  ui.chips.forEach((chip) => {
    const chipValue = Number(chip.dataset.bet);
    chip.classList.toggle("active", serverState.selectedBet === chipValue);
    chip.disabled = !serverState.canBet;
  });

  // Buttons
  ui.startBtn.disabled = !serverState.canStart;
  ui.skipBtn.disabled = serverState.phase !== "simulating";
  ui.resetBtn.disabled = !serverState.canReset;

  // Canvas overlay
  if (serverState.phase === "round-over") {
    ui.canvasStatus.classList.remove("hidden");
    ui.canvasStatus.textContent = serverState.winnerPlanet
      ? `Planet ${serverState.winnerPlanet} survived!`
      : "All planets destroyed!";
  } else if (serverState.phase === "simulating") {
    ui.canvasStatus.classList.add("hidden");
  }

  // Round result
  if (serverState.phase === "round-over" && serverState.lastRoundResult) {
    const label = serverState.lastRoundResult === "win" ? "YOU WIN" : "YOU LOSE";
    ui.roundResult.textContent = label;
    ui.roundResult.className = `round-result ${serverState.lastRoundResult}`;
    animateRoundResult(ui.roundResult);
  } else {
    ui.roundResult.textContent = "";
    ui.roundResult.className = "round-result hidden";
  }

  // Planet elimination FX — only fire once per new elimination
  if (serverState.eliminatedOrder && serverState.eliminatedOrder.length > lastElimCount) {
    const newlyEliminated = serverState.eliminatedOrder.slice(lastElimCount);
    lastElimCount = serverState.eliminatedOrder.length;
    newlyEliminated.forEach((name) => {
      if (name && serverState.planets) {
        const idxMap = { A: 0, B: 1, C: 2 };
        const idx = idxMap[name];
        if (idx !== undefined && serverState.planets[idx]) {
          const p = serverState.planets[idx];
          const { sx, sy } = worldToScreen(p.x, p.y);
          spawnEliminationParticles(sx, sy, p.color);
          playSound("elimination");
          triggerDomEliminationFx(p.color);
        }
      }
    });
  }

  if (serverState.status) {
    setStatus(serverState.status);
  }

  // Update trails with current planet positions when simulating
  if (serverState.phase === "simulating" && serverState.planets) {
    const idxMap = { A: 0, B: 1, C: 2 };
    serverState.planets.forEach((p) => {
      const idx = idxMap[p.name];
      if (idx !== undefined) {
        const { sx, sy } = worldToScreen(p.x, p.y);
        trails[idx].push({ x: sx, y: sy });
        if (trails[idx].length > 120) trails[idx].shift();
      }
    });
  }

  // Start or stop canvas animation
  if (serverState.phase === "simulating" && serverState.totalFrames > 0) {
    startSimulation();
    lastElimCount = (serverState.eliminatedOrder || []).length;
  } else if (serverState.phase === "round-over") {
    stopSimulation();
    drawFrame(serverState);
  } else {
    stopSimulation();
    drawIdleScene(serverState);
  }
}

// ─── CANVAS ────────────────────────────────────────────────────────────────────

function initCanvas() {
  const dpr = window.devicePixelRatio || 1;
  const rect = ui.canvas.getBoundingClientRect();
  canvasWidth = rect.width;
  canvasHeight = rect.height;
  ui.canvas.width = canvasWidth * dpr;
  ui.canvas.height = canvasHeight * dpr;
  const ctx = ui.canvas.getContext("2d");
  ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
  generateStars();
}

function generateStars() {
  stars = [];
  for (let i = 0; i < 200; i++) {
    stars.push({
      x: Math.random() * canvasWidth,
      y: Math.random() * canvasHeight,
      r: Math.random() * 1.8 + 0.3,
      twinkle: Math.random() * Math.PI * 2,
      speed: Math.random() * 0.02 + 0.005,
    });
  }
}

function drawStars(ctx) {
  for (const star of stars) {
    star.twinkle += star.speed;
    const alpha = 0.4 + Math.sin(star.twinkle) * 0.35;
    ctx.fillStyle = `rgba(255, 255, 255, ${alpha})`;
    ctx.beginPath();
    ctx.arc(star.x, star.y, star.r, 0, Math.PI * 2);
    ctx.fill();
  }
}

function worldToScreen(x, y) {
  const margin = 60;
  const wx = (x + EjectionRadius) / (EjectionRadius * 2);
  const wy = (y + EjectionRadius) / (EjectionRadius * 2);
  return {
    sx: margin + wx * (canvasWidth - margin * 2),
    sy: margin + wy * (canvasHeight - margin * 2),
  };
}

const EjectionRadius = 800;

function drawIdleScene(state) {
  const ctx = ui.canvas.getContext("2d");
  ctx.clearRect(0, 0, canvasWidth, canvasHeight);
  drawStars(ctx);

  // Draw nebula
  const gradient = ctx.createRadialGradient(
    canvasWidth / 2, canvasHeight / 2, canvasWidth * 0.1,
    canvasWidth / 2, canvasHeight / 2, canvasWidth * 0.55
  );
  gradient.addColorStop(0, "rgba(80, 30, 120, 0.18)");
  gradient.addColorStop(0.5, "rgba(20, 40, 100, 0.08)");
  gradient.addColorStop(1, "rgba(0, 0, 0, 0)");
  ctx.fillStyle = gradient;
  ctx.fillRect(0, 0, canvasWidth, canvasHeight);

  // Draw idle planets at spread-out default positions (matches simulation scale)
  const defaults = [
    { x: -160, y: 40, color: "#ff6b6b", radius: 32 },
    { x: 80, y: -120, color: "#4ecdc4", radius: 24 },
    { x: 120, y: 100, color: "#ffe66d", radius: 28 },
  ];

  defaults.forEach((p) => {
    const { sx, sy } = worldToScreen(p.x, p.y);
    drawPlanetWithGlow(ctx, sx, sy, p.radius * 0.55, p.color, 1);
  });

  // Label
  ctx.fillStyle = "rgba(255, 255, 255, 0.5)";
  ctx.font = "14px 'Trebuchet MS', 'Segoe UI', Arial, sans-serif";
  ctx.textAlign = "center";
  ctx.fillText("Select a planet and place your bet", canvasWidth / 2, canvasHeight - 30);
}

function drawPlanetWithGlow(ctx, x, y, r, color, alpha) {
  // Glow
  const glow = ctx.createRadialGradient(x, y, r * 0.2, x, y, r * 2.2);
  glow.addColorStop(0, color.replace(")", `, ${alpha * 0.8})`).replace("rgb", "rgba"));
  glow.addColorStop(1, "rgba(0,0,0,0)");
  ctx.fillStyle = glow;
  ctx.beginPath();
  ctx.arc(x, y, r * 2.2, 0, Math.PI * 2);
  ctx.fill();

  // Body
  const body = ctx.createRadialGradient(x - r * 0.3, y - r * 0.3, r * 0.05, x, y, r);
  body.addColorStop(0, "rgba(255,255,255,0.7)");
  body.addColorStop(0.4, color);
  body.addColorStop(1, "rgba(0,0,0,0.6)");
  ctx.fillStyle = body;
  ctx.globalAlpha = alpha;
  ctx.beginPath();
  ctx.arc(x, y, r, 0, Math.PI * 2);
  ctx.fill();
  ctx.globalAlpha = 1;
}

function drawTrails(ctx, planetIndex, color) {
  const trail = trails[planetIndex];
  if (trail.length < 2) return;

  ctx.lineCap = "round";
  for (let i = 0; i < trail.length - 1; i++) {
    const alpha = (i / trail.length) * 0.6;
    const width = (i / trail.length) * 3;
    ctx.strokeStyle = color.replace(")", `, ${alpha})`).replace("rgb", "rgba");
    ctx.lineWidth = width;
    ctx.beginPath();
    ctx.moveTo(trail[i].x, trail[i].y);
    ctx.lineTo(trail[i + 1].x, trail[i + 1].y);
    ctx.stroke();
  }
}

function drawFrame(state) {
  const ctx = ui.canvas.getContext("2d");
  ctx.clearRect(0, 0, canvasWidth, canvasHeight);
  drawStars(ctx);

  // Nebula
  const gradient = ctx.createRadialGradient(
    canvasWidth / 2, canvasHeight / 2, canvasWidth * 0.15,
    canvasWidth / 2, canvasHeight / 2, canvasWidth * 0.6
  );
  gradient.addColorStop(0, "rgba(80, 30, 120, 0.1)");
  gradient.addColorStop(1, "rgba(0, 0, 0, 0)");
  ctx.fillStyle = gradient;
  ctx.fillRect(0, 0, canvasWidth, canvasHeight);

  const aliveSet = new Set(state.alivePlanets || []);
  const planets = state.planets || [];
  const idxMap = { A: 0, B: 1, C: 2 };

  for (let i = 0; i < planets.length; i++) {
    const p = planets[i];
    const { sx, sy } = worldToScreen(p.x, p.y);
    const alive = aliveSet.has(p.name);
    const alpha = alive ? 1 : 0.25;
    const radius = p.radius * 0.55;

    // Trail
    drawTrails(ctx, i, p.color);

    // Planet
    drawPlanetWithGlow(ctx, sx, sy, radius, p.color, alpha);

    // Label
    if (alive) {
      ctx.fillStyle = p.color;
      ctx.font = "bold 12px 'Trebuchet MS', 'Segoe UI', Arial, sans-serif";
      ctx.textAlign = "center";
      ctx.fillText(p.name, sx, sy - radius - 12);
    }
  }

  // Draw particles
  drawParticles(ctx);

  // Progress
  if (state.totalFrames > 0) {
    const progress = state.currentFrame / state.totalFrames;
    ctx.fillStyle = "rgba(0,0,0,0.6)";
    ctx.fillRect(0, canvasHeight - 6, canvasWidth, 6);
    ctx.fillStyle = "#c0ff5a";
    ctx.fillRect(0, canvasHeight - 6, canvasWidth * progress, 6);
  }
}

function drawParticles(ctx) {
  for (const p of particles) {
    const alpha = p.life / p.maxLife;
    ctx.fillStyle = p.color.replace(")", `, ${alpha})`).replace("rgb", "rgba");
    ctx.beginPath();
    ctx.arc(p.x, p.y, p.r * alpha, 0, Math.PI * 2);
    ctx.fill();
  }
}

function updateParticles() {
  particles = particles.filter((p) => {
    p.x += p.vx;
    p.y += p.vy;
    p.life--;
    return p.life > 0;
  });
}

function spawnEliminationParticles(sx, sy, color) {
  const count = 60;
  for (let i = 0; i < count; i++) {
    const angle = Math.random() * Math.PI * 2;
    const speed = 2 + Math.random() * 6;
    particles.push({
      x: sx,
      y: sy,
      vx: Math.cos(angle) * speed,
      vy: Math.sin(angle) * speed,
      r: 2 + Math.random() * 5,
      color: color,
      life: 40 + Math.random() * 40,
      maxLife: 80,
    });
  }
}

// ─── SIMULATION LOOP ───────────────────────────────────────────────────────────

let simInterval = null;
let lastElimCount = 0;

async function simulationPoll() {
  try {
    const state = await apiGetState();
    applyServerState(state);

    if (state.phase !== "simulating") {
      stopSimulation();
    }
  } catch (error) {
    if (!(error instanceof HandledApiError)) {
      stopSimulation();
    }
  }
}

function startSimulation() {
  if (simInterval) return;

  const idxMap = { A: 0, B: 1, C: 2 };
  const current = serverState.planets;
  if (current) {
    current.forEach((p) => {
      const idx = idxMap[p.name];
      if (idx !== undefined) {
        const { sx, sy } = worldToScreen(p.x, p.y);
        trails[idx].push({ x: sx, y: sy });
      }
    });
  }

  simInterval = setInterval(simulationPoll, 33);
}

function stopSimulation() {
  if (simInterval) {
    clearInterval(simInterval);
    simInterval = null;
  }
}

// ─── CANVAS ANIMATION LOOP ─────────────────────────────────────────────────────

function canvasLoop() {
  animFrameId = requestAnimationFrame(canvasLoop);
  updateParticles();

  if (serverState && serverState.phase === "simulating") {
    drawFrame(serverState);
  } else if (serverState && serverState.phase === "round-over") {
    drawFrame(serverState);
  } else if (serverState) {
    drawIdleScene(serverState);
  }

  updateParticles();
}

// ─── FX ────────────────────────────────────────────────────────────────────────

function triggerDomEliminationFx(color) {
  if (!ui.vfxLayer) return;
  const rect = ui.vfxLayer.getBoundingClientRect();
  for (let i = 0; i < 30; i++) {
    const piece = document.createElement("span");
    piece.className = "fx-particle";
    piece.style.setProperty("--fx-x", `${rect.width * 0.5}px`);
    piece.style.setProperty("--fx-y", `${rect.height * 0.4}px`);
    piece.style.setProperty("--fx-dx", `${(Math.random() - 0.5) * 500}`);
    piece.style.setProperty("--fx-dy", `${(Math.random() - 0.5) * 300}`);
    piece.style.setProperty("--fx-color", color);
    piece.style.width = `${4 + Math.random() * 10}px`;
    piece.style.height = `${4 + Math.random() * 10}px`;
    ui.vfxLayer.appendChild(piece);
    setTimeout(() => piece.remove(), 800);
  }
}

// ─── ACTIONS ───────────────────────────────────────────────────────────────────

function setBet(amount) {
  sendAction("bet", { amount: Number(amount), planet: serverState.betOnPlanet || "A" }, "button");
}

function selectPlanet(planet) {
  if (!serverState || !serverState.canBet) return;
  sendAction("bet", { amount: serverState.selectedBet, planet: planet }, "button");
}

function startGame() {
  trails = [[], [], []];
  particles = [];
  sendAction("start", {}, "start");
}

function skipToEnd() {
  stopSimulation();
  sendAction("skip", {}, "skip");
}

function reset() {
  stopSimulation();
  trails = [[], [], []];
  particles = [];
  sendAction("reset", {}, "reset");
}

// ─── AUDIO ─────────────────────────────────────────────────────────────────────

function ensureAudioContext() {
  if (!window.AudioContext && !window.webkitAudioContext) return null;
  if (!audioContext) {
    const AudioCtor = window.AudioContext || window.webkitAudioContext;
    audioContext = new AudioCtor();
  }
  if (audioContext.state === "suspended") audioContext.resume();
  return audioContext;
}

function playTone(freq, duration, type = "sine", volume = 0.05, delay = 0) {
  const ctx = ensureAudioContext();
  if (!ctx) return;
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
  if (kind === "button") {
    playTone(430, 0.05, "triangle", 0.03);
  } else if (kind === "start") {
    playTone(120, 0.15, "sine", 0.06);
    playTone(180, 0.2, "sawtooth", 0.05, 0.08);
    playTone(300, 0.25, "triangle", 0.04, 0.16);
  } else if (kind === "skip") {
    playTone(600, 0.08, "square", 0.06);
    playTone(800, 0.06, "sawtooth", 0.04, 0.04);
  } else if (kind === "elimination") {
    playTone(80, 0.35, "sawtooth", 0.08);
    playTone(55, 0.4, "sine", 0.07, 0.1);
    playTone(200, 0.12, "triangle", 0.03, 0.2);
    playTone(150, 0.15, "square", 0.03, 0.3);
  } else if (kind === "reset") {
    playTone(350, 0.08, "triangle", 0.04);
    playTone(500, 0.06, "sine", 0.03, 0.06);
  }
}

// ─── ANIMATION HELPERS ─────────────────────────────────────────────────────────

function animateBalanceChange(el) {
  if (!window.anime) return;
  anime({ targets: el, scale: [1, 1.15, 1], duration: 400, easing: "easeOutQuad" });
}

function animateRoundResult(el) {
  if (!window.anime) return;
  anime.set(el, { scale: 0.5, opacity: 0, rotate: -180 });
  anime({
    targets: el,
    scale: [0.5, 1.15, 1],
    opacity: 1,
    rotate: 0,
    duration: 700,
    easing: "easeOutElastic(1, .7)",
  });
}

function animateButtonPress(el) {
  if (!window.anime) return;
  anime({ targets: el, scale: [1, 0.95, 1], duration: 200, easing: "easeOutQuad" });
}

function animateChipSelect(el) {
  if (!window.anime) return;
  anime({ targets: el, scale: [1, 1.2, 1], duration: 300, easing: "easeOutBack" });
}

// ─── EVENT WIRING ──────────────────────────────────────────────────────────────

ui.planetCards.forEach((card) => {
  card.addEventListener("click", () => {
    animateButtonPress(card);
    selectPlanet(card.dataset.planet);
  });
});

ui.chips.forEach((chip) => {
  chip.addEventListener("click", () => {
    animateChipSelect(chip);
    setBet(chip.dataset.bet);
  });
});

ui.startBtn.addEventListener("click", () => {
  animateButtonPress(ui.startBtn);
  startGame();
});

ui.skipBtn.addEventListener("click", () => {
  animateButtonPress(ui.skipBtn);
  skipToEnd();
});

ui.resetBtn.addEventListener("click", () => {
  animateButtonPress(ui.resetBtn);
  reset();
});

// ─── INIT ──────────────────────────────────────────────────────────────────────

function init() {
  initCanvas();
  refreshState();
  canvasLoop();
}

window.addEventListener("resize", () => {
  initCanvas();
  if (serverState) {
    if (serverState.phase === "betting") drawIdleScene(serverState);
    else drawFrame(serverState);
  }
});

window.addEventListener("focus", refreshState);

init();
