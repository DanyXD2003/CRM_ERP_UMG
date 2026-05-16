# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Run the app (Development mode, auto-applies migrations and seeds data)
dotnet run

# Build
dotnet build

# Add a new EF Core migration
dotnet ef migrations add <NombreMigracion>

# Apply migrations manually (normally done automatically on startup)
dotnet ef database update
```

The app runs at `http://localhost:5001` / `https://localhost:7032`.

## Architecture

ASP.NET Core 9 MVC application with PostgreSQL (Neon.tech cloud) via EF Core + Npgsql. All identifiers, view names, and user-facing strings are in **Spanish**.

### Key layers

- `Models/EntidadesSistema.cs` — all EF entities in a single file
- `Models/ModelosVista.cs` — view models (login, user creation, user listing)
- `Data/ContextoSistema.cs` — `IdentityDbContext<UsuarioAplicacion>` with all DbSets and Fluent API config (all monetary/quantity columns are `numeric(18,2)`; JSONB columns explicitly typed)
- `Data/SembradorDatos.cs` — seed runs on every startup: creates roles, default admin, a sample dynamic module, a "Consumidor Final" client, and two test products
- `Services/ServicioFormulas.cs` — custom expression evaluator using the Shunting-yard algorithm (RPN); supports `+`, `-`, `*`, `/`, parentheses, and named variables mapped from record fields
- `Controllers/` — one controller per feature; `RegistrosController`, `ModulosController`, `OperacionesController` handle the dynamic module system; `VentasController`, `ClientesController`, `ProductosController` handle fixed CRM/ERP entities

### Dynamic module system

The core architectural feature. `ModuloDinamico` stores a field schema as `Dictionary<string, CampoDinamico>` in a PostgreSQL `jsonb` column (`EsquemaCampos`). `RegistroDinamico` stores record data as `Dictionary<string, string>` in `jsonb` (`Datos`). `OperacionDinamica` defines named formulas over module records, evaluated at runtime by `ServicioFormulas`.

Field keys are normalized via `NormalizarClave()` (lowercase, spaces→underscores, strip non-alphanumeric). When a field is added or removed from a module schema, the controller backfills/cleans that key in all existing `RegistroDinamico` rows in the same save operation.

### Auth and roles

ASP.NET Core Identity with four roles: `Admin`, `Editor`, `Viewer`, `Vendedor`.

| Role | Access |
|------|--------|
| Admin | Full access including user management and module deletion |
| Editor | Create/edit modules, operations, clients, products |
| Vendedor | Create and view sales only |
| Viewer | Read-only access to modules and records |

Default admin seed credentials: `admin@umg.com` / `Admin123*`

Login redirect: `/Cuenta/Login`. Access denied: `/Cuenta/AccesoDenegado`.

### Sales (Ventas)

Sales creation runs inside a database transaction. It validates stock, reduces `Producto.Existencia`, computes `Impuesto = Subtotal * 0.12`, and generates a `NumeroVenta` timestamp key (`V-yyyyMMddHHmmss`). Viewer role cannot access the Ventas controller at all.

### Conventions

- Controllers use `TempData["Ok"]` / `TempData["Error"]` for feedback messages shown in views.
- Complex or dynamic forms use `IFormCollection` instead of model-bound parameters.
- All `decimal` parsing uses `CultureInfo.InvariantCulture` with comma→period normalization before parsing.
- Migrations are applied automatically at startup via `contexto.Database.MigrateAsync()`.
- The default route is `{controller=Modulos}/{action=Index}/{id?}`, so the app opens to the dynamic modules list.
- Npgsql dynamic JSON is enabled via `NpgsqlDataSourceBuilder.EnableDynamicJson()` in `Program.cs`, required for `jsonb` columns to deserialize correctly.
