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
public record SlotSpinDTO(
    string[][] Grid,
    List<SlotLineWinDTO> LineWins,
    int ScatterCount,
    decimal ScatterWin,
    decimal SpinWin,
    bool Free);

// Rezultat cijele runde — popunjen samo u odgovoru na /spin.
public record SlotRoundDTO(
    List<SlotSpinDTO> Spins,    // [0] je osnovni spin, ostalo free spinovi
    int FreeSpinsAwarded,       // ukupno dodijeljenih free spinova (s retriggerom)
    bool FeatureTriggered,      // je li osnovni spin pokrenuo free spinove
    decimal BaseWin,            // dobitak osnovnog spina
    decimal FreeWin,            // zbroj free-spin dobitaka
    decimal TotalWin,
    int BetAmount,
    string Result);             // "win" | "loss"

public record SlotStateDTO(
    long Version,
    string Status,
    string PlayerName,
    decimal Balance,            // stvarni Player.Balance iz baze
    int SelectedBet,            // ukupni ulog (line bet = ukupni / 10)
    int[] AllowedBets,
    List<SlotSymbolDTO> Symbols,
    int[][] Paylines,           // 10 linija, svaka [redak po stupcu] 0..2
    int Reels,
    int Rows,
    int Lines,
    decimal LastWin,
    int Spins,
    int FeatureHits,
    bool CanSpin,
    SlotRoundDTO? Round);       // null osim u odgovoru na /spin
