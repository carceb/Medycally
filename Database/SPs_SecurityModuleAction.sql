-- =============================================================================
-- Permisos a nivel de "acción" por módulo (botones individuales dentro de
-- una vista). Complementa el sistema existente CanView/CanCreate/CanEdit/CanDelete
-- con permisos finos por acción nombrada.
--
-- Catálogo:  SecurityModuleAction       (ActionKey por módulo)
-- Junction:  SecurityRoleModuleAction   (rol → acción permitida)
--
-- Idempotente: re-ejecutable.
-- =============================================================================

SET NOCOUNT ON;

-- =============================================================================
-- PASO 1: SecurityModuleAction (catálogo de acciones)
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SecurityModuleAction')
BEGIN
    CREATE TABLE dbo.SecurityModuleAction (
        SecurityModuleActionId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SecurityModuleId       INT          NOT NULL,
        ActionKey              VARCHAR(50)  NOT NULL,   -- 'doctors', 'schedule', 'pricing', ...
        ActionName             NVARCHAR(80) NOT NULL,
        ActionOrder            TINYINT      NOT NULL DEFAULT 99,
        CONSTRAINT FK_SecurityModuleAction_Module
            FOREIGN KEY (SecurityModuleId)
            REFERENCES dbo.SecurityModule(SecurityModuleId)
            ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX UX_SecurityModuleAction_Module_Key
        ON dbo.SecurityModuleAction (SecurityModuleId, ActionKey);
END

-- =============================================================================
-- PASO 2: SecurityRoleModuleAction (junction rol → acción)
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SecurityRoleModuleAction')
BEGIN
    CREATE TABLE dbo.SecurityRoleModuleAction (
        SecurityRoleId         INT NOT NULL,
        SecurityModuleActionId INT NOT NULL,
        IsAllowed              BIT NOT NULL DEFAULT 1,
        CONSTRAINT PK_SecurityRoleModuleAction PRIMARY KEY (SecurityRoleId, SecurityModuleActionId),
        CONSTRAINT FK_SecurityRoleModuleAction_Role
            FOREIGN KEY (SecurityRoleId)
            REFERENCES dbo.SecurityRole(SecurityRoleId)
            ON DELETE CASCADE,
        CONSTRAINT FK_SecurityRoleModuleAction_Action
            FOREIGN KEY (SecurityModuleActionId)
            REFERENCES dbo.SecurityModuleAction(SecurityModuleActionId)
            ON DELETE CASCADE
    );
END

-- =============================================================================
-- PASO 3: Seed inicial — Clínicas y Médicos
-- =============================================================================
DECLARE @ClinicId INT = (SELECT SecurityModuleId FROM dbo.SecurityModule WHERE ModuleUrl = '/Admin/Clinic');
DECLARE @DoctorId INT = (SELECT SecurityModuleId FROM dbo.SecurityModule WHERE ModuleUrl = '/Admin/Doctor');
DECLARE @UserId   INT = (SELECT SecurityModuleId FROM dbo.SecurityModule WHERE ModuleUrl = '/Admin/User');

IF @ClinicId IS NULL OR @DoctorId IS NULL OR @UserId IS NULL
BEGIN
    PRINT 'Aviso: faltan los módulos /Admin/Clinic, /Admin/Doctor o /Admin/User en SecurityModule. Ejecuta primero SPs_SecurityModule_AdminMenu.sql.';
END

-- Clínicas
IF @ClinicId IS NOT NULL
BEGIN
    INSERT INTO dbo.SecurityModuleAction (SecurityModuleId, ActionKey, ActionName, ActionOrder)
    SELECT @ClinicId, x.ActionKey, x.ActionName, x.ActionOrder
    FROM (VALUES
        ('doctors',  N'Asignar Médicos',         1),
        ('schedule', N'Editar Horarios',         2),
        ('pricing',  N'Estructura de Precios',   3)
    ) x(ActionKey, ActionName, ActionOrder)
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.SecurityModuleAction
        WHERE SecurityModuleId = @ClinicId AND ActionKey = x.ActionKey
    );
END

-- Médicos
IF @DoctorId IS NOT NULL
BEGIN
    INSERT INTO dbo.SecurityModuleAction (SecurityModuleId, ActionKey, ActionName, ActionOrder)
    SELECT @DoctorId, x.ActionKey, x.ActionName, x.ActionOrder
    FROM (VALUES
        ('specialties', N'Asignar Especialidades', 1),
        ('pricing',     N'Asignar Precios',        2)
    ) x(ActionKey, ActionName, ActionOrder)
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.SecurityModuleAction
        WHERE SecurityModuleId = @DoctorId AND ActionKey = x.ActionKey
    );
END

-- Usuarios
IF @UserId IS NOT NULL
BEGIN
    INSERT INTO dbo.SecurityModuleAction (SecurityModuleId, ActionKey, ActionName, ActionOrder)
    SELECT @UserId, x.ActionKey, x.ActionName, x.ActionOrder
    FROM (VALUES
        ('clinics', N'Asignar Clínicas',      1),
        ('resend',  N'Reenviar Activación',   2)
    ) x(ActionKey, ActionName, ActionOrder)
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.SecurityModuleAction
        WHERE SecurityModuleId = @UserId AND ActionKey = x.ActionKey
    );
END

-- =============================================================================
-- PASO 4: Grant SuperAdmin todas las acciones
-- =============================================================================
DECLARE @SuperAdminRoleId INT = (SELECT TOP 1 SecurityRoleId FROM dbo.SecurityRole WHERE IsSuperAdmin = 1);

IF @SuperAdminRoleId IS NOT NULL
BEGIN
    INSERT INTO dbo.SecurityRoleModuleAction (SecurityRoleId, SecurityModuleActionId, IsAllowed)
    SELECT @SuperAdminRoleId, a.SecurityModuleActionId, 1
    FROM   dbo.SecurityModuleAction a
    WHERE  NOT EXISTS (
        SELECT 1 FROM dbo.SecurityRoleModuleAction srma
        WHERE  srma.SecurityRoleId         = @SuperAdminRoleId
          AND  srma.SecurityModuleActionId = a.SecurityModuleActionId
    );
END
GO

-- =============================================================================
-- PASO 5: SP — SecurityModuleAction_GetByRole
-- Devuelve todas las acciones del catálogo + IsAllowed para el rol indicado.
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.SecurityModuleAction_GetByRole
    @SecurityRoleId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT a.SecurityModuleActionId,
           a.SecurityModuleId,
           a.ActionKey,
           a.ActionName,
           a.ActionOrder,
           CAST(CASE WHEN srma.SecurityRoleId IS NULL THEN 0 ELSE 1 END AS BIT) AS IsAllowed
    FROM       dbo.SecurityModuleAction a
    LEFT  JOIN dbo.SecurityRoleModuleAction srma
            ON  srma.SecurityModuleActionId = a.SecurityModuleActionId
            AND srma.SecurityRoleId         = @SecurityRoleId
            AND srma.IsAllowed              = 1
    ORDER BY a.SecurityModuleId, a.ActionOrder;
END
GO

-- =============================================================================
-- PASO 6: SP — SecurityRoleModuleAction_SaveBatch
-- Sustituye las acciones permitidas del rol por la lista pasada.
-- @AllowedActionIds: NVARCHAR con CSV de SecurityModuleActionId (ej. '3,7,12').
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.SecurityRoleModuleAction_SaveBatch
    @SecurityRoleId    INT,
    @AllowedActionIds  NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Ids TABLE (Id INT PRIMARY KEY);

    IF @AllowedActionIds IS NOT NULL AND LEN(@AllowedActionIds) > 0
    BEGIN
        INSERT INTO @Ids (Id)
        SELECT DISTINCT TRY_CAST(value AS INT)
        FROM   STRING_SPLIT(@AllowedActionIds, ',')
        WHERE  TRY_CAST(value AS INT) IS NOT NULL;
    END

    -- Borrar las que ya no están en la lista
    DELETE FROM dbo.SecurityRoleModuleAction
    WHERE  SecurityRoleId         = @SecurityRoleId
      AND  SecurityModuleActionId NOT IN (SELECT Id FROM @Ids);

    -- Insertar las nuevas
    INSERT INTO dbo.SecurityRoleModuleAction (SecurityRoleId, SecurityModuleActionId, IsAllowed)
    SELECT @SecurityRoleId, i.Id, 1
    FROM   @Ids i
    WHERE  NOT EXISTS (
        SELECT 1 FROM dbo.SecurityRoleModuleAction srma
        WHERE  srma.SecurityRoleId         = @SecurityRoleId
          AND  srma.SecurityModuleActionId = i.Id
    );
END
GO

-- =============================================================================
-- PASO 7: SP — SecurityModuleAction_GetByUserAndModule
-- Devuelve las ActionKeys permitidas para el usuario en el módulo indicado.
-- Usada por IPermissionService.GetModuleActions.
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.SecurityModuleAction_GetByUserAndModule
    @SecurityUserId INT,
    @ModuleUrl      VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @SecurityRoleId INT;
    SELECT @SecurityRoleId = SecurityRoleId
    FROM   dbo.SecurityUser
    WHERE  SecurityUserId = @SecurityUserId;

    DECLARE @SecurityModuleId INT;
    SELECT @SecurityModuleId = SecurityModuleId
    FROM   dbo.SecurityModule
    WHERE  ModuleUrl = @ModuleUrl;

    IF @SecurityRoleId IS NULL OR @SecurityModuleId IS NULL
    BEGIN
        SELECT TOP 0 CAST('' AS VARCHAR(50)) AS ActionKey;
        RETURN;
    END

    SELECT a.ActionKey
    FROM       dbo.SecurityModuleAction      a
    INNER JOIN dbo.SecurityRoleModuleAction  srma
            ON  srma.SecurityModuleActionId = a.SecurityModuleActionId
            AND srma.SecurityRoleId         = @SecurityRoleId
            AND srma.IsAllowed              = 1
    WHERE  a.SecurityModuleId = @SecurityModuleId
    ORDER BY a.ActionOrder;
END
GO

PRINT 'OK — SecurityModuleAction + SecurityRoleModuleAction creados, acciones seed insertadas y SPs aplicados.';
