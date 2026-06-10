using aspnet.Models.DTO;

namespace aspnet.Tests;

public class CasinoApiTests : ApiTestBase
{
    public CasinoApiTests(CasinoApiFactory factory) : base(factory) { }

    [Fact]
    public async Task GetAll_ShouldReturnOkWithCollection()
    {
        var casino = await CreateCasinoAsync();

        var response = await Client.GetAsync("/api/casino");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dtos = await response.Content.ReadFromJsonAsync<List<CasinoDTO>>();
        dtos.Should().NotBeNull();
        dtos.Should().Contain(c => c.Id == casino.Id && c.Name == casino.Name);
    }

    [Fact]
    public async Task GetAll_WithQuery_ShouldFilterResults()
    {
        var casino = await CreateCasinoAsync();
        await CreateCasinoAsync();

        var response = await Client.GetAsync($"/api/casino?q={casino.LicenseNumber}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dtos = await response.Content.ReadFromJsonAsync<List<CasinoDTO>>();
        dtos.Should().OnlyContain(c => c.LicenseNumber == casino.LicenseNumber);
        dtos.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetById_ShouldReturnCasino_WhenExists()
    {
        var casino = await CreateCasinoAsync();

        var response = await Client.GetAsync($"/api/casino/{casino.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<CasinoDTO>();
        dto!.Id.Should().Be(casino.Id);
        dto.Name.Should().Be(casino.Name);
        dto.Address.Should().Be(casino.Address);
    }

    [Fact]
    public async Task GetById_ShouldReturn404_WhenMissing()
    {
        var response = await Client.GetAsync("/api/casino/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_ShouldCreateCasino_AndReturn201()
    {
        var model = new CasinoInputDTO
        {
            Name = "Novi Casino",
            Address = "Nova adresa 5, Osijek",
            LicenseNumber = "HR-NEW-100",
            FoundedDate = new DateTime(2020, 3, 1)
        };

        var response = await AdminClient.PostAsJsonAsync("/api/casino", model);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<CasinoDTO>();
        dto!.Id.Should().BeGreaterThan(0);
        dto.Name.Should().Be(model.Name);

        var exists = await WithDbAsync(db => Task.FromResult(db.Casinos.Any(c => c.Id == dto.Id)));
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task Post_ShouldReturn400_WhenModelInvalid()
    {
        var model = new { Address = "Bez naziva 1" };

        var response = await AdminClient.PostAsJsonAsync("/api/casino", model);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ShouldReturn401_WhenAnonymous()
    {
        var model = new CasinoInputDTO
        {
            Name = "Anonimni Casino",
            Address = "Adresa 1",
            LicenseNumber = "HR-ANON-1",
            FoundedDate = new DateTime(2020, 1, 1)
        };

        var response = await Client.PostAsJsonAsync("/api/casino", model);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_ShouldUpdateCasino()
    {
        var casino = await CreateCasinoAsync();
        var model = new CasinoInputDTO
        {
            Id = casino.Id,
            Name = "Izmijenjeni naziv",
            Address = casino.Address,
            LicenseNumber = casino.LicenseNumber,
            FoundedDate = casino.FoundedDate
        };

        var response = await AdminClient.PutAsJsonAsync($"/api/casino/{casino.Id}", model);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<CasinoDTO>();
        dto!.Name.Should().Be("Izmijenjeni naziv");

        var dbName = await WithDbAsync(db => Task.FromResult(db.Casinos.Find(casino.Id)!.Name));
        dbName.Should().Be("Izmijenjeni naziv");
    }

    [Fact]
    public async Task Put_ShouldReturn404_WhenMissing()
    {
        var model = new CasinoInputDTO
        {
            Name = "Nepostojeći",
            Address = "Adresa 1",
            LicenseNumber = "HR-X-1",
            FoundedDate = new DateTime(2020, 1, 1)
        };

        var response = await AdminClient.PutAsJsonAsync("/api/casino/999999", model);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Put_ShouldReturn400_WhenIdMismatch()
    {
        var casino = await CreateCasinoAsync();
        var model = new CasinoInputDTO
        {
            Id = casino.Id + 1,
            Name = "Mismatch",
            Address = "Adresa 1",
            LicenseNumber = "HR-X-2",
            FoundedDate = new DateTime(2020, 1, 1)
        };

        var response = await AdminClient.PutAsJsonAsync($"/api/casino/{casino.Id}", model);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_ShouldRemoveCasino_AndReturn204()
    {
        var casino = await CreateCasinoAsync();

        var response = await AdminClient.DeleteAsync($"/api/casino/{casino.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var exists = await WithDbAsync(db => Task.FromResult(db.Casinos.Any(c => c.Id == casino.Id)));
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ShouldReturn404_WhenMissing()
    {
        var response = await AdminClient.DeleteAsync("/api/casino/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ShouldReturn403_WhenManager()
    {
        var casino = await CreateCasinoAsync();

        var response = await ManagerClient.DeleteAsync($"/api/casino/{casino.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
