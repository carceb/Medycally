-- =============================================================================
-- SPs para módulos Admin: Doctor, SpecialtyDoctor, SecurityUser, SecurityRole
-- Ejecutar completo en SSMS sobre la BD de Medycally
-- =============================================================================

-- =============================================================================
-- PASO 1: Agregar columnas faltantes a la tabla Doctor (si no existen)
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Doctor') AND name = 'DoctorIdNumber')
    ALTER TABLE dbo.Doctor ADD DoctorIdNumber INT NOT NULL DEFAULT 0;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Doctor') AND name = 'DoctorSecondPhone')
    ALTER TABLE dbo.Doctor ADD DoctorSecondPhone VARCHAR(20) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Doctor') AND name = 'DoctorEmail')
    ALTER TABLE dbo.Doctor ADD DoctorEmail VARCHAR(150) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Doctor') AND name = 'StatusId')
    ALTER TABLE dbo.Doctor ADD StatusId INT NOT NULL DEFAULT 1;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Doctor') AND name = 'SexId')
    ALTER TABLE dbo.Doctor ADD SexId INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Doctor') AND name = 'StateId')
    ALTER TABLE dbo.Doctor ADD StateId INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Doctor') AND name = 'DoctorAddress')
    ALTER TABLE dbo.Doctor ADD DoctorAddress VARCHAR(300) NULL;
GO

-- =============================================================================
-- PASO 2: Doctor_GetAll
-- =============================================================================
IF OBJECT_ID('dbo.Doctor_GetAll', 'P') IS NOT NULL DROP PROCEDURE dbo.Doctor_GetAll;
GO

CREATE PROCEDURE dbo.Doctor_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        d.DoctorId,
        d.DoctorName,
        d.DoctorIdNumber,
        d.SexId,
        sx.SexName,
        d.DoctorMainPhone,
        d.DoctorSecondPhone,
        d.DoctorEmail,
        d.StateId,
        st.StateName,
        d.DoctorAddress,
        d.StatusId,
        sta.StatusName,
        (
            SELECT STRING_AGG(sp.SpecialtyName, ', ') WITHIN GROUP (ORDER BY sp.SpecialtyName)
            FROM   dbo.SpecialtyDoctor sd
            JOIN   dbo.Specialty       sp ON sp.SpecialtyId = sd.SpecialtyId
            WHERE  sd.DoctorId = d.DoctorId
        ) AS SpecialtyNames
    FROM       dbo.Doctor  d
    LEFT JOIN  dbo.Sex     sx  ON sx.SexId    = d.SexId
    LEFT JOIN  dbo.State   st  ON st.StateId  = d.StateId
    LEFT JOIN  dbo.Status  sta ON sta.StatusId = d.StatusId
    ORDER BY d.DoctorName;
END
GO

-- =============================================================================
-- PASO 3: Doctor_AddOrEdit
-- Retorna el DoctorId (nuevo o existente)
-- =============================================================================
IF OBJECT_ID('dbo.Doctor_AddOrEdit', 'P') IS NOT NULL DROP PROCEDURE dbo.Doctor_AddOrEdit;
GO

CREATE PROCEDURE dbo.Doctor_AddOrEdit
    @DoctorId          INT,
    @DoctorName        VARCHAR(150),
    @DoctorIdNumber    INT,
    @SexId             INT          = NULL,
    @DoctorMainPhone   VARCHAR(20)  = NULL,
    @DoctorSecondPhone VARCHAR(20)  = NULL,
    @DoctorEmail       VARCHAR(150) = NULL,
    @StateId           INT          = NULL,
    @DoctorAddress     VARCHAR(300) = NULL,
    @StatusId          INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @DoctorId = 0
    BEGIN
        INSERT INTO dbo.Doctor
            (DoctorName, DoctorIdNumber, SexId, DoctorMainPhone, DoctorSecondPhone, DoctorEmail, StateId, DoctorAddress, StatusId)
        VALUES
            (@DoctorName, @DoctorIdNumber, @SexId, @DoctorMainPhone, @DoctorSecondPhone, @DoctorEmail, @StateId, @DoctorAddress, @StatusId);

        SELECT SCOPE_IDENTITY() AS DoctorId;
    END
    ELSE
    BEGIN
        UPDATE dbo.Doctor
        SET    DoctorName        = @DoctorName,
               DoctorIdNumber    = @DoctorIdNumber,
               SexId             = @SexId,
               DoctorMainPhone   = @DoctorMainPhone,
               DoctorSecondPhone = @DoctorSecondPhone,
               DoctorEmail       = @DoctorEmail,
               StateId           = @StateId,
               DoctorAddress     = @DoctorAddress,
               StatusId          = @StatusId
        WHERE  DoctorId = @DoctorId;

        SELECT @DoctorId AS DoctorId;
    END
END
GO

-- =============================================================================
-- PASO 4: Doctor_Delete
-- =============================================================================
IF OBJECT_ID('dbo.Doctor_Delete', 'P') IS NOT NULL DROP PROCEDURE dbo.Doctor_Delete;
GO

CREATE PROCEDURE dbo.Doctor_Delete
    @DoctorId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Eliminar asignaciones de especialidades
    DELETE FROM dbo.SpecialtyDoctor WHERE DoctorId = @DoctorId;

    DELETE FROM dbo.Doctor WHERE DoctorId = @DoctorId;
END
GO

-- =============================================================================
-- PASO 5: SpecialtyDoctor_GetByDoctorId
-- Retorna TODAS las especialidades con un flag IsAssigned indicando
-- cuáles están asignadas al médico
-- =============================================================================
IF OBJECT_ID('dbo.SpecialtyDoctor_GetByDoctorId', 'P') IS NOT NULL DROP PROCEDURE dbo.SpecialtyDoctor_GetByDoctorId;
GO

CREATE PROCEDURE dbo.SpecialtyDoctor_GetByDoctorId
    @DoctorId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        s.SpecialtyId,
        s.SpecialtyName,
        CASE WHEN sd.DoctorId IS NOT NULL THEN CAST(1 AS BIT)
             ELSE                               CAST(0 AS BIT)
        END AS IsAssigned
    FROM      dbo.Specialty       s
    LEFT JOIN dbo.SpecialtyDoctor sd ON sd.SpecialtyId = s.SpecialtyId
                                    AND sd.DoctorId    = @DoctorId
    ORDER BY s.SpecialtyName;
END
GO

-- =============================================================================
-- PASO 6: SpecialtyDoctor_Save
-- Recibe los SpecialtyIds separados por coma (o NULL para quitar todos).
-- Borra todas las asignaciones actuales del médico y reinserta las nuevas.
-- =============================================================================
IF OBJECT_ID('dbo.SpecialtyDoctor_Save', 'P') IS NOT NULL DROP PROCEDURE dbo.SpecialtyDoctor_Save;
GO

CREATE PROCEDURE dbo.SpecialtyDoctor_Save
    @DoctorId     INT,
    @SpecialtyIds VARCHAR(MAX) = NULL   -- ej: '1,3,5'  o NULL para quitar todos
AS
BEGIN
    SET NOCOUNT ON;

    -- Eliminar asignaciones actuales
    DELETE FROM dbo.SpecialtyDoctor WHERE DoctorId = @DoctorId;

    -- Reinsertar las seleccionadas
    IF @SpecialtyIds IS NOT NULL AND LEN(TRIM(@SpecialtyIds)) > 0
    BEGIN
        INSERT INTO dbo.SpecialtyDoctor (DoctorId, SpecialtyId)
        SELECT @DoctorId, TRY_CAST(TRIM(value) AS INT)
        FROM   STRING_SPLIT(@SpecialtyIds, ',')
        WHERE  TRY_CAST(TRIM(value) AS INT) IS NOT NULL;
    END
END
GO

-- =============================================================================
-- PASO 7: SecurityUser_GetAll
-- Convierte IsActive BIT → StatusId INT (1=Activo, 2=Inactivo)
-- =============================================================================
IF OBJECT_ID('dbo.SecurityUser_GetAll', 'P') IS NOT NULL DROP PROCEDURE dbo.SecurityUser_GetAll;
GO

CREATE PROCEDURE dbo.SecurityUser_GetAll
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
        CASE WHEN u.IsActive = 1 THEN 1 ELSE 2 END AS StatusId
    FROM      dbo.SecurityUser u
    INNER JOIN dbo.SecurityRole r ON r.SecurityRoleId = u.SecurityRoleId
    ORDER BY  u.UserName;
END
GO

-- =============================================================================
-- PASO 8: SecurityUser_AddOrEdit
-- @PasswordHash = NULL → mantiene la contraseña existente (para ediciones)
-- @StatusId 1=Activo / 2=Inactivo → mapea a IsActive BIT
-- =============================================================================
IF OBJECT_ID('dbo.SecurityUser_AddOrEdit', 'P') IS NOT NULL DROP PROCEDURE dbo.SecurityUser_AddOrEdit;
GO

CREATE PROCEDURE dbo.SecurityUser_AddOrEdit
    @SecurityUserId INT,
    @UserName       VARCHAR(100),
    @UserEmail      VARCHAR(100),
    @UserIdNumber   INT,
    @SecurityRoleId INT,
    @StatusId       INT,
    @PasswordHash   VARCHAR(256) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IsActive BIT = CASE WHEN @StatusId = 1 THEN 1 ELSE 0 END;

    IF @SecurityUserId = 0
    BEGIN
        INSERT INTO dbo.SecurityUser
            (SecurityRoleId, UserIdNumber, UserEmail, UserPasswordHash, UserName, IsActive)
        VALUES
            (@SecurityRoleId, @UserIdNumber, @UserEmail,
             ISNULL(@PasswordHash, ''), @UserName, @IsActive);

        SELECT SCOPE_IDENTITY() AS SecurityUserId;
    END
    ELSE
    BEGIN
        UPDATE dbo.SecurityUser
        SET    UserName         = @UserName,
               UserEmail        = @UserEmail,
               UserIdNumber     = @UserIdNumber,
               SecurityRoleId   = @SecurityRoleId,
               IsActive         = @IsActive,
               UserPasswordHash = CASE WHEN @PasswordHash IS NOT NULL
                                       THEN @PasswordHash
                                       ELSE UserPasswordHash
                                   END
        WHERE  SecurityUserId = @SecurityUserId;

        SELECT @SecurityUserId AS SecurityUserId;
    END
END
GO

-- =============================================================================
-- PASO 9: SecurityUser_Delete
-- =============================================================================
IF OBJECT_ID('dbo.SecurityUser_Delete', 'P') IS NOT NULL DROP PROCEDURE dbo.SecurityUser_Delete;
GO

CREATE PROCEDURE dbo.SecurityUser_Delete
    @SecurityUserId INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.SecurityUserClinic      WHERE SecurityUserId = @SecurityUserId;
    DELETE FROM dbo.SecurityUserClinicGroup WHERE SecurityUserId = @SecurityUserId;
    DELETE FROM dbo.SecurityUser            WHERE SecurityUserId = @SecurityUserId;
END
GO

-- =============================================================================
-- PASO 10: SecurityRole_GetAll
-- =============================================================================
IF OBJECT_ID('dbo.SecurityRole_GetAll', 'P') IS NOT NULL DROP PROCEDURE dbo.SecurityRole_GetAll;
GO

CREATE PROCEDURE dbo.SecurityRole_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT SecurityRoleId, RoleName, RoleLevel
    FROM   dbo.SecurityRole
    ORDER BY RoleLevel;
END
GO

-- =============================================================================
-- PASO 11: Clinic_GetAll
-- Asegura que StateId y MunicipalityId se retornan correctamente via JOINs
-- a Municipality y State.
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.Clinic_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        c.ClinicId,
        c.ClinicRif,
        c.ClinicTypeId,
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
    FROM       dbo.Clinic       c
    LEFT JOIN  dbo.Municipality  m  ON  m.MunicipalityId = c.MunicipalityId
    LEFT JOIN  dbo.State         s  ON  s.StateId        = m.StateId
    ORDER BY c.ClinicName;
END
GO

-- =============================================================================
-- PASO 12: Clinic_AddOrEdit
-- Declara @Latitude y @Longitude como DECIMAL(18,8) para evitar el error
-- "Error converting data type numeric to decimal" que ocurre cuando ADO.NET
-- infiere NUMERIC(38,0) al usar AddWithValue con un decimal C#.
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.Clinic_AddOrEdit
    @ClinicId           INT,
    @ClinicRif          VARCHAR(20)      = NULL,
    @ClinicTypeId       INT,
    @ClinicGroupId      INT,
    @ClinicName         VARCHAR(150)     = NULL,
    @MunicipalityId     INT,
    @ClinicAddress      VARCHAR(250)     = NULL,
    @ClinicPhones       VARCHAR(100)     = NULL,
    @GoogleMapsUrl      VARCHAR(500)     = NULL,
    @Latitude           DECIMAL(18, 8)   = NULL,
    @Longitude          DECIMAL(18, 8)   = NULL,
    @RepresentativeName VARCHAR(150)     = NULL,
    @LandingPage        VARCHAR(500)     = NULL,
    @StatusId           INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @ClinicId = 0
    BEGIN
        INSERT INTO dbo.Clinic
            (ClinicRif, ClinicTypeId, ClinicGroupId, ClinicName, MunicipalityId,
             ClinicAddress, ClinicPhones, GoogleMapsUrl, Latitude, Longitude,
             RepresentativeName, LandingPage, StatusId)
        VALUES
            (@ClinicRif, @ClinicTypeId, @ClinicGroupId, @ClinicName, @MunicipalityId,
             @ClinicAddress, @ClinicPhones, @GoogleMapsUrl, @Latitude, @Longitude,
             @RepresentativeName, @LandingPage, @StatusId);

        SELECT SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.Clinic SET
            ClinicRif           = @ClinicRif,
            ClinicTypeId        = @ClinicTypeId,
            ClinicGroupId       = @ClinicGroupId,
            ClinicName          = @ClinicName,
            MunicipalityId      = @MunicipalityId,
            ClinicAddress       = @ClinicAddress,
            ClinicPhones        = @ClinicPhones,
            GoogleMapsUrl       = @GoogleMapsUrl,
            Latitude            = @Latitude,
            Longitude           = @Longitude,
            RepresentativeName  = @RepresentativeName,
            LandingPage         = @LandingPage,
            StatusId            = @StatusId
        WHERE ClinicId = @ClinicId;

        SELECT @ClinicId;
    END
END
GO

-- =============================================================================
-- PASO 13: Ampliar columnas Latitude y Longitude en tabla Clinic
-- Si las columnas tienen precision insuficiente (ej: DECIMAL(9,6)), valores
-- como 666666 causan "Error converting data type numeric to decimal".
-- Cambiarlas a DECIMAL(18,8) permite almacenar cualquier valor que el SP acepta.
-- =============================================================================
ALTER TABLE dbo.Clinic ALTER COLUMN Latitude  DECIMAL(18, 8) NULL;
ALTER TABLE dbo.Clinic ALTER COLUMN Longitude DECIMAL(18, 8) NULL;
GO

-- =============================================================================
-- PASO 14: Cambiar Latitude y Longitude a FLOAT en tabla y SP
-- DECIMAL(18,8) causa "Error converting data type numeric to decimal" cuando
-- ADO.NET envía un decimal C# con scale=0 (ej: 90000M). FLOAT evita por
-- completo el problema de precision/scale y es el tipo estándar para coordenadas.
-- =============================================================================
ALTER TABLE dbo.Clinic ALTER COLUMN Latitude  FLOAT NULL;
ALTER TABLE dbo.Clinic ALTER COLUMN Longitude FLOAT NULL;
GO

CREATE OR ALTER PROCEDURE dbo.Clinic_AddOrEdit
    @ClinicId           INT,
    @ClinicRif          VARCHAR(20)      = NULL,
    @ClinicTypeId       INT,
    @ClinicGroupId      INT,
    @ClinicName         VARCHAR(150)     = NULL,
    @MunicipalityId     INT,
    @ClinicAddress      VARCHAR(250)     = NULL,
    @ClinicPhones       VARCHAR(100)     = NULL,
    @GoogleMapsUrl      VARCHAR(500)     = NULL,
    @Latitude           FLOAT            = NULL,
    @Longitude          FLOAT            = NULL,
    @RepresentativeName VARCHAR(150)     = NULL,
    @LandingPage        VARCHAR(500)     = NULL,
    @StatusId           INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @ClinicId = 0
    BEGIN
        INSERT INTO dbo.Clinic
            (ClinicRif, ClinicTypeId, ClinicGroupId, ClinicName, MunicipalityId,
             ClinicAddress, ClinicPhones, GoogleMapsUrl, Latitude, Longitude,
             RepresentativeName, LandingPage, StatusId)
        VALUES
            (@ClinicRif, @ClinicTypeId, @ClinicGroupId, @ClinicName, @MunicipalityId,
             @ClinicAddress, @ClinicPhones, @GoogleMapsUrl, @Latitude, @Longitude,
             @RepresentativeName, @LandingPage, @StatusId);

        SELECT SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.Clinic SET
            ClinicRif           = @ClinicRif,
            ClinicTypeId        = @ClinicTypeId,
            ClinicGroupId       = @ClinicGroupId,
            ClinicName          = @ClinicName,
            MunicipalityId      = @MunicipalityId,
            ClinicAddress       = @ClinicAddress,
            ClinicPhones        = @ClinicPhones,
            GoogleMapsUrl       = @GoogleMapsUrl,
            Latitude            = @Latitude,
            Longitude           = @Longitude,
            RepresentativeName  = @RepresentativeName,
            LandingPage         = @LandingPage,
            StatusId            = @StatusId
        WHERE ClinicId = @ClinicId;

        SELECT @ClinicId;
    END
END
GO

-- =============================================================================
-- PASO 15: Crear tabla ClinicDoctor si no existe
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ClinicDoctor' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.ClinicDoctor (
        ClinicDoctorId INT          IDENTITY(1,1) NOT NULL,
        ClinicId       INT          NOT NULL,
        DoctorId       INT          NOT NULL,
        CONSTRAINT PK_ClinicDoctor             PRIMARY KEY CLUSTERED (ClinicDoctorId ASC),
        CONSTRAINT UQ_ClinicDoctor             UNIQUE (ClinicId, DoctorId),
        CONSTRAINT FK_ClinicDoctor_Clinic      FOREIGN KEY (ClinicId) REFERENCES dbo.Clinic(ClinicId),
        CONSTRAINT FK_ClinicDoctor_Doctor      FOREIGN KEY (DoctorId) REFERENCES dbo.Doctor(DoctorId)
    );
END
GO

-- =============================================================================
-- PASO 16: ClinicDoctor_GetByClinicId
-- Retorna todos los médicos activos con flag IsAssigned para la clínica indicada.
-- DoctorName incluye la abreviatura del sexo (Dr./Dra.) si está disponible.
-- =============================================================================
IF OBJECT_ID('dbo.ClinicDoctor_GetByClinicId', 'P') IS NOT NULL DROP PROCEDURE dbo.ClinicDoctor_GetByClinicId;
GO

CREATE PROCEDURE dbo.ClinicDoctor_GetByClinicId
    @ClinicId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        d.DoctorId,
        ISNULL(sx.DoctorAbbreviation + ' ', '') + d.DoctorName AS DoctorName,
        CASE WHEN cd.ClinicId IS NOT NULL THEN CAST(1 AS BIT)
             ELSE                               CAST(0 AS BIT)
        END AS IsAssigned
    FROM      dbo.Doctor       d
    LEFT JOIN dbo.Sex          sx ON sx.SexId    = d.SexId
    LEFT JOIN dbo.ClinicDoctor cd ON cd.DoctorId = d.DoctorId
                                 AND cd.ClinicId = @ClinicId
    WHERE d.StatusId = 1
    ORDER BY d.DoctorName;
END
GO

-- =============================================================================
-- PASO 17: ClinicDoctor_Save
-- Reemplaza las asignaciones de médicos de la clínica con la nueva lista.
-- =============================================================================
IF OBJECT_ID('dbo.ClinicDoctor_Save', 'P') IS NOT NULL DROP PROCEDURE dbo.ClinicDoctor_Save;
GO

CREATE PROCEDURE dbo.ClinicDoctor_Save
    @ClinicId  INT,
    @DoctorIds VARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.ClinicDoctor WHERE ClinicId = @ClinicId;

    IF @DoctorIds IS NOT NULL AND LEN(TRIM(@DoctorIds)) > 0
    BEGIN
        INSERT INTO dbo.ClinicDoctor (ClinicId, DoctorId)
        SELECT @ClinicId, TRY_CAST(TRIM(value) AS INT)
        FROM   STRING_SPLIT(@DoctorIds, ',')
        WHERE  TRY_CAST(TRIM(value) AS INT) IS NOT NULL;
    END
END
GO

-- =============================================================================
-- PASO 18: Actualizar Doctor_Delete para limpiar ClinicDoctor al eliminar médico
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.Doctor_Delete
    @DoctorId INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.SpecialtyDoctor WHERE DoctorId = @DoctorId;
    DELETE FROM dbo.ClinicDoctor     WHERE DoctorId = @DoctorId;
    DELETE FROM dbo.Doctor           WHERE DoctorId = @DoctorId;
END
GO

-- =============================================================================
-- PASO 19: DoctorSchedule_GetByClinicAndDoctor
-- Retorna los horarios de un médico en una clínica específica.
-- StartTime y EndTime se devuelven en formato HH:mm (VARCHAR(5)).
-- =============================================================================
IF OBJECT_ID('dbo.DoctorSchedule_GetByClinicAndDoctor', 'P') IS NOT NULL DROP PROCEDURE dbo.DoctorSchedule_GetByClinicAndDoctor;
GO

CREATE PROCEDURE dbo.DoctorSchedule_GetByClinicAndDoctor
    @ClinicId INT,
    @DoctorId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ds.DoctorScheduleId,
        ds.DoctorId,
        ds.ClinicId,
        ds.DayOfWeek,
        LEFT(CONVERT(VARCHAR(8), ds.StartTime, 108), 5) AS StartTime,
        LEFT(CONVERT(VARCHAR(8), ds.EndTime,   108), 5) AS EndTime,
        ds.SlotDurationMinutes,
        ds.IsActive
    FROM dbo.DoctorSchedule ds
    WHERE ds.ClinicId = @ClinicId
      AND ds.DoctorId = @DoctorId
    ORDER BY ds.DayOfWeek, ds.StartTime;
END
GO

-- =============================================================================
-- PASO 20: DoctorSchedule_Save
-- Inserta o actualiza un horario. DoctorScheduleId = 0 → INSERT; > 0 → UPDATE.
-- Devuelve el DoctorScheduleId resultante.
-- =============================================================================
IF OBJECT_ID('dbo.DoctorSchedule_Save', 'P') IS NOT NULL DROP PROCEDURE dbo.DoctorSchedule_Save;
GO

CREATE PROCEDURE dbo.DoctorSchedule_Save
    @DoctorScheduleId    INT,
    @DoctorId            INT,
    @ClinicId            INT,
    @DayOfWeek           TINYINT,
    @StartTime           TIME(7),
    @EndTime             TIME(7),
    @SlotDurationMinutes INT,
    @IsActive            BIT
AS
BEGIN
    SET NOCOUNT ON;

    -- Verificar solapamiento de horario para el mismo médico en el mismo día,
    -- sin importar la clínica (un médico no puede tener turnos solapados en ninguna clínica).
    DECLARE @ConflictInfo NVARCHAR(200);

    SELECT TOP 1
        @ConflictInfo =
            c.ClinicName + N' · ' +
            LEFT(CONVERT(VARCHAR(8), ds.StartTime, 108), 5) + N' – ' +
            LEFT(CONVERT(VARCHAR(8), ds.EndTime,   108), 5)
    FROM dbo.DoctorSchedule ds
    INNER JOIN dbo.Clinic c ON c.ClinicId = ds.ClinicId
    WHERE ds.DoctorId        = @DoctorId
      AND ds.DayOfWeek       = @DayOfWeek
      AND ds.DoctorScheduleId <> @DoctorScheduleId
      AND ds.StartTime        < @EndTime
      AND ds.EndTime          > @StartTime;

    IF @ConflictInfo IS NOT NULL
    BEGIN
        DECLARE @Msg NVARCHAR(500) =
            N'Conflicto de horario: el médico ya tiene un turno ese día que se superpone con ' + @ConflictInfo + N'.';
        THROW 50001, @Msg, 1;
    END

    IF @DoctorScheduleId = 0
    BEGIN
        INSERT INTO dbo.DoctorSchedule (DoctorId, ClinicId, DayOfWeek, StartTime, EndTime, SlotDurationMinutes, IsActive)
        VALUES (@DoctorId, @ClinicId, @DayOfWeek, @StartTime, @EndTime, @SlotDurationMinutes, @IsActive);
        SELECT CAST(SCOPE_IDENTITY() AS INT);
    END
    ELSE
    BEGIN
        UPDATE dbo.DoctorSchedule
        SET DayOfWeek           = @DayOfWeek,
            StartTime           = @StartTime,
            EndTime             = @EndTime,
            SlotDurationMinutes = @SlotDurationMinutes,
            IsActive            = @IsActive
        WHERE DoctorScheduleId = @DoctorScheduleId;
        SELECT @DoctorScheduleId;
    END
END
GO

-- =============================================================================
-- PASO 21: DoctorSchedule_Delete
-- Elimina un horario por su ID.
-- =============================================================================
IF OBJECT_ID('dbo.DoctorSchedule_Delete', 'P') IS NOT NULL DROP PROCEDURE dbo.DoctorSchedule_Delete;
GO

CREATE PROCEDURE dbo.DoctorSchedule_Delete
    @DoctorScheduleId INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.DoctorSchedule WHERE DoctorScheduleId = @DoctorScheduleId;
END
GO

-- =============================================================================
-- FIN DEL SCRIPT
-- =============================================================================
