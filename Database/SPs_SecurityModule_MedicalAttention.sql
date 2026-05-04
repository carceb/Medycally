-- =============================================================================
-- Agrega el módulo "Atención Médica" a SecurityModule para que aparezca
-- en la matriz de permisos de Roles.
-- Ejecutar en SSMS sobre la BD de Medycally.
-- =============================================================================

-- PASO 1: Insertar módulo Atención Médica (si no existe)
IF NOT EXISTS (SELECT 1 FROM dbo.SecurityModule WHERE ModuleUrl = '/Medical/Index')
    INSERT INTO dbo.SecurityModule (ModuleName, ModuleUrl, ModuleIcon, ModuleOrder, IsActive)
    VALUES (N'Atención Médica', '/Medical/Index', 'fa-stethoscope', 4, 1);
GO

-- PASO 2: Ver qué módulos existen actualmente en el sistema
SELECT SecurityModuleId, ModuleName, ModuleUrl, ModuleIcon, ModuleOrder, IsActive
FROM   dbo.SecurityModule
ORDER BY ModuleOrder, SecurityModuleId;
GO
