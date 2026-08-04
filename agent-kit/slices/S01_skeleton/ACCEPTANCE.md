# S01 ACCEPTANCE

- [ ] `dotnet build modern/Ecommerce.Modern.sln` succeeds (host)
- [ ] `modern/ecommerce-web` `npm run build` succeeds (host)
- [ ] `docker compose -f modern/docker-compose.yml config` succeeds
- [ ] Compose has **api**, **gateway**, **web** only — **no** SQL/mssql service
- [ ] `tools/modern-up.ps1` runs compose against `modern/`
- [ ] No project reference from modern → Ecommerce.Web/Services/Data/Core
- [ ] Solution contains Api, Gateway, Api.Tests only (.NET) + separate Vue folder
