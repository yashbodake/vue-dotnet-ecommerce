# Legacy Ecommerce

> **Migration redo:** To rebuild the modern stack from scratch with the agent kit, read **[AGENTS.md](AGENTS.md)** and start at [`agent-kit/NEXT_TASK.md`](agent-kit/NEXT_TASK.md). The finished prior migration lives in [Ecommerce-Modern](https://github.com/yashbodake/Ecommerce-Modern) (reference only).

A full-stack **.NET Framework 4.7.2** eCommerce demo built with **ASP.NET MVC 5**, **Entity Framework 6 (Database-First)**, **SQL Server Express**, **Unity DI**, **ASP.NET Identity (OWIN)**, and **jQuery**.

Specs **00–10** are implemented. Suitable as a portfolio / interview sample of layered “legacy stack” architecture.

**Live local URL (IIS Express):** `http://localhost:44300/`

---

## Architecture

```
Ecommerce.Web       → MVC controllers, Razor, Identity, bundling
        ↓
Ecommerce.Services  → Business logic + Unity registrations
        ↓
Ecommerce.Data      → EF6 DbContext (EDMX), repositories
        ↓
Ecommerce.Core      → POCOs, interfaces, view models (no EF/MVC refs)
```

**Rule:** Web never references Data directly. Controllers depend on Core interfaces; Unity resolves implementations.

| Project | Role |
|---------|------|
| `Ecommerce.Core` | Domain models, `IProductService` / `ICartService` / `IOrderService` |
| `Ecommerce.Data` | `EcommerceEntities`, repositories, EDMX |
| `Ecommerce.Services` | Product / Cart / Order services, mapping, DI |
| `Ecommerce.Web` | Site UI, Identity, Admin |

---

## Features

| Area | What you get |
|------|----------------|
| **Catalog** | Filter, search, sort, pagination (12/page), Fancybox detail gallery |
| **Cart** | Guest cart (session), merge on login, AJAX add/update/remove |
| **Accounts** | Register / login / log off, order history (IDOR → 404) |
| **Checkout** | Address → Shipping → Payment → Confirmation (fake card; stock checks) |
| **Admin** | Product soft-delete CRUD, order status; non-admins get **403** |
| **Hardening** | Anti-forgery, `HandleError` + `Error.cshtml`, bundling, DB indexes |

---

## Prerequisites

- Windows + [Visual Studio 2022](https://visualstudio.microsoft.com/) (ASP.NET / .NET desktop workload)
- [SQL Server Express](https://www.microsoft.com/sql-server/sql-server-downloads) instance `.\SQLEXPRESS`
- .NET Framework 4.7.2 targeting pack
- (Optional) Python 3 — only if regenerating DummyJSON catalog SQL

---

## Quick start

### 1. Database

Using classic SQLCMD (adjust path if needed):

```bat
set SQLCMD="C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\SQLCMD.EXE"
set DB=.\SQLEXPRESS

%SQLCMD% -S %DB% -i database\00_CreateSchema.sql
%SQLCMD% -S %DB% -i database\01_SeedData.sql
%SQLCMD% -S %DB% -i database\02_AspNetIdentity.sql
%SQLCMD% -S %DB% -i database\03_SeedAdmin.sql
%SQLCMD% -S %DB% -i database\04_IndexMaintenance.sql
%SQLCMD% -S %DB% -i database\05_SeedCatalogFromDummyJson.sql
```

`05_…` soft-deactivates prior catalog products and loads ~194 DummyJSON products with CDN thumbnails (safe with existing order FKs).

### 2. Run the site

1. Open `Ecommerce.sln` in Visual Studio 2022  
2. Set **Ecommerce.Web** as startup project  
3. Restore NuGet packages → F5 (or IIS Express on port **44300**)

Or from a developer command prompt:

```bat
msbuild Ecommerce.sln /p:Configuration=Debug
"C:\Program Files\IIS Express\iisexpress.exe" /path:"%CD%\Ecommerce.Web" /port:44300
```

### 3. Smoke checks

| URL | Expected |
|-----|----------|
| `/Test` | `OK — product count: …` |
| `/` | Catalog with products |
| `/Account/Login` | Sign-in form |

---

## Demo accounts

| Role | Email | Password |
|------|-------|----------|
| **Admin** | `admin@legacy.local` | `Admin123!` |

Register any other email for a normal customer. Admin is also ensured on app startup (`AdminSeed`).

---

## Connection strings

Configured in `Ecommerce.Web/Web.config` (and mirrored in Data/Services configs):

```
data source=lpc:.\SQLEXPRESS;initial catalog=LegacyEcommerceDb;integrated security=True;MultipleActiveResultSets=True;Connect Timeout=5;…
```

`lpc:` forces **shared memory** so local named-instance connections do not hang on SQL Browser / TCP timeouts. If you use a remote SQL host, change `data source` accordingly (and drop `lpc:`).

---

## Project layout

```
Ecommerce/
├── Ecommerce.sln
├── README.md                 ← you are here
├── docs/                     ← deeper documentation
│   ├── README.md
│   ├── SETUP.md
│   └── Legacy_Ecommerce_Project_Plan_and_Specs.md
├── database/                 ← SQL schema + seeds
├── tools/                    ← seed generator + E2E scripts
│   ├── seed_from_dummyjson.py
│   ├── e2e-spec-verify.ps1
│   └── e2e-full-az.ps1
├── Ecommerce.Core/
├── Ecommerce.Data/
├── Ecommerce.Services/
└── Ecommerce.Web/
```

---

## Specs (00–10)

| Spec | Deliverable |
|------|-------------|
| 00 | Schema + seed SQL |
| 01 | Solution skeleton + Unity DI |
| 02 | Core models / interfaces |
| 03 | Repositories |
| 04 | Product / Cart / Order services |
| 05 | Catalog UI |
| 06 | Cart UI + `cart.js` |
| 07 | Identity, orders, guest cart merge |
| 08 | Checkout wizard |
| 09 | Admin CRUD + order status |
| 10 | Bundling, errors, indexes, antiforgery |

Full write-up: [`docs/Legacy_Ecommerce_Project_Plan_and_Specs.md`](docs/Legacy_Ecommerce_Project_Plan_and_Specs.md)

---

## Tools

| Script | Purpose |
|--------|---------|
| `tools/seed_from_dummyjson.py` | Fetch DummyJSON → regenerate `database/05_SeedCatalogFromDummyJson.sql` |
| `tools/e2e-spec-verify.ps1` | Spec-oriented HTTP checks |
| `tools/e2e-full-az.ps1` | Full A–Z E2E against a running site |

Example:

```powershell
powershell -ExecutionPolicy Bypass -File tools\e2e-full-az.ps1 -BaseUrl http://localhost:44300
```

---

## Performance notes

- Catalog is **paged** (12 items); first page is server-rendered.
- Mini-cart badge uses a **count-only** query (no product joins on every page).
- Prefer `lpc:.\SQLEXPRESS` locally; keep SQL Express running to avoid cold-start stalls.
- Product images are absolute DummyJSON CDN URLs — fine for demos; mirror locally for offline/air-gapped use.

---

## License / intent

Educational / portfolio sample. Not a production payment system — checkout cards are **demo-only** and are never charged.
