using aspnet.Models.DTO;

namespace aspnet.Tests;

public class GameApiTests : ApiTestBase
{
    public GameApiTests(CasinoApiFactory factory) : base(factory) { }

    [Fact]
    public async Task GetAll_ShouldReturnOkWithCollection()
    {
        var game = await CreateGameAsync();

        var response = await Client.GetAsync("/api/game");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dtos = await response.Content.ReadFromJsonAsync<List<GameDTO>>();
        dtos.Should().Contain(g => g.Id == game.Id && g.Name == game.Name);
    }

    [Fact]
    public async Task GetAll_WithQuery_ShouldFilterResults()
    {
        var game = await CreateGameAsync();
        await CreateGameAsync();

        var response = await Client.GetAsync($"/api/game?q={game.Name}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dtos = await response.Content.ReadFromJsonAsync<List<GameDTO>>();
        dtos.Should().HaveCount(1);
        dtos![0].Id.Should().Be(game.Id);
    }

    [Fact]
    public async Task GetById_ShouldReturnGame_WhenExists()
    {
        var game = await CreateGameAsync();

        var response = await Client.GetAsync($"/api/game/{game.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<GameDTO>();
        dto!.Id.Should().Be(game.Id);
        dto.Type.Should().Be(game.Type.ToString());
    }

    [Fact]
    public async Task GetById_ShouldReturn404_WhenMissing()
    {
        var response = await Client.GetAsync("/api/game/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_ShouldCreateGame_AndReturn201()
    {
        var model = new GameInputDTO
        {
            Name = "Nova igra",
            Type = aspnet.Models.GameType.Roulette,
            MinBet = 5,
            MaxBet = 200,
            Description = "Opis nove igre"
        };

        var response = await AdminClient.PostAsJsonAsync("/api/game", model);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<GameDTO>();
        dto!.Id.Should().BeGreaterThan(0);
        dto.Type.Should().Be("Roulette");
    }

    [Fact]
    public async Task Post_ShouldReturn400_WhenModelInvalid()
    {
        // MinBet izvan dopuštenog raspona i bez naziva
        var model = new { MinBet = 0, MaxBet = 100 };

        var response = await AdminClient.PostAsJsonAsync("/api/game", model);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ShouldReturn401_WhenAnonymous()
    {
        var model = new GameInputDTO
        {
            Name = "Anonimna igra",
            Type = aspnet.Models.GameType.Slots,
            MinBet = 1,
            MaxBet = 10
        };

        var response = await Client.PostAsJsonAsync("/api/game", model);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_ShouldUpdateGame()
    {
        var game = await CreateGameAsync();
        var model = new GameInputDTO
        {
            Id = game.Id,
            Name = "Izmijenjena igra",
            Type = game.Type,
            MinBet = 25,
            MaxBet = 750,
            Description = game.Description
        };

        var response = await AdminClient.PutAsJsonAsync($"/api/game/{game.Id}", model);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<GameDTO>();
        dto!.Name.Should().Be("Izmijenjena igra");
        dto.MinBet.Should().Be(25);
    }

    [Fact]
    public async Task Put_ShouldReturn404_WhenMissing()
    {
        var model = new GameInputDTO
        {
            Name = "Nepostojeća",
            Type = aspnet.Models.GameType.Poker,
            MinBet = 10,
            MaxBet = 100
        };

        var response = await AdminClient.PutAsJsonAsync("/api/game/999999", model);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ShouldRemoveGame_AndReturn204()
    {
        var game = await CreateGameAsync();

        var response = await AdminClient.DeleteAsync($"/api/game/{game.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var exists = await WithDbAsync(db => Task.FromResult(db.Games.Any(g => g.Id == game.Id)));
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ShouldReturn404_WhenMissing()
    {
        var response = await AdminClient.DeleteAsync("/api/game/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
