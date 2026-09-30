-- =============================================================================
-- Borra filas SecurityRoleModule asociadas a módulos PADRE (agrupadores).
-- Convención del proyecto: los módulos padre (ParentSecurityModuleId IS NULL
-- y ModuleUrl IS NULL) no tienen permisos directos — el SP los incluye en el
-- resultado por su EXISTS sobre hijos accesibles.
--
-- Síntoma que arregla: el sidebar muestra el menú padre DUPLICADO porque el
-- SP Security_GetUserModulePermissions hace UNION ALL entre AccessibleLeaves
-- (donde el padre cae si tiene SRM con CanView=1) y AccessibleParents (donde
-- cae por tener hijos accesibles).
--
-- Idempotente: re-ejecutable.
-- =============================================================================

SET NOCOUNT ON;

-- 1) Diagnóstico: ver qué padres tienen SRM huérfana
PRINT '== Módulos padre (sin URL, sin padre) que tienen SecurityRoleModule ==';
SELECT sm.SecurityModuleId,
       sm.ModuleName,
       COUNT(srm.SecurityRoleModuleId) AS RoleRows
FROM       dbo.SecurityModule sm
LEFT JOIN  dbo.SecurityRoleModule srm ON srm.SecurityModuleId = sm.SecurityModuleId
WHERE  sm.ParentSecurityModuleId IS NULL
  AND  sm.ModuleUrl IS NULL
GROUP BY sm.SecurityModuleId, sm.ModuleName
HAVING COUNT(srm.SecurityRoleModuleId) > 0;

-- 2) Cleanup: borrar SecurityRoleModule de todos los padres-agrupadores
DELETE srm
FROM       dbo.SecurityRoleModule srm
INNER JOIN dbo.SecurityModule     sm ON sm.SecurityModuleId = srm.SecurityModuleId
WHERE  sm.ParentSecurityModuleId IS NULL
  AND  sm.ModuleUrl IS NULL;

PRINT 'Listo. Filas SRM de padres-agrupadores eliminadas. Los hijos conservan sus permisos.';
