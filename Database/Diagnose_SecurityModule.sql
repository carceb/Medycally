-- =============================================================================
-- Diagnose_SecurityModule.sql
-- Diagnóstico rápido del estado actual de SecurityModule y SecurityRoleModule.
-- Solo SELECTs — no modifica nada. Ejecutar en SSMS, copiar resultados.
-- =============================================================================

-- ── 1) Todas las filas de SecurityModule (verifica que solo haya UNA por nombre)
SELECT  SecurityModuleId,
        ParentSecurityModuleId,
        ModuleName,
        ModuleUrl,
        ModuleIcon,
        ModuleOrder,
        IsActive
FROM    dbo.SecurityModule
ORDER BY ModuleName, SecurityModuleId;

-- ── 2) Conteo por ModuleName (cualquier fila con Cnt > 1 es duplicado)
SELECT  ModuleName,
        COUNT(*) AS Cnt
FROM    dbo.SecurityModule
GROUP BY ModuleName
HAVING  COUNT(*) > 1
ORDER BY ModuleName;

-- ── 3) Lo que devuelve el SP para el SuperAdmin (RoleId=1)
--      Reemplaza @TestUserId si quieres probar con otro usuario.
DECLARE @SuperAdminUserId INT = (
    SELECT TOP 1 u.SecurityUserId
    FROM   dbo.SecurityUser u
    INNER JOIN dbo.SecurityRole r ON r.SecurityRoleId = u.SecurityRoleId
    WHERE  r.IsSuperAdmin = 1
);

SELECT 'Llamando SP con SecurityUserId = ' + CAST(@SuperAdminUserId AS VARCHAR) AS Info;

EXEC dbo.Security_GetUserModulePermissions @SecurityUserId = @SuperAdminUserId;

-- ── 4) SecurityRoleModule del SuperAdmin — ver si hay filas duplicadas por (Role, Module)
SELECT  srm.SecurityRoleId,
        srm.SecurityModuleId,
        sm.ModuleName,
        srm.CanView,
        srm.CanCreate,
        srm.CanEdit,
        srm.CanDelete
FROM       dbo.SecurityRoleModule srm
INNER JOIN dbo.SecurityModule    sm ON sm.SecurityModuleId = srm.SecurityModuleId
INNER JOIN dbo.SecurityRole      r  ON r.SecurityRoleId    = srm.SecurityRoleId
WHERE  r.IsSuperAdmin = 1
ORDER BY sm.ModuleName, srm.SecurityModuleId;
