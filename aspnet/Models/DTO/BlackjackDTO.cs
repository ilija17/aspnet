namespace aspnet.Models.DTO;

// Pogled na stol personaliziran za jednog klijenta: skrivena dealerova karta
// se ne šalje, a Can* zastavice server računa da klijent ne mora znati pravila.
public record BlackjackCardDTO(string? Rank, string? Suit, string? Color, bool Hidden);

public record BlackjackSeatDTO(
    bool Occupied,
    bool IsYou,
    int Balance,
    int SelectedBet,
    int CurrentBet,
    List<BlackjackCardDTO> Hand,
    int Total,
    bool Stood,
    bool Bust,
    bool Blackjack,
    int Wins,
    int Losses,
    int Pushes);

public record BlackjackStateDTO(
    long Version,
    string Phase,
    string Status,
    bool SoloMode,
    bool RevealDealer,
    int? TurnSeat,
    int? YourSeat,
    List<BlackjackCardDTO> DealerHand,
    int? DealerTotal,
    Dictionary<int, string?> LastRoundResults,
    Dictionary<int, BlackjackSeatDTO> Players,
    bool CanJoin1,
    bool CanJoin2,
    bool CanLeave,
    bool CanToggleSolo,
    bool CanReset,
    bool CanSetBet,
    bool CanDeal,
    bool CanHit,
    bool CanStand,
    bool CanDouble);
