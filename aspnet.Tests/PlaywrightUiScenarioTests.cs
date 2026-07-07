using System.Text.RegularExpressions;
using aspnet.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Playwright;

namespace aspnet.Tests;

/// <summary>
/// Kao CasinoApiFactory, ali bez test-autentikacije: prijava ide kroz pravi
/// Identity cookie flow u browseru. Kestrel server na slučajnom portu da se
/// Playwright (pravi Chromium) može spojiti preko HTTP-a.
/// </summary>
public class PlaywrightServerFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"CasinoPlaywright-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "TestConnection"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<CasinoDbContext>));
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<CasinoDbContext>));

            services.AddDbContext<CasinoDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });
    }
}

/// <summary>
/// Pravi end-to-end Playwright scenarij od 10 koraka: headless Chromium
/// prolazi prijavu, CRUD nad kasinom kroz MVC forme, globalnu pretragu
/// i odjavu — sve kroz UI, bez direktnih API poziva.
/// </summary>
public class PlaywrightUiScenarioTests : IAsyncLifetime
{
    private PlaywrightServerFactory _factory = null!;
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private string _baseUrl = null!;

    public async Task InitializeAsync()
    {
        _factory = new PlaywrightServerFactory();
        _factory.UseKestrel(0);
        _factory.StartServer();
        // U Kestrel modu CreateClient vraća pravi HttpClient uperen na server
        _baseUrl = _factory.CreateClient().BaseAddress!.ToString().TrimEnd('/');

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync();
    }

    public async Task DisposeAsync()
    {
        await _browser.DisposeAsync();
        _playwright.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task TenStepScenario_LoginCrudGlobalSearchLogout()
    {
        var page = await _browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 800 }
        });

        // ── Korak 1: početna stranica se učita, korisnik je anoniman ──
        await page.GotoAsync(_baseUrl + "/");
        await Assertions.Expect(page.Locator("a", new PageLocatorOptions { HasTextString = "Prijava" }).First)
            .ToBeVisibleAsync();

        // ── Korak 2: prijava kroz login formu (seedani admin) ──
        await page.ClickAsync("a:has-text('Prijava')");
        await page.FillAsync("#Email", "admin@casino.local");
        await page.FillAsync("#Password", "Admin123$");
        await page.ClickAsync("button:has-text('Prijavi se')");

        // ── Korak 3: navbar pokazuje prijavljenog korisnika (gumb Odjava) ──
        await Assertions.Expect(page.Locator(".btn-logout")).ToBeVisibleAsync();

        // ── Korak 4: popis kasina s gumbom za dodavanje ──
        await page.GotoAsync(_baseUrl + "/kasina");
        await Assertions.Expect(page.Locator("a[href='/kasina/novi']")).ToBeVisibleAsync();

        // ── Korak 5: kreiranje novog kasina kroz MVC formu ──
        await page.ClickAsync("a[href='/kasina/novi']");
        await page.FillAsync("#Name", "Playwright Palace");
        await page.FillAsync("#Address", "Testna 10, Zagreb");
        await page.FillAsync("#LicenseNumber", "HR-PW-001");
        // _DatePicker koristi custom widget nad skrivenim inputom
        await page.EvalOnSelectorAsync("#FoundedDate", "el => el.value = '2020-06-15'");
        await page.ClickAsync("button:has-text('Spremi')");

        // ── Korak 6: redirect na detalje, novi casino je vidljiv ──
        await page.WaitForURLAsync(new Regex(@"/kasina/\d+$"));
        await Assertions.Expect(page.Locator("body")).ToContainTextAsync("Playwright Palace");
        var detailUrl = page.Url;

        // ── Korak 7: uređivanje kroz MVC formu ──
        await page.ClickAsync("a:has-text('Uredi')");
        await page.FillAsync("#Name", "Playwright Palace Deluxe");
        await page.ClickAsync("button:has-text('Spremi')");
        await page.WaitForURLAsync(new Regex(@"/kasina/\d+$"));
        await Assertions.Expect(page.Locator("body")).ToContainTextAsync("Playwright Palace Deluxe");

        // ── Korak 8: globalna pretraga nalazi podatke i stranice ──
        await page.FillAsync(".global-search-input", "Playwright Palace");
        await Assertions.Expect(page.Locator(".global-search-hit").First).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".global-search-hit").First)
            .ToContainTextAsync("Playwright Palace Deluxe");

        await page.FillAsync(".global-search-input", "playground");
        await Assertions.Expect(page.Locator(".global-search-hit[href='/playground']"))
            .ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Escape");

        // ── Korak 9: brisanje kroz UI (uz potvrdu confirm dijaloga) ──
        page.Dialog += async (_, dialog) => await dialog.AcceptAsync();
        await page.GotoAsync(detailUrl);
        await page.ClickAsync("button:has-text('Obriši')");
        await page.WaitForURLAsync(new Regex(@"/kasina$"));
        await Assertions.Expect(page.Locator("body")).Not.ToContainTextAsync("Playwright Palace Deluxe");

        // ── Korak 10: odjava vraća anonimno stanje ──
        await page.ClickAsync(".btn-logout");
        await Assertions.Expect(page.Locator("a", new PageLocatorOptions { HasTextString = "Prijava" }).First)
            .ToBeVisibleAsync();
    }
}
