namespace aspnet.Models.DTO;

public static class DtoMapper
{
    public static CasinoDTO ToDTO(this Casino casino) => new()
    {
        Id = casino.Id,
        Name = casino.Name,
        Address = casino.Address,
        LicenseNumber = casino.LicenseNumber,
        FoundedDate = casino.FoundedDate
    };

    public static GameDTO ToDTO(this Game game) => new()
    {
        Id = game.Id,
        Name = game.Name,
        Type = game.Type.ToString(),
        MinBet = game.MinBet,
        MaxBet = game.MaxBet,
        Description = game.Description
    };

    public static PlayerDTO ToDTO(this Player player) => new()
    {
        Id = player.Id,
        FirstName = player.FirstName,
        LastName = player.LastName,
        Email = player.Email,
        DateOfBirth = player.DateOfBirth,
        Balance = player.Balance
    };

    public static EmployeeDTO ToDTO(this Employee employee) => new()
    {
        Id = employee.Id,
        FirstName = employee.FirstName,
        LastName = employee.LastName,
        Position = employee.Position,
        Casino = employee.Casino?.ToDTO()
    };

    public static TableDTO ToDTO(this Table table) => new()
    {
        Id = table.Id,
        TableNumber = table.TableNumber,
        IsAvailable = table.IsAvailable,
        MinBet = table.MinBet,
        MaxBet = table.MaxBet,
        Casino = table.Casino?.ToDTO(),
        Game = table.Game?.ToDTO()
    };

    public static TransactionDTO ToDTO(this Transaction transaction) => new()
    {
        Id = transaction.Id,
        Amount = transaction.Amount,
        Type = transaction.Type.ToString(),
        CreatedAt = transaction.CreatedAt,
        Player = transaction.Player?.ToDTO()
    };

    public static ReservationDTO ToDTO(this Reservation reservation) => new()
    {
        Id = reservation.Id,
        ReservedAt = reservation.ReservedAt,
        Player = reservation.Player?.ToDTO(),
        Table = reservation.Table?.ToDTO()
    };

    public static AttachmentDTO ToDTO(this Attachment attachment) => new()
    {
        Id = attachment.Id,
        CasinoId = attachment.CasinoId,
        FileName = attachment.FileName,
        FilePath = attachment.FilePath,
        ContentType = attachment.ContentType,
        FileSize = attachment.FileSize,
        CreatedAt = attachment.CreatedAt
    };
}
