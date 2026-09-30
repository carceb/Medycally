-- =============================================================================
-- Diagnóstico + limpieza de filas duplicadas para "Configuración" en SecurityModule.
-- Mantiene la fila con el SecurityModuleId más bajo, reparenta los hijos de las
-- duplicadas hacia la que se conserva, y borra los SecurityRoleModule huérfanos.
-- Idempotente: re-ejecutable.
-- =============================================================================

SET NOCOUNT ON;

-- 1) Diagnóstico — ver qué hay
PRINT '== Filas Configuración en SecurityModule ==';
SELECT SecurityModuleId, ModuleName, ParentSecurityModuleId, ModuleUrl, ModuleIcon, ModuleOrder, IsActive
FROM   dbo.SecurityModule
WHERE  ModuleName = N'Configuración'
ORDER BY SecurityModuleId;

-- 2) Identificar la fila a conservar
DECLARE @KeepId INT;
SELECT TOP 1 @KeepId = SecurityModuleId
FROM   dbo.SecurityModule
WHERE  ModuleName = N'Configuración' AND ParentSecurityModuleId IS NULL
ORDER BY SecurityModuleId;

IF @KeepId IS NULL
BEGIN
    PRINT 'No hay ninguna fila Configuración. Ejecuta primero SPs_SecurityModule_AdminMenu.sql.';
    RETURN;
END

DECLARE @DupCount INT = (
    SELECT COUNT(*) FROM dbo.SecurityModule
    WHERE ModuleName = N'Configuración' AND ParentSecurityModuleId IS NULL
      AND SecurityModuleId <> @KeepId
);

IF @DupCount = 0
BEGIN
    PRINT 'Sin duplicados — sólo existe 1 Configuración (Id = ' + CAST(@KeepId AS VARCHAR) + '). Nada que limpiar.';
    RETURN;
END

PRINT 'Encontrados ' + CAST(@DupCount AS VARCHAR) + ' duplicados. Limpiando…';

DECLARE @DuplicateIds TABLE (Id INT PRIMARY KEY);
INSERT INTO @DuplicateIds (Id)
SELECT SecurityModuleId
FROM   dbo.SecurityModule
WHERE  ModuleName = N'Configuración' AND ParentSecurityModuleId IS NULL
  AND  SecurityModuleId <> @KeepId;

-- 3) Reparentar cualquier hijo de los duplicados hacia el que conservamos
UPDATE dbo.SecurityModule
SET    ParentSecurityModuleId = @KeepId
WHERE  ParentSecurityModuleId IN (SELECT Id FROM @DuplicateIds);

-- 4) Borrar los SecurityRoleModule de los duplicados (los del @KeepId quedan intactos)
DELETE FROM dbo.SecurityRoleModule
WHERE SecurityModuleId IN (SELECT Id FROM @DuplicateIds);

-- 5) Borrar las filas duplicadas
DELETE FROM dbo.SecurityModule
WHERE SecurityModuleId IN (SELECT Id FROM @DuplicateIds);

PRINT 'Listo. Mantenido SecurityModuleId = ' + CAST(@KeepId AS VARCHAR) + '. Eliminados ' + CAST(@DupCount AS VARCHAR) + ' duplicados.';
