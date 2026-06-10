using aspnet.Models;
using aspnet.Models.DTO;

namespace aspnet.Tests;

public class TransactionApiTests : ApiTestBase
{
    public TransactionApiTests(CasinoApiFactory factory) : base(factory) { }

    [Fact]
    public async Task GetAll_ShouldReturnOkWithCollection()
    {
        var transaction = await CreateTransactionAsync();

        var response = await Client.GetAsync("/api/transaction");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dtos = await response.Content.ReadFromJsonAsync<List<TransactionDTO>>();
        dtos.Should().Contain(t => t.Id == transaction.Id);
    }

    [Fact]
    public async Task GetAll_WithPlayerFilter_ShouldFilterResults()
    {
        var transaction = await CreateTransactionAsync();
        await CreateTransactionAsync(); // drugi igrač

        var response = await Client.GetAsync($"/api/transaction?playerId={transaction.PlayerId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dtos = await response.Content.ReadFromJsonAsync<List<TransactionDTO>>();
        dtos.Should().HaveCount(1);
        dtos![0].Id.Should().Be(transaction.Id);
    }

    [Fact]
    public async Task GetById_ShouldReturnTransactionWithNestedPlayer_WhenExists()
    {
        var transaction = await CreateTransactionAsync();

        var response = await Client.GetAsync($"/api/transaction/{transaction.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<TransactionDTO>();
        dto!.Id.Should().Be(transaction.Id);
        dto.Player.Should().NotBeNull();
        dto.Player!.Id.Should().Be(transaction.PlayerId);
    }

    [Fact]
    public async Task GetById_ShouldReturn404_WhenMissing()
    {
        var response = await Client.GetAsync("/api/transaction/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_ShouldCreateTransaction_AndReturn201()
    {
        var player = await CreatePlayerAsync();
        var model = new TransactionInputDTO
        {
            Amount = 333,
            Type = TransactionType.Win,
            CreatedAt = new DateTime(2024, 5, 10),
            PlayerId = player.Id
        };

        var response = await AdminClient.PostAsJsonAsync("/api/transaction", model);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<TransactionDTO>();
        dto!.Id.Should().BeGreaterThan(0);
        dto.Amount.Should().Be(333);
        dto.Type.Should().Be("Win");
        dto.Player!.Id.Should().Be(player.Id);
    }

    [Fact]
    public async Task Post_ShouldReturn400_WhenAmountInvalid()
    {
        var player = await CreatePlayerAsync();
        var model = new TransactionInputDTO
        {
            Amount = 0, // izvan dopuštenog raspona
            Type = TransactionType.Bet,
            CreatedAt = new DateTime(2024, 5, 10),
            PlayerId = player.Id
        };

        var response = await AdminClient.PostAsJsonAsync("/api/transaction", model);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ShouldReturn400_WhenPlayerMissing()
    {
        var model = new TransactionInputDTO
        {
            Amount = 50,
            Type = TransactionType.Deposit,
            CreatedAt = new DateTime(2024, 5, 10),
            PlayerId = 999999
        };

        var response = await AdminClient.PostAsJsonAsync("/api/transaction", model);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ShouldReturn401_WhenAnonymous()
    {
        var model = new TransactionInputDTO
        {
            Amount = 50,
            Type = TransactionType.Deposit,
            CreatedAt = new DateTime(2024, 5, 10),
            PlayerId = 1
        };

        var response = await Client.PostAsJsonAsync("/api/transaction", model);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_ShouldUpdateTransaction()
    {
        var transaction = await CreateTransactionAsync();
        var model = new TransactionInputDTO
        {
            Id = transaction.Id,
            Amount = 777,
            Type = TransactionType.Withdrawal,
            CreatedAt = transaction.CreatedAt,
            PlayerId = transaction.PlayerId
        };

        var response = await AdminClient.PutAsJsonAsync($"/api/transaction/{transaction.Id}", model);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<TransactionDTO>();
        dto!.Amount.Should().Be(777);
        dto.Type.Should().Be("Withdrawal");
    }

    [Fact]
    public async Task Put_ShouldReturn404_WhenMissing()
    {
        var player = await CreatePlayerAsync();
        var model = new TransactionInputDTO
        {
            Amount = 50,
            Type = TransactionType.Deposit,
            CreatedAt = new DateTime(2024, 5, 10),
            PlayerId = player.Id
        };

        var response = await AdminClient.PutAsJsonAsync("/api/transaction/999999", model);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ShouldRemoveTransaction_AndReturn204()
    {
        var transaction = await CreateTransactionAsync();

        var response = await AdminClient.DeleteAsync($"/api/transaction/{transaction.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var exists = await WithDbAsync(db => Task.FromResult(db.Transactions.Any(t => t.Id == transaction.Id)));
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ShouldReturn404_WhenMissing()
    {
        var response = await AdminClient.DeleteAsync("/api/transaction/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
