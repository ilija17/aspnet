using aspnet.Models.DTO;

namespace aspnet.Tests;

public class TableApiTests : ApiTestBase
{
    public TableApiTests(CasinoApiFactory factory) : base(factory) { }

    [Fact]
    public async Task GetAll_ShouldReturnOkWithCollection()
    {
        var table = await CreateTableAsync();

        var response = await Client.GetAsync("/api/table");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dtos = await response.Content.ReadFromJsonAsync<List<TableDTO>>();
        dtos.Should().Contain(t => t.Id == table.Id);
    }

    [Fact]
    public async Task GetAll_WithCasinoFilter_ShouldFilterResults()
    {
        var table = await CreateTableAsync();
        await CreateTableAsync(); // drugi casino

        var response = await Client.GetAsync($"/api/table?casinoId={table.CasinoId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dtos = await response.Content.ReadFromJsonAsync<List<TableDTO>>();
        dtos.Should().HaveCount(1);
        dtos![0].Id.Should().Be(table.Id);
    }

    [Fact]
    public async Task GetById_ShouldReturnTableWithNestedDtos_WhenExists()
    {
        var table = await CreateTableAsync();

        var response = await Client.GetAsync($"/api/table/{table.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<TableDTO>();
        dto!.Id.Should().Be(table.Id);
        dto.Casino.Should().NotBeNull();
        dto.Game.Should().NotBeNull();
        dto.Casino!.Id.Should().Be(table.CasinoId);
        dto.Game!.Id.Should().Be(table.GameId);
    }

    [Fact]
    public async Task GetById_ShouldReturn404_WhenMissing()
    {
        var response = await Client.GetAsync("/api/table/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_ShouldCreateTable_AndReturn201()
    {
        var casino = await CreateCasinoAsync();
        var game = await CreateGameAsync();
        var model = new TableInputDTO
        {
            TableNumber = 42,
            IsAvailable = true,
            MinBet = 15,
            MaxBet = 600,
            CasinoId = casino.Id,
            GameId = game.Id
        };

        var response = await AdminClient.PostAsJsonAsync("/api/table", model);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<TableDTO>();
        dto!.Id.Should().BeGreaterThan(0);
        dto.TableNumber.Should().Be(42);
        dto.Casino!.Id.Should().Be(casino.Id);
        dto.Game!.Id.Should().Be(game.Id);
    }

    [Fact]
    public async Task Post_ShouldReturn400_WhenForeignKeysMissing()
    {
        var model = new TableInputDTO
        {
            TableNumber = 1,
            IsAvailable = true,
            MinBet = 10,
            MaxBet = 100,
            CasinoId = 999999,
            GameId = 999999
        };

        var response = await AdminClient.PostAsJsonAsync("/api/table", model);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ShouldReturn401_WhenAnonymous()
    {
        var model = new TableInputDTO
        {
            TableNumber = 1,
            IsAvailable = true,
            MinBet = 10,
            MaxBet = 100,
            CasinoId = 1,
            GameId = 1
        };

        var response = await Client.PostAsJsonAsync("/api/table", model);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_ShouldUpdateTable()
    {
        var table = await CreateTableAsync();
        var model = new TableInputDTO
        {
            Id = table.Id,
            TableNumber = table.TableNumber,
            IsAvailable = false,
            MinBet = 50,
            MaxBet = 2000,
            CasinoId = table.CasinoId,
            GameId = table.GameId
        };

        var response = await AdminClient.PutAsJsonAsync($"/api/table/{table.Id}", model);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<TableDTO>();
        dto!.IsAvailable.Should().BeFalse();
        dto.MinBet.Should().Be(50);
    }

    [Fact]
    public async Task Put_ShouldReturn404_WhenMissing()
    {
        var casino = await CreateCasinoAsync();
        var game = await CreateGameAsync();
        var model = new TableInputDTO
        {
            TableNumber = 1,
            IsAvailable = true,
            MinBet = 10,
            MaxBet = 100,
            CasinoId = casino.Id,
            GameId = game.Id
        };

        var response = await AdminClient.PutAsJsonAsync("/api/table/999999", model);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ShouldRemoveTable_AndReturn204()
    {
        var table = await CreateTableAsync();

        var response = await AdminClient.DeleteAsync($"/api/table/{table.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var exists = await WithDbAsync(db => Task.FromResult(db.Tables.Any(t => t.Id == table.Id)));
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ShouldReturn404_WhenMissing()
    {
        var response = await AdminClient.DeleteAsync("/api/table/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
