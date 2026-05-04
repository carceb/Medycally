-- =============================================================================
-- Seguridad: Activación por email + Gestión de Roles
-- Ejecutar completo en SSMS sobre la BD de Medycally
-- =============================================================================

-- =============================================================================
-- PASO 1: Agregar columnas de activación a SecurityUser
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SecurityUser') AND name = 'ActivationToken')
    ALTER TABLE dbo.SecurityUser ADD ActivationToken VARCHAR(100) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SecurityUser') AND name = 'IsActivated')
    ALTER TABLE dbo.SecurityUser ADD IsActivated BIT NOT NULL DEFAULT 0;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SecurityUser') AND name = 'TokenExpiresAt')
    ALTER TABLE dbo.SecurityUser ADD TokenExpiresAt SMALLDATETIME NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SecurityUser') AND name = 'DoctorId')
    ALTER TABLE dbo.SecurityUser ADD DoctorId INT NULL;
GO

-- Todos los usuarios existentes (creados con contraseña directa) ya están activos
UPDATE dbo.SecurityUser SET IsActivated = 1 WHERE IsActivated = 0;
GO

-- =============================================================================
-- PASO 2: SecurityUser_GetAll (con DoctorId, DoctorName, IsActivated)
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.SecurityUser_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.SecurityUserId,
        u.UserName,
        u.UserEmail,
        u.UserIdNumber,
        u.SecurityRoleId,
        r.RoleName,
        r.RoleLevel,
        CASE WHEN u.IsActive = 1 THEN 1 ELSE 2 END AS StatusId,
        u.IsActivated,
        u.DoctorId,
        d.DoctorName
    FROM       dbo.SecurityUser u
    INNER JOIN dbo.SecurityRole r ON r.SecurityRoleId = u.SecurityRoleId
    LEFT  JOIN dbo.Doctor       d ON d.DoctorId       = u.DoctorId
    ORDER BY   u.UserName;
END
GO

-- =============================================================================
-- PASO 3: SecurityUser_AddOrEdit
-- INSERT: genera token de activación, no requiere contraseña
-- UPDATE: actualiza datos sin tocar IsActivated ni token
-- Retorna SecurityUserId + ActivationToken (NULL en edición)
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.SecurityUser_AddOrEdit
    @SecurityUserId INT,
    @UserName       VARCHAR(100),
    @UserEmail      VARCHAR(100),
    @UserIdNumber   INT,
    @SecurityRoleId INT,
    @StatusId       INT,
    @DoctorId       INT          = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IsActive BIT = CASE WHEN @StatusId = 1 THEN 1 ELSE 0 END;

    IF @SecurityUserId = 0
    BEGIN
        DECLARE @Token VARCHAR(100) = REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', '');

        INSERT INTO dbo.SecurityUser
            (SecurityRoleId, UserIdNumber, UserEmail, UserPasswordHash,
             UserName, IsActive, DoctorId, ActivationToken, IsActivated, TokenExpiresAt)
        VALUES
            (@SecurityRoleId, @UserIdNumber, @UserEmail, '',
             @UserName, @IsActive, @DoctorId, @Token, 0, DATEADD(DAY, 7, GETDATE()));

        SELECT SCOPE_IDENTITY() AS SecurityUserId, @Token AS ActivationToken;
    END
    ELSE
    BEGIN
        UPDATE dbo.SecurityUser
        SET    UserName       = @UserName,
               UserEmail      = @UserEmail,
               UserIdNumber   = @UserIdNumber,
               SecurityRoleId = @SecurityRoleId,
               IsActive       = @IsActive,
               DoctorId       = @DoctorId
        WHERE  SecurityUserId = @SecurityUserId;

        SELECT @SecurityUserId AS SecurityUserId, NULL AS ActivationToken;
    END
END
GO

-- =============================================================================
-- PASO 4: Security_UserLogin — requiere IsActivated = 1
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.Security_UserLogin
    @UserEmail        VARCHAR(100),
    @UserPasswordHash VARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.SecurityUser
    SET    LastLoginAt = GETDATE()
    WHERE  UserEmail        = @UserEmail
      AND  UserPasswordHash = @UserPasswordHash
      AND  IsActivated      = 1
      AND  IsActive         = 1;

    SELECT
        u.SecurityUserId,
        u.UserName,
        u.UserEmail,
        u.UserIdNumber,
        u.SecurityRoleId,
        r.RoleName,
        r.RoleLevel,
        u.DoctorId
    FROM       dbo.SecurityUser u
    INNER JOIN dbo.SecurityRole r ON r.SecurityRoleId = u.SecurityRoleId
    WHERE  u.UserEmail        = @UserEmail
      AND  u.UserPasswordHash = @UserPasswordHash
      AND  u.IsActivated      = 1
      AND  u.IsActive         = 1;
END
GO

-- =============================================================================
-- PASO 5: SecurityUser_GetByToken — valida token para página de activación
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.SecurityUser_GetByToken
    @Token VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT SecurityUserId, UserName, UserEmail
    FROM   dbo.SecurityUser
    WHERE  ActivationToken = @Token
      AND  TokenExpiresAt  >= GETDATE()
      AND  IsActivated      = 0;
END
GO

-- =============================================================================
-- PASO 6: SecurityUser_Activate — establece contraseña y activa la cuenta
-- Retorna 1 si éxito, 0 si token inválido/expirado
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.SecurityUser_Activate
    @Token        VARCHAR(100),
    @PasswordHash VARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.SecurityUser
    SET    UserPasswordHash = @PasswordHash,
           IsActivated      = 1,
           ActivationToken  = NULL,
           TokenExpiresAt   = NULL
    WHERE  ActivationToken = @Token
      AND  TokenExpiresAt  >= GETDATE()
      AND  IsActivated      = 0;

    SELECT @@ROWCOUNT AS Success;
END
GO

-- =============================================================================
-- PASO 7: SecurityUser_ResendToken — genera nuevo token de activación
-- Funciona para usuarios no activados o para resetear acceso
-- Retorna el nuevo token
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.SecurityUser_ResendToken
    @SecurityUserId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Token VARCHAR(100) = REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', '');

    UPDATE dbo.SecurityUser
    SET    ActivationToken  = @Token,
           TokenExpiresAt   = DATEADD(DAY, 7, GETDATE()),
           IsActivated      = 0,
           UserPasswordHash = ''
    WHERE  SecurityUserId = @SecurityUserId;

    SELECT @Token AS ActivationToken;
END
GO

-- =============================================================================
-- PASO 8: SecurityRole_AddOrEdit
-- No permite modificar el rol SuperAdmin (SecurityRoleId = 1)
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.SecurityRole_AddOrEdit
    @SecurityRoleId INT,
    @RoleName       VARCHAR(50),
    @RoleLevel      TINYINT
AS
BEGIN
    SET NOCOUNT ON;

    IF @SecurityRoleId = 0
    BEGIN
        INSERT INTO dbo.SecurityRole (RoleName, RoleLevel)
        VALUES (@RoleName, @RoleLevel);
        SELECT SCOPE_IDENTITY() AS SecurityRoleId;
    END
    ELSE
    BEGIN
        IF @SecurityRoleId = 1
        BEGIN
            RAISERROR('No se puede modificar el rol de Super Administrador.', 16, 1);
            RETURN;
        END

        UPDATE dbo.SecurityRole
        SET    RoleName  = @RoleName,
               RoleLevel = @RoleLevel
        WHERE  SecurityRoleId = @SecurityRoleId;

        SELECT @SecurityRoleId AS SecurityRoleId;
    END
END
GO

-- =============================================================================
-- PASO 9: SecurityRole_Delete
-- No permite eliminar roles con usuarios asignados ni el rol SuperAdmin
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.SecurityRole_Delete
    @SecurityRoleId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @SecurityRoleId = 1
    BEGIN
        RAISERROR('No se puede eliminar el rol de Super Administrador.', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.SecurityUser WHERE SecurityRoleId = @SecurityRoleId)
    BEGIN
        RAISERROR('No se puede eliminar el rol porque tiene usuarios asignados.', 16, 1);
        RETURN;
    END

    DELETE FROM dbo.SecurityRoleModule WHERE SecurityRoleId = @SecurityRoleId;
    DELETE FROM dbo.SecurityRole        WHERE SecurityRoleId = @SecurityRoleId;
END
GO

-- =============================================================================
-- PASO 10: SecurityRoleModule_GetByRole
-- Retorna todos los módulos activos (sin parent) con permisos del rol dado
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.SecurityRoleModule_GetByRole
    @SecurityRoleId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        sm.SecurityModuleId,
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
    WHERE  sm.IsActive               = 1
      AND  sm.ParentSecurityModuleId IS NULL
    ORDER BY sm.ModuleOrder;
END
GO

-- =============================================================================
-- PASO 11: SecurityRoleModule_Save
-- Upsert de un módulo para un rol
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.SecurityRoleModule_Save
    @SecurityRoleId   INT,
    @SecurityModuleId INT,
    @CanView          BIT,
    @CanCreate        BIT,
    @CanEdit          BIT,
    @CanDelete        BIT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1 FROM dbo.SecurityRoleModule
        WHERE SecurityRoleId = @SecurityRoleId AND SecurityModuleId = @SecurityModuleId
    )
        UPDATE dbo.SecurityRoleModule
        SET    CanView   = @CanView,
               CanCreate = @CanCreate,
               CanEdit   = @CanEdit,
               CanDelete = @CanDelete
        WHERE  SecurityRoleId   = @SecurityRoleId
          AND  SecurityModuleId = @SecurityModuleId;
    ELSE
        INSERT INTO dbo.SecurityRoleModule
            (SecurityRoleId, SecurityModuleId, CanView, CanCreate, CanEdit, CanDelete)
        VALUES
            (@SecurityRoleId, @SecurityModuleId, @CanView, @CanCreate, @CanEdit, @CanDelete);
END
GO
