namespace aspnet.Models.DTO;

public record PlanetData(string Name, string Color, double Mass, double Radius, double X, double Y);

public record ThreeBodyStateDTO(
    long Version,
    string Phase,               // "betting" | "simulating" | "round-over"
    string Status,
    string PlayerName,
    decimal Balance,
    int SelectedBet,
    string? BetOnPlanet,        // "A", "B", "C" or null
    List<PlanetData> Planets,   // current frame planet positions
    int CurrentFrame,
    int TotalFrames,
    List<string> AlivePlanets,  // planet names still alive
    List<string> EliminatedOrder, // order planets were eliminated (first = died first)
    string? WinnerPlanet,       // last surviving planet name
    string? LastRoundResult,    // "win" | "loss" | null
    int Wins,
    int Losses,
    bool CanBet,
    bool CanStart,
    bool CanReset);
