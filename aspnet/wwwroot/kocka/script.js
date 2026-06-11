const API_BASE = "/api/blackjack";
const CLIENT_KEY = "blackjack-multiplayer-client-id";
const POLL_INTERVAL_MS = 1500;

function getOrCreateClientId() {
  const existing = sessionStorage.getItem(CLIENT_KEY);
  if (existing) {
    return existing;
  }
  const created = `${Math.random().toString(36).slice(2, 8)}-${Date.now().toString(36)}`;
  sessionStorage.setItem(CLIENT_KEY, created);
  return created;
}

const localClientId = getOrCreateClientId();
let serverState = null;
let lastRenderedVersion = -1;
let lastRevealDealer = false;
let connectionLost = false;
let pollInFlight = false;
let audioContext;

const ui = {
  dealerCards: document.getElementById("dealer-cards"),
  dealerTotal: document.getElementById("dealer-total"),
  status: document.getElementById("status"),
  roundResult: document.getElementById("round-result"),
  hitBtn: document.getElementById("hit-btn"),
  standBtn: document.getElementById("stand-btn"),
  doubleBtn: document.getElementById("double-btn"),
  dealBtn: document.getElementById("new-game-btn"),
  wins: document.getElementById("wins"),
  losses: document.getElementById("losses"),
  pushes: document.getElementById("pushes"),
  balance: document.getElementById("balance"),
  betDisplay: document.getElementById("bet-display"),
  chips: Array.from(document.querySelectorAll(".chip")),
  playerAreas: {
    1: {
      container: document.getElementById("player-1-cards")?.closest(".player-area"),
      label: document.getElementById("player-1-label"),
      total: document.getElementById("player-1-total"),
      meta: document.getElementById("player-1-meta"),
      cards: document.getElementById("player-1-cards"),
    },
    2: {
      container: document.getElementById("player-2-cards")?.closest(".player-area"),
      label: document.getElementById("player-2-label"),
      total: document.getElementById("player-2-total"),
      meta: document.getElementById("player-2-meta"),
      cards: document.getElementById("player-2-cards"),
    },
  },
  playersGrid: document.querySelector(".players-grid"),
  youAre: document.getElementById("you-are"),
  seat1Status: document.getElementById("seat-1-status"),
  seat2Status: document.getElementById("seat-2-status"),
  joinSeat1Btn: document.getElementById("join-seat-1"),
  joinSeat2Btn: document.getElementById("join-seat-2"),
  leaveSeatBtn: document.getElementById("leave-seat"),
  soloModeBtn: document.getElementById("solo-mode-btn"),
  resetTableBtn: document.getElementById("reset-table-btn"),
  vfxLayer: document.getElementById("vfx-layer"),
};

// ============================================
// SERVER API LAYER
// ============================================

async function apiGetState() {
  const res = await fetch(`${API_BASE}/state?clientId=${encodeURIComponent(localClientId)}`, {
    headers: { Accept: "application/json" },
  });
  if (!res.ok) {
    throw new Error(`GET state failed: ${res.status}`);
  }
  return res.json();
}

async function apiPost(action, extra = {}) {
  const res = await fetch(`${API_BASE}/${action}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", Accept: "application/json" },
    body: JSON.stringify({ clientId: localClientId, ...extra }),
  });
  if (!res.ok) {
    throw new Error(`POST ${action} failed: ${res.status}`);
  }
  return res.json();
}

function handleConnectionError(error) {
  console.error("Blackjack server error:", error);
  connectionLost = true;
  setStatus("Connection lost. Retrying…", true);
}

function applyServerState(state) {
  if (connectionLost) {
    connectionLost = false;
    lastRenderedVersion = -1; // force re-render to restore real status text
  }
  serverState = state;
  render();
}

async function sendAction(action, extra, sound) {
  if (sound) {
    playSound(sound);
    triggerActionFx(sound);
  }
  try {
    const state = await apiPost(action, extra);
    applyServerState(state);
  } catch (error) {
    handleConnectionError(error);
  }
}

async function pollState() {
  if (pollInFlight) {
    return;
  }
  pollInFlight = true;
  try {
    const state = await apiGetState();
    applyServerState(state);
  } catch (error) {
    handleConnectionError(error);
  } finally {
    pollInFlight = false;
  }
}

// ============================================
// RENDERING
// ============================================

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
    deal: { count: 120, color: "#70ff3a", spread: 360, flash: "flash-deal", waves: 2, words: ["SYNC", "READY", "ARMED"] },
    hit: { count: 90, color: "#b4ff66", spread: 310, flash: "flash-hit", waves: 2, words: ["IMPACT", "BREACH", "PULSE"] },
    stand: { count: 66, color: "#ff952a", spread: 270, flash: "flash-stand", waves: 1, words: ["LOCK", "HOLD", "GUARD"] },
    double: { count: 180, color: "#ff53b8", spread: 520, flash: "flash-double", waves: 3, words: ["BERSERK", "OVERDRIVE", "MAX POWER"] },
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
  // so the 1.5s poll never makes cards flicker or replay animations.
  if (serverState.version === lastRenderedVersion) {
    return;
  }
  lastRenderedVersion = serverState.version;

  const revealJustHappened = serverState.revealDealer && !lastRevealDealer;
  lastRevealDealer = serverState.revealDealer;

  const yourSeat = serverState.yourSeat;
  const localPlayer = yourSeat ? serverState.players[yourSeat] : null;
  const occupiedSeats = [1, 2].filter((seat) => Boolean(serverState.players[seat]?.occupied));
  const isSinglePlayerLayout = occupiedSeats.length === 1;

  ui.youAre.textContent = yourSeat ? `You are: Player ${yourSeat}` : "You are: Spectator";
  ui.seat1Status.textContent = `Seat 1: ${serverState.players[1]?.occupied ? "Occupied" : "Open"}`;
  ui.seat2Status.textContent = `Seat 2: ${serverState.players[2]?.occupied ? "Occupied" : "Open"}`;
  ui.soloModeBtn.textContent = `Solo Mode: ${serverState.soloMode ? "On" : "Off"}`;

  // Button availability comes straight from the server's can* flags.
  ui.joinSeat1Btn.disabled = !serverState.canJoin1;
  ui.joinSeat2Btn.disabled = !serverState.canJoin2;
  ui.leaveSeatBtn.disabled = !serverState.canLeave;
  ui.soloModeBtn.disabled = !serverState.canToggleSolo;
  ui.resetTableBtn.disabled = !serverState.canReset;

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

  [1, 2].forEach((seat) => {
    const player = serverState.players[seat];
    const area = ui.playerAreas[seat];
    if (!player || !area) {
      return;
    }
    area.cards.innerHTML = "";
    const cardElements = [];
    (player.hand || []).forEach((card) => {
      const cardEl = renderCard(card, Boolean(card.hidden));
      area.cards.appendChild(cardEl);
      cardElements.push(cardEl);
    });
    if (cardElements.length > 0) {
      animateCardCascade(cardElements);
    }

    const youTag = seat === yourSeat ? " (You)" : "";
    area.label.textContent = `Player ${seat}${youTag}`;
    area.total.textContent = `Total: ${player.hand?.length ? player.total ?? 0 : 0}`;
    area.meta.textContent = `Bal: $${player.balance ?? 0} | Bet: $${player.selectedBet ?? 0} | In: $${player.currentBet ?? 0}`;
    area.container?.classList.toggle("is-hidden", isSinglePlayerLayout && !player.occupied);
  });
  ui.playersGrid?.classList.toggle("single-player", isSinglePlayerLayout);

  ui.wins.textContent = `Wins: ${localPlayer ? localPlayer.wins : 0}`;
  ui.losses.textContent = `Losses: ${localPlayer ? localPlayer.losses : 0}`;
  ui.pushes.textContent = `Pushes: ${localPlayer ? localPlayer.pushes : 0}`;
  ui.balance.textContent = `Balance: ${localPlayer ? `$${localPlayer.balance}` : "-"}`;
  if (localPlayer) {
    animateBalanceChange(ui.balance);
  }
  ui.betDisplay.textContent = `Your Bet: ${localPlayer ? `$${localPlayer.selectedBet}` : "-"}`;

  const localResult = yourSeat ? serverState.lastRoundResults?.[yourSeat] : null;
  if (serverState.phase === "round-over" && localResult && ui.roundResult) {
    const label = localResult === "win" ? "YOU WIN" : localResult === "push" ? "PUSH" : "YOU LOSE";
    ui.roundResult.textContent = label;
    ui.roundResult.className = `round-result ${localResult}`;
    animateRoundResult(ui.roundResult);
  } else if (ui.roundResult) {
    ui.roundResult.textContent = "";
    ui.roundResult.className = "round-result hidden";
  }

  ui.chips.forEach((chip) => {
    const chipValue = Number(chip.dataset.bet);
    chip.classList.toggle("active", Boolean(localPlayer && localPlayer.selectedBet === chipValue));
    chip.disabled = !serverState.canSetBet;
  });

  ui.dealBtn.disabled = !serverState.canDeal;
  ui.hitBtn.disabled = !serverState.canHit;
  ui.standBtn.disabled = !serverState.canStand;
  ui.doubleBtn.disabled = !serverState.canDouble;

  if (serverState.phase === "waiting" && serverState.soloMode && isSinglePlayerLayout) {
    setStatus("Solo mode: press Deal when ready.", false);
  } else if (serverState.status) {
    setStatus(serverState.status, false);
  }
}

// ============================================
// PLAYER ACTIONS (server-backed)
// ============================================

function joinSeat(seat) {
  sendAction("join", { seat }, "button");
}

function leaveSeat() {
  sendAction("leave", {}, "button");
}

function toggleSoloMode() {
  sendAction("solo", {}, "button");
}

function resetTable() {
  sendAction("reset", {}, "button");
}

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

ui.joinSeat1Btn.addEventListener("click", () => {
  animateButtonPress(ui.joinSeat1Btn);
  joinSeat(1);
});
ui.joinSeat2Btn.addEventListener("click", () => {
  animateButtonPress(ui.joinSeat2Btn);
  joinSeat(2);
});
ui.leaveSeatBtn.addEventListener("click", () => {
  animateButtonPress(ui.leaveSeatBtn);
  leaveSeat();
});
ui.soloModeBtn.addEventListener("click", () => {
  animateButtonPress(ui.soloModeBtn);
  toggleSoloMode();
});
ui.resetTableBtn.addEventListener("click", () => {
  animateButtonPress(ui.resetTableBtn);
  resetTable();
});
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

window.addEventListener("beforeunload", () => {
  if (!serverState || !serverState.yourSeat) {
    return;
  }
  if (serverState.phase === "player-turns" || serverState.phase === "dealer-turn") {
    return;
  }
  navigator.sendBeacon(
    `${API_BASE}/leave`,
    new Blob([JSON.stringify({ clientId: localClientId })], { type: "application/json" })
  );
});

// Initial sync + multiplayer polling (also serves as the server heartbeat).
pollState();
setInterval(pollState, POLL_INTERVAL_MS);

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

// Animate card flip
function animateCardFlip(cardElement) {
  if (!window.anime) return;
  anime({
    targets: cardElement,
    rotateY: [0, 180],
    duration: 400,
    easing: "easeInOutQuad",
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

// Animate player area highlight
function animatePlayerHighlight(playerArea) {
  if (!window.anime) return;
  anime({
    targets: playerArea,
    borderColor: ["rgba(112, 255, 58, 0.2)", "rgba(112, 255, 58, 0.8)", "rgba(112, 255, 58, 0.2)"],
    duration: 500,
    easing: "easeInOutQuad",
  });
}

// Animate blackjack celebration
function animateBlackjackWin(element) {
  if (!window.anime) return;
  anime({
    targets: element,
    scale: [1, 1.25, 1],
    rotate: [0, 10, -10, 0],
    duration: 800,
    easing: "easeOutElastic(1, .6)",
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

// Animate hand total increase
function animateTotalIncrease(totalElement) {
  if (!window.anime) return;
  anime({
    targets: totalElement,
    scale: [1, 1.2, 1],
    color: ["#ff952a", "#70ff3a", "#ff952a"],
    duration: 400,
    easing: "easeOutQuad",
  });
}

// Animate bust shake
function animateBustShake(playerArea) {
  if (!window.anime) return;
  anime({
    targets: playerArea,
    translateX: [-8, 8, -8, 8, 0],
    duration: 400,
    easing: "easeInOutQuad",
  });
}

// Animate smooth panel transitions
function animatePanelSlideIn(panelElement) {
  if (!window.anime) return;
  anime.set(panelElement, {
    opacity: 0,
    translateX: -30,
  });
  anime({
    targets: panelElement,
    opacity: 1,
    translateX: 0,
    duration: 500,
    easing: "easeOutQuad",
  });
}

// Smooth color transition for status states
function animateStatusColor(statusElement, newColor) {
  if (!window.anime) return;
  anime({
    targets: statusElement,
    color: newColor,
    duration: 400,
    easing: "easeOutQuad",
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

// Bounce effect for seat status changes
function animateSeatBounce(seatElement) {
  if (!window.anime) return;
  anime({
    targets: seatElement,
    translateY: [0, -8, 0],
    duration: 400,
    easing: "easeOutQuad",
  });
}
