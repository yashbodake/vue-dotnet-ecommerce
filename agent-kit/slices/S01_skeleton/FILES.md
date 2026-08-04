# S01 FILES

## Allowed to read
- `database/` (confirm exists)
- Root `README.md` (optional)
- This slice pack + `00_GLOBAL/*`

## Allowed to write
- `modern/**` (create)
- `modern/docker-compose.yml`, `modern/.dockerignore`, `**/Dockerfile`
- `tools/modern-up.ps1`
- `tools/db-setup.ps1` (optional, host SQL only)
- `modern/.gitignore` or root entries for `modern/**/bin|obj|node_modules`
- `modern/.env.example` (no real secrets)

## Forbidden
- Editing `Ecommerce.Web/**`, `Ecommerce.Services/**`, `Ecommerce.Data/**`, `Ecommerce.Core/**`
- Implementing catalog/auth business logic (S02+)
- Adding a SQL Server / mssql Docker service
