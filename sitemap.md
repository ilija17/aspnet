# Sitemap – Casino App URL Routing

## Custom routes (attribute routing)

| URL | Controller | Action | View |
|---|---|---|---|
| `/kasina` | CasinoController | Index | Views/Casino/Index.cshtml |
| `/kasina/{id:int}` | CasinoController | Details | Views/Casino/Details.cshtml |
| `/igraci` | PlayerController | Index | Views/Player/Index.cshtml |
| `/igraci/{id:int}` | PlayerController | Details | Views/Player/Details.cshtml |
| `/igraci/{id:int}/uredi` | PlayerController | Edit (GET) | Views/Player/Edit.cshtml |
| `/igraci/{id:int}/uredi` (POST) | PlayerController | Edit (POST) | redirect → /igraci/{id} |
| `/igre` | GameController | Index | Views/Game/Index.cshtml |
| `/igre/{id:int}` | GameController | Details | Views/Game/Details.cshtml |
| `/stolovi` | TableController | Index | Views/Table/Index.cshtml |
| `/stolovi/{id:int}` | TableController | Details | Views/Table/Details.cshtml |
| `/transakcije` | TransactionController | Index | Views/Transaction/Index.cshtml |

## Default routes (conventional routing: `{controller}/{action}/{id?}`)

| URL | Controller | Action | View |
|---|---|---|---|
| `/` | HomeController | Index | Views/Home/Index.cshtml |
| `/Home/Privacy` | HomeController | Privacy | Views/Home/Privacy.cshtml |
| `/Employee` | EmployeeController | Index | Views/Employee/Index.cshtml |
| `/Employee/Details/{id}` | EmployeeController | Details | Views/Employee/Details.cshtml |
| `/Reservation` | ReservationController | Index | Views/Reservation/Index.cshtml |
| `/Reservation/Details/{id}` | ReservationController | Details | Views/Reservation/Details.cshtml |

## Shared views (no direct URL)

| File | Used by |
|---|---|
| Views/Shared/_Layout.cshtml | All pages (master layout) |
| Views/Shared/Error.cshtml | Error handler |
| Views/Shared/_ValidationScriptsPartial.cshtml | Edit/create forms |
| Views/_ViewImports.cshtml | Injects tag helpers globally |
| Views/_ViewStart.cshtml | Sets default layout |
