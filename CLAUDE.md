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
// ICommonData, IAppointment, IAppointmentQuery, ISecurityUser, ISecurityModule,
// IDoctor, IAdminUser, IMedicalAttention, IPatientHistory
```

### Routes

```csharp
"{area:exists}/{controller=Home}/{action=Index}/{id?}"   // /Admin/...
"Patient/{action=Index}/{id?}"                           // /Patient/...
"{controller=Home}/{action=Landing}/{id?}"               // default
```

**IMPORTANT — Patient route parameter name:** The `Patient` route uses `{id?}`. Any `PatientController` action that receives a route segment (e.g. `/Patient/History/6`) must name its parameter `id`, not `patientId` or anything else — ASP.NET Core does NOT auto-map `id` → `patientId`.

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

## Database SQL Scripts

All scripts are idempotent (`CREATE OR ALTER`, `IF NOT EXISTS`). Execute in SSMS when setting up or after pulling changes.

| File | Contents |
|---|---|
| `SPs_Admin_Modules.sql` | Doctor, SpecialtyDoctor, SecurityUser, SecurityRole, Clinic_GetAll (PASO 11) |
| `SPs_Dashboard.sql` | Appointment_Detail view, Appointment_GetByClinic, Appointment_GetById |
| `SPs_Patient_GuardianFix.sql` | PatientHistory nullable cols, IsGuardianOnly, Patient_GetAll/AddOrEdit/GetFamily fixes (PASOs 1-8) |
| `SPs_MedicalAttention.sql` | MedicalAttention table + all SPs (PASOs 1-22+) |
| `SPs_PatientId_Appointment.sql` | Appointment.PatientId FK + SetPatientId + GetConfirmedByClinic (PASOs 1-7) |
| `SPs_PatientHistory.sql` | **NEW** — PatientHistory table + PatientHistory_GetByPatientId + PatientHistory_Save (PASOs 1-3) |

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

### PatientHistory — Historia Médica
`PatientHistory` table has a 1:1 relationship with `Patient` (UNIQUE constraint on `PatientId`). One row per patient, upserted via `PatientHistory_Save`.

- `IPatientHistory.GetByPatientId(int)` → returns null if no record yet (patient has no history filled in)
- `IPatientHistory.Save(model, userId)` → upsert; sets `LastUpdated` and `UpdatedByUserId`
- Accessible from two places:
  - `/Patient/History/{id}` — full page (Tab 1: Antecedentes form, Tab 2: Consultas accordion)
  - `/Medical/Patient/{appointmentId}` — collapsed accordion "Antecedentes del Paciente"
- Both save via `POST /Patient/SaveHistory`
- `SmokingStatus` / `AlcoholStatus` / `PhysicalActivity` stored as `TINYINT` (0/1/2). Use `SqlDbType.TinyInt` explicitly — not `AddWithValue` — to avoid type mismatch.

### IsGuardianOnly
`IsGuardianOnly = 1` excludes the record from `Patient_GetAll`. Set in two flows:
1. **Patient module** — minor save flow (`saveMinor()` 3-step): guardian saved with `IsGuardianOnly = true`
2. **Dashboard modal "Registrar Paciente"** — `HomeController.RegisterPatient` forces `request.Guardian.IsGuardianOnly = true`

SP protection: `Patient_AddOrEdit` UPDATE uses `CASE WHEN IsGuardianOnly = 0 THEN 0 ELSE @IsGuardianOnly END` — a real patient (0) never downgrades to guardian-only (1).

## Code conventions

- Identifiers (JS variables, HTML IDs, CSS classes, C# model properties): **English**
- UI text, labels, user-facing error messages, C# exception messages: **Spanish**
- Clinic type is hardcoded: 1=Clínica, 2=Hospital, 3=Consultorio — no separate table
