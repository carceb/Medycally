-- =============================================================================
-- Alcance global por rol (HasGlobalScope) + rol "Configuración" / back-office
--
-- Permite que un rol NO-SuperAdmin administre los catálogos del sistema
-- (Clínicas, Usuarios, Médicos, Tarifas, Especialidades, Roles, Módulos)
-- viendo los datos de TODAS las clínicas, sin necesidad de asignación manual
-- por clínica. El SuperAdmin sigue funcionando igual (su bypass es transversal).
--
-- Idempotente (CREATE OR ALTER / IF NOT EXISTS). Ejecutar completo en SSMS.
-- =============================================================================

-- =============================================================================
-- PASO 1: Columna SecurityRole.HasGlobalScope
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SecurityRole') AND name = 'HasGlobalScope')
    ALTER TABLE dbo.SecurityRole
        ADD HasGlobalScope BIT NOT NULL CONSTRAINT DF_SecurityRole_HasGlobalScope DEFAULT(0);
GO

-- El SuperAdmin (Id=1) implícitamente ya ve todo; mantenemos su flag en 1 por consistencia.
UPDATE dbo.SecurityRole SET HasGlobalScope = 1 WHERE SecurityRoleId = 1 AND HasGlobalScope = 0;
GO

-- =============================================================================
-- PASO 2: SecurityRole_GetAll — incluye HasGlobalScope
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.SecurityRole_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT SecurityRoleId, RoleName, IsSuperAdmin, HasGlobalScope
    FROM   dbo.SecurityRole
    ORDER BY IsSuperAdmin DESC, RoleName;
END
GO

-- =============================================================================
-- PASO 3: SecurityRole_AddOrEdit — persiste HasGlobalScope
-- El rol SuperAdmin (Id=1) sigue siendo inmutable.
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.SecurityRole_AddOrEdit
    @SecurityRoleId INT,
    @RoleName       VARCHAR(50),
    @HasGlobalScope BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    IF @SecurityRoleId = 0
    BEGIN
        INSERT INTO dbo.SecurityRole (RoleName, IsSuperAdmin, HasGlobalScope)
        VALUES (@RoleName, 0, @HasGlobalScope);
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
        SET    RoleName       = @RoleName,
               HasGlobalScope = @HasGlobalScope
        WHERE  SecurityRoleId = @SecurityRoleId;

        SELECT @SecurityRoleId AS SecurityRoleId;
    END
END
GO

-- =============================================================================
-- PASO 4: Security_UserLogin — devuelve HasGlobalScope para hornearlo en el claim
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
        r.HasGlobalScope,
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
-- PASO 5: Clinic_GetByUser — agrega @HasGlobalScope
--   - SuperAdmin O HasGlobalScope → todas las clínicas
--   - Resto → SecurityUserClinic UNION ClinicDoctor
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.Clinic_GetByUser
    @SecurityUserId INT,
    @IsSuperAdmin   BIT,
    @DoctorId       INT = NULL,
    @HasGlobalScope BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    IF @IsSuperAdmin = 1 OR @HasGlobalScope = 1
    BEGIN
        SELECT
            c.ClinicId,
            c.ClinicRif,
            c.ClinicTypeId,
            ct.ClinicTypeName,
            c.ClinicGroupId,
            c.ClinicName,
            c.MunicipalityId,
            ISNULL(m.MunicipalityName, '')  AS MunicipalityName,
            ISNULL(m.StateId, 0)            AS StateId,
            ISNULL(s.StateName,  '')        AS StateName,
            c.ClinicAddress,
            c.ClinicPhones,
            c.GoogleMapsUrl,
            ISNULL(c.Latitude,   0)         AS Latitude,
            ISNULL(c.Longitude,  0)         AS Longitude,
            c.RepresentativeName,
            c.LandingPage,
            c.ClinicDateCreated,
            c.StatusId
        FROM       dbo.Clinic        c
        LEFT JOIN  dbo.Municipality  m   ON m.MunicipalityId = c.MunicipalityId
        LEFT JOIN  dbo.State         s   ON s.StateId        = m.StateId
        LEFT JOIN  dbo.ClinicType    ct  ON ct.ClinicTypeId  = c.ClinicTypeId
        ORDER BY c.ClinicName;
        RETURN;
    END

    SELECT
        c.ClinicId,
        c.ClinicRif,
        c.ClinicTypeId,
        ct.ClinicTypeName,
        c.ClinicGroupId,
        c.ClinicName,
        c.MunicipalityId,
        ISNULL(m.MunicipalityName, '')  AS MunicipalityName,
        ISNULL(m.StateId, 0)            AS StateId,
        ISNULL(s.StateName,  '')        AS StateName,
        c.ClinicAddress,
        c.ClinicPhones,
        c.GoogleMapsUrl,
        ISNULL(c.Latitude,   0)         AS Latitude,
        ISNULL(c.Longitude,  0)         AS Longitude,
        c.RepresentativeName,
        c.LandingPage,
        c.ClinicDateCreated,
        c.StatusId
    FROM dbo.Clinic c
    LEFT JOIN  dbo.Municipality  m   ON m.MunicipalityId = c.MunicipalityId
    LEFT JOIN  dbo.State         s   ON s.StateId        = m.StateId
    LEFT JOIN  dbo.ClinicType    ct  ON ct.ClinicTypeId  = c.ClinicTypeId
    WHERE c.ClinicId IN (
        SELECT ClinicId FROM dbo.SecurityUserClinic WHERE SecurityUserId = @SecurityUserId
        UNION
        SELECT ClinicId FROM dbo.ClinicDoctor       WHERE @DoctorId IS NOT NULL AND DoctorId = @DoctorId
    )
    ORDER BY c.ClinicName;
END
GO

-- =============================================================================
-- PASO 6: SecurityUser_GetByUser — agrega @HasGlobalScope
--   - SuperAdmin → todos los usuarios
--   - HasGlobalScope (no SuperAdmin) → todos los usuarios NO-SuperAdmin
--     (se sigue ocultando a los SuperAdmins de quien no lo es)
--   - Resto → scoping por clínica compartida (lógica previa intacta)
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.SecurityUser_GetByUser
    @SecurityUserId INT,
    @IsSuperAdmin   BIT,
    @DoctorId       INT = NULL,
    @HasGlobalScope BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    -- ── SuperAdmin: ve a todos ──────────────────────────────────────────────
    IF @IsSuperAdmin = 1
    BEGIN
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
        ORDER BY u.UserName;
        RETURN;
    END

    -- ── Alcance global (no SuperAdmin): ve a todos los usuarios NO-SuperAdmin ─
    IF @HasGlobalScope = 1
    BEGIN
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
        WHERE  r.IsSuperAdmin = 0
        ORDER BY u.UserName;
        RETURN;
    END

    -- ── Usuario regular: scoping por clínica ────────────────────────────────
    DECLARE @MyClinics TABLE (ClinicId INT PRIMARY KEY);

    INSERT INTO @MyClinics (ClinicId)
    SELECT DISTINCT ClinicId
    FROM (
        SELECT suc.ClinicId
        FROM   dbo.SecurityUserClinic suc
        WHERE  suc.SecurityUserId = @SecurityUserId
        UNION
        SELECT cd.ClinicId
        FROM   dbo.ClinicDoctor cd
        WHERE  @DoctorId IS NOT NULL
          AND  cd.DoctorId = @DoctorId
    ) src;

    SELECT DISTINCT
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
    WHERE  r.IsSuperAdmin = 0
      AND  (
            u.SecurityUserId = @SecurityUserId
            OR
            EXISTS (
                SELECT 1
                FROM   dbo.SecurityUserClinic suc
                INNER JOIN @MyClinics mc ON mc.ClinicId = suc.ClinicId
                WHERE  suc.SecurityUserId = u.SecurityUserId
            )
            OR
            EXISTS (
                SELECT 1
                FROM   dbo.ClinicDoctor cd
                INNER JOIN @MyClinics    mc ON mc.ClinicId = cd.ClinicId
                WHERE  u.DoctorId IS NOT NULL
                  AND  cd.DoctorId = u.DoctorId
            )
      )
    ORDER BY u.UserName;
END
GO

-- =============================================================================
-- PASO 7: Registrar el módulo "Roles" (/Admin/Role) si no existe.
-- (/Admin/Module ya se registra en SPs_Security_Modules_v2.sql.)
-- Sin esta fila, el sistema de permisos no aplica enforcement sobre el módulo.
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.SecurityModule WHERE ModuleUrl = '/Admin/Role')
    INSERT INTO dbo.SecurityModule
        (ModuleName, ModuleUrl, ModuleIcon, ModuleOrder, IsActive, ParentSecurityModuleId)
    VALUES
        (N'Roles', '/Admin/Role', 'fa-shield-alt', 96, 1, NULL);
GO

-- =============================================================================
-- PASO 8 (SEED): Rol "Configuración" con HasGlobalScope = 1
-- Otorga CanView/Create/Edit/Delete sobre los módulos de configuración.
-- NO toca lo operativo (cola, atención, reportes) → no aparece en el sidebar.
-- Ajusta la lista @ConfigUrls si tus URLs difieren.
-- =============================================================================
DECLARE @ConfigRoleId INT;

SELECT @ConfigRoleId = SecurityRoleId FROM dbo.SecurityRole WHERE RoleName = N'Configuración';

IF @ConfigRoleId IS NULL
BEGIN
    INSERT INTO dbo.SecurityRole (RoleName, IsSuperAdmin, HasGlobalScope)
    VALUES (N'Configuración', 0, 1);
    SET @ConfigRoleId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE dbo.SecurityRole SET HasGlobalScope = 1 WHERE SecurityRoleId = @ConfigRoleId;
END

-- URLs de los módulos de configuración a otorgar
DECLARE @ConfigUrls TABLE (ModuleUrl VARCHAR(150) PRIMARY KEY);
INSERT INTO @ConfigUrls (ModuleUrl) VALUES
    ('/Admin/Specialty'),
    ('/Admin/Clinic'),
    ('/Admin/Doctor'),
    ('/Admin/Fee'),
    ('/Admin/Role'),
    ('/Admin/Module'),
    ('/Admin/User');

-- Upsert de permisos completos (View/Create/Edit/Delete) sobre cada módulo existente
MERGE dbo.SecurityRoleModule AS tgt
USING (
    SELECT sm.SecurityModuleId
    FROM   dbo.SecurityModule sm
    INNER JOIN @ConfigUrls cu ON cu.ModuleUrl = sm.ModuleUrl
) AS src
ON  tgt.SecurityRoleId   = @ConfigRoleId
AND tgt.SecurityModuleId = src.SecurityModuleId
WHEN MATCHED THEN
    UPDATE SET CanView = 1, CanCreate = 1, CanEdit = 1, CanDelete = 1
WHEN NOT MATCHED BY TARGET THEN
    INSERT (SecurityRoleId, SecurityModuleId, CanView, CanCreate, CanEdit, CanDelete)
    VALUES (@ConfigRoleId, src.SecurityModuleId, 1, 1, 1, 1);

PRINT 'OK — Rol Configuración (Id=' + CAST(@ConfigRoleId AS VARCHAR) + ') configurado con alcance global.';
GO

-- =============================================================================
-- Verificación
-- =============================================================================
SELECT SecurityRoleId, RoleName, IsSuperAdmin, HasGlobalScope
FROM   dbo.SecurityRole
ORDER BY SecurityRoleId;
GO
