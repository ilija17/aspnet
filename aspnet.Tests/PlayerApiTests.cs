using aspnet.Models.DTO;

namespace aspnet.Tests;

public class PlayerApiTests : ApiTestBase
{
    public PlayerApiTests(CasinoApiFactory factory) : base(factory) { }

    [Fact]
    public async Task GetAll_ShouldReturnOkWithCollection()
    {
        var player = await CreatePlayerAsync();

        var response = await Client.GetAsync("/api/player");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dtos = await response.Content.ReadFromJsonAsync<List<PlayerDTO>>();
        dtos.Should().Contain(p => p.Id == player.Id && p.Email == player.Email);
    }

    [Fact]
    public async Task GetAll_WithQuery_ShouldFilterResults()
    {
        var player = await CreatePlayerAsync();
        await CreatePlayerAsync();

        var response = await Client.GetAsync($"/api/player?q={player.Email}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dtos = await response.Content.ReadFromJsonAsync<List<PlayerDTO>>();
        dtos.Should().HaveCount(1);
        dtos![0].Id.Should().Be(player.Id);
    }

    [Fact]
    public async Task GetById_ShouldReturnPlayer_WhenExists()
    {
        var player = await CreatePlayerAsync();

        var response = await Client.GetAsync($"/api/player/{player.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<PlayerDTO>();
        dto!.Id.Should().Be(player.Id);
        dto.Balance.Should().Be(player.Balance);
    }

    [Fact]
    public async Task GetById_ShouldReturn404_WhenMissing()
    {
        var response = await Client.GetAsync("/api/player/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_ShouldCreatePlayer_AndReturn201()
    {
        var model = new PlayerInputDTO
        {
            FirstName = "Novi",
            LastName = "Igrač",
            Email = $"novi-{Guid.NewGuid():N}@mail.com",
            DateOfBirth = new DateTime(1992, 7, 15),
            Balance = 250
        };

        var response = await AdminClient.PostAsJsonAsync("/api/player", model);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<PlayerDTO>();
        dto!.Id.Should().BeGreaterThan(0);
        dto.Email.Should().Be(model.Email);
    }

    [Fact]
    public async Task Post_ShouldReturn400_WhenEmailInvalid()
    {
        var model = new
        {
            FirstName = "Novi",
            LastName = "Igrač",
            Email = "nije-email",
            DateOfBirth = "1992-07-15",
            Balance = 250
        };

        var response = await AdminClient.PostAsJsonAsync("/api/player", model);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ShouldReturn401_WhenAnonymous()
    {
        var model = new PlayerInputDTO
        {
            FirstName = "Anon",
            LastName = "Igrač",
            Email = "anon@mail.com",
            DateOfBirth = new DateTime(1990, 1, 1),
            Balance = 0
        };

        var response = await Client.PostAsJsonAsync("/api/player", model);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_ShouldUpdatePlayer()
    {
        var player = await CreatePlayerAsync();
        var model = new PlayerInputDTO
        {
            Id = player.Id,
            FirstName = "Izmijenjeni",
            LastName = player.LastName,
            Email = player.Email,
            DateOfBirth = player.DateOfBirth,
            Balance = 9999
        };

        var response = await AdminClient.PutAsJsonAsync($"/api/player/{player.Id}", model);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<PlayerDTO>();
        dto!.FirstName.Should().Be("Izmijenjeni");
        dto.Balance.Should().Be(9999);
    }

    [Fact]
    public async Task Put_ShouldReturn404_WhenMissing()
    {
        var model = new PlayerInputDTO
        {
            FirstName = "Nepostojeći",
            LastName = "Igrač",
            Email = "x@mail.com",
            DateOfBirth = new DateTime(1990, 1, 1),
            Balance = 0
        };

        var response = await AdminClient.PutAsJsonAsync("/api/player/999999", model);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ShouldRemovePlayer_AndReturn204()
    {
        var player = await CreatePlayerAsync();

        var response = await AdminClient.DeleteAsync($"/api/player/{player.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var exists = await WithDbAsync(db => Task.FromResult(db.Players.Any(p => p.Id == player.Id)));
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ShouldReturn404_WhenMissing()
    {
        var response = await AdminClient.DeleteAsync("/api/player/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
