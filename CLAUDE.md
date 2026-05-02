# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Stack

- **Framework**: ASP.NET Core 10.0 MVC (.NET 10) — `Medycally.csproj`
- **Database**: SQL Server — `Microsoft.Data.SqlClient` v7.0.0 — **no Entity Framework, no LINQ-to-SQL**
- **UI**: Razor Views + Bootstrap 5 + Font Awesome 6 + vanilla JS

## Running the app

```bash
dotnet run
```

Development environment uses `Server=CARLOS-LAPTOP\\SQLEXPRESS;Initial Catalog=Medycally;...` automatically via `ASPNETCORE_ENVIRONMENT`.

## Architecture

### Data Access Layer

All CRUD goes through `Core/Data/ISqlConnectionFactory` — never create `SqlConnection` directly. Every DAL class lives in `Core/`, implements an interface in `Core/Interfaces.cs`, and must include `using Medycally.Core.Data;`.

- **Views** → `CommandType.Text`
- **Stored Procedures** → `CommandType.StoredProcedure`

Reading rules (always follow):
- Never `SELECT *` — always explicit column list
- Never direct cast `(string)dr["col"]` — always `dr.GetOrdinal` + `dr.IsDBNull` for nullable fields
- `if (dr.Read())` for single rows, `while (dr.Read())` for lists

### Adding a new entity

1. `Models/XxxModel.cs`
2. Interface `IXxx` in `Core/Interfaces.cs`
3. `Core/Xxx.cs` implementing it — add `using Medycally.Core.Data;`
4. `builder.Services.AddScoped<IXxx, Xxx>()` in `Program.cs`
5. Inject into the controller

### DI registration (Program.cs)

```csharp
builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
// All others: AddScoped<IXxx, Xxx>()
// IClinic, ISpecialty, IDoctorSchedule, IPatient, IReason, IGeography,
// ICommonData, IAppointment, ISecurityUser, ISecurityModule, IDoctor, IAdminUser
```

### Routes

```csharp
"{area:exists}/{controller=Home}/{action=Index}/{id?}"   // /Admin/...
"{controller=Home}/{action=Landing}/{id?}"               // default
```

## Authentication

Cookie auth with 8-hour sliding expiration. Claims stored: `NameIdentifier`=SecurityUserId, `Name`=UserName, `Email`=UserEmail, `"SecurityRoleId"`, `Role`=RoleName, `"RoleLevel"`.

Password hashing: `SHA256.HashData(Encoding.UTF8.GetBytes(password))` → `Convert.ToHexString().ToLower()`

To compute a hash from SQL: `SELECT LOWER(CONVERT(VARCHAR(256), HASHBYTES('SHA2_256', 'TuClave'), 2))`

Demo user: `admin@medycally.com` / `Admin123` — hash: `3b612c75a7b5048a435fb6ec81e52ff92d6d795a8b5a9c17070f6a63c97a53b2`

QuickLogin button appears in `Views/Account/Login.cshtml` only in Development — auto-logs in as the demo user.

## Admin Area (`Areas/Admin/`)

Controllers carry `[Area("Admin")]` and `[Authorize]`. Layout resolves to `Views/Shared/_Layout.cshtml` (shared with main site).

### Admin CRUD pattern (all four modules follow this exactly)

**Controller:**
- `Index()` GET → load list + `ViewBag` with dropdown data → `return View(list)`
- `Save([FromBody] Model)` POST → call DAL → `return Ok(new { id })`
- `Delete([FromBody] int id)` POST → call DAL → `return Ok()`

**View — data-driven JS:**
- `allXxx` serialized from `@Html.Raw(JsonSerializer.Serialize(Model))` — sole source of truth, **always PascalCase**
- `applyFilters()` filters `allXxx`, resets `currentPage=1`, calls `renderTable()`
- `renderTable()` renders the current page slice
- `renderPagination()` — smart pagination with ellipsis
- `PAGE_SIZE = 10`
- After save: build `newDoc` with **clean PascalCase properties** (never mix with the camelCase fetch payload), then `allXxx.push(newDoc)` or `allXxx[idx] = newDoc`, then `applyFilters()`. **Never use `Object.assign()`**.
- After delete: `allXxx.splice(idx, 1)`, then `applyFilters()`.
- `return Json()` from controllers produces PascalCase (no custom JSON options configured).

## Database SQL Script

`Database/SPs_Admin_Modules.sql` contains all SPs for Doctor, SpecialtyDoctor, SecurityUser, SecurityRole, and the updated `Clinic_GetAll` (PASO 11). Each block is idempotent. Execute in SSMS when setting up or after pulling changes.

## Key gotchas

### DoctorSchedule — DayOfWeek column conflict
`DayOfWeek` column name conflicts with `SqlDataReader` and throws `IndexOutOfRangeException`. It is omitted from the SELECT and derived in C# from `DayName` via a switch statement. Never add `DayOfWeek` back to the query.

### Geography — GetMunicipalities
`Geography.GetAllMunicipalities()` must explicitly select `StateId` — it's used in JS for synchronous filtering. Never use `SELECT *` here. The `GetMunicipalities` endpoint still exists in `ClinicController` but the Clinic view no longer calls it — municipios are preloaded via `ViewBag.Municipalities` as `allMunicipalities` and filtered synchronously.

### ClinicGroupId
`Clinic.ClinicGroupId = 0` means the clinic is a group root. `ClinicGroupId > 0` means it's a branch — the value is the `ClinicId` of the parent. The `ClinicGroup` table has been discarded and should not be referenced.

### return Json() vs Razor serialization

`return Json(list)` en controllers produce JSON **camelCase** (`specialtyName`, `specialtyId`). `@Html.Raw(JsonSerializer.Serialize(Model))` en Razor produce **PascalCase** (`SpecialtyName`, `SpecialtyId`). Nunca mezclarlos sin normalizar. Patrón seguro: `s.SpecialtyName ?? s.specialtyName`.

### Latitude/Longitude
Use `SqlParameter(SqlDbType.Decimal) { Precision=18, Scale=8 }` — not `AddWithValue`. The SP parameters and table columns are `DECIMAL(18,8)`. Values with zero decimal places (e.g. `666666`) can still cause a "numeric to decimal" error whose root cause is unconfirmed.

### AdminUser password
`AdminUser.cs` hashes `UserPassword` before calling the SP. The SP receives `@PasswordHash`. If `@PasswordHash` is NULL, the SP keeps the existing hash (edit without password change).

## Code conventions

- Identifiers (JS variables, HTML IDs, CSS classes, C# model properties): **English**
- UI text, labels, user-facing error messages, C# exception messages: **Spanish**
- Clinic type is hardcoded: 1=Clínica, 2=Hospital, 3=Consultorio — no separate table
