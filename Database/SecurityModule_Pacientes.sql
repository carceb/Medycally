-- =============================================================================
-- Agrega el módulo "Pacientes" al menú lateral.
-- Ejecutar en SSMS sobre la BD de Medycally.
-- =============================================================================

-- PASO 1: Insertar módulo Pacientes
IF NOT EXISTS (SELECT 1 FROM dbo.SecurityModule WHERE ModuleUrl = '/Patient/Index')
    INSERT INTO dbo.SecurityModule (ModuleName, ModuleUrl, ModuleIcon)
    VALUES (N'Pacientes', '/Patient/Index', 'fa-users');
GO

-- PASO 2: Asignar a todos los roles existentes
DECLARE @ModuleId INT = (SELECT SecurityModuleId FROM dbo.SecurityModule WHERE ModuleUrl = '/Patient/Index');

INSERT INTO dbo.SecurityRoleModule (SecurityRoleId, SecurityModuleId, CanCreate, CanEdit, CanDelete)
SELECT r.SecurityRoleId, @ModuleId, 1, 1, 1
FROM dbo.SecurityRole r
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.SecurityRoleModule x
    WHERE x.SecurityRoleId   = r.SecurityRoleId
      AND x.SecurityModuleId = @ModuleId
);
GO
