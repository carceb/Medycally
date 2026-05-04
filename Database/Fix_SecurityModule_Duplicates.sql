-- =============================================================================
-- DIAGNÓSTICO: Ver todos los módulos actuales
-- =============================================================================
SELECT SecurityModuleId, ModuleName, ModuleUrl, ModuleOrder, IsActive
FROM   dbo.SecurityModule
ORDER BY ModuleName, SecurityModuleId;
GO

-- =============================================================================
-- LIMPIEZA: Eliminar duplicados
-- Regla: para cada nombre duplicado, conservar el de menor SecurityModuleId
-- (el primero que se insertó), eliminar los posteriores.
-- También migra los permisos al módulo correcto antes de borrar.
-- =============================================================================

-- Paso 1: eliminar permisos huérfanos que apuntan al duplicado
DELETE srm
FROM   dbo.SecurityRoleModule srm
WHERE  srm.SecurityModuleId IN (
    -- IDs duplicados a eliminar: para cada nombre, quitar todos menos el primero
    SELECT sm2.SecurityModuleId
    FROM   dbo.SecurityModule sm2
    WHERE  sm2.SecurityModuleId <> (
        SELECT MIN(sm3.SecurityModuleId)
        FROM   dbo.SecurityModule sm3
        WHERE  sm3.ModuleName = sm2.ModuleName
    )
);
GO

-- Paso 2: eliminar los módulos duplicados (conserva el de menor Id por nombre)
DELETE FROM dbo.SecurityModule
WHERE SecurityModuleId <> (
    SELECT MIN(sm2.SecurityModuleId)
    FROM   dbo.SecurityModule sm2
    WHERE  sm2.ModuleName = dbo.SecurityModule.ModuleName
);
GO

-- Verificar resultado
SELECT SecurityModuleId, ModuleName, ModuleUrl, ModuleOrder, IsActive
FROM   dbo.SecurityModule
ORDER BY ModuleOrder, SecurityModuleId;
GO
