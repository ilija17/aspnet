const API_BASE = "/api/slot";
const LOGIN_URL = "/Account/Login?returnUrl=/slot/index.html";
const PROFILE_URL = "/Account/Profile";

const REELS = 5;
const ROWS = 3;

// Visual presentation for each symbol key. Picture symbols use emoji; the card
// ranks render as styled plaques. `lady` is wild + scatter and always special.
const SYMBOL_ART = {
  lady: { glyph: "💃", kind: "lady" },
  clover: { glyph: "🍀", kind: "pic" },
  ladybug: { glyph: "🐞", kind: "pic" },
  horseshoe: { glyph: "🧲", kind: "pic" },
  coin: { glyph: "🪙", kind: "pic" },
  ace: { glyph: "A", kind: "card ace" },
  king: { glyph: "K", kind: "card king" },
  queen: { glyph: "Q", kind: "card queen" },
  jack: { glyph: "J", kind: "card jack" },
  ten: { glyph: "10", kind: "card ten" },
};

// Fallback symbol order, only used to build idle reels before the first state.
const FALLBACK_KEYS = ["lady", "clover", "ladybug", "horseshoe", "coin", "ace", "king", "queen", "jack", "ten"];

let serverState = null;
let lastRenderedVersion = -1;
let selectedBet = null; // client-side: chosen total bet
let busy = false; // a POST is in flight
let spinning = false; // a full round (incl. free spins) is replaying
let showResultBanner = false;
let audioContext;

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
  chips: document.getElementById("chips"),
  spinBtn: document.getElementById("spin-btn"),
  paytable: document.getElementById("paytable"),
  vfxLayer: document.getElementById("vfx-layer"),
};

// reelCells[reel][row] -> the .cell element currently visible in that position.
const reelCells = [];

// ============================================
// REEL CONSTRUCTION
// ============================================

function symbolArt(key) {
  return SYMBOL_ART[key] || { glyph: key, kind: "pic" };
}

function makeCell(key) {
  const cell = document.createElement("div");
  cell.className = "cell";
  cell.dataset.key = key;
  const pip = document.createElement("span");
  const art = symbolArt(key);
  pip.className = `pip ${art.kind}`;
  pip.textContent = art.glyph;
  cell.appendChild(pip);
  return cell;
}

function buildReels(initialGrid) {
  ui.reels.innerHTML = "";
  reelCells.length = 0;
  for (let r = 0; r < REELS; r += 1) {
    const reel = document.createElement("div");
    reel.className = "reel";
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
  const art = symbolArt(key);
  const pip = cell.querySelector(".pip");
  pip.className = `pip ${art.kind}`;
  pip.textContent = art.glyph;
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
  if (busy || spinning) {
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

function allowedBets() {
  const list = serverState?.allowedBets;
  return Array.isArray(list) && list.length ? list : [10, 20, 50, 100, 200];
}

async function setBet(amount) {
  if (busy || spinning || !serverState) {
    return;
  }
  busy = true;
  selectedBet = amount;
  playSound("bet");
  triggerActionFx("bet");
  renderChips();
  try {
    applyServerState(await apiPost("bet", { amount }));
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

async function spin() {
  if (busy || spinning || !serverState || !serverState.canSpin) {
    return;
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
    return;
  }

  busy = false;
  const round = result.round;

  if (!round || !Array.isArray(round.spins) || round.spins.length === 0) {
    spinning = false;
    showResultBanner = true;
    applyServerState(result);
    updateControls();
    return;
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
}

// ============================================
// ROUND PLAYBACK (one-shot; the client replays the recorded spins)
// ============================================

const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

async function playRound(round) {
  let runningWin = 0;
  setReelsFeature(false);
  hideFreeBanner();

  // Base spin.
  runningWin = await playSpin(round.spins[0], runningWin, { free: false });

  // Feature: replay the free spins with distinct presentation.
  if (round.featureTriggered && round.spins.length > 1) {
    await announceFeature(round.freeSpinsAwarded);
    setReelsFeature(true);
    const freeSpins = round.spins.slice(1);
    for (let i = 0; i < freeSpins.length; i += 1) {
      showFreeBanner(freeSpins.length - i);
      runningWin = await playSpin(freeSpins[i], runningWin, { free: true });
      await sleep(360);
    }
    hideFreeBanner();
    setReelsFeature(false);
  }
}

async function playSpin(spin, runningWin, { free }) {
  clearCellStates();
  await spinReelsTo(spin.grid);

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

  await sleep(lineWins.length > 0 || spin.scatterCount >= 3 ? 950 : 520);
  return runningWin;
}

// Animate every reel: spin a blur, then stop reels one-by-one left to right.
async function spinReelsTo(grid) {
  const reelEls = Array.from(ui.reels.querySelectorAll(".reel"));
  reelEls.forEach((reel) => reel.classList.add("spinning"));
  spinReelWhir();

  for (let r = 0; r < REELS; r += 1) {
    await sleep(r === 0 ? 360 : 150);
    // Drop in the final symbols for this reel as it "stops".
    for (let row = 0; row < ROWS; row += 1) {
      setCellSymbol(reelCells[r][row], grid[r][row]);
    }
    reelEls[r].classList.remove("spinning");
    reelStopBounce(reelEls[r]);
    playSound("reelStop");
  }
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

function showFreeBanner(remaining) {
  ui.freeBanner.classList.remove("hidden");
  ui.freeBannerCount.textContent = String(remaining);
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
  if (allowedBets().includes(fromServer)) {
    return fromServer;
  }
  if (allowedBets().includes(selectedBet)) {
    return selectedBet;
  }
  return allowedBets()[0];
}

function renderChips() {
  const bets = allowedBets();
  const active = effectiveBet();
  // Rebuild only if the set of chips changed.
  const want = bets.join(",");
  if (ui.chips.dataset.bets !== want) {
    ui.chips.dataset.bets = want;
    ui.chips.innerHTML = "";
    bets.forEach((amount) => {
      const chip = document.createElement("button");
      chip.type = "button";
      chip.className = "chip";
      chip.dataset.bet = String(amount);
      chip.textContent = `$${amount}`;
      chip.addEventListener("click", () => {
        if (chip.disabled) {
          return;
        }
        animateChipSelect(chip);
        setBet(amount);
      });
      ui.chips.appendChild(chip);
    });
  }
  Array.from(ui.chips.children).forEach((chip) => {
    const value = Number(chip.dataset.bet);
    chip.classList.toggle("active", value === active);
    const tooDear = serverState ? value > Number(serverState.balance) && value !== active : false;
    chip.disabled = busy || spinning || tooDear;
  });
}

function updateControls() {
  renderChips();
  const bet = effectiveBet();
  const insufficient = serverState ? Number(serverState.balance) < bet : false;
  ui.spinBtn.disabled =
    busy || spinning || !serverState || !serverState.canSpin || insufficient;
  ui.spinBtn.textContent = spinning ? "Spinning…" : insufficient ? "No Funds" : "Spin";
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
  ui.totalBet.textContent = `Bet: $${effectiveBet()}`;
  ui.spinsCount.textContent = `Spins: ${serverState.spins ?? 0}`;
  ui.featureCount.textContent = `Features: ${serverState.featureHits ?? 0}`;
  animateBalanceChange(ui.balance);

  if (allowedBets().includes(Number(serverState.selectedBet))) {
    selectedBet = Number(serverState.selectedBet);
  }

  ui.betDisplay.textContent = `Total Bet: $${effectiveBet()} · Line Bet: ${formatMoney(effectiveBet() / 10)}`;

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

    const art = symbolArt(sym.key);
    const symBox = document.createElement("div");
    symBox.className = "pay-symbol";
    const pip = document.createElement("span");
    pip.className = `pip ${art.kind}`;
    pip.textContent = art.glyph;
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

function setMuted(value) {
  muted = value;
  ui.muteBtn.classList.toggle("muted", muted);
  ui.muteBtn.textContent = muted ? "🔇" : "🔊";
  ui.muteBtn.setAttribute("aria-pressed", muted ? "true" : "false");
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

function animateChipSelect(el) {
  if (!window.anime) return;
  anime({ targets: el, scale: [1, 1.2, 1], duration: 300, easing: "easeOutBack" });
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
