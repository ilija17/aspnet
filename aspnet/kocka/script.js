const SUITS = [
  { key: "hearts", symbol: "\u2665", color: "red" },
  { key: "diamonds", symbol: "\u2666", color: "red" },
  { key: "clubs", symbol: "\u2663", color: "black" },
  { key: "spades", symbol: "\u2660", color: "black" },
];

const RANKS = [
  { key: "A", value: 11 },
  { key: "K", value: 10 },
  { key: "Q", value: 10 },
  { key: "J", value: 10 },
  { key: "10", value: 10 },
  { key: "9", value: 9 },
  { key: "8", value: 8 },
  { key: "7", value: 7 },
  { key: "6", value: 6 },
  { key: "5", value: 5 },
  { key: "4", value: 4 },
  { key: "3", value: 3 },
  { key: "2", value: 2 },
];

const STARTING_BALANCE = 1000;
const STORAGE_KEY = "blackjack-multiplayer-v1";
const CHANNEL_NAME = "blackjack-multiplayer-channel";
const CLIENT_KEY = "blackjack-multiplayer-client-id";

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
let localSeat = null;
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

const channel = "BroadcastChannel" in window ? new BroadcastChannel(CHANNEL_NAME) : null;
let gameState = loadOrCreateState();

function createPlayer() {
  return {
    id: null,
    balance: STARTING_BALANCE,
    selectedBet: 25,
    currentBet: 0,
    hand: [],
    stood: false,
    bust: false,
    blackjack: false,
    wins: 0,
    losses: 0,
    pushes: 0,
  };
}

function createDefaultState() {
  return {
    version: 1,
    phase: "waiting",
    status: "Join Seat 1 or Seat 2 to play.",
    soloMode: false,
    dealerHand: [],
    revealDealer: false,
    deck: [],
    turnSeat: null,
    lastRoundResults: { 1: null, 2: null },
    players: {
      1: createPlayer(),
      2: createPlayer(),
    },
  };
}

function isValidState(data) {
  return (
    data &&
    typeof data === "object" &&
    data.players &&
    data.players["1"] &&
    data.players["2"] &&
    Array.isArray(data.dealerHand) &&
    Array.isArray(data.deck)
  );
}

function loadOrCreateState() {
  const raw = localStorage.getItem(STORAGE_KEY);
  if (!raw) {
    const fresh = createDefaultState();
    localStorage.setItem(STORAGE_KEY, JSON.stringify(fresh));
    return fresh;
  }

  try {
    const parsed = JSON.parse(raw);
    if (isValidState(parsed)) {
      parsed.soloMode = Boolean(parsed.soloMode);
      parsed.lastRoundResults = parsed.lastRoundResults && typeof parsed.lastRoundResults === "object"
        ? parsed.lastRoundResults
        : { 1: null, 2: null };
      return parsed;
    }
  } catch (error) {
    console.error("Failed to parse saved multiplayer state:", error);
  }

  const fresh = createDefaultState();
  localStorage.setItem(STORAGE_KEY, JSON.stringify(fresh));
  return fresh;
}

function publishState(sound = null) {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(gameState));
  if (channel) {
    channel.postMessage({ from: localClientId, sound });
  }
}

function runMutation(mutator, sound = null) {
  gameState = loadOrCreateState();
  mutator(gameState);
  gameState.version += 1;
  publishState(sound);
  render();
  if (sound) {
    playSound(sound);
    triggerActionFx(sound);
  }
}

function makeDeck() {
  const deck = [];
  for (const suit of SUITS) {
    for (const rank of RANKS) {
      deck.push({
        rank: rank.key,
        suit: suit.symbol,
        color: suit.color,
        value: rank.value,
      });
    }
  }
  return deck;
}

function shuffle(deck) {
  for (let i = deck.length - 1; i > 0; i -= 1) {
    const j = Math.floor(Math.random() * (i + 1));
    [deck[i], deck[j]] = [deck[j], deck[i]];
  }
}

function drawCard(state) {
  if (state.deck.length === 0) {
    state.deck = makeDeck();
    shuffle(state.deck);
  }
  return state.deck.pop();
}

function scoreHand(hand) {
  let total = hand.reduce((sum, card) => sum + card.value, 0);
  let aces = hand.filter((card) => card.rank === "A").length;
  while (total > 21 && aces > 0) {
    total -= 10;
    aces -= 1;
  }
  return total;
}

function isBlackjack(hand) {
  return hand.length === 2 && scoreHand(hand) === 21;
}

function nextTurnSeat(state, currentSeat) {
  const order = [1, 2];
  const start = currentSeat ? order.indexOf(currentSeat) + 1 : 0;
  for (let offset = 0; offset < order.length; offset += 1) {
    const seat = order[(start + offset) % order.length];
    const player = state.players[seat];
    if (
      player.id &&
      player.hand.length > 0 &&
      !player.stood &&
      !player.bust &&
      !player.blackjack
    ) {
      return seat;
    }
  }
  return null;
}

function activeSeats(state) {
  return [1, 2].filter((seat) => Boolean(state.players[seat].id));
}

function seatsReadyToDeal(state) {
  const seats = activeSeats(state);
  const requiredSeats = state.soloMode ? 1 : 2;
  if (seats.length < requiredSeats) {
    return false;
  }
  return seats.every((seat) => {
    const player = state.players[seat];
    return player.id && player.balance >= player.selectedBet && player.selectedBet > 0;
  });
}

function resolveRound(state) {
  const dealerTotal = scoreHand(state.dealerHand);
  const dealerBJ = isBlackjack(state.dealerHand);
  const summaries = [];
  state.lastRoundResults = { 1: null, 2: null };

  activeSeats(state).forEach((seat) => {
    const player = state.players[seat];
    if (!player.id || player.currentBet <= 0) {
      return;
    }

    const playerTotal = scoreHand(player.hand);
    let result = "loss";
    let payout = 0;

    if (player.bust) {
      result = "loss";
    } else if (player.blackjack && !dealerBJ) {
      result = "win";
      payout = Math.floor(player.currentBet * 2.5);
    } else if (dealerTotal > 21 || playerTotal > dealerTotal) {
      result = "win";
      payout = player.currentBet * 2;
    } else if (playerTotal === dealerTotal) {
      result = "push";
      payout = player.currentBet;
    }

    if (result === "win") {
      player.wins += 1;
      player.balance += payout;
      summaries.push(`P${seat} wins`);
    } else if (result === "push") {
      player.pushes += 1;
      player.balance += payout;
      summaries.push(`P${seat} pushes`);
    } else {
      player.losses += 1;
      summaries.push(`P${seat} loses`);
    }

    state.lastRoundResults[seat] = result;
    player.currentBet = 0;
    player.stood = true;
  });

  state.phase = "round-over";
  state.turnSeat = null;
  state.revealDealer = true;
  state.status = `Round over: ${summaries.join(", ")}. Press Deal for next hand.`;
}

function runDealerIfNeeded(state) {
  state.phase = "dealer-turn";
  state.revealDealer = true;

  while (scoreHand(state.dealerHand) < 17) {
    state.dealerHand.push(drawCard(state));
  }

  resolveRound(state);
}

function advanceTurn(state, seatThatMoved) {
  const next = nextTurnSeat(state, seatThatMoved);
  if (next) {
    state.turnSeat = next;
    state.phase = "player-turns";
    state.status = `Player ${next}'s turn.`;
    return;
  }

  runDealerIfNeeded(state);
}

function playerForLocalSeat(state) {
  if (!localSeat) {
    return null;
  }
  const player = state.players[localSeat];
  if (!player || player.id !== localClientId) {
    localSeat = null;
    return null;
  }
  return player;
}

function isMyTurn(state) {
  const player = playerForLocalSeat(state);
  return Boolean(player && state.phase === "player-turns" && state.turnSeat === localSeat);
}

function renderCard(card, hidden = false) {
  const cardEl = document.createElement("div");
  cardEl.className = `card${hidden ? " back" : ""}${card.color === "red" && !hidden ? " red" : ""}`;
  if (hidden) {
    cardEl.innerHTML = '<span class="rank">?</span><span class="suit">\u25C6</span>';
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
      piece.className = "eva-particle";
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
    ring.className = "eva-ring";
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
      shard.className = "eva-shard";
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
  word.className = "eva-banner";
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
  gameState = loadOrCreateState();
  if (!localSeat) {
    const claimedSeat = [1, 2].find((seat) => gameState.players[seat].id === localClientId);
    if (claimedSeat) {
      localSeat = claimedSeat;
    }
  }
  const localPlayer = playerForLocalSeat(gameState);
  const occupiedSeats = [1, 2].filter((seat) => Boolean(gameState.players[seat].id));
  const isSinglePlayerLayout = occupiedSeats.length === 1;

  const seat1Taken = Boolean(gameState.players[1].id && gameState.players[1].id !== localClientId);
  const seat2Taken = Boolean(gameState.players[2].id && gameState.players[2].id !== localClientId);
  const someoneIsSeated = occupiedSeats.length > 0;
  const inActiveHand = gameState.phase === "player-turns" || gameState.phase === "dealer-turn";

  ui.youAre.textContent = localSeat ? `You are: Player ${localSeat}` : "You are: Spectator";
  ui.seat1Status.textContent = `Seat 1: ${gameState.players[1].id ? "Occupied" : "Open"}`;
  ui.seat2Status.textContent = `Seat 2: ${gameState.players[2].id ? "Occupied" : "Open"}`;
  ui.soloModeBtn.textContent = `Solo Mode: ${gameState.soloMode ? "On" : "Off"}`;

  ui.joinSeat1Btn.disabled = seat1Taken || localSeat === 1 || (gameState.soloMode && someoneIsSeated && !gameState.players[1].id);
  ui.joinSeat2Btn.disabled = seat2Taken || localSeat === 2 || (gameState.soloMode && someoneIsSeated && !gameState.players[2].id);
  ui.leaveSeatBtn.disabled = !localSeat || inActiveHand;
  ui.soloModeBtn.disabled = inActiveHand;
  ui.resetTableBtn.disabled = inActiveHand;

  ui.dealerCards.innerHTML = "";
  gameState.dealerHand.forEach((card, index) => {
    const hidden = !gameState.revealDealer && index === 1;
    const cardEl = renderCard(card, hidden);
    ui.dealerCards.appendChild(cardEl);
    animateCardIn(cardEl);
    if (!hidden && hidden !== gameState.revealDealer) {
      setTimeout(() => animateDealerReveal(cardEl), 300);
    }
  });
  ui.dealerTotal.textContent = gameState.revealDealer
    ? `Total: ${scoreHand(gameState.dealerHand)}`
    : "Total: ?";

  [1, 2].forEach((seat) => {
    const player = gameState.players[seat];
    const area = ui.playerAreas[seat];
    area.cards.innerHTML = "";
    const cardElements = [];
    player.hand.forEach((card) => {
      const cardEl = renderCard(card);
      area.cards.appendChild(cardEl);
      cardElements.push(cardEl);
    });
    if (cardElements.length > 0) {
      animateCardCascade(cardElements);
    }

    const youTag = seat === localSeat ? " (You)" : "";
    area.label.textContent = `Player ${seat}${youTag}`;
    area.total.textContent = player.hand.length ? `Total: ${scoreHand(player.hand)}` : "Total: 0";
    area.meta.textContent = `Bal: $${player.balance} | Bet: $${player.selectedBet} | In: $${player.currentBet}`;
    area.container?.classList.toggle("is-hidden", isSinglePlayerLayout && !player.id);
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

  const localResult = localSeat ? gameState.lastRoundResults?.[localSeat] : null;
  if (gameState.phase === "round-over" && localResult && ui.roundResult) {
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
    chip.disabled = !localPlayer || gameState.phase === "player-turns" || gameState.phase === "dealer-turn";
  });

  const canDeal = Boolean(localPlayer && seatsReadyToDeal(gameState) && !inActiveHand);
  ui.dealBtn.disabled = !canDeal;
  ui.hitBtn.disabled = !isMyTurn(gameState);
  ui.standBtn.disabled = !isMyTurn(gameState);
  ui.doubleBtn.disabled = !(
    isMyTurn(gameState) &&
    localPlayer &&
    localPlayer.hand.length === 2 &&
    localPlayer.balance >= localPlayer.currentBet
  );

  if (gameState.phase === "waiting" && gameState.soloMode && isSinglePlayerLayout) {
    setStatus("Solo mode: press Deal when ready.", false);
  } else if (gameState.status) {
    setStatus(gameState.status, false);
  }
}

function joinSeat(seat) {
  runMutation((state) => {
    if (state.phase === "player-turns" || state.phase === "dealer-turn") {
      return;
    }

    const target = state.players[seat];
    if (state.soloMode) {
      const occupied = activeSeats(state);
      const otherOccupied = occupied.find((occupiedSeat) => occupiedSeat !== seat);
      if (otherOccupied) {
        state.status = "Solo mode allows only one occupied seat. Leave or reset first.";
        return;
      }
    }

    if (target.id && target.id !== localClientId) {
      state.status = `Seat ${seat} is already taken.`;
      return;
    }

    if (localSeat && localSeat !== seat) {
      const current = state.players[localSeat];
      if (current.id === localClientId) {
        current.id = null;
      }
    }

    target.id = localClientId;
    localSeat = seat;
    state.status = `Player ${seat} joined. Select your bet and press Deal when ready.`;
  }, "button");
}

function leaveSeat() {
  if (!localSeat) {
    return;
  }

  runMutation((state) => {
    if (state.phase === "player-turns" || state.phase === "dealer-turn") {
      state.status = "Cannot leave seat during an active hand.";
      return;
    }

    const player = state.players[localSeat];
    if (player.id === localClientId) {
      player.id = null;
      player.hand = [];
      player.currentBet = 0;
      localSeat = null;
      state.status = "Seat released.";
    }
  }, "button");
}

function toggleSoloMode() {
  runMutation((state) => {
    if (state.phase === "player-turns" || state.phase === "dealer-turn") {
      return;
    }

    if (!state.soloMode && activeSeats(state).length > 1) {
      state.status = "Leave one seat first, then enable Solo Mode.";
      return;
    }

    state.soloMode = !state.soloMode;
    state.status = state.soloMode
      ? "Solo Mode enabled. One occupied seat can play."
      : "Solo Mode disabled. Two occupied seats are required.";
  }, "button");
}

function resetTable() {
  runMutation((state) => {
    if (state.phase === "player-turns" || state.phase === "dealer-turn") {
      return;
    }

    state.phase = "waiting";
    state.turnSeat = null;
    state.deck = [];
    state.dealerHand = [];
    state.revealDealer = false;
    state.lastRoundResults = { 1: null, 2: null };
    state.players = {
      1: createPlayer(),
      2: createPlayer(),
    };
    localSeat = null;
    state.status = "Table reset. Join a seat to start.";
  }, "button");
}

function setBet(amount) {
  if (!localSeat) {
    return;
  }

  runMutation((state) => {
    if (state.phase === "player-turns" || state.phase === "dealer-turn") {
      return;
    }

    const player = state.players[localSeat];
    if (player.id !== localClientId) {
      return;
    }

    const bet = Number(amount);
    if (bet > player.balance) {
      state.status = "Bet too high for your current balance.";
      return;
    }

    player.selectedBet = bet;
    state.status = `Player ${localSeat} selected $${bet}.`;
  }, "button");
}

function startRound() {
  if (!localSeat) {
    return;
  }

  runMutation((state) => {
    if (!seatsReadyToDeal(state)) {
      state.status = state.soloMode
        ? "Solo mode needs one occupied seat with a valid bet and balance."
        : "Two occupied seats with valid bets and balances are required.";
      return;
    }

    if (state.phase === "player-turns" || state.phase === "dealer-turn") {
      return;
    }

    state.deck = makeDeck();
    shuffle(state.deck);
    state.dealerHand = [drawCard(state), drawCard(state)];
    state.revealDealer = false;
    state.phase = "player-turns";
    state.lastRoundResults = { 1: null, 2: null };

    const seatsInRound = activeSeats(state);
    seatsInRound.forEach((seat) => {
      const player = state.players[seat];
      player.balance -= player.selectedBet;
      player.currentBet = player.selectedBet;
      player.hand = [drawCard(state), drawCard(state)];
      player.bust = false;
      player.stood = false;
      player.blackjack = isBlackjack(player.hand);
      if (player.blackjack) {
        player.stood = true;
      }
    });

    const firstTurn = nextTurnSeat(state, null);
    state.turnSeat = firstTurn;
    if (!firstTurn) {
      runDealerIfNeeded(state);
      return;
    }

    state.status = `Round started. Player ${firstTurn}'s turn.`;
  }, "deal");
}

function reclaimSoloSeatIfNeeded() {
  gameState = loadOrCreateState();
  const ownedSeat = [1, 2].find((seat) => gameState.players[seat].id === localClientId);
  if (ownedSeat) {
    localSeat = ownedSeat;
    return;
  }

  const occupiedSeats = activeSeats(gameState);
  if (gameState.phase !== "player-turns" || occupiedSeats.length !== 1) {
    return;
  }

  const seat = occupiedSeats[0];
  gameState.players[seat].id = localClientId;
  gameState.version += 1;
  gameState.status = `Solo mode: reconnected as Player ${seat}.`;
  localSeat = seat;
  localStorage.setItem(STORAGE_KEY, JSON.stringify(gameState));
  if (channel) {
    channel.postMessage({ from: localClientId, sound: null });
  }
}

function hit() {
  if (!localSeat) {
    return;
  }

  runMutation((state) => {
    if (!(state.phase === "player-turns" && state.turnSeat === localSeat)) {
      return;
    }

    const player = state.players[localSeat];
    player.hand.push(drawCard(state));
    if (scoreHand(player.hand) > 21) {
      player.bust = true;
      player.stood = true;
      state.status = `Player ${localSeat} busts.`;
      advanceTurn(state, localSeat);
      return;
    }

    state.status = `Player ${localSeat} hits.`;
  }, "hit");
}

function stand() {
  if (!localSeat) {
    return;
  }

  runMutation((state) => {
    if (!(state.phase === "player-turns" && state.turnSeat === localSeat)) {
      return;
    }

    const player = state.players[localSeat];
    player.stood = true;
    state.status = `Player ${localSeat} stands.`;
    advanceTurn(state, localSeat);
  }, "stand");
}

function doubleDown() {
  if (!localSeat) {
    return;
  }

  runMutation((state) => {
    if (!(state.phase === "player-turns" && state.turnSeat === localSeat)) {
      return;
    }

    const player = state.players[localSeat];
    if (player.hand.length !== 2 || player.balance < player.currentBet) {
      state.status = "Double Down unavailable.";
      return;
    }

    player.balance -= player.currentBet;
    player.currentBet *= 2;
    player.hand.push(drawCard(state));
    player.stood = true;
    if (scoreHand(player.hand) > 21) {
      player.bust = true;
      state.status = `Player ${localSeat} busts after Double Down.`;
    } else {
      state.status = `Player ${localSeat} doubles to $${player.currentBet}.`;
    }
    advanceTurn(state, localSeat);
  }, "double");
}

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

if (channel) {
  channel.addEventListener("message", (event) => {
    if (event.data?.from === localClientId) {
      return;
    }
    gameState = loadOrCreateState();
    render();
  });
}

window.addEventListener("storage", (event) => {
  if (event.key !== STORAGE_KEY) {
    return;
  }
  gameState = loadOrCreateState();
  render();
});

window.addEventListener("beforeunload", () => {
  if (!localSeat) {
    return;
  }
  gameState = loadOrCreateState();
  if (gameState.phase === "player-turns" || gameState.phase === "dealer-turn") {
    return;
  }
  const player = gameState.players[localSeat];
  if (player.id === localClientId) {
    player.id = null;
    gameState.version += 1;
    localStorage.setItem(STORAGE_KEY, JSON.stringify(gameState));
  }
});

reclaimSoloSeatIfNeeded();
render();

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

