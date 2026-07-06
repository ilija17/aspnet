namespace aspnet.Models.DTO;

// Pogled na slot igru. Cijela runda (osnovni spin + eventualni free spinovi)
// razriješi se u jednom pozivu na /spin; klijent dobije snimljene mreže i
// dobitke pa ih sam animira — nema server-side playbacka (isti obrazac kao
// rulet / three body).

// Statički katalog simbola za klijent: ključ, prikazni naziv i isplate
// (višekratnici line-beta) za 3/4/5 u nizu. IsWild/IsScatter opisuju Lady.
public record SlotSymbolDTO(
    string Key,
    string Name,
    bool IsWild,
    bool IsScatter,
    int Pay3,
    int Pay4,
    int Pay5);

// Jedan dobitak po liniji. Cells = indeks retka (0=gore,1=sredina,2=dolje)
// po svakom dobitnom stupcu, da klijent može iscrtati liniju preko polja.
public record SlotLineWinDTO(
    int Line,            // 1..10
    string Symbol,       // ključ simbola koji je platio
    int Count,           // 3..5
    decimal Amount,
    int[] Cells);        // dužina = Count; redak po stupcu 0..Count-1

// Jedan spin (osnovni ili free). Grid je [stupac][redak] ključeva simbola.
// FreeSpinsAdded = koliko je free spinova ovaj spin dodijelio (osnovni spin:
// 15 kod triggera; free spin: retrigger 1→+1, 2→+2, 3+→+15), da klijent može
// prikazivati "spin X / Y" s rastućim ukupnim brojem.
public record SlotSpinDTO(
    string[][] Grid,
    List<SlotLineWinDTO> LineWins,
    int ScatterCount,
    decimal ScatterWin,
    decimal SpinWin,
    bool Free,
    int FreeSpinsAdded);

// Rezultat cijele runde — popunjen samo u odgovoru na /spin.
public record SlotRoundDTO(
    List<SlotSpinDTO> Spins,    // [0] je osnovni spin, ostalo free spinovi
    int FreeSpinsAwarded,       // ukupno dodijeljenih free spinova (s retriggerom)
    bool FeatureTriggered,      // je li osnovni spin pokrenuo free spinove
    decimal BaseWin,            // dobitak osnovnog spina
    decimal FreeWin,            // zbroj free-spin dobitaka
    decimal TotalWin,
    decimal BetAmount,
    string Result);             // "win" | "loss"

// Karta izvučena u gamble (crveno/crno) rundi.
public record SlotGambleCardDTO(
    string Rank,             // "2".."10", "J", "Q", "K", "A"
    string Suit,             // "hearts" | "diamonds" | "spades" | "clubs"
    string Color);           // "red" | "black"

// Stanje gamble (double-or-nothing) igre. Nakon dobitnog spina Offer = zadnji
// dobitak; prvi pick prebacuje Offer s balansa u Stake. Pogodak boje duplira
// Stake (može se nastaviti ili pokupiti), promašaj gubi sve.
public record SlotGambleDTO(
    decimal Offer,               // dobitak dostupan za gamble (0 = ništa)
    decimal Stake,               // trenutni iznos u igri (0 = nije aktivno)
    bool Active,                 // je li gamble u tijeku (Stake na stolu)
    int Step,                    // broj odigranih pickova u ovoj gamble rundi
    SlotGambleCardDTO? LastCard, // zadnja izvučena karta
    string? LastPick,            // "red" | "black" — zadnji odabir igrača
    bool? LastWon,               // je li zadnji pick pogođen
    List<SlotGambleCardDTO> History); // izvučene karte ove runde, redom

public record SlotStateDTO(
    long Version,
    string Status,
    string PlayerName,
    decimal Balance,            // stvarni Player.Balance iz baze
    decimal SelectedBet,        // ukupni ulog (line bet = ukupni / 10)
    decimal[] AllowedBets,
    List<SlotSymbolDTO> Symbols,
    int[][] Paylines,           // 10 linija, svaka [redak po stupcu] 0..2
    int Reels,
    int Rows,
    int Lines,
    decimal LastWin,
    int Spins,
    int FeatureHits,
    bool CanSpin,
    SlotGambleDTO Gamble,       // stanje red/black gamble igre
    SlotRoundDTO? Round);       // null osim u odgovoru na /spin
