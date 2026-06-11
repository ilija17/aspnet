namespace aspnet.Models.DTO;

// Pogled na igru za prijavljenog igrača: skrivena dealerova karta se ne šalje,
// a Can* zastavice server računa da klijent ne mora znati pravila.
public record BlackjackCardDTO(string? Rank, string? Suit, string? Color, bool Hidden);

public record BlackjackStateDTO(
    long Version,
    string Phase,        // "betting" | "player-turn" | "round-over"
    string Status,
    string PlayerName,
    decimal Balance,     // stvarni Player.Balance iz baze
    int SelectedBet,
    int CurrentBet,
    bool RevealDealer,
    List<BlackjackCardDTO> DealerHand,
    int? DealerTotal,
    List<BlackjackCardDTO> Hand,
    int Total,
    bool Bust,
    bool Blackjack,
    string? LastRoundResult, // "win" | "loss" | "push" | null
    int Wins,
    int Losses,
    int Pushes,
    bool CanSetBet,
    bool CanDeal,
    bool CanHit,
    bool CanStand,
    bool CanDouble);
