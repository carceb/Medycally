-- =============================================================================
-- Diagnóstico — verifica que un rol tenga las acciones esperadas asignadas.
-- Reemplaza @RoleName con el nombre del rol que estás depurando.
-- =============================================================================

DECLARE @RoleName NVARCHAR(80) = N'<NombreRol>';   -- ej: 'Administrador de Clínica'

-- 1) Acciones del catálogo para /Admin/Clinic
PRINT '== Catálogo de acciones para /Admin/Clinic ==';
SELECT a.SecurityModuleActionId, a.ActionKey, a.ActionName, a.ActionOrder
FROM       dbo.SecurityModuleAction a
INNER JOIN dbo.SecurityModule       m ON m.SecurityModuleId = a.SecurityModuleId
WHERE  m.ModuleUrl = '/Admin/Clinic'
ORDER BY a.ActionOrder;

-- 2) Acciones que el rol tiene asignadas
PRINT '== Acciones permitidas al rol ==';
SELECT r.RoleName,
       a.ActionKey,
       a.ActionName,
       srma.IsAllowed
FROM       dbo.SecurityRole              r
INNER JOIN dbo.SecurityRoleModuleAction  srma ON srma.SecurityRoleId         = r.SecurityRoleId
INNER JOIN dbo.SecurityModuleAction      a    ON a.SecurityModuleActionId    = srma.SecurityModuleActionId
INNER JOIN dbo.SecurityModule            m    ON m.SecurityModuleId          = a.SecurityModuleId
WHERE  r.RoleName  = @RoleName
  AND  m.ModuleUrl = '/Admin/Clinic'
ORDER BY a.ActionOrder;

-- 3) Lo que devuelve el SP usado por la app (cambia el userId)
DECLARE @SampleUserId INT = (
    SELECT TOP 1 u.SecurityUserId
    FROM   dbo.SecurityUser u
    INNER JOIN dbo.SecurityRole r ON r.SecurityRoleId = u.SecurityRoleId
    WHERE  r.RoleName = @RoleName
);

IF @SampleUserId IS NOT NULL
BEGIN
    PRINT '== Lo que devuelve SecurityModuleAction_GetByUserAndModule para un usuario de ese rol ==';
    EXEC dbo.SecurityModuleAction_GetByUserAndModule @SecurityUserId = @SampleUserId, @ModuleUrl = '/Admin/Clinic';
END
ELSE
BEGIN
    PRINT 'No hay usuarios asignados a este rol — no puedo probar el SP en vivo.';
END
