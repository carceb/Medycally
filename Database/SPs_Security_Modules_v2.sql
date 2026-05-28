-- =============================================================================
-- Seguridad v2: reemplaza RoleLevel por IsSuperAdmin + CRUD de Módulos
-- Idempotente. Ejecutar completo en SSMS sobre la BD de Medycally.
-- =============================================================================

-- =============================================================================
-- PASO 1: Agregar SecurityRole.IsSuperAdmin y migrar el rol Id=1
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SecurityRole') AND name = 'IsSuperAdmin')
    ALTER TABLE dbo.SecurityRole ADD IsSuperAdmin BIT NOT NULL DEFAULT 0;
GO

-- El rol con Id = 1 es siempre el SuperAdmin del sistema
UPDATE dbo.SecurityRole SET IsSuperAdmin = 1 WHERE SecurityRoleId = 1;
GO

-- Eliminar la columna RoleLevel ahora que IsSuperAdmin la reemplaza
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SecurityRole') AND name = 'RoleLevel')
BEGIN
    DECLARE @ConstraintName SYSNAME;
    SELECT  @ConstraintName = dc.name
    FROM    sys.default_constraints dc
    INNER JOIN sys.columns c
            ON c.default_object_id = dc.object_id
    WHERE   c.object_id = OBJECT_ID('dbo.SecurityRole')
      AND   c.name      = 'RoleLevel';

    IF @ConstraintName IS NOT NULL
        EXEC('ALTER TABLE dbo.SecurityRole DROP CONSTRAINT ' + @ConstraintName);

    ALTER TABLE dbo.SecurityRole DROP COLUMN RoleLevel;
END
GO

-- =============================================================================
-- PASO 2: SecurityRole_GetAll — sin RoleLevel, con IsSuperAdmin
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.SecurityRole_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT SecurityRoleId, RoleName, IsSuperAdmin
    FROM   dbo.SecurityRole
    ORDER BY IsSuperAdmin DESC, RoleName;
END
GO

-- =============================================================================
-- PASO 3: SecurityRole_AddOrEdit — solo nombre, no más Nivel
-- El rol SuperAdmin (Id=1) sigue siendo inmutable.
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.SecurityRole_AddOrEdit
    @SecurityRoleId INT,
    @RoleName       VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    IF @SecurityRoleId = 0
    BEGIN
        INSERT INTO dbo.SecurityRole (RoleName, IsSuperAdmin)
        VALUES (@RoleName, 0);
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
        SET    RoleName = @RoleName
        WHERE  SecurityRoleId = @SecurityRoleId;

        SELECT @SecurityRoleId AS SecurityRoleId;
    END
END
GO

-- =============================================================================
-- PASO 4: SecurityUser_GetAll — devuelve IsSuperAdmin en lugar de RoleLevel
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
        r.IsSuperAdmin,
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
-- PASO 5: Security_UserLogin — devuelve IsSuperAdmin en lugar de RoleLevel
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
        r.IsSuperAdmin,
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
-- PASO 6: SecurityModule_GetAll — listado completo para el CRUD admin
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.SecurityModule_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        sm.SecurityModuleId,
        sm.ParentSecurityModuleId,
        p.ModuleName              AS ParentModuleName,
        sm.ModuleName,
        sm.ModuleUrl,
        sm.ModuleIcon,
        sm.ModuleOrder,
        sm.IsActive
    FROM       dbo.SecurityModule sm
    LEFT JOIN  dbo.SecurityModule p ON p.SecurityModuleId = sm.ParentSecurityModuleId
    ORDER BY   COALESCE(sm.ParentSecurityModuleId, sm.SecurityModuleId),
               sm.ParentSecurityModuleId,
               sm.ModuleOrder;
END
GO

-- =============================================================================
-- PASO 7: SecurityModule_AddOrEdit
-- ModuleUrl NULL = padre (agrupador). Validamos que ModuleUrl sea único.
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.SecurityModule_AddOrEdit
    @SecurityModuleId       INT,
    @ParentSecurityModuleId INT          = NULL,
    @ModuleName             NVARCHAR(80),
    @ModuleUrl              VARCHAR(150) = NULL,
    @ModuleIcon             VARCHAR(50)  = NULL,
    @ModuleOrder            TINYINT      = 99,
    @IsActive               BIT          = 1
AS
BEGIN
    SET NOCOUNT ON;

    -- Un padre no puede apuntarse a sí mismo
    IF @SecurityModuleId <> 0 AND @ParentSecurityModuleId = @SecurityModuleId
    BEGIN
        RAISERROR('Un módulo no puede ser su propio padre.', 16, 1);
        RETURN;
    END

    -- Si se especifica padre, debe existir y ser un padre puro (ModuleUrl NULL)
    -- — esto evita anidar más de 2 niveles que la UI no soporta.
    IF @ParentSecurityModuleId IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.SecurityModule WHERE SecurityModuleId = @ParentSecurityModuleId)
        BEGIN
            RAISERROR('El módulo padre no existe.', 16, 1);
            RETURN;
        END
    END

    -- ModuleUrl único (cuando no es NULL)
    IF @ModuleUrl IS NOT NULL AND EXISTS (
        SELECT 1
        FROM   dbo.SecurityModule
        WHERE  ModuleUrl       = @ModuleUrl
          AND  SecurityModuleId <> @SecurityModuleId
    )
    BEGIN
        RAISERROR('Ya existe un módulo con esa URL.', 16, 1);
        RETURN;
    END

    IF @SecurityModuleId = 0
    BEGIN
        INSERT INTO dbo.SecurityModule
            (ParentSecurityModuleId, ModuleName, ModuleUrl, ModuleIcon, ModuleOrder, IsActive)
        VALUES
            (@ParentSecurityModuleId, @ModuleName, @ModuleUrl, @ModuleIcon, @ModuleOrder, @IsActive);

        SELECT CAST(SCOPE_IDENTITY() AS INT) AS SecurityModuleId;
    END
    ELSE
    BEGIN
        UPDATE dbo.SecurityModule
        SET    ParentSecurityModuleId = @ParentSecurityModuleId,
               ModuleName             = @ModuleName,
               ModuleUrl              = @ModuleUrl,
               ModuleIcon             = @ModuleIcon,
               ModuleOrder            = @ModuleOrder,
               IsActive               = @IsActive
        WHERE  SecurityModuleId = @SecurityModuleId;

        SELECT @SecurityModuleId AS SecurityModuleId;
    END
END
GO

-- =============================================================================
-- PASO 8: SecurityModule_Delete
-- No permite eliminar un padre que tenga hijos activos.
-- Borra en cascada los permisos asociados en SecurityRoleModule.
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.SecurityModule_Delete
    @SecurityModuleId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.SecurityModule WHERE ParentSecurityModuleId = @SecurityModuleId)
    BEGIN
        RAISERROR('No se puede eliminar el módulo porque tiene hijos. Elimina o reasigna los hijos primero.', 16, 1);
        RETURN;
    END

    DELETE FROM dbo.SecurityRoleModule WHERE SecurityModuleId = @SecurityModuleId;
    DELETE FROM dbo.SecurityModule     WHERE SecurityModuleId = @SecurityModuleId;
END
GO

-- =============================================================================
-- PASO 9: Registrar el módulo "Módulos" en el sidebar (visible para SuperAdmin
-- automáticamente; el filtro lo bloquea para los demás a menos que se otorgue
-- permiso explícito).
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.SecurityModule WHERE ModuleUrl = '/Admin/Module')
    INSERT INTO dbo.SecurityModule
        (ModuleName, ModuleUrl, ModuleIcon, ModuleOrder, IsActive, ParentSecurityModuleId)
    VALUES
        (N'Módulos', '/Admin/Module', 'fa-th-list', 95, 1, NULL);
GO

-- =============================================================================
-- Verificación
-- =============================================================================
SELECT SecurityRoleId, RoleName, IsSuperAdmin FROM dbo.SecurityRole ORDER BY SecurityRoleId;
SELECT SecurityModuleId, ParentSecurityModuleId, ModuleName, ModuleUrl, ModuleOrder, IsActive
FROM   dbo.SecurityModule
ORDER BY COALESCE(ParentSecurityModuleId, SecurityModuleId), ParentSecurityModuleId, ModuleOrder;
GO
