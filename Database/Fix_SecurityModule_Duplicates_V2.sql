-- =============================================================================
-- Fix_SecurityModule_Duplicates_V2.sql
--
-- Consolida filas duplicadas en SecurityModule (mismo ModuleName, distintos
-- SecurityModuleId). Para cada nombre, conserva el SecurityModuleId más bajo
-- (canónico) y:
--   * Re-apunta cualquier hijo cuyo ParentSecurityModuleId sea un duplicado.
--   * Mergea permisos en SecurityRoleModule (OR bit a bit) hacia el canónico.
--   * Elimina las filas duplicadas en SecurityModule y SecurityRoleModule.
--   * Re-establece "Atención Médica" como padre puro (ModuleUrl=NULL) y
--     re-vincula los tres hijos al parent canónico.
--
-- Idempotente: si no hay duplicados, sólo re-afirma el estado canónico.
-- Ejecutar en SSMS sobre la BD de Medycally.
-- =============================================================================

SET NOCOUNT ON;
GO

-- =============================================================================
-- PASO 1: Diagnóstico — estado actual de SecurityModule
-- =============================================================================
PRINT '=== ANTES: SecurityModule actual ===';

SELECT  SecurityModuleId,
        ParentSecurityModuleId,
        ModuleName,
        ModuleUrl,
        ModuleIcon,
        ModuleOrder,
        IsActive
FROM    dbo.SecurityModule
ORDER BY ModuleName, SecurityModuleId;
GO

-- =============================================================================
-- PASO 2: Construir mapeo DupeId -> CanonicalId
-- =============================================================================
IF OBJECT_ID('tempdb..#NameMap') IS NOT NULL DROP TABLE #NameMap;

;WITH Counts AS (
    SELECT  SecurityModuleId,
            ModuleName,
            MIN(SecurityModuleId) OVER (PARTITION BY ModuleName) AS CanonicalId,
            COUNT(*)              OVER (PARTITION BY ModuleName) AS Cnt
    FROM    dbo.SecurityModule
)
SELECT  SecurityModuleId AS DupeId,
        CanonicalId,
        ModuleName
INTO    #NameMap
FROM    Counts
WHERE   Cnt > 1
  AND   SecurityModuleId <> CanonicalId;

PRINT '=== Duplicados detectados (DupeId -> CanonicalId) ===';
SELECT * FROM #NameMap ORDER BY ModuleName, DupeId;
GO

-- =============================================================================
-- PASO 3: Re-apuntar hijos cuyos parents sean duplicados
-- =============================================================================
UPDATE  c
SET     c.ParentSecurityModuleId = m.CanonicalId
FROM    dbo.SecurityModule c
INNER JOIN #NameMap m ON m.DupeId = c.ParentSecurityModuleId;
GO

-- =============================================================================
-- PASO 4: Consolidar SecurityRoleModule
--   a) MERGE: por cada (RoleId, CanonicalId), agrega los permisos de todos
--      los duplicados (OR bit a bit). Inserta la fila canónica si no existe.
--   b) Borra todas las filas que apuntan a IDs duplicados.
-- =============================================================================
MERGE dbo.SecurityRoleModule AS target
USING (
    SELECT  srm.SecurityRoleId,
            m.CanonicalId AS SecurityModuleId,
            CAST(MAX(CAST(srm.CanView   AS INT)) AS BIT) AS CanView,
            CAST(MAX(CAST(srm.CanCreate AS INT)) AS BIT) AS CanCreate,
            CAST(MAX(CAST(srm.CanEdit   AS INT)) AS BIT) AS CanEdit,
            CAST(MAX(CAST(srm.CanDelete AS INT)) AS BIT) AS CanDelete
    FROM    dbo.SecurityRoleModule srm
    INNER JOIN #NameMap m ON m.DupeId = srm.SecurityModuleId
    GROUP BY srm.SecurityRoleId, m.CanonicalId
) AS source
   ON target.SecurityRoleId   = source.SecurityRoleId
  AND target.SecurityModuleId = source.SecurityModuleId
WHEN MATCHED THEN
    UPDATE SET
        target.CanView   = target.CanView   | source.CanView,
        target.CanCreate = target.CanCreate | source.CanCreate,
        target.CanEdit   = target.CanEdit   | source.CanEdit,
        target.CanDelete = target.CanDelete | source.CanDelete
WHEN NOT MATCHED BY TARGET THEN
    INSERT (SecurityRoleId, SecurityModuleId, CanView, CanCreate, CanEdit, CanDelete)
    VALUES (source.SecurityRoleId, source.SecurityModuleId, source.CanView, source.CanCreate, source.CanEdit, source.CanDelete);

DELETE  srm
FROM    dbo.SecurityRoleModule srm
INNER JOIN #NameMap m ON m.DupeId = srm.SecurityModuleId;
GO

-- =============================================================================
-- PASO 5: Borrar filas duplicadas en SecurityModule
-- =============================================================================
DELETE  sm
FROM    dbo.SecurityModule sm
INNER JOIN #NameMap m ON m.DupeId = sm.SecurityModuleId;
GO

-- =============================================================================
-- PASO 6: Re-afirmar el estado canónico
--   * "Atención Médica" → padre puro (ModuleUrl=NULL)
--   * Los 3 hijos apuntando al parent canónico
-- =============================================================================
UPDATE dbo.SecurityModule
SET    ModuleUrl              = NULL,
       ModuleIcon             = ISNULL(ModuleIcon, 'fa-stethoscope'),
       IsActive               = 1,
       ParentSecurityModuleId = NULL
WHERE  ModuleName = N'Atención Médica';

DECLARE @MedicalParentId INT = (
    SELECT SecurityModuleId
    FROM   dbo.SecurityModule
    WHERE  ModuleName = N'Atención Médica'
);

UPDATE dbo.SecurityModule
SET    ParentSecurityModuleId = @MedicalParentId
WHERE  ModuleName IN (N'Cola de Pacientes', N'Pacientes Atendidos', N'Calendario de Citas');
GO

-- =============================================================================
-- PASO 7: Limpieza
-- =============================================================================
DROP TABLE IF EXISTS #NameMap;
GO

-- =============================================================================
-- PASO 8: Re-aplicar SPs canónicos (idempotente)
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.Security_GetUserModulePermissions
    @SecurityUserId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @SecurityRoleId INT;
    SELECT @SecurityRoleId = SecurityRoleId
    FROM   dbo.SecurityUser
    WHERE  SecurityUserId = @SecurityUserId;

    ;WITH AccessibleLeaves AS (
        SELECT sm.SecurityModuleId,
               sm.ParentSecurityModuleId,
               sm.ModuleName,
               sm.ModuleUrl,
               sm.ModuleIcon,
               sm.ModuleOrder,
               srm.CanCreate,
               srm.CanEdit,
               srm.CanDelete
        FROM       dbo.SecurityModule    sm
        INNER JOIN dbo.SecurityRoleModule srm
                ON  srm.SecurityModuleId = sm.SecurityModuleId
                AND srm.SecurityRoleId   = @SecurityRoleId
                AND srm.CanView          = 1
        WHERE  sm.IsActive = 1
    ),
    AccessibleParents AS (
        SELECT sm.SecurityModuleId,
               sm.ParentSecurityModuleId,
               sm.ModuleName,
               sm.ModuleUrl,
               sm.ModuleIcon,
               sm.ModuleOrder,
               CAST(0 AS BIT) AS CanCreate,
               CAST(0 AS BIT) AS CanEdit,
               CAST(0 AS BIT) AS CanDelete
        FROM   dbo.SecurityModule sm
        WHERE  sm.IsActive               = 1
          AND  sm.ParentSecurityModuleId IS NULL
          AND  EXISTS (
                SELECT 1 FROM AccessibleLeaves al
                WHERE  al.ParentSecurityModuleId = sm.SecurityModuleId
          )
    )
    SELECT SecurityModuleId,
           ParentSecurityModuleId,
           ModuleName,
           ModuleUrl,
           ModuleIcon,
           CanCreate,
           CanEdit,
           CanDelete
    FROM (
        SELECT SecurityModuleId, ParentSecurityModuleId, ModuleName, ModuleUrl, ModuleIcon,
               CanCreate, CanEdit, CanDelete, ModuleOrder
        FROM   AccessibleLeaves
        UNION ALL
        SELECT SecurityModuleId, ParentSecurityModuleId, ModuleName, ModuleUrl, ModuleIcon,
               CanCreate, CanEdit, CanDelete, ModuleOrder
        FROM   AccessibleParents
    ) AS combined
    ORDER BY COALESCE(ParentSecurityModuleId, SecurityModuleId),
             ParentSecurityModuleId,
             ModuleOrder;
END
GO

CREATE OR ALTER PROCEDURE dbo.SecurityRoleModule_GetByRole
    @SecurityRoleId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        sm.SecurityModuleId,
        sm.ParentSecurityModuleId,
        sm.ModuleName,
        sm.ModuleUrl,
        sm.ModuleIcon,
        sm.ModuleOrder,
        ISNULL(srm.CanView,   0) AS CanView,
        ISNULL(srm.CanCreate, 0) AS CanCreate,
        ISNULL(srm.CanEdit,   0) AS CanEdit,
        ISNULL(srm.CanDelete, 0) AS CanDelete
    FROM       dbo.SecurityModule    sm
    LEFT JOIN  dbo.SecurityRoleModule srm
           ON  srm.SecurityModuleId = sm.SecurityModuleId
           AND srm.SecurityRoleId   = @SecurityRoleId
    WHERE  sm.IsActive = 1
    ORDER BY COALESCE(sm.ParentSecurityModuleId, sm.SecurityModuleId),
             sm.ParentSecurityModuleId,
             sm.ModuleOrder;
END
GO

-- =============================================================================
-- PASO 9: Verificación final
-- =============================================================================
PRINT '=== DESPUÉS: SecurityModule final ===';

SELECT  SecurityModuleId,
        ParentSecurityModuleId,
        ModuleName,
        ModuleUrl,
        ModuleIcon,
        ModuleOrder,
        IsActive
FROM    dbo.SecurityModule
ORDER BY COALESCE(ParentSecurityModuleId, SecurityModuleId),
         ParentSecurityModuleId,
         ModuleOrder;
GO
