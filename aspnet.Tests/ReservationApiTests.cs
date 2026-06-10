using aspnet.Models.DTO;

namespace aspnet.Tests;

public class ReservationApiTests : ApiTestBase
{
    public ReservationApiTests(CasinoApiFactory factory) : base(factory) { }

    [Fact]
    public async Task GetAll_ShouldReturnOkWithCollection()
    {
        var reservation = await CreateReservationAsync();

        var response = await Client.GetAsync("/api/reservation");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dtos = await response.Content.ReadFromJsonAsync<List<ReservationDTO>>();
        dtos.Should().Contain(r => r.Id == reservation.Id);
    }

    [Fact]
    public async Task GetAll_WithPlayerFilter_ShouldFilterResults()
    {
        var reservation = await CreateReservationAsync();
        await CreateReservationAsync(); // drugi igrač

        var response = await Client.GetAsync($"/api/reservation?playerId={reservation.PlayerId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dtos = await response.Content.ReadFromJsonAsync<List<ReservationDTO>>();
        dtos.Should().HaveCount(1);
        dtos![0].Id.Should().Be(reservation.Id);
    }

    [Fact]
    public async Task GetById_ShouldReturnReservationWithNestedDtos_WhenExists()
    {
        var reservation = await CreateReservationAsync();

        var response = await Client.GetAsync($"/api/reservation/{reservation.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<ReservationDTO>();
        dto!.Id.Should().Be(reservation.Id);
        dto.Player.Should().NotBeNull();
        dto.Table.Should().NotBeNull();
        dto.Player!.Id.Should().Be(reservation.PlayerId);
        dto.Table!.Id.Should().Be(reservation.TableId);
    }

    [Fact]
    public async Task GetById_ShouldReturn404_WhenMissing()
    {
        var response = await Client.GetAsync("/api/reservation/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_ShouldCreateReservation_AndReturn201()
    {
        var player = await CreatePlayerAsync();
        var table = await CreateTableAsync();
        var model = new ReservationInputDTO
        {
            ReservedAt = new DateTime(2024, 7, 1, 21, 0, 0),
            PlayerId = player.Id,
            TableId = table.Id
        };

        var response = await AdminClient.PostAsJsonAsync("/api/reservation", model);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<ReservationDTO>();
        dto!.Id.Should().BeGreaterThan(0);
        dto.Player!.Id.Should().Be(player.Id);
        dto.Table!.Id.Should().Be(table.Id);
    }

    [Fact]
    public async Task Post_ShouldReturn400_WhenForeignKeysMissing()
    {
        var model = new ReservationInputDTO
        {
            ReservedAt = new DateTime(2024, 7, 1),
            PlayerId = 999999,
            TableId = 999999
        };

        var response = await AdminClient.PostAsJsonAsync("/api/reservation", model);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ShouldReturn400_WhenModelInvalid()
    {
        // PlayerId i TableId izvan dopuštenog raspona
        var model = new { ReservedAt = "2024-07-01T20:00:00", PlayerId = 0, TableId = 0 };

        var response = await AdminClient.PostAsJsonAsync("/api/reservation", model);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ShouldReturn401_WhenAnonymous()
    {
        var model = new ReservationInputDTO
        {
            ReservedAt = new DateTime(2024, 7, 1),
            PlayerId = 1,
            TableId = 1
        };

        var response = await Client.PostAsJsonAsync("/api/reservation", model);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_ShouldUpdateReservation()
    {
        var reservation = await CreateReservationAsync();
        var newDate = new DateTime(2024, 8, 15, 19, 30, 0);
        var model = new ReservationInputDTO
        {
            Id = reservation.Id,
            ReservedAt = newDate,
            PlayerId = reservation.PlayerId,
            TableId = reservation.TableId
        };

        var response = await AdminClient.PutAsJsonAsync($"/api/reservation/{reservation.Id}", model);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<ReservationDTO>();
        dto!.ReservedAt.Should().Be(newDate);
    }

    [Fact]
    public async Task Put_ShouldReturn404_WhenMissing()
    {
        var player = await CreatePlayerAsync();
        var table = await CreateTableAsync();
        var model = new ReservationInputDTO
        {
            ReservedAt = new DateTime(2024, 7, 1),
            PlayerId = player.Id,
            TableId = table.Id
        };

        var response = await AdminClient.PutAsJsonAsync("/api/reservation/999999", model);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ShouldRemoveReservation_AndReturn204()
    {
        var reservation = await CreateReservationAsync();

        var response = await AdminClient.DeleteAsync($"/api/reservation/{reservation.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var exists = await WithDbAsync(db => Task.FromResult(db.Reservations.Any(r => r.Id == reservation.Id)));
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ShouldReturn404_WhenMissing()
    {
        var response = await AdminClient.DeleteAsync("/api/reservation/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
