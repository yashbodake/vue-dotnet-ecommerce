# Setup guide

Step-by-step local setup for Legacy Ecommerce.

## 1. Install prerequisites

1. **Visual Studio 2022** with workloads:
   - ASP.NET and web development
   - .NET desktop development (for targeting pack)
2. **SQL Server Express** with instance name `SQLEXPRESS` (default).
3. Confirm the service is running: `MSSQL$SQLEXPRESS`.

Optional: Python 3.10+ if you will regenerate the DummyJSON catalog seed.

## 2. Clone and open

```bat
git clone https://github.com/yashbodake/Legacy-Ecommerce.git
cd Legacy-Ecommerce
```

Open `Ecommerce.sln` (or the solution at the repo root, depending on how the repo is laid out).

## 3. Create the database

Run scripts in order against `.\SQLEXPRESS`:

| Order | File | Purpose |
|------:|------|---------|
| 1 | `database/00_CreateSchema.sql` | Tables + FKs |
| 2 | `database/01_SeedData.sql` | Baseline categories / sample data |
| 3 | `database/02_AspNetIdentity.sql` | Identity tables |
| 4 | `database/03_SeedAdmin.sql` | Admin role/user (optional; also seeded at runtime) |
| 5 | `database/04_IndexMaintenance.sql` | Helpful indexes |
| 6 | `database/05_SeedCatalogFromDummyJson.sql` | Full catalog (~194 products) |

Example:

```bat
"C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\SQLCMD.EXE" -S .\SQLEXPRESS -i database\00_CreateSchema.sql
```

Repeat for `01` … `05`.

### Regenerate catalog SQL from DummyJSON

```bat
python tools\seed_from_dummyjson.py --out database\05_SeedCatalogFromDummyJson.sql
```

Then re-run `05_…` with SQLCMD.

## 4. Connection string

`Ecommerce.Web/Web.config`:

```xml
data source=lpc:.\SQLEXPRESS;initial catalog=LegacyEcommerceDb;integrated security=True;MultipleActiveResultSets=True;Connect Timeout=5;App=EntityFramework
```

- `lpc:` = local shared memory (avoids SQL Browser TCP lookup delays).
- For a remote server use `data source=YOURHOST\INSTANCE` without `lpc:`.

Identity uses `DefaultConnection` with the same server/database.

## 5. Build and run

**Visual Studio:** set `Ecommerce.Web` as startup → F5.

**CLI:**

```bat
msbuild Ecommerce.sln /p:Configuration=Debug /v:m
"C:\Program Files\IIS Express\iisexpress.exe" /path:"%CD%\Ecommerce.Web" /port:44300
```

Browse: http://localhost:44300/

## 6. Verify

| Check | How |
|-------|-----|
| DI + SQL | Open `/Test` → product count |
| Catalog | Home shows products (SSR first page) |
| Auth | Login as `admin@legacy.local` / `Admin123!` |
| Admin | `/Admin/Products` as admin; customer should get 403 |
| Checkout | Add to cart → checkout → address → shipping → payment |

## 7. E2E scripts

With the site running:

```powershell
powershell -ExecutionPolicy Bypass -File tools\e2e-full-az.ps1 -BaseUrl http://localhost:44300
```

## Troubleshooting

| Symptom | Likely cause | Fix |
|---------|----------------|-----|
| Pages hang ~8–25s then work | SQL named instance / TCP / Browser | Keep using `lpc:.\SQLEXPRESS`; ensure SQL Express is running |
| `/Test` fails | DB missing or wrong connection | Re-run schema scripts; check Web.config |
| Empty catalog filters | No active products | Run `05_SeedCatalogFromDummyJson.sql` |
| Cannot leave Shipping step | Old Unicode radio values | Fixed: methods are `Standard` / `Express` (ASCII). Hard-refresh and try again |
| Images missing | CDN blocked | Thumbnails are `cdn.dummyjson.com` URLs; allow outbound HTTPS or re-seed with local paths |
| 403 on Admin | Not in Admin role | Use `admin@legacy.local` or re-run admin seed / restart app |

## Demo credentials

- **Admin:** `admin@legacy.local` / `Admin123!`
- **Customer:** register any new account via `/Account/Register`
