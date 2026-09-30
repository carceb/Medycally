-- =============================================================================
-- Registra el menú "Configuración" como SecurityModule data-driven, para que
-- aparezca en el modal de Roles y en el sidebar via SidebarViewComponent.
-- Idempotente: re-ejecutable en SSMS.
--
-- Estructura final:
--   Configuración (parent, sin URL, ícono fa-cog)
--     ├── Especialidades  (/Admin/Specialty)
--     ├── Clínicas        (/Admin/Clinic)
--     ├── Médicos         (/Admin/Doctor)
--     ├── Usuarios        (/Admin/User)
--     ├── Tarifas         (/Admin/Fee)
--     ├── Roles           (/Admin/Role)
--     └── Módulos         (/Admin/Module)    -- reparentado desde top-level
-- =============================================================================

SET NOCOUNT ON;

DECLARE @SuperAdminRoleId INT = (
    SELECT TOP 1 SecurityRoleId
    FROM   dbo.SecurityRole
    WHERE  IsSuperAdmin = 1
);

IF @SuperAdminRoleId IS NULL
BEGIN
    RAISERROR('No se encontró un rol con IsSuperAdmin = 1.', 16, 1);
    RETURN;
END

-- =============================================================================
-- PASO 1: Configuración (módulo padre, sin URL)
-- =============================================================================
IF NOT EXISTS (
    SELECT 1 FROM dbo.SecurityModule
    WHERE  ModuleName = N'Configuración' AND ParentSecurityModuleId IS NULL
)
BEGIN
    INSERT INTO dbo.SecurityModule
        (ModuleName, ModuleUrl, ModuleIcon, ModuleOrder, IsActive, ParentSecurityModuleId)
    VALUES
        (N'Configuración', NULL, 'fa-cog', 95, 1, NULL);
END

DECLARE @ConfigParentId INT = (
    SELECT TOP 1 SecurityModuleId
    FROM   dbo.SecurityModule
    WHERE  ModuleName = N'Configuración' AND ParentSecurityModuleId IS NULL
);

-- =============================================================================
-- PASO 2: Insertar los hijos que falten
-- =============================================================================
DECLARE @Children TABLE (
    ModuleName VARCHAR(80),
    ModuleUrl  VARCHAR(150),
    ModuleIcon VARCHAR(50),
    Ord        TINYINT
);
INSERT INTO @Children VALUES
    (N'Especialidades', '/Admin/Specialty', 'fa-stethoscope', 1),
    (N'Clínicas',       '/Admin/Clinic',    'fa-hospital',    2),
    (N'Médicos',        '/Admin/Doctor',    'fa-user-md',     3),
    (N'Usuarios',       '/Admin/User',      'fa-users-cog',   4),
    (N'Tarifas',        '/Admin/Fee',       'fa-dollar-sign', 5),
    (N'Roles',          '/Admin/Role',      'fa-shield-alt',  6);

INSERT INTO dbo.SecurityModule (ModuleName, ModuleUrl, ModuleIcon, ModuleOrder, IsActive, ParentSecurityModuleId)
SELECT c.ModuleName, c.ModuleUrl, c.ModuleIcon, c.Ord, 1, @ConfigParentId
FROM   @Children c
WHERE  NOT EXISTS (
    SELECT 1 FROM dbo.SecurityModule sm WHERE sm.ModuleUrl = c.ModuleUrl
);

-- =============================================================================
-- PASO 3: Reparentar /Admin/Module bajo Configuración (estaba en top-level)
-- =============================================================================
UPDATE dbo.SecurityModule
SET    ParentSecurityModuleId = @ConfigParentId,
       ModuleOrder            = 7,
       ModuleIcon             = ISNULL(ModuleIcon, 'fa-th-list')
WHERE  ModuleUrl = '/Admin/Module';

-- =============================================================================
-- PASO 4: Otorgar SuperAdmin permisos full SÓLO en los hijos.
-- IMPORTANTE: los padres-agrupadores NO deben tener SecurityRoleModule.
-- Si la tienen, el SP Security_GetUserModulePermissions los duplica en el
-- sidebar (caen en AccessibleLeaves y AccessibleParents simultáneamente).
-- =============================================================================
INSERT INTO dbo.SecurityRoleModule (SecurityRoleId, SecurityModuleId, CanView, CanCreate, CanEdit, CanDelete)
SELECT @SuperAdminRoleId, sm.SecurityModuleId, 1, 1, 1, 1
FROM   dbo.SecurityModule sm
WHERE  sm.ParentSecurityModuleId = @ConfigParentId
  AND  NOT EXISTS (
        SELECT 1 FROM dbo.SecurityRoleModule srm
        WHERE  srm.SecurityRoleId   = @SuperAdminRoleId
          AND  srm.SecurityModuleId = sm.SecurityModuleId
  );

-- Defensive: si por alguna razón el padre Configuración quedó con SRM, bórralo
DELETE FROM dbo.SecurityRoleModule
WHERE SecurityModuleId = @ConfigParentId;

PRINT 'OK — Configuración + submódulos registrados y permisos SuperAdmin aplicados.';
PRINT '   Recuerda reiniciar la app si tocaste _Layout.cshtml para remover el bloque hardcoded.';
