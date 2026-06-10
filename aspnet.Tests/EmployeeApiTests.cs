using aspnet.Models.DTO;

namespace aspnet.Tests;

public class EmployeeApiTests : ApiTestBase
{
    public EmployeeApiTests(CasinoApiFactory factory) : base(factory) { }

    [Fact]
    public async Task GetAll_ShouldReturnOkWithCollection()
    {
        var employee = await CreateEmployeeAsync();

        var response = await Client.GetAsync("/api/employee");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dtos = await response.Content.ReadFromJsonAsync<List<EmployeeDTO>>();
        dtos.Should().Contain(e => e.Id == employee.Id);
    }

    [Fact]
    public async Task GetAll_WithCasinoFilter_ShouldFilterResults()
    {
        var employee = await CreateEmployeeAsync();
        await CreateEmployeeAsync(); // drugi casino

        var response = await Client.GetAsync($"/api/employee?casinoId={employee.CasinoId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dtos = await response.Content.ReadFromJsonAsync<List<EmployeeDTO>>();
        dtos.Should().HaveCount(1);
        dtos![0].Id.Should().Be(employee.Id);
    }

    [Fact]
    public async Task GetById_ShouldReturnEmployeeWithNestedCasino_WhenExists()
    {
        var employee = await CreateEmployeeAsync();

        var response = await Client.GetAsync($"/api/employee/{employee.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<EmployeeDTO>();
        dto!.Id.Should().Be(employee.Id);
        dto.Casino.Should().NotBeNull();
        dto.Casino!.Id.Should().Be(employee.CasinoId);
    }

    [Fact]
    public async Task GetById_ShouldReturn404_WhenMissing()
    {
        var response = await Client.GetAsync("/api/employee/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_ShouldCreateEmployee_AndReturn201()
    {
        var casino = await CreateCasinoAsync();
        var model = new EmployeeInputDTO
        {
            FirstName = "Novi",
            LastName = "Djelatnik",
            Position = "Cashier",
            CasinoId = casino.Id
        };

        var response = await AdminClient.PostAsJsonAsync("/api/employee", model);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<EmployeeDTO>();
        dto!.Id.Should().BeGreaterThan(0);
        dto.Casino!.Id.Should().Be(casino.Id);
    }

    [Fact]
    public async Task Post_ShouldReturn400_WhenCasinoMissing()
    {
        var model = new EmployeeInputDTO
        {
            FirstName = "Novi",
            LastName = "Djelatnik",
            Position = "Cashier",
            CasinoId = 999999
        };

        var response = await AdminClient.PostAsJsonAsync("/api/employee", model);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ShouldReturn400_WhenModelInvalid()
    {
        var model = new { Position = "Cashier", CasinoId = 1 };

        var response = await AdminClient.PostAsJsonAsync("/api/employee", model);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ShouldReturn401_WhenAnonymous()
    {
        var casino = await CreateCasinoAsync();
        var model = new EmployeeInputDTO
        {
            FirstName = "Anon",
            LastName = "Djelatnik",
            Position = "Dealer",
            CasinoId = casino.Id
        };

        var response = await Client.PostAsJsonAsync("/api/employee", model);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_ShouldUpdateEmployee()
    {
        var employee = await CreateEmployeeAsync();
        var model = new EmployeeInputDTO
        {
            Id = employee.Id,
            FirstName = "Izmijenjeni",
            LastName = employee.LastName,
            Position = "Manager",
            CasinoId = employee.CasinoId
        };

        var response = await AdminClient.PutAsJsonAsync($"/api/employee/{employee.Id}", model);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<EmployeeDTO>();
        dto!.FirstName.Should().Be("Izmijenjeni");
        dto.Position.Should().Be("Manager");
    }

    [Fact]
    public async Task Put_ShouldReturn404_WhenMissing()
    {
        var casino = await CreateCasinoAsync();
        var model = new EmployeeInputDTO
        {
            FirstName = "Nepostojeći",
            LastName = "Djelatnik",
            Position = "Dealer",
            CasinoId = casino.Id
        };

        var response = await AdminClient.PutAsJsonAsync("/api/employee/999999", model);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ShouldRemoveEmployee_AndReturn204()
    {
        var employee = await CreateEmployeeAsync();

        var response = await AdminClient.DeleteAsync($"/api/employee/{employee.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var exists = await WithDbAsync(db => Task.FromResult(db.Employees.Any(e => e.Id == employee.Id)));
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ShouldReturn404_WhenMissing()
    {
        var response = await AdminClient.DeleteAsync("/api/employee/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
