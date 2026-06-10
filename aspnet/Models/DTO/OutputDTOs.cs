namespace aspnet.Models.DTO;

public class CasinoDTO
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public string LicenseNumber { get; set; }
    public DateTime FoundedDate { get; set; }
}

public class GameDTO
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public decimal MinBet { get; set; }
    public decimal MaxBet { get; set; }
    public string? Description { get; set; }
}

public class PlayerDTO
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public DateTime DateOfBirth { get; set; }
    public decimal Balance { get; set; }
}

public class EmployeeDTO
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Position { get; set; }
    public CasinoDTO? Casino { get; set; }
}

public class TableDTO
{
    public int Id { get; set; }
    public int TableNumber { get; set; }
    public bool IsAvailable { get; set; }
    public decimal MinBet { get; set; }
    public decimal MaxBet { get; set; }
    public CasinoDTO? Casino { get; set; }
    public GameDTO? Game { get; set; }
}

public class TransactionDTO
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public PlayerDTO? Player { get; set; }
}

public class ReservationDTO
{
    public int Id { get; set; }
    public DateTime ReservedAt { get; set; }
    public PlayerDTO? Player { get; set; }
    public TableDTO? Table { get; set; }
}

public class AttachmentDTO
{
    public int Id { get; set; }
    public int CasinoId { get; set; }
    public string FileName { get; set; }
    public string FilePath { get; set; }
    public string? ContentType { get; set; }
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; }
}
