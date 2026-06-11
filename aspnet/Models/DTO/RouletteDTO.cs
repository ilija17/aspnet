namespace aspnet.Models.DTO;

// Pogled na rulet za prijavljenog igrača; Can* zastavice server računa
// da klijent ne mora znati pravila.
public record RouletteBetDTO(string Kind, int? Number, int Amount, string Label);

public record RouletteStateDTO(
    long Version,
    string Status,
    string PlayerName,
    decimal Balance,     // stvarni Player.Balance iz baze
    List<RouletteBetDTO> Bets,
    int TotalBet,
    int? LastNumber,
    string? LastColor,   // "red" | "black" | "green"
    decimal LastPayout,
    List<int> History,   // zadnjih 10 brojeva, najnoviji prvi
    bool CanBet,
    bool CanSpin,
    bool CanClear);
