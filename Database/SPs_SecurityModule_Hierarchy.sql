-- =============================================================================
-- Convierte el sidebar a data-driven jerárquico:
--   * 'Atención Médica' pasa a ser un módulo padre (sin URL, sólo agrupador)
--   * Se crean tres módulos hijos: Cola de Pacientes, Pacientes Atendidos,
--     Calendario de Citas — cada uno con su propio permiso por rol
--   * Las SPs Security_GetUserModulePermissions y SecurityRoleModule_GetByRole
--     pasan a devolver ParentSecurityModuleId para que la UI arme el árbol
--
-- Idempotente: ejecutar todo el script en SSMS.
-- =============================================================================

-- =============================================================================
-- PASO 1: Convertir 'Atención Médica' en padre puro (ModuleUrl = NULL)
-- =============================================================================
DECLARE @MedicalParentId INT = (
    SELECT SecurityModuleId
    FROM   dbo.SecurityModule
    WHERE  ModuleName = N'Atención Médica'
);

IF @MedicalParentId IS NULL
BEGIN
    INSERT INTO dbo.SecurityModule (ModuleName, ModuleUrl, ModuleIcon, ModuleOrder, IsActive, ParentSecurityModuleId)
    VALUES (N'Atención Médica', NULL, 'fa-stethoscope', 4, 1, NULL);
    SET @MedicalParentId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE dbo.SecurityModule
    SET    ModuleUrl  = NULL,
           ModuleIcon = ISNULL(ModuleIcon, 'fa-stethoscope'),
           IsActive   = 1,
           ParentSecurityModuleId = NULL
    WHERE  SecurityModuleId = @MedicalParentId;
END
GO

-- =============================================================================
-- PASO 2: Insertar los tres hijos (idempotente — sólo si no existen)
-- =============================================================================
DECLARE @MedicalParentId INT = (
    SELECT SecurityModuleId
    FROM   dbo.SecurityModule
    WHERE  ModuleName = N'Atención Médica'
);

IF NOT EXISTS (SELECT 1 FROM dbo.SecurityModule WHERE ModuleName = N'Cola de Pacientes')
    INSERT INTO dbo.SecurityModule (ModuleName, ModuleUrl, ModuleIcon, ModuleOrder, IsActive, ParentSecurityModuleId)
    VALUES (N'Cola de Pacientes', '/Medical/Index', 'fa-list-ul', 1, 1, @MedicalParentId);

IF NOT EXISTS (SELECT 1 FROM dbo.SecurityModule WHERE ModuleName = N'Pacientes Atendidos')
    INSERT INTO dbo.SecurityModule (ModuleName, ModuleUrl, ModuleIcon, ModuleOrder, IsActive, ParentSecurityModuleId)
    VALUES (N'Pacientes Atendidos', '/Medical/AttendedPatients', 'fa-user-check', 2, 1, @MedicalParentId);

IF NOT EXISTS (SELECT 1 FROM dbo.SecurityModule WHERE ModuleName = N'Calendario de Citas')
    INSERT INTO dbo.SecurityModule (ModuleName, ModuleUrl, ModuleIcon, ModuleOrder, IsActive, ParentSecurityModuleId)
    VALUES (N'Calendario de Citas', '/Medical/Calendar', 'fa-calendar-alt', 3, 1, @MedicalParentId);
GO

-- =============================================================================
-- PASO 3: Migrar permisos existentes — todo rol que tenía CanView sobre el
-- padre 'Atención Médica' recibe los mismos permisos sobre los tres hijos.
-- Sólo inserta filas que no existan (idempotente).
-- =============================================================================
DECLARE @MedicalParentId INT = (
    SELECT SecurityModuleId
    FROM   dbo.SecurityModule
    WHERE  ModuleName = N'Atención Médica'
);

INSERT INTO dbo.SecurityRoleModule
    (SecurityRoleId, SecurityModuleId, CanView, CanCreate, CanEdit, CanDelete)
SELECT srm.SecurityRoleId,
       child.SecurityModuleId,
       srm.CanView,
       srm.CanCreate,
       srm.CanEdit,
       srm.CanDelete
FROM   dbo.SecurityRoleModule srm
INNER JOIN dbo.SecurityModule  child
        ON child.ParentSecurityModuleId = @MedicalParentId
WHERE  srm.SecurityModuleId = @MedicalParentId
  AND  NOT EXISTS (
        SELECT 1 FROM dbo.SecurityRoleModule x
        WHERE  x.SecurityRoleId   = srm.SecurityRoleId
          AND  x.SecurityModuleId = child.SecurityModuleId
  );
GO

-- =============================================================================
-- PASO 4: Security_GetUserModulePermissions
-- Devuelve módulos que el usuario puede VER (CanView = 1), incluyendo
-- ParentSecurityModuleId para que el sidebar arme el árbol. Un padre se
-- incluye si tiene al menos un hijo accesible (los padres no aparecen
-- en SecurityRoleModule porque son sólo agrupadores).
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

    -- Hijos accesibles + módulos top-level con permiso explícito
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
    -- Padres a renderizar: aquellos cuyos hijos están en AccessibleLeaves
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

-- =============================================================================
-- PASO 5: SecurityRoleModule_GetByRole
-- Devuelve TODOS los módulos activos (padres + hijos) con ParentSecurityModuleId
-- para que la matriz de permisos del admin pueda mostrarlos jerárquicamente.
-- Los padres aparecen pero no se asignan permisos (son sólo agrupadores) —
-- la UI los renderiza como cabecera de sección.
-- =============================================================================
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
-- PASO 6: Security_GetAllActiveModuleUrls
-- Devuelve la lista de ModuleUrls activas (no NULL) usada por el filtro de
-- autorización para detectar si una ruta está "gated" por algún módulo.
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.Security_GetAllActiveModuleUrls
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ModuleUrl
    FROM   dbo.SecurityModule
    WHERE  IsActive  = 1
      AND  ModuleUrl IS NOT NULL;
END
GO

-- =============================================================================
-- Verificación
-- =============================================================================
SELECT SecurityModuleId,
       ParentSecurityModuleId,
       ModuleName,
       ModuleUrl,
       ModuleIcon,
       ModuleOrder,
       IsActive
FROM   dbo.SecurityModule
ORDER BY COALESCE(ParentSecurityModuleId, SecurityModuleId),
         ParentSecurityModuleId,
         ModuleOrder;
GO
