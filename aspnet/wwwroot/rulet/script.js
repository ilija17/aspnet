const API_BASE = "/api/roulette";
const LOGIN_URL = "/Account/Login?returnUrl=/rulet/index.html";
const PROFILE_URL = "/Account/Profile";

const RED_NUMBERS = new Set([1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36]);
const CHIP_AMOUNTS = [25, 50, 100, 200];

let serverState = null;
let lastRenderedVersion = -1;
let selectedChip = 25; // client-side only: which chip is used for the next bet
let spinning = false;
let showResultBanner = false; // only show WIN banner right after a spin
let audioContext;

const ui = {
  table: document.getElementById("table"),
  gate: document.getElementById("gate"),
  gateMessage: document.getElementById("gate-message"),
  gateLink: document.getElementById("gate-link"),
  playerName: document.getElementById("player-name"),
  balance: document.getElementById("balance"),
  totalBet: document.getElementById("total-bet"),
  betDisplay: document.getElementById("bet-display"),
  chips: Array.from(document.querySelectorAll(".chip")),
  board: document.getElementById("board"),
  resultNumber: document.getElementById("result-number"),
  history: document.getElementById("history"),
  status: document.getElementById("status"),
  roundResult: document.getElementById("round-result"),
  betsList: document.getElementById("bets-list"),
  betsTotal: document.getElementById("bets-total"),
  spinBtn: document.getElementById("spin-btn"),
  clearBtn: document.getElementById("clear-btn"),
  vfxLayer: document.getElementById("vfx-layer"),
};

// ============================================
// BETTING BOARD CONSTRUCTION
// ============================================

const spotsByKey = new Map(); // "kind:number" -> spot element
const allSpots = [];

function spotKey(kind, number) {
  return kind === "straight" ? `straight:${number}` : `${kind}:`;
}

function createSpot({ kind, number = null, label, classes = [], column, row, colSpan = 1, rowSpan = 1 }) {
  const spot = document.createElement("button");
  spot.type = "button";
  spot.className = ["spot", ...classes].join(" ");
  spot.textContent = label;
  if (column) {
    spot.style.gridColumn = colSpan > 1 ? `${column} / span ${colSpan}` : `${column}`;
  }
  if (row) {
    spot.style.gridRow = rowSpan > 1 ? `${row} / span ${rowSpan}` : `${row}`;
  }
  spot.addEventListener("click", () => {
    animateSpotPress(spot);
    placeBet(kind, number);
  });
  spotsByKey.set(spotKey(kind, number), spot);
  allSpots.push(spot);
  ui.board.appendChild(spot);
  return spot;
}

function buildBoard() {
  // Zero (spans the three number rows).
  createSpot({ kind: "straight", number: 0, label: "0", classes: ["spot-green"], column: 1, row: 1, rowSpan: 3 });

  // 3 x 12 number grid. Top row holds 3,6,…,36; bottom row holds 1,4,…,34.
  for (let r = 1; r <= 3; r += 1) {
    for (let c = 0; c < 12; c += 1) {
      const n = 3 * (c + 1) - (r - 1);
      createSpot({
        kind: "straight",
        number: n,
        label: String(n),
        classes: [RED_NUMBERS.has(n) ? "spot-red" : "spot-black"],
        column: c + 2,
        row: r,
      });
    }
  }

  // Column bets ("2 to 1") at the right edge of each number row.
  const columnKinds = ["col3", "col2", "col1"];
  columnKinds.forEach((kind, index) => {
    createSpot({ kind, label: "2 to 1", classes: ["spot-outside", "spot-col"], column: 14, row: index + 1 });
  });

  // Dozens.
  createSpot({ kind: "dozen1", label: "1st 12", classes: ["spot-outside"], column: 2, row: 4, colSpan: 4 });
  createSpot({ kind: "dozen2", label: "2nd 12", classes: ["spot-outside"], column: 6, row: 4, colSpan: 4 });
  createSpot({ kind: "dozen3", label: "3rd 12", classes: ["spot-outside"], column: 10, row: 4, colSpan: 4 });

  // Even-money bets.
  createSpot({ kind: "low", label: "1–18", classes: ["spot-outside"], column: 2, row: 5, colSpan: 2 });
  createSpot({ kind: "even", label: "Even", classes: ["spot-outside"], column: 4, row: 5, colSpan: 2 });
  createSpot({ kind: "red", label: "Red", classes: ["spot-red"], column: 6, row: 5, colSpan: 2 });
  createSpot({ kind: "black", label: "Black", classes: ["spot-black"], column: 8, row: 5, colSpan: 2 });
  createSpot({ kind: "odd", label: "Odd", classes: ["spot-outside"], column: 10, row: 5, colSpan: 2 });
  createSpot({ kind: "high", label: "19–36", classes: ["spot-outside"], column: 12, row: 5, colSpan: 2 });
}

buildBoard();

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
  if (spinning) {
    return;
  }
  try {
    applyServerState(await apiGetState());
  } catch (error) {
    if (!(error instanceof HandledApiError)) {
      console.error("Roulette error:", error);
      setStatus("Something went wrong. Try again.", true);
    }
  }
}

// ============================================
// PLAYER ACTIONS (server-backed)
// ============================================

async function placeBet(kind, number) {
  if (spinning || !serverState || !serverState.canBet) {
    return;
  }
  playSound("bet");
  triggerActionFx("bet");
  const body = { kind, amount: selectedChip };
  if (kind === "straight") {
    body.number = number;
  }
  try {
    applyServerState(await apiPost("bet", body));
  } catch (error) {
    if (!(error instanceof HandledApiError)) {
      console.error("Roulette error:", error);
      setStatus("Something went wrong. Try again.", true);
    }
  }
}

async function clearBets() {
  if (spinning) {
    return;
  }
  playSound("clear");
  triggerActionFx("clear");
  try {
    applyServerState(await apiPost("clear", {}));
  } catch (error) {
    if (!(error instanceof HandledApiError)) {
      console.error("Roulette error:", error);
      setStatus("Something went wrong. Try again.", true);
    }
  }
}

async function spin() {
  if (spinning) {
    return;
  }
  spinning = true;
  setControlsDisabled(true);
  playSound("spin");
  triggerActionFx("spin");
  setStatus("No more bets — spinning…");

  let finalState;
  try {
    finalState = await apiPost("spin", {});
  } catch (error) {
    spinning = false;
    setControlsDisabled(false);
    render(true);
    if (!(error instanceof HandledApiError)) {
      console.error("Roulette error:", error);
      setStatus("Something went wrong. Try again.", true);
    }
    return;
  }

  // Build suspense before revealing the server's number.
  await animateWheelRoll(1600);

  spinning = false;
  showResultBanner = true;
  applyServerState(finalState);

  if (Number(finalState.lastPayout) > 0) {
    playSound("win");
    triggerActionFx("win");
  } else {
    playSound("lose");
  }
}

// ============================================
// RENDERING
// ============================================

function formatMoney(value) {
  const amount = Number(value ?? 0);
  return `$${amount.toFixed(2)}`;
}

function colorOfNumber(n) {
  if (n === 0) {
    return "green";
  }
  return RED_NUMBERS.has(n) ? "red" : "black";
}

function setStatus(text, danger = false) {
  ui.status.textContent = text;
  ui.status.classList.toggle("danger", danger);
  ui.status.classList.remove("pulse");
  animateStatusPulse(ui.status);
  void ui.status.offsetWidth;
  ui.status.classList.add("pulse");
}

function setResultNumber(value, color) {
  ui.resultNumber.textContent = value;
  ui.resultNumber.className = `result-number ${color}`;
}

function renderChips() {
  ui.chips.forEach((chip) => {
    const chipValue = Number(chip.dataset.bet);
    chip.classList.toggle("active", selectedChip === chipValue);
    chip.disabled = spinning || Boolean(serverState && !serverState.canBet);
  });
  ui.betDisplay.textContent = `Chip: $${selectedChip}`;
}

function renderMarkers() {
  document.querySelectorAll(".bet-marker").forEach((marker) => marker.remove());
  const sums = new Map();
  (serverState?.bets || []).forEach((bet) => {
    const key = spotKey(bet.kind, bet.number);
    sums.set(key, (sums.get(key) || 0) + Number(bet.amount));
  });
  sums.forEach((amount, key) => {
    const spot = spotsByKey.get(key);
    if (!spot) {
      return;
    }
    const marker = document.createElement("span");
    marker.className = "bet-marker";
    marker.textContent = `$${amount}`;
    spot.appendChild(marker);
  });
}

function renderBetsList() {
  ui.betsList.innerHTML = "";
  const bets = serverState?.bets || [];
  if (bets.length === 0) {
    const empty = document.createElement("li");
    empty.className = "bets-empty";
    empty.textContent = "No bets placed.";
    ui.betsList.appendChild(empty);
  } else {
    bets.forEach((bet) => {
      const item = document.createElement("li");
      const label = document.createElement("span");
      label.textContent = bet.label;
      const amount = document.createElement("span");
      amount.className = "amount";
      amount.textContent = formatMoney(bet.amount);
      item.append(label, amount);
      ui.betsList.appendChild(item);
    });
  }
  ui.betsTotal.textContent = `Total: ${formatMoney(serverState?.totalBet)}`;
}

function renderHistory() {
  ui.history.innerHTML = "";
  const history = serverState?.history || [];
  if (history.length === 0) {
    const empty = document.createElement("span");
    empty.className = "history-empty";
    empty.textContent = "No spins yet.";
    ui.history.appendChild(empty);
    return;
  }
  history.forEach((n, index) => {
    const chip = document.createElement("span");
    chip.className = `hist-chip ${colorOfNumber(n)}${index === 0 ? " newest" : ""}`;
    chip.textContent = String(n);
    ui.history.appendChild(chip);
  });
}

function setControlsDisabled(disabled) {
  allSpots.forEach((spot) => {
    spot.disabled = disabled || Boolean(serverState && !serverState.canBet);
  });
  ui.spinBtn.disabled = disabled || Boolean(serverState && !serverState.canSpin);
  ui.clearBtn.disabled = disabled || Boolean(serverState && !serverState.canClear);
  renderChips();
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
  ui.totalBet.textContent = `Bet: ${formatMoney(serverState.totalBet)}`;
  animateBalanceChange(ui.balance);

  renderChips();
  renderMarkers();
  renderBetsList();
  renderHistory();

  // Last spun number.
  if (!spinning) {
    if (serverState.lastNumber !== null && serverState.lastNumber !== undefined) {
      setResultNumber(String(serverState.lastNumber), serverState.lastColor || colorOfNumber(serverState.lastNumber));
      animateResultReveal(ui.resultNumber);
    } else {
      setResultNumber("?", "neutral");
    }
  }

  // WIN banner (only right after a spin response).
  if (showResultBanner) {
    showResultBanner = false;
    if (Number(serverState.lastPayout) > 0) {
      ui.roundResult.textContent = `WIN ${formatMoney(serverState.lastPayout)}`;
      ui.roundResult.className = "round-result win";
      animateRoundResult(ui.roundResult);
    } else {
      ui.roundResult.textContent = "NO WIN";
      ui.roundResult.className = "round-result loss";
      animateRoundResult(ui.roundResult);
    }
  } else if (!spinning) {
    ui.roundResult.textContent = "";
    ui.roundResult.className = "round-result hidden";
  }

  // Availability comes straight from the server's can* flags — the client
  // never computes game rules or payouts.
  allSpots.forEach((spot) => {
    spot.disabled = spinning || !serverState.canBet;
  });
  ui.spinBtn.disabled = spinning || !serverState.canSpin;
  ui.clearBtn.disabled = spinning || !serverState.canClear;

  if (serverState.status) {
    setStatus(serverState.status);
  }
}

// ============================================
// WHEEL ROLL ANIMATION
// ============================================

function animateWheelRoll(duration) {
  return new Promise((resolve) => {
    const start = performance.now();
    ui.resultNumber.classList.add("rolling");

    const tick = () => {
      const elapsed = performance.now() - start;
      if (elapsed >= duration) {
        ui.resultNumber.classList.remove("rolling");
        resolve();
        return;
      }
      const n = Math.floor(Math.random() * 37);
      setResultNumber(String(n), colorOfNumber(n));
      ui.resultNumber.classList.add("rolling");
      playTone(180 + Math.random() * 240, 0.03, "square", 0.02);
      // Decelerate: ticks get slower as the ball settles.
      const progress = elapsed / duration;
      const delay = 50 + progress * progress * 260;
      setTimeout(tick, delay);
    };
    tick();
  });
}

// ============================================
// VFX (same flair as blackjack)
// ============================================

function triggerActionFx(kind) {
  if (!ui.vfxLayer) {
    return;
  }

  const config = {
    bet: { count: 60, color: "#b4ff66", spread: 260, flash: "flash-bet", waves: 1, words: ["BET", "PLACED", "CHIP DOWN"] },
    clear: { count: 66, color: "#ff952a", spread: 270, flash: "flash-clear", waves: 1, words: ["CLEAR", "WIPE", "RESET"] },
    spin: { count: 140, color: "#ff53b8", spread: 460, flash: "flash-spin", waves: 2, words: ["SPIN", "NO MORE BETS", "RIEN NE VA PLUS"] },
    win: { count: 200, color: "#70ff3a", spread: 540, flash: "flash-bet", waves: 3, words: ["WINNER", "JACKPOT", "PAYOUT"] },
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

  document.body.classList.remove("action-flash", "flash-bet", "flash-spin", "flash-clear");
  document.body.classList.remove("action-shake", "action-glitch", "action-overdrive", "action-strobe", "action-tilt");
  void document.body.offsetWidth;
  const heavy = kind === "spin" || kind === "win";
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
  } else if (kind === "clear") {
    playTone(320, 0.08, "sawtooth", 0.05);
    playTone(180, 0.12, "triangle", 0.04, 0.07);
  } else if (kind === "spin") {
    playTone(120, 0.08, "sawtooth", 0.07);
    playTone(240, 0.08, "square", 0.06, 0.06);
    playTone(360, 0.09, "triangle", 0.05, 0.12);
    playTone(520, 0.1, "sawtooth", 0.05, 0.18);
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
  });
});

ui.spinBtn.addEventListener("click", () => {
  animateButtonPress(ui.spinBtn);
  animateButtonGlow(ui.spinBtn);
  spin();
});

ui.clearBtn.addEventListener("click", () => {
  animateButtonPress(ui.clearBtn);
  clearBets();
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

function animateSpotPress(spotElement) {
  if (!window.anime) return;
  anime({
    targets: spotElement,
    scale: [1, 0.92, 1.04, 1],
    duration: 260,
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

function animateResultReveal(resultElement) {
  if (!window.anime) return;
  anime({
    targets: resultElement,
    scale: [0.6, 1.12, 1],
    rotate: [-120, 0],
    duration: 650,
    easing: "easeOutElastic(1, .65)",
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
