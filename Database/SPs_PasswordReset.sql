-- =============================================================================
-- Restablecimiento de contraseña por correo
-- Ejecutar en SSMS sobre la BD de Medycally
-- =============================================================================

-- =============================================================================
-- PASO 1: Agregar columnas de reset a SecurityUser
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SecurityUser') AND name = 'ResetToken')
    ALTER TABLE dbo.SecurityUser ADD ResetToken VARCHAR(100) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SecurityUser') AND name = 'ResetTokenExpiresAt')
    ALTER TABLE dbo.SecurityUser ADD ResetTokenExpiresAt SMALLDATETIME NULL;
GO

-- =============================================================================
-- PASO 2: SecurityUser_ForgotPassword
-- Genera un token de reset para el email dado (solo si el usuario está activo).
-- Retorna el token y el nombre del usuario para armar el email.
-- Si el email no existe, no retorna nada (sin mensaje de error por seguridad).
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.SecurityUser_ForgotPassword
    @UserEmail VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Token VARCHAR(100) = REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', '');

    UPDATE dbo.SecurityUser
    SET    ResetToken          = @Token,
           ResetTokenExpiresAt = DATEADD(HOUR, 2, GETDATE())
    WHERE  UserEmail   = @UserEmail
      AND  IsActivated = 1
      AND  IsActive    = 1;

    IF @@ROWCOUNT > 0
        SELECT u.SecurityUserId, u.UserName, u.UserEmail, @Token AS ResetToken
        FROM   dbo.SecurityUser u
        WHERE  u.UserEmail = @UserEmail;
END
GO

-- =============================================================================
-- PASO 3: SecurityUser_GetByResetToken
-- Valida que el token de reset exista y no haya expirado.
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.SecurityUser_GetByResetToken
    @Token VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT SecurityUserId, UserName, UserEmail
    FROM   dbo.SecurityUser
    WHERE  ResetToken          = @Token
      AND  ResetTokenExpiresAt >= GETDATE();
END
GO

-- =============================================================================
-- PASO 4: SecurityUser_ResetPassword
-- Establece la nueva contraseña y limpia el token.
-- Retorna 1 si éxito, 0 si token inválido o expirado.
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.SecurityUser_ResetPassword
    @Token        VARCHAR(100),
    @PasswordHash VARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.SecurityUser
    SET    UserPasswordHash    = @PasswordHash,
           ResetToken          = NULL,
           ResetTokenExpiresAt = NULL
    WHERE  ResetToken          = @Token
      AND  ResetTokenExpiresAt >= GETDATE();

    SELECT @@ROWCOUNT AS Success;
END
GO
