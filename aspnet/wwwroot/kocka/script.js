const API_BASE = "/api/blackjack";
const LOGIN_URL = "/Account/Login?returnUrl=/kocka/index.html";
const PROFILE_URL = "/Account/Profile";

let serverState = null;
let lastRenderedVersion = -1;
let lastRevealDealer = false;
let audioContext;

const ui = {
  table: document.getElementById("table"),
  gate: document.getElementById("gate"),
  gateMessage: document.getElementById("gate-message"),
  gateLink: document.getElementById("gate-link"),
  playerName: document.getElementById("player-name"),
  balance: document.getElementById("balance"),
  dealerCards: document.getElementById("dealer-cards"),
  dealerTotal: document.getElementById("dealer-total"),
  playerLabel: document.getElementById("player-label"),
  playerTotal: document.getElementById("player-total"),
  playerCards: document.getElementById("player-cards"),
  playerArea: document.getElementById("player-cards")?.closest(".player-area"),
  status: document.getElementById("status"),
  roundResult: document.getElementById("round-result"),
  betDisplay: document.getElementById("bet-display"),
  chips: Array.from(document.querySelectorAll(".chip")),
  dealBtn: document.getElementById("deal-btn"),
  hitBtn: document.getElementById("hit-btn"),
  standBtn: document.getElementById("stand-btn"),
  doubleBtn: document.getElementById("double-btn"),
  wins: document.getElementById("wins"),
  losses: document.getElementById("losses"),
  pushes: document.getElementById("pushes"),
  vfxLayer: document.getElementById("vfx-layer"),
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

async function sendAction(action, body, sound) {
  if (sound) {
    playSound(sound);
    triggerActionFx(sound);
  }
  try {
    applyServerState(await apiPost(action, body));
  } catch (error) {
    if (!(error instanceof HandledApiError)) {
      console.error("Blackjack error:", error);
      setStatus("Something went wrong. Try again.", true);
    }
  }
}

async function refreshState() {
  try {
    applyServerState(await apiGetState());
  } catch (error) {
    if (!(error instanceof HandledApiError)) {
      console.error("Blackjack error:", error);
      setStatus("Something went wrong. Try again.", true);
    }
  }
}

// ============================================
// RENDERING
// ============================================

function formatMoney(value) {
  const amount = Number(value ?? 0);
  return `$${amount.toFixed(2)}`;
}

function renderCard(card, hidden = false) {
  const cardEl = document.createElement("div");
  cardEl.className = `card${hidden ? " back" : ""}${card.color === "red" && !hidden ? " red" : ""}`;
  if (hidden) {
    cardEl.innerHTML = '<span class="rank">?</span><span class="suit">◆</span>';
    return cardEl;
  }
  cardEl.innerHTML = `<span class="rank">${card.rank}</span><span class="suit">${card.suit}</span>`;
  return cardEl;
}

function setStatus(text, danger = false) {
  ui.status.textContent = text;
  ui.status.classList.toggle("danger", danger);
  ui.status.classList.remove("pulse");
  animateStatusPulse(ui.status);
  void ui.status.offsetWidth;
  ui.status.classList.add("pulse");
}

function triggerActionFx(kind) {
  if (!ui.vfxLayer || !["deal", "hit", "stand", "double"].includes(kind)) {
    return;
  }

  const config = {
    deal: { count: 120, color: "#70ff3a", spread: 360, flash: "flash-deal", waves: 2, words: ["DEAL", "SHUFFLE", "READY"] },
    hit: { count: 90, color: "#b4ff66", spread: 310, flash: "flash-hit", waves: 2, words: ["HIT", "DRAW", "CARD"] },
    stand: { count: 66, color: "#ff952a", spread: 270, flash: "flash-stand", waves: 1, words: ["STAND", "HOLD", "LOCK"] },
    double: { count: 180, color: "#ff53b8", spread: 520, flash: "flash-double", waves: 3, words: ["DOUBLE", "ALL IN", "MAX BET"] },
  }[kind];

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

  document.body.classList.remove("action-flash", "flash-deal", "flash-hit", "flash-stand", "flash-double");
  document.body.classList.remove("action-shake", "action-glitch", "action-overdrive", "action-strobe", "action-tilt");
  void document.body.offsetWidth;
  document.body.classList.add(
    "action-flash",
    config.flash,
    "action-shake",
    "action-glitch",
    "action-overdrive",
    "action-strobe",
    "action-tilt"
  );
  setTimeout(() => {
    document.body.classList.remove(
      "action-flash",
      config.flash,
      "action-shake",
      "action-glitch",
      "action-overdrive",
      "action-strobe",
      "action-tilt"
    );
  }, kind === "double" ? 980 : 680);
}

function render() {
  if (!serverState) {
    return;
  }
  // Only re-render (and re-animate cards) when the server state actually changed,
  // so focus re-fetches never make cards flicker or replay animations.
  if (serverState.version === lastRenderedVersion) {
    return;
  }
  lastRenderedVersion = serverState.version;

  const revealJustHappened = serverState.revealDealer && !lastRevealDealer;
  lastRevealDealer = serverState.revealDealer;

  // Header: real player identity and real casino balance (server-authoritative).
  ui.playerName.textContent = serverState.playerName || "—";
  ui.balance.textContent = `Balance: ${formatMoney(serverState.balance)}`;
  animateBalanceChange(ui.balance);
  ui.playerLabel.textContent = serverState.playerName || "Player";

  // Dealer hand.
  ui.dealerCards.innerHTML = "";
  serverState.dealerHand.forEach((card, index) => {
    const hidden = Boolean(card.hidden);
    const cardEl = renderCard(card, hidden);
    ui.dealerCards.appendChild(cardEl);
    animateCardIn(cardEl);
    if (!hidden && index === 1 && revealJustHappened) {
      setTimeout(() => animateDealerReveal(cardEl), 300);
    }
  });
  ui.dealerTotal.textContent = serverState.dealerTotal !== null && serverState.dealerTotal !== undefined
    ? `Total: ${serverState.dealerTotal}`
    : "Total: ?";

  // Player hand.
  ui.playerCards.innerHTML = "";
  const cardElements = [];
  (serverState.hand || []).forEach((card) => {
    const cardEl = renderCard(card, false);
    ui.playerCards.appendChild(cardEl);
    cardElements.push(cardEl);
  });
  if (cardElements.length > 0) {
    animateCardCascade(cardElements);
  }
  ui.playerTotal.textContent = `Total: ${serverState.hand?.length ? serverState.total ?? 0 : 0}`;
  if (serverState.bust) {
    animateBustShake(ui.playerArea);
  }

  // Scoreboard and bet.
  ui.wins.textContent = `Wins: ${serverState.wins ?? 0}`;
  ui.losses.textContent = `Losses: ${serverState.losses ?? 0}`;
  ui.pushes.textContent = `Pushes: ${serverState.pushes ?? 0}`;
  ui.betDisplay.textContent = serverState.phase === "betting"
    ? `Your Bet: ${formatMoney(serverState.selectedBet)}`
    : `Your Bet: ${formatMoney(serverState.currentBet)}`;

  // Round result banner.
  const result = serverState.lastRoundResult;
  if (serverState.phase === "round-over" && result) {
    const label = result === "win" ? "YOU WIN" : result === "push" ? "PUSH" : "YOU LOSE";
    ui.roundResult.textContent = label;
    ui.roundResult.className = `round-result ${result}`;
    animateRoundResult(ui.roundResult);
  } else {
    ui.roundResult.textContent = "";
    ui.roundResult.className = "round-result hidden";
  }

  // Button availability comes straight from the server's can* flags —
  // the client never computes game rules.
  ui.chips.forEach((chip) => {
    const chipValue = Number(chip.dataset.bet);
    chip.classList.toggle("active", serverState.selectedBet === chipValue);
    chip.disabled = !serverState.canSetBet;
  });
  ui.dealBtn.disabled = !serverState.canDeal;
  ui.hitBtn.disabled = !serverState.canHit;
  ui.standBtn.disabled = !serverState.canStand;
  ui.doubleBtn.disabled = !serverState.canDouble;

  if (serverState.status) {
    setStatus(serverState.status, Boolean(serverState.bust));
  }
}

// ============================================
// PLAYER ACTIONS (server-backed)
// ============================================

function setBet(amount) {
  sendAction("bet", { amount: Number(amount) }, "button");
}

function startRound() {
  sendAction("deal", {}, "deal");
}

function hit() {
  sendAction("hit", {}, "hit");
}

function stand() {
  sendAction("stand", {}, "stand");
}

function doubleDown() {
  sendAction("double", {}, "double");
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
  if (kind === "deal") {
    playTone(190, 0.09, "square", 0.06);
    playTone(280, 0.1, "square", 0.055, 0.06);
    playTone(420, 0.1, "triangle", 0.05, 0.12);
  } else if (kind === "hit") {
    playTone(140, 0.06, "sawtooth", 0.06);
    playTone(320, 0.08, "triangle", 0.05, 0.05);
    playTone(520, 0.06, "square", 0.04, 0.12);
  } else if (kind === "stand") {
    playTone(160, 0.12, "sine", 0.06);
    playTone(130, 0.18, "triangle", 0.04, 0.1);
  } else if (kind === "double") {
    playTone(120, 0.08, "sawtooth", 0.08);
    playTone(240, 0.08, "square", 0.07, 0.05);
    playTone(350, 0.09, "triangle", 0.06, 0.1);
    playTone(520, 0.1, "sawtooth", 0.06, 0.16);
    playTone(740, 0.14, "triangle", 0.04, 0.24);
  } else if (kind === "button") {
    playTone(430, 0.05, "triangle", 0.03);
  }
}

// ============================================
// EVENT WIRING
// ============================================

ui.dealBtn.addEventListener("click", () => {
  animateButtonPress(ui.dealBtn);
  animateButtonGlow(ui.dealBtn);
  startRound();
});
ui.hitBtn.addEventListener("click", () => {
  animateButtonPress(ui.hitBtn);
  hit();
});
ui.standBtn.addEventListener("click", () => {
  animateButtonPress(ui.standBtn);
  stand();
});
ui.doubleBtn.addEventListener("click", () => {
  animateButtonPress(ui.doubleBtn);
  doubleDown();
});
ui.chips.forEach((chip) => {
  chip.addEventListener("click", () => {
    animateChipSelect(chip);
    setBet(chip.dataset.bet);
  });
});

// Singleplayer: fetch once on load and after each action; re-sync on focus
// in case the balance changed elsewhere (no polling needed).
refreshState();
window.addEventListener("focus", refreshState);

// ============================================
// ANIME.JS ANIMATION FUNCTIONS
// ============================================

// Animate card entrance
function animateCardIn(cardElement) {
  if (!window.anime) return;
  anime.set(cardElement, {
    opacity: 0,
    translateY: 30,
    rotate: -15,
  });
  anime({
    targets: cardElement,
    opacity: 1,
    translateY: 0,
    rotate: 0,
    duration: 450,
    easing: "easeOutElastic(1, .6)",
    delay: Math.random() * 100,
  });
}

// Animate button click/press
function animateButtonPress(buttonElement) {
  if (!window.anime) return;
  anime({
    targets: buttonElement,
    scale: [1, 0.95, 1],
    duration: 200,
    easing: "easeOutQuad",
  });
}

// Animate status text pulse
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

// Animate round result popup with bounce
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

// Animate chip active state
function animateChipSelect(chipElement) {
  if (!window.anime) return;
  anime({
    targets: chipElement,
    scale: [1, 1.2, 1],
    duration: 300,
    easing: "easeOutBack",
  });
}

// Animate balance change
function animateBalanceChange(balanceElement) {
  if (!window.anime) return;
  anime({
    targets: balanceElement,
    scale: [1, 1.15, 1],
    duration: 400,
    easing: "easeOutQuad",
  });
}

// Animate dealer card reveal
function animateDealerReveal(cardElement) {
  if (!window.anime) return;
  anime({
    targets: cardElement,
    rotateY: [180, 0],
    duration: 500,
    easing: "easeOutQuad",
  });
}

// Animate bust shake
function animateBustShake(playerArea) {
  if (!window.anime || !playerArea) return;
  anime({
    targets: playerArea,
    translateX: [-8, 8, -8, 8, 0],
    duration: 400,
    easing: "easeInOutQuad",
  });
}

// Glow/pulse effect for active buttons
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

// Cascade effect for multiple cards
function animateCardCascade(cardElements) {
  if (!window.anime) return;
  anime({
    targets: cardElements,
    opacity: [0, 1],
    translateY: [40, 0],
    rotate: [-20, 0],
    delay: anime.stagger(80),
    duration: 500,
    easing: "easeOutElastic(1, .6)",
  });
}
