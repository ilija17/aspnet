const API_BASE = "/api/slot";
const LOGIN_URL = "/Account/Login?returnUrl=/slot/index.html";
const PROFILE_URL = "/Account/Profile";

const REELS = 5;
const ROWS = 3;

// Visual presentation for each symbol key. Every tile is ORIGINAL inline-SVG
// art in the charm/ornate-card genre — no emoji, no bare letters, and nothing
// traced from any real Novomatic game. `lady` is wild + scatter and special.
//
// High "charm" symbols → detailed icon tiles.
// Low symbols (ace/king/queen/jack/ten) → ornate gold playing-card faces.
const SYMBOL_ART = {
  lady: { kind: "lady", svg: svgLady },
  clover: { kind: "pic charm clover", svg: svgClover },
  ladybug: { kind: "pic charm ladybug", svg: svgLadybug },
  horseshoe: { kind: "pic charm horseshoe", svg: svgHorseshoe },
  coin: { kind: "pic charm coin", svg: svgCoin },
  ace: { kind: "card ace", svg: () => svgCard("A", "heart") },
  king: { kind: "card king", svg: () => svgCard("K", "spade") },
  queen: { kind: "card queen", svg: () => svgCard("Q", "diamond") },
  jack: { kind: "card jack", svg: () => svgCard("J", "club") },
  ten: { kind: "card ten", svg: () => svgCard("10", "heart") },
};

// Fallback symbol order, only used to build idle reels before the first state.
const FALLBACK_KEYS = ["lady", "clover", "ladybug", "horseshoe", "coin", "ace", "king", "queen", "jack", "ten"];

// ============================================
// ORIGINAL SYMBOL ART (inline SVG, self-contained)
// ============================================

// Ornate card-rank face: gilded rank glyph on a gem-cut card with a suit
// flourish. Original design — not copied from any commercial slot art.
function svgCard(rank, suit) {
  const suitPaths = {
    heart: "M16 13.4c-1.4-3-6.2-2.6-6.2 1.1 0 2.7 3.4 5 6.2 7 2.8-2 6.2-4.3 6.2-7 0-3.7-4.8-4.1-6.2-1.1z",
    diamond: "M16 7l5.4 8L16 23l-5.4-8z",
    spade: "M16 7c2.2 3 6.2 5 6.2 8.4 0 2.4-2 3.8-3.9 3.4.3 1.4 1 2.4 2.1 3.2h-8.8c1.1-.8 1.8-1.8 2.1-3.2-1.9.4-3.9-1-3.9-3.4C9.8 12 13.8 10 16 7z",
    club: "M16 8.2a2.9 2.9 0 1 1-2.4 4.5 2.9 2.9 0 1 1-1.1 5.4 2.9 2.9 0 1 1 6.9 0 2.9 2.9 0 1 1-1.1-5.4A2.9 2.9 0 0 1 16 8.2zM15 19h2c-.1 1.5.4 2.7 1.6 3.6h-5.2c1.2-.9 1.7-2.1 1.6-3.6z",
  };
  const fs = rank.length > 1 ? 26 : 34;
  return `
  <svg viewBox="0 0 56 56" class="sym-svg" aria-hidden="true">
    <defs>
      <linearGradient id="cardface" x1="0" y1="0" x2="0" y2="1">
        <stop offset="0" stop-color="#26304f"/>
        <stop offset="1" stop-color="#0f1326"/>
      </linearGradient>
      <linearGradient id="gildrank" x1="0" y1="0" x2="0" y2="1">
        <stop offset="0" stop-color="#fff3c4"/>
        <stop offset="0.5" stop-color="#ffd24a"/>
        <stop offset="1" stop-color="#b9831f"/>
      </linearGradient>
    </defs>
    <rect x="4" y="3" width="48" height="50" rx="8" fill="url(#cardface)" stroke="#ffd24a" stroke-opacity="0.55" stroke-width="2"/>
    <rect x="7" y="6" width="42" height="44" rx="6" fill="none" stroke="#ffd24a" stroke-opacity="0.22"/>
    <path d="M11 10c4-2 8-2 12 0M45 46c-4 2-8 2-12 0" stroke="#ffd24a" stroke-opacity="0.4" stroke-width="1.2" fill="none" stroke-linecap="round"/>
    <g transform="translate(28 14) scale(0.42)" fill="url(#gildrank)" opacity="0.9">
      <path d="${suitPaths[suit]}" transform="translate(-16 -15)"/>
    </g>
    <text x="28" y="40" text-anchor="middle" font-family="Georgia, 'Times New Roman', serif" font-weight="900" font-size="${fs}" fill="url(#gildrank)" stroke="#7a5410" stroke-width="0.8">${rank}</text>
  </svg>`;
}

// Four-leaf clover charm.
function svgClover() {
  return `
  <svg viewBox="0 0 56 56" class="sym-svg" aria-hidden="true">
    <defs><radialGradient id="leaf" cx="0.4" cy="0.35" r="0.8">
      <stop offset="0" stop-color="#b6ff7a"/><stop offset="1" stop-color="#2f8a35"/>
    </radialGradient></defs>
    <g transform="translate(28 27)">
      <g fill="url(#leaf)" stroke="#1c5c22" stroke-width="1.2">
        <path d="M0 -2C-3 -13 -16 -11 -13 -2 -11 4 -3 4 0 -2z"/>
        <path d="M2 0C13 -3 11 -16 2 -13 -4 -11 -4 -3 2 0z"/>
        <path d="M0 2C3 13 16 11 13 2 11 -4 3 -4 0 2z"/>
        <path d="M-2 0C-13 3 -11 16 -2 13 4 11 4 3 -2 0z"/>
      </g>
      <path d="M1 2C3 10 4 16 5 20" stroke="#2f8a35" stroke-width="2.2" fill="none" stroke-linecap="round"/>
      <circle cx="0" cy="0" r="2.4" fill="#e9ffce"/>
    </g>
  </svg>`;
}

// Ladybug charm.
function svgLadybug() {
  return `
  <svg viewBox="0 0 56 56" class="sym-svg" aria-hidden="true">
    <defs><radialGradient id="shell" cx="0.4" cy="0.3" r="0.9">
      <stop offset="0" stop-color="#ff7a7a"/><stop offset="1" stop-color="#c11616"/>
    </radialGradient></defs>
    <g transform="translate(28 29)">
      <ellipse cx="0" cy="0" rx="16" ry="17" fill="url(#shell)" stroke="#7a0d0d" stroke-width="1.5"/>
      <path d="M0 -17V16" stroke="#1a0606" stroke-width="2"/>
      <circle cx="0" cy="-14" r="6" fill="#160606"/>
      <g fill="#2a0808">
        <circle cx="-7" cy="-4" r="2.6"/><circle cx="7" cy="-4" r="2.6"/>
        <circle cx="-9" cy="5" r="2.4"/><circle cx="9" cy="5" r="2.4"/>
        <circle cx="-5" cy="11" r="2.2"/><circle cx="5" cy="11" r="2.2"/>
      </g>
      <circle cx="-2.4" cy="-15" r="1.3" fill="#ffd24a"/>
      <circle cx="2.4" cy="-15" r="1.3" fill="#ffd24a"/>
    </g>
  </svg>`;
}

// Lucky horseshoe charm.
function svgHorseshoe() {
  return `
  <svg viewBox="0 0 56 56" class="sym-svg" aria-hidden="true">
    <defs><linearGradient id="shoe" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#fff3c4"/><stop offset="0.5" stop-color="#ffd24a"/><stop offset="1" stop-color="#a8761c"/>
    </linearGradient></defs>
    <path d="M16 46V28a12 12 0 0 1 24 0v18" fill="none" stroke="url(#shoe)" stroke-width="7" stroke-linecap="round"/>
    <path d="M16 46V28a12 12 0 0 1 24 0v18" fill="none" stroke="#7a5410" stroke-opacity="0.45" stroke-width="7.6" stroke-linecap="round" transform="translate(0 1)" style="mix-blend-mode:multiply"/>
    <g fill="#3a2a08">
      <circle cx="20" cy="40" r="1.5"/><circle cx="36" cy="40" r="1.5"/>
      <circle cx="18.5" cy="32" r="1.5"/><circle cx="37.5" cy="32" r="1.5"/>
      <circle cx="22" cy="25" r="1.5"/><circle cx="34" cy="25" r="1.5"/>
    </g>
  </svg>`;
}

// Gold coin charm.
function svgCoin() {
  return `
  <svg viewBox="0 0 56 56" class="sym-svg" aria-hidden="true">
    <defs><radialGradient id="coing" cx="0.4" cy="0.35" r="0.85">
      <stop offset="0" stop-color="#fff6cf"/><stop offset="0.6" stop-color="#ffd24a"/><stop offset="1" stop-color="#a8761c"/>
    </radialGradient></defs>
    <circle cx="28" cy="28" r="20" fill="url(#coing)" stroke="#7a5410" stroke-width="2"/>
    <circle cx="28" cy="28" r="15" fill="none" stroke="#7a5410" stroke-opacity="0.5" stroke-width="1.5"/>
    <text x="28" y="37" text-anchor="middle" font-family="Georgia, serif" font-weight="900" font-size="22" fill="#7a5410">$</text>
  </svg>`;
}

// Lucky Lady — the shimmering gold wild + scatter. Original stylised charm
// portrait (gem + radiant crown), clearly distinct from the card and pip art.
function svgLady() {
  return `
  <svg viewBox="0 0 56 56" class="sym-svg" aria-hidden="true">
    <defs>
      <radialGradient id="ladyhalo" cx="0.5" cy="0.4" r="0.7">
        <stop offset="0" stop-color="#fff7d6"/><stop offset="0.6" stop-color="#ffd24a"/><stop offset="1" stop-color="#b07c1c"/>
      </radialGradient>
      <linearGradient id="ladygem" x1="0" y1="0" x2="0" y2="1">
        <stop offset="0" stop-color="#ffb3e6"/><stop offset="1" stop-color="#9b2fcf"/>
      </linearGradient>
    </defs>
    <g transform="translate(28 28)">
      <g class="lady-rays" stroke="url(#ladyhalo)" stroke-width="1.6" stroke-linecap="round">
        ${Array.from({ length: 12 }, (_, i) => {
          const a = (i * Math.PI) / 6;
          const x1 = Math.cos(a) * 17, y1 = Math.sin(a) * 17;
          const x2 = Math.cos(a) * 23, y2 = Math.sin(a) * 23;
          return `<line x1="${x1.toFixed(1)}" y1="${y1.toFixed(1)}" x2="${x2.toFixed(1)}" y2="${y2.toFixed(1)}"/>`;
        }).join("")}
      </g>
      <circle cx="0" cy="0" r="16" fill="url(#ladyhalo)" stroke="#7a5410" stroke-width="1.5"/>
      <path d="M-11 -7l4 5 5-7 5 7 4-5-2 9h-18z" fill="#ffe9a3" stroke="#7a5410" stroke-width="0.8"/>
      <path d="M0 -1l6 10-6 5-6-5z" fill="url(#ladygem)" stroke="#fff" stroke-opacity="0.6" stroke-width="0.7"/>
      <circle cx="0" cy="3" r="1.6" fill="#fff7d6"/>
    </g>
  </svg>`;
}

let serverState = null;
let lastRenderedVersion = -1;
let selectedBet = null; // client-side: chosen total bet (decimal)
let busy = false; // a POST is in flight
let spinning = false; // a full round (incl. free spins) is replaying
let showResultBanner = false;
let audioContext;

// Spin-speed mode: "normal" | "fast" | "instant" (persisted in localStorage).
let speedMode = "normal";

// Autospin sequence state.
let autoActive = false;
let autoRemaining = 0; // remaining paid spins; -1 === infinite/until-stop
let autoSelectedCount = 10; // currently selected count button value

// Red/black gamble (double-or-nothing) UI state.
let gambleOpen = false; // the gamble panel is showing
let gambleBusy = false; // a gamble POST / reveal animation is in flight

const ui = {
  table: document.getElementById("table"),
  gate: document.getElementById("gate"),
  gateMessage: document.getElementById("gate-message"),
  gateLink: document.getElementById("gate-link"),
  playerName: document.getElementById("player-name"),
  balance: document.getElementById("balance"),
  totalBet: document.getElementById("total-bet"),
  spinsCount: document.getElementById("spins-count"),
  featureCount: document.getElementById("feature-count"),
  muteBtn: document.getElementById("mute-btn"),
  reels: document.getElementById("reels"),
  paylineOverlay: document.getElementById("payline-overlay"),
  freeBanner: document.getElementById("free-banner"),
  freeBannerCount: document.getElementById("free-banner-count"),
  reelsFrame: null, // set after build
  winMeter: document.getElementById("win-meter"),
  winMeterValue: document.getElementById("win-meter-value"),
  status: document.getElementById("status"),
  roundResult: document.getElementById("round-result"),
  betDisplay: document.getElementById("bet-display"),
  betValue: document.getElementById("bet-value"),
  betDown: document.getElementById("bet-down"),
  betUp: document.getElementById("bet-up"),
  betMin: document.getElementById("bet-min"),
  betMax: document.getElementById("bet-max"),
  spinBtn: document.getElementById("spin-btn"),
  paytable: document.getElementById("paytable"),
  vfxLayer: document.getElementById("vfx-layer"),
  // Speed control
  speedControl: document.getElementById("speed-control"),
  speedModes: Array.from(document.querySelectorAll(".speed-mode")),
  // Autospin
  autospin: document.getElementById("autospin"),
  autospinBtn: document.getElementById("autospin-btn"),
  autostopBtn: document.getElementById("autostop-btn"),
  autospinRemaining: document.getElementById("autospin-remaining"),
  autoCounts: Array.from(document.querySelectorAll(".auto-count")),
  // Gamble (red/black)
  gamblePanel: document.getElementById("gamble-panel"),
  gambleStakeLabel: document.getElementById("gamble-stake-label"),
  gambleStakeValue: document.getElementById("gamble-stake-value"),
  gambleCard: document.getElementById("gamble-card"),
  gambleCardInner: document.querySelector("#gamble-card .gamble-card-inner"),
  gambleCardFront: document.getElementById("gamble-card-front"),
  gambleCardRank: document.getElementById("gamble-card-rank"),
  gambleCardSuit: document.getElementById("gamble-card-suit"),
  gambleMsg: document.getElementById("gamble-msg"),
  gambleRed: document.getElementById("gamble-red"),
  gambleBlack: document.getElementById("gamble-black"),
  gambleCollect: document.getElementById("gamble-collect"),
  gambleHistory: document.getElementById("gamble-history"),
};

// reelCells[reel][row] -> the .cell element currently visible in that position.
const reelCells = [];

// ============================================
// REEL CONSTRUCTION
// ============================================

function symbolArt(key) {
  return SYMBOL_ART[key] || { kind: "pic", svg: () => `<span class="sym-fallback">${key}</span>` };
}

function paintPip(pip, key) {
  const art = symbolArt(key);
  pip.className = `pip ${art.kind}`;
  pip.innerHTML = typeof art.svg === "function" ? art.svg() : art.svg || "";
}

function makeCell(key) {
  const cell = document.createElement("div");
  cell.className = "cell";
  cell.dataset.key = key;
  const pip = document.createElement("span");
  paintPip(pip, key);
  cell.appendChild(pip);
  return cell;
}

function buildReels(initialGrid) {
  ui.reels.innerHTML = "";
  reelCells.length = 0;
  for (let r = 0; r < REELS; r += 1) {
    const reel = document.createElement("div");
    reel.className = "reel";
    reel.dataset.reelIndex = String(r);
    const strip = document.createElement("div");
    strip.className = "reel-strip";
    reel.appendChild(strip);
    ui.reels.appendChild(reel);

    const cells = [];
    for (let row = 0; row < ROWS; row += 1) {
      const key = initialGrid ? initialGrid[r][row] : FALLBACK_KEYS[(r + row) % FALLBACK_KEYS.length];
      const cell = makeCell(key);
      strip.appendChild(cell);
      cells.push(cell);
    }
    reelCells.push(cells);
  }
  ui.reelsFrame = ui.reels.closest(".reels-frame");
}

buildReels(null);

function setCellSymbol(cell, key) {
  cell.dataset.key = key;
  paintPip(cell.querySelector(".pip"), key);
}

function setReelGrid(grid) {
  for (let r = 0; r < REELS; r += 1) {
    for (let row = 0; row < ROWS; row += 1) {
      setCellSymbol(reelCells[r][row], grid[r][row]);
    }
  }
}

function clearCellStates() {
  for (let r = 0; r < REELS; r += 1) {
    for (let row = 0; row < ROWS; row += 1) {
      reelCells[r][row].classList.remove("win", "dim", "scatter-hit");
    }
  }
  hidePaylines();
}

// ============================================
// SERVER API LAYER (authenticated, singleplayer)
// ============================================

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
  if (busy || spinning || autoActive) {
    return;
  }
  try {
    applyServerState(await apiGetState());
  } catch (error) {
    if (!(error instanceof HandledApiError)) {
      console.error("Slot error:", error);
      setStatus("Something went wrong. Try again.", true);
    }
  }
}

// ============================================
// PLAYER ACTIONS
// ============================================

const BET_LADDER_FALLBACK = [0.1, 0.2, 0.5, 1, 2, 5, 10, 20, 50, 100];

function allowedBets() {
  const list = serverState?.allowedBets;
  return (Array.isArray(list) && list.length ? list : BET_LADDER_FALLBACK).map(Number);
}

// Index of `value` in the allowed-bet ladder, with a cents-level tolerance so
// decimal round-trips (e.g. 0.1) match reliably.
function betIndex(value) {
  const bets = allowedBets();
  const v = Number(value);
  for (let i = 0; i < bets.length; i += 1) {
    if (Math.abs(bets[i] - v) < 0.0001) {
      return i;
    }
  }
  return -1;
}

async function setBet(amount) {
  if (busy || spinning || autoActive || !serverState) {
    return;
  }
  if (betIndex(amount) < 0) {
    return;
  }
  busy = true;
  selectedBet = Number(amount);
  playSound("bet");
  triggerActionFx("bet");
  renderBetSelector();
  try {
    applyServerState(await apiPost("bet", { amount: Number(amount) }));
  } catch (error) {
    if (!(error instanceof HandledApiError)) {
      console.error("Slot error:", error);
      setStatus("Something went wrong. Try again.", true);
    }
  } finally {
    busy = false;
    updateControls();
  }
}

// Walk the allowed-bet ladder by `dir` (−1/+1) from the current bet.
function stepBet(dir) {
  const bets = allowedBets();
  let idx = betIndex(effectiveBet());
  if (idx < 0) {
    idx = 0;
  }
  const next = Math.min(bets.length - 1, Math.max(0, idx + dir));
  if (next !== idx) {
    setBet(bets[next]);
  }
}

async function spin() {
  if (busy || spinning || !serverState || !serverState.canSpin) {
    return false;
  }
  busy = true;
  spinning = true;
  updateControls();
  clearCellStates();
  hideWinMeter();
  ui.roundResult.textContent = "";
  ui.roundResult.className = "round-result hidden";
  playSound("spin");
  triggerActionFx("spin");
  setStatus("Spinning…");

  let result;
  try {
    result = await apiPost("spin", {});
  } catch (error) {
    busy = false;
    spinning = false;
    updateControls();
    render(true);
    if (!(error instanceof HandledApiError)) {
      console.error("Slot error:", error);
      setStatus("Something went wrong. Try again.", true);
    }
    return false;
  }

  busy = false;
  const round = result.round;

  if (!round || !Array.isArray(round.spins) || round.spins.length === 0) {
    spinning = false;
    showResultBanner = true;
    applyServerState(result);
    updateControls();
    return { ok: true, feature: false };
  }

  await playRound(round);

  // Reveal the authoritative final state (real balance, counters) after playback.
  spinning = false;
  showResultBanner = true;
  applyServerState(result);
  updateControls();

  if (round.result === "win") {
    playSound("win");
    triggerActionFx("win");
  } else {
    playSound("lose");
  }

  // Manual winning spins: offer the red/black gamble (never during autospin —
  // autospin just keeps spinning and the next spin clears the offer).
  const gamble = result.gamble;
  if (!autoActive && round.result === "win" && gamble && Number(gamble.offer) > 0) {
    openGambleOffer(gamble);
  }

  return { ok: true, feature: !!round.featureTriggered };
}

// ============================================
// ROUND PLAYBACK (one-shot; the client replays the recorded spins)
// ============================================

const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

// Per-mode animation timings. Fast ≈ half of Normal; Instant skips travel.
const SPEED_PROFILES = {
  normal: { spinUp: 240, sustain: 520, firstStop: 360, stagger: 170, settle: 320, anticip: 460, betweenFree: 360, winHold: 950, noWinHold: 520 },
  fast: { spinUp: 120, sustain: 240, firstStop: 180, stagger: 85, settle: 200, anticip: 240, betweenFree: 180, winHold: 520, noWinHold: 280 },
  instant: { spinUp: 0, sustain: 0, firstStop: 0, stagger: 0, settle: 0, anticip: 0, betweenFree: 90, winHold: 360, noWinHold: 160 },
};

function speed() {
  return SPEED_PROFILES[speedMode] || SPEED_PROFILES.normal;
}

const FX_KEYS = ["lady", "clover", "ladybug", "horseshoe", "coin", "ace", "king", "queen", "jack", "ten"];
const randKey = () => FX_KEYS[(Math.random() * FX_KEYS.length) | 0];

async function playRound(round) {
  let runningWin = 0;
  setReelsFeature(false);
  hideFreeBanner();

  // Base spin.
  runningWin = await playSpin(round.spins[0], runningWin, { free: false });

  // Feature: replay the free spins with distinct presentation. The banner
  // shows "Spin X / Y" where Y grows as retriggers land (freeSpinsAdded).
  if (round.featureTriggered && round.spins.length > 1) {
    const initialAward = Number(round.spins[0].freeSpinsAdded) || 15;
    await announceFeature(initialAward);
    setReelsFeature(true);
    startFreeSpinMusic();
    let totalAwarded = initialAward;
    const freeSpins = round.spins.slice(1);
    for (let i = 0; i < freeSpins.length; i += 1) {
      showFreeBanner(i + 1, totalAwarded);
      runningWin = await playSpin(freeSpins[i], runningWin, { free: true });
      const added = Number(freeSpins[i].freeSpinsAdded) || 0;
      if (added > 0) {
        totalAwarded += added;
        showFreeBanner(i + 1, totalAwarded);
        await announceRetrigger(added);
      }
      await sleep(speed().betweenFree);
    }
    stopFreeSpinMusic();
    hideFreeBanner();
    setReelsFeature(false);
  }
}

async function playSpin(spin, runningWin, { free }) {
  clearCellStates();
  if (free) {
    await starRevealTo(spin.grid);
  } else {
    await spinReelsTo(spin.grid);
  }

  // Highlight scatters (lucky ladies anywhere).
  if (spin.scatterCount >= 3) {
    flagScatters(spin.grid);
    playSound("scatter");
    triggerActionFx("scatter");
  }

  // Line wins: draw paylines, light up winning cells, tally up.
  const lineWins = spin.lineWins || [];
  if (lineWins.length > 0) {
    dimAll();
    for (const w of lineWins) {
      drawPayline(w);
      lightLineWin(w);
    }
    playSound("lineWin");
  }

  const spinWin = Number(spin.spinWin || 0);
  if (spinWin > 0) {
    runningWin = await tallyWin(runningWin, runningWin + spinWin);
    showWinMeter(runningWin);
  }

  setStatus(
    free
      ? `Free spin — won ${formatMoney(spinWin)} (round total ${formatMoney(runningWin)})`
      : spinWin > 0
        ? `Win ${formatMoney(spinWin)} on this spin!`
        : "No win on this spin."
  );

  const sp = speed();
  await sleep(lineWins.length > 0 || spin.scatterCount >= 3 ? sp.winHold : sp.noWinHold);
  return runningWin;
}

// Animate every reel like a real machine, with one continuous JS-driven
// transform per strip: the strip keeps the CURRENTLY visible symbols in the
// window on frame 1 (no spawn pop), accelerates, cruises with motion blur —
// symbols falling DOWNWARD through the window like a physical reel — then the
// reels stop left-to-right with a gentle overshoot-and-settle bounce onto the
// final `grid`. The old version handed off between a looping CSS keyframe and
// an inline transform, which snapped the strip to a cell boundary at the
// moment of the handoff — that was the visible glitch.
// Anticipation slows the remaining reels once 2 scatter Ladies have landed.
const SETTLE_BACK = 1.2; // easeOutBack overshoot strength for the reel settle

function easeOutBackGentle(t) {
  const c1 = SETTLE_BACK;
  const c3 = c1 + 1;
  return 1 + c3 * Math.pow(t - 1, 3) + c1 * Math.pow(t - 1, 2);
}

async function spinReelsTo(grid) {
  const reelEls = Array.from(ui.reels.querySelectorAll(".reel"));
  const sp = speed();

  // Instant mode: no reel travel, just reveal the final grid.
  if (speedMode === "instant") {
    setReelGrid(grid);
    reelEls.forEach((reel) => reelStopBounce(reel));
    playSound("reelStop");
    return;
  }

  const cellH = reelCells[0][0] ? reelCells[0][0].getBoundingClientRect().height : 0;
  if (!cellH) {
    // Layout not measurable (hidden tab etc.) — just show the result.
    setReelGrid(grid);
    playSound("reelStop");
    return;
  }

  spinReelWhir();

  // Anticipation is known up front (the final grid is already decided):
  // once 2 Ladies sit on earlier reels, later reels drag out their stop.
  let ladies = 0;
  const anticipates = [];
  for (let r = 0; r < REELS; r += 1) {
    anticipates.push(ladies >= 2 && r >= 2);
    if (grid[r].includes("lady")) ladies += 1;
  }

  const msPerCell = speedMode === "fast" ? 42 : 58; // cruise pace per symbol
  let extraDelay = 0;
  const runs = [];
  for (let r = 0; r < REELS; r += 1) {
    if (anticipates[r]) {
      extraDelay += sp.anticip;
    }
    runs.push(
      spinOneReel(reelEls[r], grid[r], {
        spinUp: Math.max(80, sp.spinUp),
        cruise: sp.sustain + sp.firstStop + sp.stagger * r + extraDelay,
        settle: sp.settle + 180,
        cellH,
        vmaxBase: cellH / msPerCell,
        anticipating: anticipates[r],
        anticipDur: sp.anticip,
      })
    );
  }
  await Promise.all(runs);
}

// One reel: quadratic spin-up → linear cruise → easeOutBack settle, all as a
// single translateY tween so velocity is continuous at every phase boundary.
function spinOneReel(reelEl, column, cfg) {
  return new Promise((resolve) => {
    const { spinUp, cruise, settle, cellH, vmaxBase } = cfg;
    const strip = reelEl.querySelector(".reel-strip");
    const reelIdx = Number(reelEl.dataset.reelIndex);
    const currentKeys = reelCells[reelIdx].map((cell) => cell.dataset.key || randKey());

    // Snap total travel to a whole number of cells so the final rows land
    // exactly in the window, then derive the exact cruise velocity from it.
    const timeFactor = 0.5 * spinUp + cruise + settle / (SETTLE_BACK + 3);
    const cells = Math.max(6, Math.min(80, Math.round((vmaxBase * timeFactor) / cellH)));
    const travel = cells * cellH;
    const v = travel / timeFactor;
    const distAccel = 0.5 * v * spinUp;
    const distSettle = (v * settle) / (SETTLE_BACK + 3);
    const cruiseDist = travel - distAccel - distSettle;
    const totalDur = spinUp + cruise + settle;

    // Rebuild the strip for downward travel, top to bottom: one spare row (it
    // peeks in during the settle overshoot), the final column, random filler,
    // then the currently visible symbols — the window starts parked on those,
    // so frame 1 is pixel-identical to the resting reel. The strip is
    // absolutely positioned (see .reel-strip), so its length never stretches
    // the reel window.
    const startOffset = (cells + 1) * cellH; // window sits on the current symbols
    strip.style.transition = "none";
    strip.innerHTML = "";
    strip.appendChild(makeCell(randKey()));
    for (let row = 0; row < ROWS; row += 1) {
      strip.appendChild(makeCell(column[row]));
    }
    for (let i = 0; i < cells - ROWS; i += 1) {
      strip.appendChild(makeCell(randKey()));
    }
    currentKeys.forEach((key) => strip.appendChild(makeCell(key)));
    strip.style.transform = `translateY(${-startOffset}px)`;

    if (cfg.anticipating) {
      const glowAt = Math.max(0, totalDur - settle - cfg.anticipDur);
      setTimeout(() => reelEl.classList.add("anticipate"), glowAt);
    }

    reelEl.classList.add("spinning");

    let finished = false;
    const finish = () => {
      if (finished) {
        return;
      }
      finished = true;
      reelEl.classList.remove("spinning", "anticipate");
      strip.style.transition = "none";
      strip.style.transform = "translateY(0)";
      strip.innerHTML = "";
      for (let row = 0; row < ROWS; row += 1) {
        const cell = makeCell(column[row]);
        strip.appendChild(cell);
        reelCells[reelIdx][row] = cell;
      }
      reelStopBounce(reelEl);
      playSound("reelStop");
      resolve();
    };

    const start = performance.now();
    const frame = (now) => {
      if (finished) {
        return;
      }
      const t = now - start;
      if (t >= totalDur) {
        finish();
        return;
      }
      let y;
      if (t <= spinUp) {
        y = (0.5 * v * t * t) / spinUp;
      } else if (t <= spinUp + cruise) {
        y = distAccel + v * (t - spinUp);
      } else {
        const s = (t - spinUp - cruise) / settle;
        y = distAccel + cruiseDist + distSettle * easeOutBackGentle(s);
        // Drop the motion blur early in the settle so the symbols land readable.
        if (s > 0.12) {
          reelEl.classList.remove("spinning");
        }
      }
      // y grows 0 → travel; mapping it against startOffset moves the strip
      // downward from the current symbols onto the finals (which end at
      // translateY(-cellH), the row right under the spare overshoot cell).
      strip.style.transform = `translateY(${y - startOffset}px)`;
      requestAnimationFrame(frame);
    };
    requestAnimationFrame(frame);
    // Safety net (rAF pauses in background tabs): never leave a reel hanging.
    setTimeout(finish, totalDur + 400);
  });
}

function flagScatters(grid) {
  for (let r = 0; r < REELS; r += 1) {
    for (let row = 0; row < ROWS; row += 1) {
      if (grid[r][row] === "lady") {
        reelCells[r][row].classList.add("win", "scatter-hit");
      }
    }
  }
}

function dimAll() {
  for (let r = 0; r < REELS; r += 1) {
    for (let row = 0; row < ROWS; row += 1) {
      reelCells[r][row].classList.add("dim");
    }
  }
}

function lightLineWin(w) {
  // cells[] holds the winning row index per reel, length === count.
  (w.cells || []).forEach((row, reel) => {
    const cell = reelCells[reel]?.[row];
    if (cell) {
      cell.classList.remove("dim");
      cell.classList.add("win");
    }
  });
}

// ============================================
// PAYLINE OVERLAY (SVG polylines mapped to cell centres)
// ============================================

const PAYLINE_COLORS = [
  "#70ff3a", "#ff53b8", "#ffd24a", "#4ecdc4", "#ff6b6b",
  "#b06bff", "#c0ff5a", "#ff952a", "#6bd0ff", "#ff8ad1",
];

function cellCenterPct(reel, row) {
  // Coordinates in the overlay's own 0..100 viewBox space.
  const x = ((reel + 0.5) / REELS) * 100;
  const y = ((row + 0.5) / ROWS) * 100;
  return [x, y];
}

function drawPayline(w) {
  const overlay = ui.paylineOverlay;
  if (!overlay.getAttribute("viewBox")) {
    overlay.setAttribute("viewBox", "0 0 100 100");
  }
  const color = PAYLINE_COLORS[((w.line || 1) - 1) % PAYLINE_COLORS.length];
  const pts = (w.cells || []).map((row, reel) => cellCenterPct(reel, row).join(",")).join(" ");
  const poly = document.createElementNS("http://www.w3.org/2000/svg", "polyline");
  poly.setAttribute("points", pts);
  poly.setAttribute("stroke", color);
  poly.setAttribute("stroke-dasharray", "600");
  poly.style.color = color;
  overlay.appendChild(poly);
  // force reflow so the draw animation runs
  void poly.getBBox();
  poly.classList.add("show");
}

function hidePaylines() {
  ui.paylineOverlay.innerHTML = "";
}

// ============================================
// WIN METER + TALLY
// ============================================

function showWinMeter(value) {
  ui.winMeter.classList.remove("hidden");
  ui.winMeterValue.textContent = formatMoney(value);
}

function hideWinMeter() {
  ui.winMeter.classList.add("hidden");
  ui.winMeterValue.textContent = formatMoney(0);
}

// Count the meter up from `from` to `to`, with a coin-tinkle as it climbs.
function tallyWin(from, to) {
  return new Promise((resolve) => {
    const duration = 650;
    const start = performance.now();
    showWinMeter(from);
    let lastTick = 0;
    const step = (now) => {
      const t = Math.min(1, (now - start) / duration);
      const eased = 1 - Math.pow(1 - t, 3);
      const value = from + (to - from) * eased;
      ui.winMeterValue.textContent = formatMoney(value);
      if (now - lastTick > 60) {
        lastTick = now;
        playSound("coin");
      }
      if (t < 1) {
        requestAnimationFrame(step);
      } else {
        ui.winMeterValue.textContent = formatMoney(to);
        animateWinMeterPop();
        resolve(to);
      }
    };
    requestAnimationFrame(step);
  });
}

// ============================================
// FEATURE / FREE-SPINS PRESENTATION
// ============================================

function setReelsFeature(on) {
  if (ui.reelsFrame) {
    ui.reelsFrame.classList.toggle("feature", on);
  }
}

// "Spin X / Y" — Y is the total awarded so far and grows on retriggers.
function showFreeBanner(current, total) {
  ui.freeBanner.classList.remove("hidden");
  ui.freeBannerCount.textContent = `Spin ${current} / ${total}`;
  animateFreeBannerTick();
}

function hideFreeBanner() {
  ui.freeBanner.classList.add("hidden");
}

async function announceFeature(awarded) {
  setStatus(`FEATURE! ${awarded} FREE SPINS`);
  playSound("feature");
  triggerActionFx("feature");
  ui.roundResult.textContent = `${awarded} FREE SPINS`;
  ui.roundResult.className = "round-result win";
  animateRoundResult(ui.roundResult);
  await sleep(1500);
  ui.roundResult.textContent = "";
  ui.roundResult.className = "round-result hidden";
}

// Celebratory "+1 / +2 / +15 FREE SPINS" pop when a free spin retriggers.
async function announceRetrigger(added) {
  playSound("feature");
  triggerActionFx("scatter");
  const pop = document.createElement("div");
  pop.className = "retrigger-pop";
  pop.textContent = `+${added} FREE SPIN${added === 1 ? "" : "S"}`;
  (ui.reelsFrame || ui.reels).appendChild(pop);
  const hold = speedMode === "instant" ? 420 : speedMode === "fast" ? 700 : 1150;
  await sleep(hold);
  pop.classList.add("out");
  setTimeout(() => pop.remove(), 400);
}

// ============================================
// STARRY FREE-SPIN REVEAL
// In free spins the result appears under a twinkling star field: every cell
// is covered, then the covers fade away one at a time — left-to-right by
// column and bottom-to-top within each column — revealing the symbols.
// ============================================

function makeStarCover(cell) {
  const cover = document.createElement("div");
  cover.className = "star-cover";
  for (let i = 0; i < 6; i += 1) {
    const star = document.createElement("span");
    star.className = "star";
    star.style.left = `${8 + Math.random() * 78}%`;
    star.style.top = `${8 + Math.random() * 78}%`;
    star.style.animationDelay = `${(Math.random() * 1.2).toFixed(2)}s`;
    star.style.animationDuration = `${(0.8 + Math.random() * 0.9).toFixed(2)}s`;
    cover.appendChild(star);
  }
  cell.appendChild(cover);
  return cover;
}

async function starRevealTo(grid) {
  setReelGrid(grid);
  // Instant mode: no reveal choreography, results show immediately.
  if (speedMode === "instant") {
    playSound("reelStop");
    return;
  }

  const per = speedMode === "fast" ? 45 : 85; // gap between cell reveals
  const lead = speedMode === "fast" ? 160 : 300; // shimmer before the first reveal
  const covers = [];
  for (let r = 0; r < REELS; r += 1) {
    for (let row = 0; row < ROWS; row += 1) {
      covers.push(makeStarCover(reelCells[r][row]));
    }
  }
  void ui.reels.offsetWidth; // commit covers before any starts fading
  playSound("sparkle");

  const reveals = [];
  let order = 0;
  for (let r = 0; r < REELS; r += 1) {
    for (let row = ROWS - 1; row >= 0; row -= 1) {
      const cover = covers[r * ROWS + row];
      const delay = lead + order * per;
      order += 1;
      reveals.push(
        new Promise((res) => {
          setTimeout(() => {
            cover.classList.add("reveal");
            playSound("coin");
            setTimeout(() => {
              cover.remove();
              res();
            }, 320);
          }, delay);
        })
      );
    }
  }
  await Promise.all(reveals);
}

// ============================================
// GAMBLE — red/black double-or-nothing on the last win.
// Styled and animated like the blackjack table (wwwroot/kocka): white playing
// cards with a flip reveal, purple-striped card backs, felt panel. The first
// pick moves the offer from the balance into the stake; each correct colour
// doubles it, a wrong one loses everything; Collect banks the stake anytime.
// ============================================

const SUIT_GLYPHS = { hearts: "♥", diamonds: "♦", spades: "♠", clubs: "♣" };

function renderGambleHistory(history) {
  ui.gambleHistory.innerHTML = "";
  (history || []).forEach((card) => {
    const el = document.createElement("span");
    el.className = `gamble-mini-card${card.color === "red" ? " red" : ""}`;
    el.innerHTML = `<b>${card.rank}</b><i>${SUIT_GLYPHS[card.suit] || "?"}</i>`;
    ui.gambleHistory.appendChild(el);
  });
}

function setGambleStake(amount, label) {
  ui.gambleStakeLabel.textContent = label;
  ui.gambleStakeValue.textContent = formatMoney(amount);
}

// Return the card face-down without a visible reverse-flip; optionally play
// the deal-in slide for a freshly drawn card.
function resetGambleCard(deal) {
  ui.gambleCardInner.style.transition = "none";
  ui.gambleCard.classList.remove("flipped", "won", "lost");
  void ui.gambleCardInner.offsetWidth;
  ui.gambleCardInner.style.transition = "";
  ui.gambleCardRank.textContent = "";
  ui.gambleCardSuit.textContent = "";
  ui.gambleCardFront.classList.remove("red");
  if (deal) {
    ui.gambleCard.classList.remove("dealt");
    void ui.gambleCard.offsetWidth;
    ui.gambleCard.classList.add("dealt");
  }
}

function showGambleCardFace(card) {
  ui.gambleCardRank.textContent = card.rank;
  ui.gambleCardSuit.textContent = SUIT_GLYPHS[card.suit] || "?";
  ui.gambleCardFront.classList.toggle("red", card.color === "red");
  ui.gambleCard.classList.add("flipped");
}

function updateGambleButtons() {
  const active = !!(serverState && serverState.gamble && serverState.gamble.active);
  ui.gambleRed.disabled = gambleBusy;
  ui.gambleBlack.disabled = gambleBusy;
  ui.gambleCollect.disabled = gambleBusy;
  ui.gambleCollect.textContent = active
    ? `Collect ${formatMoney(serverState.gamble.stake)}`
    : "Keep Win";
}

function openGamblePanelBase() {
  gambleOpen = true;
  gambleBusy = false;
  ui.gamblePanel.classList.remove("hidden", "lost");
}

// Fresh offer right after a winning manual spin (nothing staked yet).
function openGambleOffer(gamble) {
  openGamblePanelBase();
  setGambleStake(Number(gamble.offer || 0), "Your win");
  ui.gambleMsg.textContent = "Pick a colour to double it — or keep the win.";
  renderGambleHistory([]);
  resetGambleCard(false);
  updateGambleButtons();
  playSound("bet");
}

// Re-attach to a gamble already in progress (stake on the table).
function openGambleActive(gamble) {
  openGamblePanelBase();
  setGambleStake(Number(gamble.stake || 0), "On the table");
  renderGambleHistory(gamble.history);
  if (gamble.lastCard) {
    showGambleCardFace(gamble.lastCard);
  } else {
    resetGambleCard(false);
  }
  ui.gambleMsg.textContent = "Double again or collect.";
  updateGambleButtons();
}

function closeGamblePanel() {
  gambleOpen = false;
  gambleBusy = false;
  ui.gamblePanel.classList.add("hidden");
  ui.gamblePanel.classList.remove("lost");
  updateControls();
}

async function pickGamble(choice) {
  if (!gambleOpen || gambleBusy) {
    return;
  }
  gambleBusy = true;
  updateGambleButtons();
  resetGambleCard(true); // slide in a fresh face-down card
  playSound("button");

  let state;
  try {
    state = await apiPost("gamble", { choice });
  } catch (error) {
    gambleBusy = false;
    updateGambleButtons();
    if (!(error instanceof HandledApiError)) {
      console.error("Slot error:", error);
      setStatus("Something went wrong. Try again.", true);
    }
    return;
  }

  const gamble = state.gamble || {};
  const card = gamble.lastCard;

  // The pick was refused (expired session, nothing to gamble, …): the server
  // set a status message and drew no card — just sync state and close.
  if (gamble.lastWon !== true && gamble.lastWon !== false) {
    applyServerState(state);
    closeGamblePanel();
    return;
  }

  await sleep(380); // beat of suspense while the card sits face-down
  if (card) {
    showGambleCardFace(card);
    playSound("reelStop"); // flip snap
    await sleep(520);
  }
  renderGambleHistory(gamble.history);
  applyServerState(state); // balance / status / version sync

  if (gamble.lastWon) {
    playSound("win");
    ui.gambleCard.classList.add("won");
    setGambleStake(Number(gamble.stake || 0), "On the table");
    ui.gambleMsg.textContent = `Correct! ${formatMoney(gamble.stake)} on the table — double again or collect.`;
    gambleBusy = false;
    updateGambleButtons();
  } else {
    // Wrong colour — the whole stake is gone.
    playSound("lose");
    ui.gambleCard.classList.add("lost");
    ui.gamblePanel.classList.add("lost");
    setGambleStake(0, "Lost");
    ui.gambleMsg.textContent = "Wrong colour — the stake is gone.";
    updateGambleButtons();
    await sleep(1500);
    closeGamblePanel();
  }
}

async function collectGamble() {
  if (!gambleOpen || gambleBusy) {
    return;
  }
  playSound("button");
  const active = !!(serverState && serverState.gamble && serverState.gamble.active);
  if (!active) {
    // Offer stage: the win is already on the balance — just decline and close.
    closeGamblePanel();
    return;
  }

  gambleBusy = true;
  updateGambleButtons();
  let state;
  try {
    state = await apiPost("gamble/collect", {});
  } catch (error) {
    gambleBusy = false;
    updateGambleButtons();
    if (!(error instanceof HandledApiError)) {
      console.error("Slot error:", error);
      setStatus("Something went wrong. Try again.", true);
    }
    return;
  }

  applyServerState(state);
  playSound("win");
  triggerActionFx("win");
  const collected = Number(state.lastWin || 0);
  setGambleStake(collected, "Collected");
  ui.gambleMsg.textContent = `Collected ${formatMoney(collected)}!`;
  gambleBusy = false;
  await sleep(900);
  closeGamblePanel();
}

ui.gambleRed.addEventListener("click", () => pickGamble("red"));
ui.gambleBlack.addEventListener("click", () => pickGamble("black"));
ui.gambleCollect.addEventListener("click", () => collectGamble());

// ============================================
// RENDERING (version-gated)
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

function effectiveBet() {
  const fromServer = Number(serverState?.selectedBet);
  if (betIndex(fromServer) >= 0) {
    return fromServer;
  }
  if (betIndex(selectedBet) >= 0) {
    return Number(selectedBet);
  }
  return allowedBets()[0];
}

// −/+ stepper that walks the allowed-bet ladder, with Min/Max shortcuts.
function renderBetSelector() {
  const bets = allowedBets();
  const active = effectiveBet();
  const idx = betIndex(active);
  const locked = busy || spinning || autoActive;

  ui.betValue.textContent = formatMoney(active);
  const overBalance = serverState ? active > Number(serverState.balance) : false;
  ui.betValue.classList.toggle("over-balance", overBalance);

  ui.betDown.disabled = locked || idx <= 0;
  ui.betUp.disabled = locked || idx >= bets.length - 1;
  ui.betMin.disabled = locked || idx <= 0;
  ui.betMax.disabled = locked || idx >= bets.length - 1;
}

function updateControls() {
  renderBetSelector();
  const bet = effectiveBet();
  const insufficient = serverState ? Number(serverState.balance) < bet : false;
  ui.spinBtn.disabled =
    busy || spinning || autoActive || !serverState || !serverState.canSpin || insufficient;
  ui.spinBtn.textContent = spinning ? "Spinning…" : insufficient ? "No Funds" : "Spin";

  // Autospin start is unavailable while a spin/sequence runs or funds are short.
  ui.autospinBtn.disabled =
    busy || spinning || autoActive || !serverState || !serverState.canSpin || insufficient;
  ui.autoCounts.forEach((b) => {
    b.disabled = autoActive;
    b.classList.toggle("active", Number(b.dataset.count) === autoSelectedCount);
  });
}

function render(force = false) {
  if (!serverState) {
    return;
  }
  if (!force && serverState.version === lastRenderedVersion) {
    return;
  }
  lastRenderedVersion = serverState.version;

  ui.playerName.textContent = serverState.playerName || "—";
  ui.balance.textContent = `Balance: ${formatMoney(serverState.balance)}`;
  ui.totalBet.textContent = `Bet: ${formatMoney(effectiveBet())}`;
  ui.spinsCount.textContent = `Spins: ${serverState.spins ?? 0}`;
  ui.featureCount.textContent = `Features: ${serverState.featureHits ?? 0}`;
  animateBalanceChange(ui.balance);

  if (betIndex(Number(serverState.selectedBet)) >= 0) {
    selectedBet = Number(serverState.selectedBet);
  }

  ui.betDisplay.textContent = `Total Bet: ${formatMoney(effectiveBet())} · Line Bet: ${formatMoney(effectiveBet() / 10)}`;

  // Paint the resting grid only when not mid-spin (state syncs / focus refetch).
  if (!spinning && serverState.lastGrid && Array.isArray(serverState.lastGrid)) {
    setReelGrid(serverState.lastGrid);
  }

  updateControls();

  if (showResultBanner) {
    showResultBanner = false;
    const win = Number(serverState.lastWin ?? 0);
    if (win > 0) {
      ui.roundResult.textContent = `WIN ${formatMoney(win)}`;
      ui.roundResult.className = "round-result win";
    } else {
      ui.roundResult.textContent = "NO WIN";
      ui.roundResult.className = "round-result loss";
    }
    animateRoundResult(ui.roundResult);
    if (win > 0) {
      showWinMeter(win);
    }
  }

  if (serverState.status && !spinning) {
    setStatus(serverState.status);
  }

  // Re-attach to an in-progress gamble (page reload / focus re-sync): the
  // stake is still on the table server-side, so bring the panel back up.
  if (serverState.gamble && serverState.gamble.active && !gambleOpen && !spinning) {
    openGambleActive(serverState.gamble);
  }
}

// ============================================
// PAYTABLE
// ============================================

function renderPaytable() {
  const symbols = serverState?.symbols;
  if (!Array.isArray(symbols) || symbols.length === 0) {
    return;
  }
  if (ui.paytable.dataset.built === "1") {
    return;
  }
  ui.paytable.dataset.built = "1";
  ui.paytable.innerHTML = "";

  symbols.forEach((sym) => {
    const card = document.createElement("div");
    card.className = "pay-card";
    if (sym.isWild || sym.isScatter) {
      card.classList.add("special");
    }

    const symBox = document.createElement("div");
    symBox.className = "pay-symbol";
    const pip = document.createElement("span");
    paintPip(pip, sym.key);
    symBox.appendChild(pip);

    const info = document.createElement("div");
    info.className = "pay-info";
    const name = document.createElement("span");
    name.className = "pay-name";
    name.textContent = sym.name || sym.key;
    if (sym.isWild && sym.isScatter) {
      const tag = document.createElement("span");
      tag.className = "tag";
      tag.textContent = "WILD+SCATTER";
      name.appendChild(tag);
    } else if (sym.isWild) {
      const tag = document.createElement("span");
      tag.className = "tag";
      tag.textContent = "WILD";
      name.appendChild(tag);
    }
    const values = document.createElement("span");
    values.className = "pay-values";
    values.innerHTML = `3: <b>${sym.pay3}×</b> · 4: <b>${sym.pay4}×</b> · 5: <b>${sym.pay5}×</b>`;

    info.append(name, values);
    card.append(symBox, info);
    ui.paytable.appendChild(card);
  });
}

// ============================================
// VFX (same flair as blackjack / roulette / threebody)
// ============================================

function triggerActionFx(kind) {
  if (!ui.vfxLayer) {
    return;
  }

  const config = {
    bet: { count: 50, color: "#b4ff66", spread: 240, flash: "flash-spin", waves: 1, words: ["BET", "LOCKED IN", "STAKE SET"] },
    spin: { count: 120, color: "#9dff60", spread: 420, flash: "flash-spin", waves: 2, words: ["SPIN", "GOOD LUCK", "REELS GO"] },
    scatter: { count: 130, color: "#ff53b8", spread: 460, flash: "flash-scatter", waves: 2, words: ["SCATTER", "LUCKY LADY", "3 LADIES"] },
    feature: { count: 200, color: "#ffd24a", spread: 540, flash: "flash-feature", waves: 3, words: ["FREE SPINS", "FEATURE", "15 FREE"] },
    win: { count: 200, color: "#70ff3a", spread: 540, flash: "flash-win", waves: 3, words: ["WINNER", "PAYOUT", "JACKPOT"] },
  }[kind];

  if (!config) {
    return;
  }

  const rect = ui.vfxLayer.getBoundingClientRect();
  const centerX = rect.width * (0.35 + Math.random() * 0.3);
  const centerY = rect.height * (0.3 + Math.random() * 0.3);

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

  document.body.classList.remove("action-flash", "flash-spin", "flash-win", "flash-feature", "flash-scatter");
  document.body.classList.remove("action-shake", "action-glitch", "action-overdrive", "action-strobe", "action-tilt");
  void document.body.offsetWidth;
  const heavy = kind === "spin" || kind === "win" || kind === "feature" || kind === "scatter";
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
// AUDIO — original Web Audio cues (no ripped game audio).
//
// Sound module: a small registry of named cues, each a function that schedules
// oscillators. A developer can later swap any cue for a royalty-free sample by
// replacing its entry in SOUND_CUES with one that plays an <audio>/AudioBuffer.
// ============================================

let muted = false;

function ensureAudioContext() {
  if (muted) {
    return null;
  }
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

// A short filtered-noise burst — used for the reel "whir".
function playNoise(duration, volume = 0.04, freq = 1200) {
  const ctx = ensureAudioContext();
  if (!ctx) {
    return;
  }
  const frames = Math.floor(ctx.sampleRate * duration);
  const buffer = ctx.createBuffer(1, frames, ctx.sampleRate);
  const data = buffer.getChannelData(0);
  for (let i = 0; i < frames; i += 1) {
    data[i] = (Math.random() * 2 - 1) * (1 - i / frames);
  }
  const src = ctx.createBufferSource();
  src.buffer = buffer;
  const filter = ctx.createBiquadFilter();
  filter.type = "bandpass";
  filter.frequency.value = freq;
  const gain = ctx.createGain();
  gain.gain.value = volume;
  src.connect(filter);
  filter.connect(gain);
  gain.connect(ctx.destination);
  src.start();
}

// Named cue registry. Swap any entry to drop in your own royalty-free sound.
const SOUND_CUES = {
  bet: () => {
    playTone(430, 0.05, "triangle", 0.04);
    playTone(560, 0.06, "triangle", 0.03, 0.05);
  },
  button: () => playTone(430, 0.05, "triangle", 0.03),
  spin: () => {
    playTone(140, 0.08, "sawtooth", 0.06);
    playTone(280, 0.08, "square", 0.05, 0.05);
    playNoise(0.22, 0.05, 900);
  },
  reelStop: () => {
    playTone(180, 0.05, "square", 0.05);
    playTone(90, 0.06, "triangle", 0.04, 0.01);
  },
  lineWin: () => {
    playTone(660, 0.08, "triangle", 0.05);
    playTone(880, 0.1, "triangle", 0.05, 0.07);
  },
  coin: () => playTone(1200 + Math.random() * 500, 0.04, "triangle", 0.03),
  scatter: () => {
    playTone(523, 0.1, "triangle", 0.06);
    playTone(784, 0.12, "triangle", 0.06, 0.08);
    playTone(1046, 0.16, "triangle", 0.06, 0.16);
  },
  feature: () => {
    // Original ascending fanfare for the free-spins trigger.
    [523, 659, 784, 1046, 1318].forEach((f, i) => {
      playTone(f, 0.18, "triangle", 0.07, i * 0.12);
    });
    playNoise(0.5, 0.03, 2200);
  },
  win: () => {
    playTone(392, 0.12, "triangle", 0.06);
    playTone(494, 0.12, "triangle", 0.06, 0.1);
    playTone(587, 0.14, "triangle", 0.06, 0.2);
    playTone(784, 0.22, "triangle", 0.06, 0.3);
  },
  lose: () => {
    playTone(220, 0.14, "sine", 0.05);
    playTone(160, 0.2, "triangle", 0.04, 0.12);
  },
  sparkle: () => {
    playTone(1568, 0.12, "triangle", 0.03);
    playTone(2093, 0.14, "triangle", 0.025, 0.08);
    playTone(2637, 0.18, "sine", 0.02, 0.16);
  },
};

function playSound(kind) {
  const cue = SOUND_CUES[kind];
  if (cue) {
    cue();
  }
}

// Convenience wrappers used during reel motion.
function spinReelWhir() {
  playSound("spin");
}

// ============================================
// FLORAL FREE-SPINS MUSIC — a gentle pastoral melody loop synthesized with
// WebAudio (no external audio files). Starts with the feature, stops after,
// and respects the mute button: muting silences it instantly and stops the
// scheduler; unmuting mid-feature picks the loop back up.
// ============================================

const music = { active: false, timer: null, gain: null };
const MUSIC_VOLUME = 0.9;

// F-major pentatonic garden stroll, 32 gentle steps (~10 s per loop).
const FLORAL_STEP = 0.32; // seconds per melody step
const FLORAL_MELODY = [
  349.23, 440.0, 523.25, 587.33, 523.25, 440.0, 392.0, 440.0,
  349.23, 440.0, 523.25, 698.46, 587.33, 523.25, 440.0, 392.0,
  293.66, 349.23, 440.0, 523.25, 440.0, 349.23, 329.63, 349.23,
  392.0, 440.0, 523.25, 587.33, 659.25, 587.33, 523.25, 440.0,
];
// One soft pad root every 8 steps: F3, C3, D3, F3.
const FLORAL_BASS = [174.61, 130.81, 146.83, 174.61];

function musicContext() {
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

function musicNote(ctx, freq, at, dur, type, vol) {
  const osc = ctx.createOscillator();
  const gain = ctx.createGain();
  osc.type = type;
  osc.frequency.setValueAtTime(freq, at);
  gain.gain.setValueAtTime(0.0001, at);
  gain.gain.exponentialRampToValueAtTime(vol, at + 0.06);
  gain.gain.exponentialRampToValueAtTime(0.0001, at + dur);
  osc.connect(gain);
  gain.connect(music.gain);
  osc.start(at);
  osc.stop(at + dur + 0.05);
}

function scheduleFloralLoop() {
  if (!music.active || muted) {
    return;
  }
  const ctx = musicContext();
  if (!ctx) {
    return;
  }
  if (!music.gain) {
    music.gain = ctx.createGain();
    music.gain.gain.value = MUSIC_VOLUME;
    music.gain.connect(ctx.destination);
  }

  const t0 = ctx.currentTime + 0.05;
  FLORAL_MELODY.forEach((freq, i) => {
    const at = t0 + i * FLORAL_STEP;
    musicNote(ctx, freq, at, FLORAL_STEP * 1.6, "triangle", 0.035);
    // Airy octave echo on the off-beats — the "floral" shimmer.
    if (i % 4 === 2) {
      musicNote(ctx, freq * 2, at + FLORAL_STEP * 0.5, FLORAL_STEP * 0.9, "sine", 0.012);
    }
    // Soft pad: root + fifth held under each 8-step phrase.
    if (i % 8 === 0) {
      const bass = FLORAL_BASS[(i / 8) % FLORAL_BASS.length];
      musicNote(ctx, bass, at, FLORAL_STEP * 7.5, "sine", 0.03);
      musicNote(ctx, bass * 1.5, at, FLORAL_STEP * 7.5, "sine", 0.016);
    }
  });

  const loopMs = FLORAL_MELODY.length * FLORAL_STEP * 1000;
  music.timer = setTimeout(scheduleFloralLoop, loopMs - 120);
}

function startFreeSpinMusic() {
  if (music.active) {
    return;
  }
  music.active = true;
  scheduleFloralLoop();
}

function stopFreeSpinMusic() {
  music.active = false;
  if (music.timer) {
    clearTimeout(music.timer);
    music.timer = null;
  }
  if (music.gain && audioContext) {
    // Fade the tail out instead of cutting scheduled notes dead.
    const gainNode = music.gain;
    const now = audioContext.currentTime;
    gainNode.gain.cancelScheduledValues(now);
    gainNode.gain.setValueAtTime(gainNode.gain.value, now);
    gainNode.gain.linearRampToValueAtTime(0.0001, now + 0.6);
    setTimeout(() => {
      try {
        gainNode.disconnect();
      } catch {
        /* already disconnected */
      }
    }, 700);
    music.gain = null;
  }
}

function setMuted(value) {
  muted = value;
  ui.muteBtn.classList.toggle("muted", muted);
  ui.muteBtn.textContent = muted ? "🔇" : "🔊";
  ui.muteBtn.setAttribute("aria-pressed", muted ? "true" : "false");

  // Free-spins music: silence instantly on mute, resume the loop on unmute.
  if (muted) {
    if (music.timer) {
      clearTimeout(music.timer);
      music.timer = null;
    }
    if (music.gain) {
      music.gain.gain.value = 0;
    }
  } else if (music.active) {
    if (music.gain && audioContext) {
      music.gain.gain.setValueAtTime(MUSIC_VOLUME, audioContext.currentTime);
    }
    scheduleFloralLoop();
  }
}

ui.muteBtn.addEventListener("click", () => {
  setMuted(!muted);
  if (!muted) {
    playSound("button");
  }
});

// ============================================
// EVENT WIRING
// ============================================

ui.spinBtn.addEventListener("click", () => {
  animateButtonPress(ui.spinBtn);
  animateButtonGlow(ui.spinBtn);
  spin();
});

// --- Bet stepper ---
ui.betDown.addEventListener("click", () => stepBet(-1));
ui.betUp.addEventListener("click", () => stepBet(1));
ui.betMin.addEventListener("click", () => setBet(allowedBets()[0]));
ui.betMax.addEventListener("click", () => {
  const bets = allowedBets();
  setBet(bets[bets.length - 1]);
});

// ============================================
// SPEED CONTROL (Normal / Fast / Instant, persisted)
// ============================================

function setSpeed(mode) {
  speedMode = ["normal", "fast", "instant"].includes(mode) ? mode : "normal";
  try {
    localStorage.setItem("slot.speed", speedMode);
  } catch {
    /* storage unavailable */
  }
  ui.speedModes.forEach((b) => b.classList.toggle("active", b.dataset.speed === speedMode));
}

ui.speedModes.forEach((btn) => {
  btn.addEventListener("click", () => {
    setSpeed(btn.dataset.speed);
    playSound("button");
  });
});

(function initSpeed() {
  let saved = "normal";
  try {
    saved = localStorage.getItem("slot.speed") || "normal";
  } catch {
    /* ignore */
  }
  setSpeed(saved);
})();

// ============================================
// AUTOSPIN
// ============================================

function updateAutospinUi() {
  ui.autospinBtn.classList.toggle("hidden", autoActive);
  ui.autostopBtn.classList.toggle("hidden", !autoActive);
  ui.autospinRemaining.textContent = autoRemaining < 0 ? "∞" : String(autoRemaining);
  ui.autospin?.classList.toggle("running", autoActive);
}

function stopAutospin(reason) {
  if (!autoActive) {
    return;
  }
  autoActive = false;
  autoRemaining = 0;
  updateAutospinUi();
  updateControls();
  if (reason) {
    setStatus(reason);
  }
}

async function startAutospin() {
  if (autoActive || busy || spinning || !serverState || !serverState.canSpin) {
    return;
  }
  autoActive = true;
  autoRemaining = autoSelectedCount; // -1 === infinite
  updateAutospinUi();
  updateControls();
  runAutospinLoop();
}

async function runAutospinLoop() {
  while (autoActive) {
    // Stop if balance can't cover the next bet.
    const bet = effectiveBet();
    if (!serverState || Number(serverState.balance) < bet || !serverState.canSpin) {
      stopAutospin("Autospin stopped — insufficient funds.");
      return;
    }
    if (autoRemaining === 0) {
      stopAutospin("Autospin complete.");
      return;
    }

    updateAutospinUi();
    const outcome = await spin(); // resolves after the whole round (incl. free spins) replays
    if (!autoActive) {
      // Stopped mid-spin by the user; let the in-flight round finish, then bail.
      return;
    }
    if (!outcome || !outcome.ok) {
      stopAutospin("Autospin stopped.");
      return;
    }

    // Count this paid spin (free spins inside the round are not charged).
    if (autoRemaining > 0) {
      autoRemaining -= 1;
    }
    updateAutospinUi();

    // Brief breath between rounds (respecting speed).
    await sleep(speedMode === "instant" ? 120 : 260);
  }
}

ui.autoCounts.forEach((btn) => {
  btn.addEventListener("click", () => {
    if (autoActive) {
      return;
    }
    autoSelectedCount = Number(btn.dataset.count);
    ui.autoCounts.forEach((b) => b.classList.toggle("active", b === btn));
  });
});

ui.autospinBtn.addEventListener("click", () => {
  animateButtonPress(ui.autospinBtn);
  startAutospin();
});

ui.autostopBtn.addEventListener("click", () => {
  animateButtonPress(ui.autostopBtn);
  stopAutospin("Autospin stopped.");
});

// Default selected autospin count highlight.
ui.autoCounts.forEach((b) => b.classList.toggle("active", Number(b.dataset.count) === autoSelectedCount));

// Singleplayer: fetch once on load and after each action; re-sync on focus.
(async function init() {
  await refreshState();
  if (serverState) {
    renderPaytable();
    if (serverState.lastGrid && Array.isArray(serverState.lastGrid)) {
      setReelGrid(serverState.lastGrid);
    }
  }
})();
window.addEventListener("focus", async () => {
  await refreshState();
  renderPaytable();
});

// ============================================
// ANIME.JS ANIMATION HELPERS
// ============================================

function animateButtonPress(el) {
  if (!window.anime) return;
  anime({ targets: el, scale: [1, 0.95, 1], duration: 200, easing: "easeOutQuad" });
}

function animateButtonGlow(el) {
  if (!window.anime) return;
  anime({
    targets: el,
    boxShadow: [
      "0 0 16px rgba(112, 255, 58, 0.24)",
      "0 0 32px rgba(112, 255, 58, 0.6)",
      "0 0 16px rgba(112, 255, 58, 0.24)",
    ],
    duration: 800,
    easing: "easeInOutQuad",
  });
}


function animateStatusPulse(el) {
  if (!window.anime) return;
  anime({ targets: el, opacity: [1, 0.5, 1], scale: [1, 1.04, 1], duration: 600, easing: "easeInOutQuad" });
}

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

function reelStopBounce(reelEl) {
  if (!window.anime) return;
  anime({
    targets: reelEl,
    translateY: [-10, 0],
    duration: 320,
    easing: "easeOutElastic(1, .6)",
  });
}

function animateWinMeterPop() {
  if (!window.anime) return;
  anime({ targets: ui.winMeter, scale: [1, 1.12, 1], duration: 320, easing: "easeOutBack" });
}

function animateFreeBannerTick() {
  if (!window.anime) return;
  anime({ targets: ui.freeBannerCount, scale: [1.4, 1], duration: 280, easing: "easeOutBack" });
}
