// ============================================================
// LUCKY LADY'S CHARM BALL — Hold & Win slot client
// The server resolves the whole round (base spin + optional
// Charm Bonus) in one POST; this client only plays back the
// recording. No win is ever computed client-side.
// ============================================================

const API_BASE = "/api/slot";
const LOGIN_URL = "/Account/Login?returnUrl=/slot/index.html";
const PROFILE_URL = "/Account/Profile";

const REELS = 5;
const ROWS = 3;

// ============================================================
// SYMBOL ART — original inline SVG, casino-grade glossy tiles.
// Keys match the server catalogue exactly.
// ============================================================

// Shared shamrock path centred on (0,0), ~10px radius.
const SHAMROCK =
  "M0 0c-2.4-5.5-9.5-3.2-7 2.4 1.5 3.1 5.4 3.1 7-2.4zm0 0c5.5-2.4 3.2-9.5-2.4-7-3.1 1.5-3.1 5.4 2.4 7zm0 0c2.4 5.5 9.5 3.2 7-2.4-1.5-3.1-5.4-3.1-7 2.4zm0 0c-5.5 2.4-3.2 9.5 2.4 7 3.1-1.5 3.1-5.4-2.4-7z";

// The Lucky Lady — WILD and top pay. A gilded portrait medallion.
function svgLady() {
  return `
  <svg viewBox="0 0 64 64" class="sym-svg" aria-hidden="true">
    <defs>
      <linearGradient id="sl-frame" x1="0" y1="0" x2="0" y2="1">
        <stop offset="0" stop-color="#fff3c4"/><stop offset="0.5" stop-color="#ffd24a"/><stop offset="1" stop-color="#a8761c"/>
      </linearGradient>
      <linearGradient id="sl-hair" x1="0" y1="0" x2="0" y2="1">
        <stop offset="0" stop-color="#c04d1c"/><stop offset="1" stop-color="#6e2408"/>
      </linearGradient>
      <linearGradient id="sl-skin" x1="0" y1="0" x2="0" y2="1">
        <stop offset="0" stop-color="#f9ddc2"/><stop offset="1" stop-color="#e8b48e"/>
      </linearGradient>
      <radialGradient id="sl-back" cx="0.5" cy="0.35" r="0.85">
        <stop offset="0" stop-color="#1c6b3a"/><stop offset="1" stop-color="#052c14"/>
      </radialGradient>
    </defs>
    <circle cx="32" cy="30" r="26" fill="url(#sl-back)" stroke="url(#sl-frame)" stroke-width="3"/>
    <circle cx="32" cy="30" r="22.6" fill="none" stroke="#7a5410" stroke-width="0.8" opacity="0.7"/>
    <g>
      <path d="M32 12 C18 13 14 26 16 38 C17 44 20 48 24 50 C21 42 20 32 22 25 L42 25 C44 32 43 42 40 50 C44 48 47 44 48 38 C50 26 46 13 32 12 Z" fill="url(#sl-hair)"/>
      <ellipse cx="32" cy="29" rx="9.6" ry="11" fill="url(#sl-skin)"/>
      <path d="M23 26 C23 17 41 17 41 26 C38 21 35 20 32 20.5 C29 20 26 21 23 26 Z" fill="url(#sl-hair)"/>
      <path d="M25.5 18.5 L28 14.5 L30.5 17.5 L32 13 L33.5 17.5 L36 14.5 L38.5 18.5 C34 16.8 30 16.8 25.5 18.5 Z" fill="url(#sl-frame)" stroke="#7a5410" stroke-width="0.5"/>
      <circle cx="32" cy="16" r="1.3" fill="#2ecc71"/>
      <ellipse cx="28.4" cy="28.4" rx="1.7" ry="1.15" fill="#fff"/>
      <ellipse cx="35.6" cy="28.4" rx="1.7" ry="1.15" fill="#fff"/>
      <circle cx="28.5" cy="28.5" r="0.85" fill="#2e8b57"/>
      <circle cx="35.5" cy="28.5" r="0.85" fill="#2e8b57"/>
      <path d="M26.4 26.6 C27.5 25.7 29.5 25.7 30.4 26.5" stroke="#5e1d07" stroke-width="0.7" fill="none"/>
      <path d="M33.6 26.5 C34.5 25.7 36.5 25.7 37.6 26.6" stroke="#5e1d07" stroke-width="0.7" fill="none"/>
      <path d="M29.2 34.6 q2.8 2.3 5.6 0 q-2.8 1.4 -5.6 0 Z" fill="#c93a52"/>
      <path d="M32 30.5 q-0.8 1.8 0.4 2.4" stroke="#d69a70" stroke-width="0.55" fill="none"/>
    </g>
    <path d="M12 44 L52 44 L48 54 L16 54 Z" fill="url(#sl-frame)" stroke="#7a5410" stroke-width="1"/>
    <text x="32" y="52" text-anchor="middle" font-family="Georgia, serif" font-weight="900" font-size="8.5" letter-spacing="2" fill="#4a2f06">WILD</text>
  </svg>`;
}

// The Charm Ball — SCATTER. A glowing golden orb; a value plate
// (cash or MINI/MAJOR/GRAND) is overlaid in HTML when it lands.
function svgCharm() {
  return `
  <svg viewBox="0 0 64 64" class="sym-svg" aria-hidden="true">
    <defs>
      <radialGradient id="sc-ball" cx="0.35" cy="0.28" r="0.9">
        <stop offset="0" stop-color="#fff8d8"/><stop offset="0.5" stop-color="#ffcf3f"/><stop offset="1" stop-color="#8f6112"/>
      </radialGradient>
      <radialGradient id="sc-halo" cx="0.5" cy="0.5" r="0.5">
        <stop offset="0" stop-color="#ffe98f" stop-opacity="0.8"/><stop offset="1" stop-color="#ffe98f" stop-opacity="0"/>
      </radialGradient>
    </defs>
    <circle cx="32" cy="32" r="30" fill="url(#sc-halo)"/>
    <g stroke="#ffe98f" stroke-width="1.6" stroke-linecap="round" opacity="0.85">
      <line x1="32" y1="4" x2="32" y2="10"/><line x1="32" y1="54" x2="32" y2="60"/>
      <line x1="4" y1="32" x2="10" y2="32"/><line x1="54" y1="32" x2="60" y2="32"/>
      <line x1="12" y1="12" x2="16.5" y2="16.5"/><line x1="47.5" y1="47.5" x2="52" y2="52"/>
      <line x1="52" y1="12" x2="47.5" y2="16.5"/><line x1="16.5" y1="47.5" x2="12" y2="52"/>
    </g>
    <circle cx="32" cy="32" r="20" fill="url(#sc-ball)" stroke="#6e4a0d" stroke-width="1.6"/>
    <circle cx="32" cy="32" r="16.6" fill="none" stroke="#fff3c4" stroke-width="0.8" opacity="0.5"/>
    <path fill="#1c6b2c" transform="translate(32 26.5)" d="${SHAMROCK}"/>
    <ellipse cx="24.5" cy="22.5" rx="6.4" ry="3.6" fill="#fff" opacity="0.55" transform="rotate(-26 24.5 22.5)"/>
  </svg>`;
}

// Pot of Gold under a rainbow.
function svgPotOfGold() {
  return `
  <svg viewBox="0 0 64 64" class="sym-svg" aria-hidden="true">
    <defs>
      <radialGradient id="sp-pot" cx="0.38" cy="0.28" r="0.95">
        <stop offset="0" stop-color="#585868"/><stop offset="1" stop-color="#101016"/>
      </radialGradient>
      <linearGradient id="sp-coin" x1="0" y1="0" x2="0" y2="1">
        <stop offset="0" stop-color="#fff3c4"/><stop offset="0.5" stop-color="#ffd24a"/><stop offset="1" stop-color="#a8761c"/>
      </linearGradient>
    </defs>
    <g fill="none" stroke-linecap="round" opacity="0.9">
      <path d="M10 30 A22 22 0 0 1 54 30" stroke="#e8534a" stroke-width="3.4"/>
      <path d="M13.4 30 A18.6 18.6 0 0 1 50.6 30" stroke="#ffb63d" stroke-width="3.2"/>
      <path d="M16.8 30 A15.2 15.2 0 0 1 47.2 30" stroke="#4fae5f" stroke-width="3"/>
      <path d="M20.2 30 A11.8 11.8 0 0 1 43.8 30" stroke="#4f8fd4" stroke-width="2.8"/>
    </g>
    <g fill="url(#sp-coin)" stroke="#7a5410" stroke-width="0.7">
      <circle cx="24" cy="31" r="5"/><circle cx="33" cy="28" r="5.6"/><circle cx="41" cy="31" r="4.8"/>
      <circle cx="28" cy="33" r="4.6"/><circle cx="37" cy="33" r="4.4"/>
    </g>
    <path d="M15 36 C11 52 53 52 49 36 C54 33 10 33 15 36 Z" fill="url(#sp-pot)"/>
    <ellipse cx="32" cy="35.4" rx="18.4" ry="4" fill="#2b2b34"/>
    <path d="M18 42 C22 46 42 46 46 42" stroke="#6a6a7c" stroke-width="1.4" fill="none" opacity="0.6"/>
    <path d="M44 20 l1.7 4.1 4.1 1.7 -4.1 1.7 -1.7 4.1 -1.7 -4.1 -4.1 -1.7 4.1 -1.7 Z" fill="#fff7d0" opacity="0.9"/>
  </svg>`;
}

// Four-Leaf Clover with dew.
function svgClover() {
  return `
  <svg viewBox="0 0 64 64" class="sym-svg" aria-hidden="true">
    <defs>
      <radialGradient id="s4-leaf" cx="0.38" cy="0.32" r="0.85">
        <stop offset="0" stop-color="#b6ff7a"/><stop offset="0.6" stop-color="#4fae3c"/><stop offset="1" stop-color="#23701f"/>
      </radialGradient>
    </defs>
    <g transform="translate(32 30)">
      <g fill="url(#s4-leaf)" stroke="#175217" stroke-width="1.2" stroke-linejoin="round">
        <path d="M-1 -2 C-6 -16 -20 -12 -15 -2 C-12 4 -4 4 -1 -2 Z"/>
        <path d="M2 -1 C16 -6 12 -20 2 -15 C-4 -12 -4 -4 2 -1 Z"/>
        <path d="M1 2 C6 16 20 12 15 2 C12 -4 4 -4 1 2 Z"/>
        <path d="M-2 1 C-16 6 -12 20 -2 15 C4 12 4 4 -2 1 Z"/>
      </g>
      <g stroke="#dff7c0" stroke-width="0.9" opacity="0.75" fill="none">
        <path d="M-3 -4 L-11 -8"/><path d="M4 -3 L8 -11"/><path d="M3 4 L11 8"/><path d="M-4 3 L-8 11"/>
      </g>
      <path d="M2 3 C4 12 6 18 8 24" stroke="#2f7a2a" stroke-width="2.6" fill="none" stroke-linecap="round"/>
      <circle cx="0" cy="0" r="2.6" fill="#eaffce"/>
      <circle cx="-8" cy="-9" r="2.2" fill="#ffffff" opacity="0.75"/>
    </g>
  </svg>`;
}

// Golden Horseshoe, points up for luck.
function svgHorseshoe() {
  return `
  <svg viewBox="0 0 64 64" class="sym-svg" aria-hidden="true">
    <defs>
      <linearGradient id="sh-gold" x1="0" y1="0" x2="1" y2="1">
        <stop offset="0" stop-color="#fff3c4"/><stop offset="0.45" stop-color="#ffd24a"/><stop offset="1" stop-color="#96691a"/>
      </linearGradient>
    </defs>
    <path d="M18 52 V30 a14 14 0 0 1 28 0 V52" fill="none" stroke="#5a3c0a" stroke-width="10.6" stroke-linecap="round" opacity="0.9"/>
    <path d="M18 52 V30 a14 14 0 0 1 28 0 V52" fill="none" stroke="url(#sh-gold)" stroke-width="8.4" stroke-linecap="round"/>
    <path d="M20.8 50 V30.5 a11.2 11.2 0 0 1 22.4 0 V50" fill="none" stroke="#fff7d0" stroke-width="1.1" opacity="0.55"/>
    <rect x="13.4" y="47" width="9.2" height="7" rx="2" fill="url(#sh-gold)" stroke="#5a3c0a" stroke-width="1"/>
    <rect x="41.4" y="47" width="9.2" height="7" rx="2" fill="url(#sh-gold)" stroke="#5a3c0a" stroke-width="1"/>
    <g fill="#4a2f06">
      <circle cx="18" cy="42" r="1.5"/><circle cx="46" cy="42" r="1.5"/>
      <circle cx="18.8" cy="33" r="1.5"/><circle cx="45.2" cy="33" r="1.5"/>
      <circle cx="24" cy="23.5" r="1.5"/><circle cx="40" cy="23.5" r="1.5"/>
      <circle cx="32" cy="20.4" r="1.5"/>
    </g>
  </svg>`;
}

// Golden Bell with a glossy shoulder.
function svgBell() {
  return `
  <svg viewBox="0 0 64 64" class="sym-svg" aria-hidden="true">
    <defs>
      <linearGradient id="sb-gold" x1="0" y1="0" x2="0" y2="1">
        <stop offset="0" stop-color="#fff3c4"/><stop offset="0.5" stop-color="#ffd24a"/><stop offset="1" stop-color="#96691a"/>
      </linearGradient>
      <radialGradient id="sb-shine" cx="0.32" cy="0.22" r="0.8">
        <stop offset="0" stop-color="#fff" stop-opacity="0.85"/><stop offset="0.4" stop-color="#fff" stop-opacity="0"/>
      </radialGradient>
    </defs>
    <circle cx="32" cy="10.6" r="3.6" fill="url(#sb-gold)" stroke="#6e4a0d" stroke-width="1"/>
    <path d="M32 11 C18 12 16 27 16 35 C16 42 12.5 45.5 10.6 47.5 L53.4 47.5 C51.5 45.5 48 42 48 35 C48 27 46 12 32 11 Z"
          fill="url(#sb-gold)" stroke="#6e4a0d" stroke-width="1.6" stroke-linejoin="round"/>
    <path d="M32 11 C18 12 16 27 16 35 C16 42 12.5 45.5 10.6 47.5 L53.4 47.5 C51.5 45.5 48 42 48 35 C48 27 46 12 32 11 Z" fill="url(#sb-shine)"/>
    <path d="M17.5 32.5 C26 34.5 38 34.5 46.5 32.5" stroke="#8a5f14" stroke-width="1.4" fill="none" opacity="0.7"/>
    <circle cx="32" cy="52" r="4.4" fill="url(#sb-gold)" stroke="#6e4a0d" stroke-width="1.2"/>
    <ellipse cx="24" cy="20" rx="4.4" ry="7.5" fill="#fff" opacity="0.4" transform="rotate(14 24 20)"/>
  </svg>`;
}

// Royals — jewelled metallic letterforms on emerald plates.
function svgRoyal(glyph, gemColor, gemDark) {
  const fs = glyph.length > 1 ? 24 : 31;
  return `
  <svg viewBox="0 0 64 64" class="sym-svg" aria-hidden="true">
    <defs>
      <linearGradient id="sr-plate" x1="0" y1="0" x2="0" y2="1">
        <stop offset="0" stop-color="#144a26"/><stop offset="1" stop-color="#04220f"/>
      </linearGradient>
      <linearGradient id="sr-gild" x1="0" y1="0" x2="0" y2="1">
        <stop offset="0" stop-color="#fff7d0"/><stop offset="0.45" stop-color="#ffd24a"/><stop offset="1" stop-color="#a8761c"/>
      </linearGradient>
    </defs>
    <rect x="7" y="6" width="50" height="52" rx="10" fill="url(#sr-plate)" stroke="#ffd24a" stroke-opacity="0.5" stroke-width="1.6"/>
    <rect x="10.4" y="9.4" width="43.2" height="45.2" rx="7" fill="none" stroke="#ffd24a" stroke-opacity="0.2" stroke-width="1"/>
    <path d="M12 12 q5 -3 10 0 M52 52 q-5 3 -10 0" stroke="#ffd24a" stroke-opacity="0.4" stroke-width="1.1" fill="none" stroke-linecap="round"/>
    <text x="32" y="41.5" text-anchor="middle" font-family="Georgia, 'Times New Roman', serif" font-weight="900"
          font-size="${fs}" fill="#1a0e02" opacity="0.6" transform="translate(0 1.6)">${glyph}</text>
    <text x="32" y="41.5" text-anchor="middle" font-family="Georgia, 'Times New Roman', serif" font-weight="900"
          font-size="${fs}" fill="url(#sr-gild)" stroke="#5a3c0a" stroke-width="0.8">${glyph}</text>
    <circle cx="32" cy="49.5" r="3" fill="${gemColor}" stroke="${gemDark}" stroke-width="0.9"/>
    <circle cx="31" cy="48.6" r="0.9" fill="#fff" opacity="0.85"/>
    <path d="M14 14 L50 10" stroke="#fff7d0" stroke-width="1.1" opacity="0.25" stroke-linecap="round"/>
  </svg>`;
}

// Dark socket shown on empty positions during the Charm Bonus.
function svgSocket() {
  return `
  <svg viewBox="0 0 64 64" class="sym-svg" aria-hidden="true">
    <circle cx="32" cy="32" r="20" fill="#060d05" stroke="#1e3512" stroke-width="1.4"/>
    <circle cx="32" cy="32" r="15" fill="none" stroke="#1e3512" stroke-width="0.9" opacity="0.7"/>
    <path fill="#16290f" transform="translate(32 28)" d="${SHAMROCK}"/>
  </svg>`;
}

const SYMBOL_ART = {
  lady: { kind: "lady-sym", svg: svgLady },
  charm: { kind: "charm-sym", svg: svgCharm },
  potofgold: { kind: "pic", svg: svgPotOfGold },
  clover: { kind: "pic", svg: svgClover },
  horseshoe: { kind: "pic", svg: svgHorseshoe },
  bell: { kind: "pic", svg: svgBell },
  ace: { kind: "royal", svg: () => svgRoyal("A", "#e8534a", "#7a1626") },
  king: { kind: "royal", svg: () => svgRoyal("K", "#4f8fd4", "#1c3d6e") },
  queen: { kind: "royal", svg: () => svgRoyal("Q", "#b98aff", "#4a2a7a") },
  jack: { kind: "royal", svg: () => svgRoyal("J", "#2ecc71", "#0d5c30") },
  ten: { kind: "royal", svg: () => svgRoyal("10", "#ffb63d", "#8a5510") },
  __blank: { kind: "socket", svg: svgSocket },
};

// Idle "attract" layout shown before the first server state.
const ATTRACT_GRID = [
  ["horseshoe", "lady", "bell"],
  ["clover", "charm", "potofgold"],
  ["charm", "lady", "charm"],
  ["potofgold", "charm", "clover"],
  ["bell", "lady", "horseshoe"],
];
const FILLER_KEYS = ["lady", "charm", "potofgold", "clover", "horseshoe", "bell", "ace", "king", "queen", "jack", "ten"];
const randKey = () => FILLER_KEYS[(Math.random() * FILLER_KEYS.length) | 0];

// ============================================================
// STATE
// ============================================================

let serverState = null;
let lastRenderedVersion = -1;
let selectedBet = null; // chosen total bet (decimal)
let busy = false; // a POST is in flight
let spinning = false; // a round (incl. bonus) is replaying
let showResultBanner = false;
let audioContext;

let speedMode = "normal"; // "normal" | "fast" | "instant" (persisted)

let autoActive = false;
let autoRemaining = 0; // -1 === infinite/until-stop
let autoSelectedCount = 10;

let gambleOpen = false;
let gambleBusy = false;
let autoGamblePrompt = false; // auto-open the gamble panel after a winning spin (persisted)

const ui = {
  game: document.getElementById("game"),
  gate: document.getElementById("gate"),
  gateMessage: document.getElementById("gate-message"),
  gateLink: document.getElementById("gate-link"),
  playerName: document.getElementById("player-name"),
  balance: document.getElementById("balance"),
  spinsCount: document.getElementById("spins-count"),
  featureCount: document.getElementById("feature-count"),
  muteBtn: document.getElementById("mute-btn"),
  jackpotMini: document.getElementById("jackpot-mini"),
  jackpotMajor: document.getElementById("jackpot-major"),
  jackpotGrand: document.getElementById("jackpot-grand"),
  reelsFrame: document.getElementById("reels-frame"),
  reels: document.getElementById("reels"),
  paylineOverlay: document.getElementById("payline-overlay"),
  bonusHud: document.getElementById("bonus-hud"),
  respinCount: document.getElementById("respin-count"),
  lockedCount: document.getElementById("locked-count"),
  winMeter: document.getElementById("win-meter"),
  winMeterValue: document.getElementById("win-meter-value"),
  status: document.getElementById("status"),
  roundResult: document.getElementById("round-result"),
  betValue: document.getElementById("bet-value"),
  betDown: document.getElementById("bet-down"),
  betUp: document.getElementById("bet-up"),
  betMin: document.getElementById("bet-min"),
  betMax: document.getElementById("bet-max"),
  spinBtn: document.getElementById("spin-btn"),
  gambleBtn: document.getElementById("gamble-btn"),
  autoGambleToggle: document.getElementById("auto-gamble-prompt"),
  buyBtn: document.getElementById("buy-btn"),
  buyCost: document.getElementById("buy-cost"),
  buyConfirm: document.getElementById("buy-confirm"),
  buyConfirmCost: document.getElementById("buy-confirm-cost"),
  buyYes: document.getElementById("buy-yes"),
  buyNo: document.getElementById("buy-no"),
  paytable: document.getElementById("paytable"),
  vfxLayer: document.getElementById("vfx-layer"),
  bannerLayer: document.getElementById("banner-layer"),
  charLady: document.getElementById("char-lady"),
  charLep: document.getElementById("char-lep"),
  speedModes: Array.from(document.querySelectorAll(".speed-mode")),
  autospin: document.getElementById("autospin"),
  autospinBtn: document.getElementById("autospin-btn"),
  autostopBtn: document.getElementById("autostop-btn"),
  autospinRemaining: document.getElementById("autospin-remaining"),
  autoCounts: Array.from(document.querySelectorAll(".auto-count")),
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

// reelCells[reel][row] -> the .cell element visible at that position.
const reelCells = [];

// ============================================================
// REEL CONSTRUCTION
// ============================================================

function symbolArt(key) {
  return SYMBOL_ART[key] || { kind: "pic", svg: () => `<span class="sym-fallback">${key}</span>` };
}

function paintPip(pip, key) {
  const art = symbolArt(key);
  pip.className = `pip ${art.kind}`;
  pip.innerHTML = art.svg();
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

function buildReels(grid) {
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
      const cell = makeCell(grid[r][row]);
      strip.appendChild(cell);
      cells.push(cell);
    }
    reelCells.push(cells);
  }
}

buildReels(ATTRACT_GRID);

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

const CELL_STATE_CLASSES = [
  "win", "dim", "charm-lit", "locked", "just-locked", "blank", "respinning", "collecting",
];

function clearCellStates() {
  for (let r = 0; r < REELS; r += 1) {
    for (let row = 0; row < ROWS; row += 1) {
      reelCells[r][row].classList.remove(...CELL_STATE_CLASSES);
    }
  }
  hidePaylines();
}

// Value / jackpot plate on a landed Charm Ball.
function labelCharm(cell, charm) {
  const pip = cell.querySelector(".pip");
  const old = pip.querySelector(".charm-value");
  if (old) old.remove();
  const plate = document.createElement("span");
  plate.className = "charm-value";
  if (charm.jackpot) {
    plate.classList.add(`jp-${charm.jackpot}`);
    plate.textContent = charm.jackpot.toUpperCase();
  } else {
    plate.textContent = formatMoney(charm.amount);
  }
  pip.appendChild(plate);
  cell.classList.add("charm-lit");
}

// ============================================================
// SERVER API LAYER (authenticated, singleplayer)
// ============================================================

class HandledApiError extends Error {}

function showGate(message, linkHref, linkText) {
  ui.gateMessage.textContent = message;
  ui.gateLink.href = linkHref;
  ui.gateLink.textContent = linkText;
  ui.gate.classList.remove("hidden");
  ui.game.classList.add("hidden");
}

function hideGate() {
  ui.gate.classList.add("hidden");
  ui.game.classList.remove("hidden");
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

// ============================================================
// BETS
// ============================================================

const BET_LADDER_FALLBACK = [0.1, 0.2, 0.5, 1, 2, 5, 10, 20, 50, 100];

function allowedBets() {
  const list = serverState?.allowedBets;
  return (Array.isArray(list) && list.length ? list : BET_LADDER_FALLBACK).map(Number);
}

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

// ============================================================
// SPEED PROFILES
// ============================================================

const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

const SPEED_PROFILES = {
  normal: {
    spinUp: 240, sustain: 500, firstStop: 340, stagger: 165, settle: 320, anticip: 480,
    winHold: 900, noWinHold: 420, smallWinHold: 550,
    respinDur: 640, lockStagger: 260, stepHold: 380, collectStagger: 170, bannerHold: 1500,
  },
  fast: {
    spinUp: 120, sustain: 230, firstStop: 170, stagger: 80, settle: 200, anticip: 250,
    winHold: 500, noWinHold: 240, smallWinHold: 320,
    respinDur: 320, lockStagger: 130, stepHold: 190, collectStagger: 85, bannerHold: 900,
  },
  instant: {
    spinUp: 0, sustain: 0, firstStop: 0, stagger: 0, settle: 0, anticip: 0,
    winHold: 340, noWinHold: 140, smallWinHold: 220,
    respinDur: 90, lockStagger: 40, stepHold: 70, collectStagger: 30, bannerHold: 550,
  },
};

function speed() {
  return SPEED_PROFILES[speedMode] || SPEED_PROFILES.normal;
}

// ============================================================
// ROUNDS — /spin and /buy share the same playback
// ============================================================

async function playPaidRound(endpoint) {
  if (busy || spinning || !serverState) {
    return false;
  }
  if (endpoint === "spin" && !serverState.canSpin) {
    return false;
  }
  if (endpoint === "buy" && !serverState.canBuy) {
    return false;
  }

  // A declined offer-stage gamble panel closes when a new round starts
  // (the server voids the unused offer on the next spin anyway).
  if (gambleOpen && !serverState.gamble?.active) {
    closeGamblePanel();
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
  setStatus(endpoint === "buy" ? "Buying the Charm Bonus…" : "Spinning…");

  let result;
  try {
    result = await apiPost(endpoint, {});
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

  // Refused round (insufficient funds, open gamble, …): just sync state.
  if (!round || !round.baseSpin) {
    spinning = false;
    applyServerState(result);
    updateControls();
    return false;
  }

  await playRound(round);

  // Reveal the authoritative final state after playback.
  spinning = false;
  showResultBanner = true;
  applyServerState(result);
  updateControls();

  if (round.result === "win") {
    playSound("win");
    triggerActionFx("win");
    if (!round.bonus && Number(round.totalWin) >= Number(round.betAmount) * 15) {
      celebrateCharacters(2200);
      showBanner("Big Win", formatMoney(round.totalWin), "grand", speed().bannerHold);
    }
  } else {
    playSound("lose");
  }

  // Manual winning rounds: auto-open the red/black gamble only when the
  // player opted in (never in autospin). Otherwise the win banks and the
  // Gamble button stays lit while the offer lasts.
  const gamble = result.gamble;
  if (autoGamblePrompt && !autoActive && round.result === "win" && gamble && Number(gamble.offer) > 0) {
    openGambleOffer(gamble);
  }

  return { ok: true, feature: !!round.featureTriggered };
}

async function playRound(round) {
  const base = round.baseSpin;
  const sp = speed();

  await spinReelsTo(base.grid);

  // Show the value / jackpot carried by every landed Charm Ball.
  for (const charm of base.charms || []) {
    const cell = reelCells[charm.reel]?.[charm.row];
    if (cell) {
      labelCharm(cell, charm);
    }
  }
  if ((base.scatterCount || 0) >= 3) {
    playSound("scatter");
  }

  // Base-game line wins: draw paylines, light cells, count up.
  const lineWins = base.lineWins || [];
  if (lineWins.length > 0) {
    dimAll();
    for (const w of lineWins) {
      drawPayline(w);
      lightLineWin(w);
    }
    playSound("lineWin");
  }

  const baseWin = Number(base.spinWin || 0);
  if (baseWin > 0) {
    await tallyWin(0, baseWin);
  }

  const smallWin = baseWin > 0 && baseWin < Number(round.betAmount) * 5;
  await sleep(baseWin > 0 ? (smallWin ? sp.smallWinHold : sp.winHold) : sp.noWinHold);

  if (round.bonus) {
    await playBonus(round.bonus, base, round.featureBought);
  }
}

// ============================================================
// REEL SPIN — one continuous JS transform per strip: keeps the
// visible symbols on frame 1, accelerates, cruises with blur,
// then stops left-to-right with an overshoot-and-settle bounce.
// With 2 Charm Balls already down, later reels drag out their
// stop under a golden anticipation glow.
// ============================================================

const SETTLE_BACK = 1.2;

function easeOutBackGentle(t) {
  const c1 = SETTLE_BACK;
  const c3 = c1 + 1;
  return 1 + c3 * Math.pow(t - 1, 3) + c1 * Math.pow(t - 1, 2);
}

async function spinReelsTo(grid) {
  const reelEls = Array.from(ui.reels.querySelectorAll(".reel"));
  const sp = speed();

  if (speedMode === "instant") {
    setReelGrid(grid);
    playSound("reelStop");
    return;
  }

  const cellH = reelCells[0][0] ? reelCells[0][0].getBoundingClientRect().height : 0;
  if (!cellH) {
    setReelGrid(grid);
    playSound("reelStop");
    return;
  }

  // Anticipation is known up front (the grid is already decided): once two
  // Charm Balls sit on earlier reels, the later reels stretch their stop.
  let charms = 0;
  const anticipates = [];
  for (let r = 0; r < REELS; r += 1) {
    anticipates.push(charms >= 2 && r >= 2);
    charms += grid[r].filter((k) => k === "charm").length;
  }

  const msPerCell = speedMode === "fast" ? 42 : 58;
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

function spinOneReel(reelEl, column, cfg) {
  return new Promise((resolve) => {
    const { spinUp, cruise, settle, cellH, vmaxBase } = cfg;
    const strip = reelEl.querySelector(".reel-strip");
    const reelIdx = Number(reelEl.dataset.reelIndex);
    const currentKeys = reelCells[reelIdx].map((cell) => cell.dataset.key || randKey());

    // Snap total travel to whole cells; derive an exact cruise velocity.
    const timeFactor = 0.5 * spinUp + cruise + settle / (SETTLE_BACK + 3);
    const cells = Math.max(6, Math.min(80, Math.round((vmaxBase * timeFactor) / cellH)));
    const travel = cells * cellH;
    const v = travel / timeFactor;
    const distAccel = 0.5 * v * spinUp;
    const distSettle = (v * settle) / (SETTLE_BACK + 3);
    const cruiseDist = travel - distAccel - distSettle;
    const totalDur = spinUp + cruise + settle;

    // Rebuild the strip top-to-bottom: spare overshoot row, the final
    // column, random filler, then the currently visible symbols — so
    // frame 1 is pixel-identical to the resting reel.
    const startOffset = (cells + 1) * cellH;
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
      setTimeout(() => {
        reelEl.classList.add("anticipate");
        playSound("anticipate");
      }, glowAt);
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
        if (s > 0.12) {
          reelEl.classList.remove("spinning");
        }
      }
      strip.style.transform = `translateY(${y - startOffset}px)`;
      requestAnimationFrame(frame);
    };
    requestAnimationFrame(frame);
    // Safety net (rAF pauses in background tabs).
    setTimeout(finish, totalDur + 400);
  });
}

function dimAll() {
  for (let r = 0; r < REELS; r += 1) {
    for (let row = 0; row < ROWS; row += 1) {
      reelCells[r][row].classList.add("dim");
    }
  }
}

function lightLineWin(w) {
  (w.cells || []).forEach((row, reel) => {
    const cell = reelCells[reel]?.[row];
    if (cell) {
      cell.classList.remove("dim");
      cell.classList.add("win");
    }
  });
}

// ============================================================
// CHARM BONUS — Hold & Win playback
// ============================================================

let bonusLocked = []; // bonusLocked[reel][row] -> charm or null

function emptyLockedMap() {
  return Array.from({ length: REELS }, () => Array(ROWS).fill(null));
}

function setRespinDisplay(value, isReset) {
  ui.respinCount.textContent = String(value);
  ui.respinCount.classList.remove("pop");
  void ui.respinCount.offsetWidth;
  if (isReset) {
    ui.respinCount.classList.add("pop");
  }
}

function setLockedDisplay(count) {
  ui.lockedCount.textContent = `${count} / ${REELS * ROWS}`;
}

function lockCharmCell(charm, justLanded) {
  const cell = reelCells[charm.reel]?.[charm.row];
  if (!cell) {
    return;
  }
  bonusLocked[charm.reel][charm.row] = charm;
  cell.classList.remove("blank", "respinning", "dim", "win");
  setCellSymbol(cell, "charm");
  labelCharm(cell, charm);
  cell.classList.add("locked");
  if (justLanded) {
    cell.classList.add("just-locked");
    setTimeout(() => cell.classList.remove("just-locked"), 450);
  }
}

function setUnlockedCellsRespinning(on) {
  for (let r = 0; r < REELS; r += 1) {
    for (let row = 0; row < ROWS; row += 1) {
      if (!bonusLocked[r][row]) {
        reelCells[r][row].classList.toggle("respinning", on);
      }
    }
  }
}

async function playBonus(bonus, baseSpin, featureBought) {
  const sp = speed();

  celebrateCharacters(3000);
  playSound("feature");
  triggerActionFx("bonus");
  startBonusMusic();
  await showBanner(
    "Charm Bonus",
    featureBought ? "Feature bought — 3 respins" : "3 respins — every new charm locks",
    "bonus",
    sp.bannerHold
  );

  // Enter bonus mode: lock the triggering balls, empty everything else.
  clearCellStates();
  hidePaylines();
  hideWinMeter();
  ui.reelsFrame.classList.add("bonus");
  ui.bonusHud.classList.remove("hidden");
  bonusLocked = emptyLockedMap();

  for (let r = 0; r < REELS; r += 1) {
    for (let row = 0; row < ROWS; row += 1) {
      const cell = reelCells[r][row];
      setCellSymbol(cell, "__blank");
      cell.classList.add("blank");
    }
  }
  let lockedTotal = 0;
  for (const charm of bonus.initialCharms || []) {
    lockCharmCell(charm, true);
    lockedTotal += 1;
    playSound("charmLock");
    await sleep(sp.lockStagger);
  }
  setRespinDisplay(3, true);
  setLockedDisplay(lockedTotal);
  setStatus("Charm Bonus — 3 respins. New charms lock and reset!");
  await sleep(sp.stepHold);

  // Play back each recorded respin.
  for (const step of bonus.respins || []) {
    setUnlockedCellsRespinning(true);
    playSound("respin");
    await sleep(sp.respinDur);
    setUnlockedCellsRespinning(false);

    const news = step.newCharms || [];
    if (news.length > 0) {
      for (const charm of news) {
        lockCharmCell(charm, true);
        lockedTotal += 1;
        playSound("charmLock");
        if (charm.jackpot) {
          playSound("scatter");
        }
        setLockedDisplay(lockedTotal);
        await sleep(sp.lockStagger);
      }
      setRespinDisplay(step.respinsLeft, true);
      setStatus(`${news.length} new charm${news.length === 1 ? "" : "s"} locked — respins reset to ${step.respinsLeft}.`);
    } else {
      setRespinDisplay(step.respinsLeft, false);
      setStatus(step.respinsLeft > 0 ? `No new charms — ${step.respinsLeft} respin${step.respinsLeft === 1 ? "" : "s"} left.` : "No new charms — collecting…");
    }
    await sleep(sp.stepHold);
  }

  if (bonus.fullGrid) {
    playSound("jackpot");
    triggerActionFx("jackpot");
    celebrateCharacters(3000);
    await showBanner("Full Grid", "COLLECT ALL — every charm pays", "grand", sp.bannerHold + 400);
  }

  await collectCharms(bonus);

  // Exit bonus mode; restore the base-game grid underneath.
  ui.reelsFrame.classList.remove("bonus");
  ui.bonusHud.classList.add("hidden");
  stopBonusMusic();
  clearCellStates();
  setReelGrid(baseSpin.grid);
  for (const charm of baseSpin.charms || []) {
    const cell = reelCells[charm.reel]?.[charm.row];
    if (cell) {
      labelCharm(cell, charm);
    }
  }
}

// Tally every locked ball into the win meter, jackpots with fanfare.
async function collectCharms(bonus) {
  const sp = speed();
  showWinMeter(0);
  let running = 0;

  for (let r = 0; r < REELS; r += 1) {
    for (let row = 0; row < ROWS; row += 1) {
      const charm = bonusLocked[r][row];
      if (!charm) {
        continue;
      }
      const cell = reelCells[r][row];
      cell.classList.remove("collecting");
      void cell.offsetWidth;
      cell.classList.add("collecting");

      if (charm.jackpot) {
        playSound("jackpot");
        triggerActionFx("jackpot");
        celebrateCharacters(2000);
        await showBanner(`${charm.jackpot} Jackpot`, formatMoney(charm.amount), charm.jackpot, sp.bannerHold);
      }

      running += Number(charm.amount || 0);
      showWinMeter(running);
      playSound("coin");
      await sleep(sp.collectStagger);
    }
  }

  // The server total is authoritative — settle the meter on it exactly.
  await tallyWin(running, Number(bonus.totalWin || 0));
  playSound("win");
  triggerActionFx("win");
  setStatus(`Charm Bonus paid ${formatMoney(bonus.totalWin)}!`);
  await sleep(sp.winHold);
}

// ============================================================
// PAYLINE OVERLAY
// ============================================================

const PAYLINE_COLORS = [
  "#8bd450", "#ff53b8", "#ffd24a", "#4ecdc4", "#ff6b6b",
  "#b98aff", "#c0ff5a", "#ff952a", "#6bd0ff", "#ff8ad1",
];

function drawPayline(w) {
  const overlay = ui.paylineOverlay;
  if (!overlay.getAttribute("viewBox")) {
    overlay.setAttribute("viewBox", "0 0 100 100");
  }
  const color = PAYLINE_COLORS[((w.line || 1) - 1) % PAYLINE_COLORS.length];
  const pts = (w.cells || [])
    .map((row, reel) => `${(((reel + 0.5) / REELS) * 100).toFixed(2)},${(((row + 0.5) / ROWS) * 100).toFixed(2)}`)
    .join(" ");
  const poly = document.createElementNS("http://www.w3.org/2000/svg", "polyline");
  poly.setAttribute("points", pts);
  poly.setAttribute("stroke", color);
  poly.setAttribute("stroke-dasharray", "600");
  poly.style.color = color;
  overlay.appendChild(poly);
  void poly.getBBox(); // force reflow so the draw animation runs
  poly.classList.add("show");
}

function hidePaylines() {
  ui.paylineOverlay.innerHTML = "";
}

// ============================================================
// WIN METER
// ============================================================

function showWinMeter(value) {
  ui.winMeter.classList.remove("hidden");
  ui.winMeterValue.textContent = formatMoney(value);
}

function hideWinMeter() {
  ui.winMeter.classList.add("hidden");
  ui.winMeterValue.textContent = formatMoney(0);
}

function tallyWin(from, to) {
  return new Promise((resolve) => {
    if (Math.abs(to - from) < 0.005) {
      showWinMeter(to);
      resolve(to);
      return;
    }
    const duration = speedMode === "instant" ? 300 : speedMode === "fast" ? 420 : 620;
    const start = performance.now();
    showWinMeter(from);
    let lastTick = 0;
    const step = (now) => {
      const t = Math.min(1, (now - start) / duration);
      const eased = 1 - Math.pow(1 - t, 3);
      const value = from + (to - from) * eased;
      ui.winMeterValue.textContent = formatMoney(value);
      if (now - lastTick > 65) {
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

// ============================================================
// GAMBLE — red/black double-or-nothing on the last win.
// ============================================================

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

function openGambleOffer(gamble) {
  openGamblePanelBase();
  setGambleStake(Number(gamble.offer || 0), "Your win");
  ui.gambleMsg.textContent = "Pick a colour to double it — or keep the win.";
  renderGambleHistory([]);
  resetGambleCard(false);
  updateGambleButtons();
  playSound("bet");
}

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
  resetGambleCard(true);
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

  // Refused pick (expired session, nothing staked): sync and close.
  if (gamble.lastWon !== true && gamble.lastWon !== false) {
    applyServerState(state);
    closeGamblePanel();
    return;
  }

  await sleep(380);
  if (card) {
    showGambleCardFace(card);
    playSound("reelStop");
    await sleep(520);
  }
  renderGambleHistory(gamble.history);
  applyServerState(state);

  if (gamble.lastWon) {
    playSound("win");
    ui.gambleCard.classList.add("won");
    setGambleStake(Number(gamble.stake || 0), "On the table");
    ui.gambleMsg.textContent = `Correct! ${formatMoney(gamble.stake)} on the table — double again or collect.`;
    gambleBusy = false;
    updateGambleButtons();
  } else {
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
    // Offer stage: the win is already on the balance — decline and close.
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

// — Gamble button (next to Spin): opens the overlay while an offer or an
//   in-progress red/black round is available. —

ui.gambleBtn.addEventListener("click", () => {
  if (ui.gambleBtn.disabled || gambleOpen) {
    return;
  }
  const gamble = serverState?.gamble;
  if (!gamble) {
    return;
  }
  animateButtonPress(ui.gambleBtn);
  playSound("button");
  if (gamble.active) {
    openGambleActive(gamble);
  } else if (Number(gamble.offer) > 0) {
    openGambleOffer(gamble);
  }
});

// — Dismissing the overlay (Escape / backdrop click) acts like "Keep Win",
//   but only when it is safe: a pick or collect in flight (gambleBusy —
//   including the forced post-loss hold) simply ignores the dismissal. —

function tryDismissGamble() {
  if (!gambleOpen || gambleBusy) {
    return;
  }
  collectGamble();
}

ui.gamblePanel.addEventListener("click", (e) => {
  if (e.target === ui.gamblePanel) {
    tryDismissGamble();
  }
});

document.addEventListener("keydown", (e) => {
  if (e.key === "Escape" && gambleOpen) {
    e.preventDefault();
    tryDismissGamble();
  }
});

// — Auto-gamble prompt checkbox (persisted; default off) —

function setAutoGamblePrompt(on) {
  autoGamblePrompt = !!on;
  ui.autoGambleToggle.checked = autoGamblePrompt;
  try {
    localStorage.setItem("slot.autoGamblePrompt", autoGamblePrompt ? "1" : "0");
  } catch {
    /* storage unavailable */
  }
}

ui.autoGambleToggle.addEventListener("change", () => {
  setAutoGamblePrompt(ui.autoGambleToggle.checked);
  playSound("button");
});

(function initAutoGamblePrompt() {
  let saved = "0";
  try {
    saved = localStorage.getItem("slot.autoGamblePrompt") || "0";
  } catch {
    /* ignore */
  }
  setAutoGamblePrompt(saved === "1");
})();

// ============================================================
// RENDERING (version-gated)
// ============================================================

function formatMoney(value) {
  const amount = Number(value ?? 0);
  return `$${amount.toFixed(2)}`;
}

function setStatus(text, danger = false) {
  ui.status.textContent = text;
  ui.status.classList.toggle("danger", danger);
}

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
  const lockedOut = busy || spinning || autoActive || !serverState;

  ui.spinBtn.disabled = lockedOut || !serverState?.canSpin || insufficient;
  ui.spinBtn.textContent = spinning ? "Spinning…" : insufficient ? "No Funds" : "Spin";

  // Gamble is available while last spin's win is still on offer, or while
  // a red/black round is in progress (the server voids offers on new spins).
  const gamble = serverState?.gamble;
  const gambleAvailable = !!gamble && (Number(gamble.offer) > 0 || gamble.active === true);
  ui.gambleBtn.disabled = lockedOut || !gambleAvailable;

  ui.buyBtn.disabled = lockedOut || !serverState?.canBuy;
  if (ui.buyBtn.disabled && !ui.buyConfirm.classList.contains("hidden")) {
    ui.buyConfirm.classList.add("hidden");
  }

  ui.autospinBtn.disabled = lockedOut || !serverState?.canSpin || insufficient;
  ui.autoCounts.forEach((b) => {
    b.disabled = autoActive;
    b.classList.toggle("active", Number(b.dataset.count) === autoSelectedCount);
  });
}

function bumpJackpot(el) {
  const plate = el.closest(".jackpot");
  if (!plate) {
    return;
  }
  plate.classList.remove("bump");
  void plate.offsetWidth;
  plate.classList.add("bump");
}

function renderJackpots() {
  const jp = serverState?.jackpots;
  if (!jp) {
    return;
  }
  const entries = [
    [ui.jackpotMini, jp.mini],
    [ui.jackpotMajor, jp.major],
    [ui.jackpotGrand, jp.grand],
  ];
  for (const [el, value] of entries) {
    const text = formatMoney(value);
    if (el.textContent !== text) {
      el.textContent = text;
      bumpJackpot(el);
    }
  }
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
  ui.spinsCount.textContent = `Spins: ${serverState.spins ?? 0}`;
  ui.featureCount.textContent = `Bonuses: ${serverState.featureHits ?? 0}`;
  animateBalanceChange(ui.balance);

  if (betIndex(Number(serverState.selectedBet)) >= 0) {
    selectedBet = Number(serverState.selectedBet);
  }

  renderJackpots();

  const buyCost = formatMoney(serverState.featureBuyCost);
  ui.buyCost.textContent = buyCost;
  ui.buyConfirmCost.textContent = buyCost;

  updateControls();
  renderPaytable();

  if (showResultBanner) {
    showResultBanner = false;
    const win = Number(serverState.lastWin ?? 0);
    if (win > 0) {
      ui.roundResult.textContent = `WIN ${formatMoney(win)}`;
      ui.roundResult.className = "round-result win";
      showWinMeter(win);
    } else {
      ui.roundResult.textContent = "NO WIN";
      ui.roundResult.className = "round-result loss";
    }
    animateRoundResult(ui.roundResult);
  }

  if (serverState.status && !spinning) {
    setStatus(serverState.status);
  }

  // Re-attach to an in-progress gamble (page reload / focus re-sync).
  if (serverState.gamble && serverState.gamble.active && !gambleOpen && !spinning) {
    openGambleActive(serverState.gamble);
  }
}

// ============================================================
// PAYTABLE
// ============================================================

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
    if (sym.isWild || sym.isScatter) {
      const tag = document.createElement("span");
      tag.className = "tag";
      tag.textContent = sym.isWild ? "WILD" : "SCATTER";
      name.appendChild(tag);
    }

    const values = document.createElement("span");
    if (sym.isScatter) {
      values.className = "pay-scatter-note";
      values.textContent = "Cash or jackpot on every ball · 3+ trigger the Charm Bonus";
    } else {
      values.className = "pay-values";
      values.innerHTML = `3: <b>${sym.pay3}×</b> · 4: <b>${sym.pay4}×</b> · 5: <b>${sym.pay5}×</b>`;
    }

    info.append(name, values);
    card.append(symBox, info);
    ui.paytable.appendChild(card);
  });
}

// ============================================================
// BANNERS + CHARACTER REACTIONS + VFX
// ============================================================

function showBanner(title, sub, theme, holdMs) {
  return new Promise((resolve) => {
    const banner = document.createElement("div");
    banner.className = `banner theme-${theme || "bonus"}`;
    const t = document.createElement("span");
    t.className = "banner-title";
    t.textContent = title;
    banner.appendChild(t);
    if (sub) {
      const s = document.createElement("span");
      s.className = "banner-sub";
      s.textContent = sub;
      banner.appendChild(s);
    }
    ui.bannerLayer.appendChild(banner);
    setTimeout(() => {
      banner.classList.add("out");
      setTimeout(() => {
        banner.remove();
        resolve();
      }, 300);
    }, Math.max(400, holdMs || 1200));
  });
}

let celebrateTimer = null;
function celebrateCharacters(ms) {
  ui.charLady?.classList.add("celebrate");
  ui.charLep?.classList.add("celebrate");
  if (celebrateTimer) {
    clearTimeout(celebrateTimer);
  }
  celebrateTimer = setTimeout(() => {
    ui.charLady?.classList.remove("celebrate");
    ui.charLep?.classList.remove("celebrate");
    celebrateTimer = null;
  }, ms || 2000);
}

function triggerActionFx(kind) {
  if (!ui.vfxLayer) {
    return;
  }
  const config = {
    spin: { count: 26, color: "#c8e26a", spread: 220, flash: "flash-spin" },
    win: { count: 90, color: "#ffd24a", spread: 420, flash: "flash-win" },
    bonus: { count: 120, color: "#8bd450", spread: 480, flash: "flash-bonus" },
    jackpot: { count: 160, color: "#ffe98f", spread: 560, flash: "flash-jackpot" },
  }[kind];
  if (!config) {
    return;
  }

  const rect = ui.vfxLayer.getBoundingClientRect();
  const centerX = rect.width * (0.38 + Math.random() * 0.24);
  const centerY = rect.height * (0.3 + Math.random() * 0.25);

  for (let i = 0; i < config.count; i += 1) {
    const piece = document.createElement("span");
    const angle = Math.random() * Math.PI * 2;
    const dist = 80 + Math.random() * config.spread;
    const size = 4 + Math.random() * 10;
    piece.className = "fx-particle";
    piece.style.setProperty("--fx-x", `${centerX}px`);
    piece.style.setProperty("--fx-y", `${centerY}px`);
    piece.style.setProperty("--fx-dx", `${Math.cos(angle) * dist}`);
    piece.style.setProperty("--fx-dy", `${Math.sin(angle) * dist * 0.7}`);
    piece.style.setProperty("--fx-color", config.color);
    piece.style.width = `${size}px`;
    piece.style.height = `${size}px`;
    piece.style.opacity = `${0.5 + Math.random() * 0.5}`;
    ui.vfxLayer.appendChild(piece);
    setTimeout(() => piece.remove(), 950);
  }

  if (kind !== "spin") {
    const ring = document.createElement("span");
    ring.className = "fx-ring";
    ring.style.setProperty("--fx-x", `${centerX}px`);
    ring.style.setProperty("--fx-y", `${centerY}px`);
    ring.style.setProperty("--fx-color", config.color);
    ui.vfxLayer.appendChild(ring);
    setTimeout(() => ring.remove(), 800);
  }

  document.body.classList.remove("action-flash", "flash-spin", "flash-win", "flash-bonus", "flash-jackpot");
  void document.body.offsetWidth;
  document.body.classList.add("action-flash", config.flash);
  setTimeout(() => {
    document.body.classList.remove("action-flash", config.flash);
  }, 650);
}

// ============================================================
// AUDIO — original WebAudio cues (no ripped game audio).
// ============================================================

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
  anticipate: () => {
    playTone(392, 0.3, "sine", 0.035);
    playTone(466, 0.3, "sine", 0.03, 0.05);
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
  charmLock: () => {
    // A weighty thud as a ball locks in.
    playTone(72, 0.16, "sine", 0.09);
    playTone(140, 0.07, "triangle", 0.05, 0.01);
    playNoise(0.08, 0.05, 300);
  },
  respin: () => {
    playNoise(0.18, 0.035, 1100);
    playTone(220, 0.08, "square", 0.025, 0.02);
  },
  feature: () => {
    [523, 659, 784, 1046, 1318].forEach((f, i) => {
      playTone(f, 0.18, "triangle", 0.07, i * 0.12);
    });
    playNoise(0.5, 0.03, 2200);
  },
  jackpot: () => {
    [392, 523, 659, 784, 1046, 1318, 1568].forEach((f, i) => {
      playTone(f, 0.22, "triangle", 0.07, i * 0.1);
    });
    playNoise(0.7, 0.035, 2600);
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
};

function playSound(kind) {
  const cue = SOUND_CUES[kind];
  if (cue) {
    cue();
  }
}

// ============================================================
// BONUS MUSIC — a gentle pastoral jig loop synthesized with
// WebAudio. Starts with the Charm Bonus, stops after; respects
// the mute button (mute silences instantly, unmute resumes).
// ============================================================

const music = { active: false, timer: null, gain: null };
const MUSIC_VOLUME = 0.9;

const JIG_STEP = 0.3; // seconds per melody step
const JIG_MELODY = [
  349.23, 440.0, 523.25, 587.33, 523.25, 440.0, 392.0, 440.0,
  349.23, 440.0, 523.25, 698.46, 587.33, 523.25, 440.0, 392.0,
  293.66, 349.23, 440.0, 523.25, 440.0, 349.23, 329.63, 349.23,
  392.0, 440.0, 523.25, 587.33, 659.25, 587.33, 523.25, 440.0,
];
const JIG_BASS = [174.61, 130.81, 146.83, 174.61];

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

function scheduleJigLoop() {
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
  JIG_MELODY.forEach((freq, i) => {
    const at = t0 + i * JIG_STEP;
    musicNote(ctx, freq, at, JIG_STEP * 1.6, "triangle", 0.035);
    if (i % 4 === 2) {
      musicNote(ctx, freq * 2, at + JIG_STEP * 0.5, JIG_STEP * 0.9, "sine", 0.012);
    }
    if (i % 8 === 0) {
      const bass = JIG_BASS[(i / 8) % JIG_BASS.length];
      musicNote(ctx, bass, at, JIG_STEP * 7.5, "sine", 0.03);
      musicNote(ctx, bass * 1.5, at, JIG_STEP * 7.5, "sine", 0.016);
    }
  });

  const loopMs = JIG_MELODY.length * JIG_STEP * 1000;
  music.timer = setTimeout(scheduleJigLoop, loopMs - 120);
}

function startBonusMusic() {
  if (music.active) {
    return;
  }
  music.active = true;
  scheduleJigLoop();
}

function stopBonusMusic() {
  music.active = false;
  if (music.timer) {
    clearTimeout(music.timer);
    music.timer = null;
  }
  if (music.gain && audioContext) {
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
    scheduleJigLoop();
  }
}

ui.muteBtn.addEventListener("click", () => {
  setMuted(!muted);
  if (!muted) {
    playSound("button");
  }
});

// ============================================================
// EVENT WIRING
// ============================================================

ui.spinBtn.addEventListener("click", () => {
  animateButtonPress(ui.spinBtn);
  ui.buyConfirm.classList.add("hidden");
  playPaidRound("spin");
});

ui.betDown.addEventListener("click", () => stepBet(-1));
ui.betUp.addEventListener("click", () => stepBet(1));
ui.betMin.addEventListener("click", () => setBet(allowedBets()[0]));
ui.betMax.addEventListener("click", () => {
  const bets = allowedBets();
  setBet(bets[bets.length - 1]);
});

// — Feature Buy (with confirm step) —

ui.buyBtn.addEventListener("click", () => {
  if (ui.buyBtn.disabled) {
    return;
  }
  animateButtonPress(ui.buyBtn);
  playSound("button");
  ui.buyConfirm.classList.toggle("hidden");
});

ui.buyNo.addEventListener("click", () => {
  playSound("button");
  ui.buyConfirm.classList.add("hidden");
});

ui.buyYes.addEventListener("click", () => {
  ui.buyConfirm.classList.add("hidden");
  playPaidRound("buy");
});

document.addEventListener("click", (e) => {
  if (!ui.buyConfirm.classList.contains("hidden") && !e.target.closest(".buy-wrap")) {
    ui.buyConfirm.classList.add("hidden");
  }
});

// — Speed control (persisted) —

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

// — Autospin —

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

function startAutospin() {
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
    const outcome = await playPaidRound("spin");
    if (!autoActive) {
      return;
    }
    if (!outcome || !outcome.ok) {
      stopAutospin("Autospin stopped.");
      return;
    }

    if (autoRemaining > 0) {
      autoRemaining -= 1;
    }
    updateAutospinUi();

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

ui.autoCounts.forEach((b) => b.classList.toggle("active", Number(b.dataset.count) === autoSelectedCount));

// ============================================================
// INIT — fetch once on load and after each action; re-sync on focus.
// ============================================================

(async function init() {
  await refreshState();
  renderPaytable();
})();

window.addEventListener("focus", async () => {
  await refreshState();
  renderPaytable();
});

// ============================================================
// ANIME.JS HELPERS (all optional — degrade gracefully)
// ============================================================

function animateButtonPress(el) {
  if (!window.anime) return;
  anime({ targets: el, scale: [1, 0.95, 1], duration: 200, easing: "easeOutQuad" });
}

function animateBalanceChange(el) {
  if (!window.anime) return;
  anime({ targets: el, scale: [1, 1.12, 1], duration: 380, easing: "easeOutQuad" });
}

function animateRoundResult(el) {
  if (!window.anime) return;
  anime.set(el, { scale: 0.6, opacity: 0 });
  anime({
    targets: el,
    scale: [0.6, 1.1, 1],
    opacity: 1,
    duration: 550,
    easing: "easeOutBack",
  });
}

function reelStopBounce(reelEl) {
  if (!window.anime) return;
  anime({
    targets: reelEl,
    translateY: [-9, 0],
    duration: 300,
    easing: "easeOutElastic(1, .6)",
  });
}

function animateWinMeterPop() {
  if (!window.anime) return;
  anime({ targets: ui.winMeter, scale: [1, 1.12, 1], duration: 300, easing: "easeOutBack" });
}
