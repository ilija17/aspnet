namespace aspnet.Models.DTO;

// Pogled na slot igru. Cijela runda (osnovni spin + eventualni Hold & Win
// bonus) razriješi se u jednom pozivu na /spin ili /buy; klijent dobije
// snimljene mreže, charm kugle i respinove pa ih sam animira — nema
// server-side playbacka (isti obrazac kao rulet / three body).

// Statički katalog simbola za klijent: ključ, prikazni naziv i isplate
// (višekratnici line-beta) za 3/4/5 u nizu. Lady je wild, Charm Ball scatter.
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

// Jedna Charm Ball kugla na mreži: pozicija + što nosi. Jackpot je
// "mini" | "major" | "grand" ili null (obična cash vrijednost).
// Amount je uvijek konačni iznos u $ (i za jackpote).
public record SlotCharmDTO(
    int Reel,            // 0..4
    int Row,             // 0..2
    decimal Amount,
    string? Jackpot);

// Jedan respin u Hold & Win bonusu: koje su nove kugle pale (i zaključale
// se), koliko respinova ostaje NAKON ovog spina (novi pogodak resetira na 3)
// i ukupan broj zaključanih kugli nakon ovog spina.
public record SlotRespinDTO(
    List<SlotCharmDTO> NewCharms,
    int RespinsLeft,
    int LockedCount);

// Cijeli Hold & Win bonus: početne (okidačke) kugle, snimljeni respinovi,
// ukupna isplata (zbroj svih kugli), je li mreža potpuno popunjena (15/15)
// i koji su jackpoti osvojeni ("mini"/"major"/"grand", s ponavljanjima).
public record SlotBonusDTO(
    List<SlotCharmDTO> InitialCharms,
    List<SlotRespinDTO> Respins,
    decimal TotalWin,
    bool FullGrid,
    List<string> JackpotsWon);

// Osnovni (plaćeni) spin. Grid je [stupac][redak] ključeva simbola.
// Charms = vrijednosti/jackpoti prikazani na svakoj Charm Ball kugli u mreži
// (kozmetika ako ih je <3; kod 3+ upravo te kugle ulaze u bonus kao početne).
public record SlotSpinDTO(
    string[][] Grid,
    List<SlotLineWinDTO> LineWins,
    int ScatterCount,
    List<SlotCharmDTO> Charms,
    decimal SpinWin);

// Rezultat cijele runde — popunjen samo u odgovoru na /spin i /buy.
public record SlotRoundDTO(
    SlotSpinDTO BaseSpin,
    SlotBonusDTO? Bonus,        // null ako bonus nije okinut
    bool FeatureTriggered,
    bool FeatureBought,         // true kad je runda pokrenuta preko /buy
    decimal BaseWin,            // dobitak linija osnovnog spina
    decimal BonusWin,           // ukupna isplata bonusa
    decimal TotalWin,
    decimal BetAmount,          // ulog spina (bez cijene Feature Buya)
    string Result);             // "win" | "loss"

// Trenutne vrijednosti jackpota pri odabranom ulogu (fiksni višekratnici
// ukupnog uloga) — za jackpot ploču iznad valjaka.
public record SlotJackpotsDTO(
    decimal Mini,
    decimal Major,
    decimal Grand);

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
    SlotJackpotsDTO Jackpots,   // Mini/Major/Grand pri trenutnom ulogu
    decimal FeatureBuyCost,     // cijena Feature Buya pri trenutnom ulogu
    bool CanBuy,                // balans pokriva Feature Buy i nema gamblea
    decimal LastWin,
    int Spins,
    int FeatureHits,
    bool CanSpin,
    SlotGambleDTO Gamble,       // stanje red/black gamble igre
    SlotRoundDTO? Round);       // null osim u odgovoru na /spin i /buy
