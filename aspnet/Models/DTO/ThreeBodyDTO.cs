namespace aspnet.Models.DTO;

// Pogled na three body igru; runda se razrješava odmah na startu,
// klijent samo reproducira snimljenu putanju (bez pollanja).
public record ThreeBodyPlanetDTO(string Name, string Color, double Mass, double Radius);

public record ThreeBodyEliminationDTO(int Frame, string Planet);

// Frames: svaki frame je [ax, ay, bx, by, cx, cy] — kompaktan JSON zapis.
public record ThreeBodyRoundDTO(
    double[][] Frames,
    List<ThreeBodyEliminationDTO> Eliminations,
    string? WinnerPlanet,
    string BetPlanet,
    int BetAmount,
    decimal Payout,      // 0 kad je gubitak
    string Result);      // "win" | "loss"

public record ThreeBodyStateDTO(
    long Version,
    string Status,
    string PlayerName,
    decimal Balance,     // stvarni Player.Balance iz baze
    int SelectedBet,
    string? BetOnPlanet, // "A", "B", "C" ili null
    List<ThreeBodyPlanetDTO> Planets,
    string? LastResult,  // "win" | "loss" | null
    string? LastWinner,
    decimal LastPayout,
    int Wins,
    int Losses,
    bool CanStart,
    ThreeBodyRoundDTO? Round); // popunjeno samo u odgovoru na start
