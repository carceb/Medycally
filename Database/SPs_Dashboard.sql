-- =============================================================================
-- Dashboard: Vista Appointment_Detail + SP Appointment_GetByClinic
-- Ejecutar en SSMS sobre la BD de Medycally
-- =============================================================================

-- =============================================================================
-- PASO 1: Vista Appointment_Detail
-- =============================================================================
IF OBJECT_ID('dbo.Appointment_Detail', 'V') IS NOT NULL DROP VIEW dbo.Appointment_Detail;
GO

CREATE VIEW dbo.Appointment_Detail AS
SELECT
    a.AppointmentId,
    a.ClinicId,
    c.ClinicName,
    a.PatientName,
    a.PatientIdNumber,
    a.PatientTypeId,
    a.SexId,
    a.SpecialtyDoctorId,
    sd.SpecialtyId,
    s.SpecialtyName,
    sd.DoctorId,
    ISNULL(sx.DoctorAbbreviation + ' ', '') + d.DoctorName AS DoctorName,
    a.AppointmentDate,
    CAST(a.AppointmentDate AS DATE)                                    AS AppointmentDay,
    LEFT(CONVERT(VARCHAR(8), CAST(a.AppointmentDate AS TIME), 108), 5) AS AppointmentTime,
    a.AppointmentStatusId,
    ast.AppointmentStatusName,
    a.Symptoms
FROM      dbo.Appointment        a
INNER JOIN dbo.Clinic             c   ON c.ClinicId            = a.ClinicId
INNER JOIN dbo.SpecialtyDoctor    sd  ON sd.SpecialtyDoctorId  = a.SpecialtyDoctorId
INNER JOIN dbo.Specialty          s   ON s.SpecialtyId         = sd.SpecialtyId
INNER JOIN dbo.Doctor             d   ON d.DoctorId            = sd.DoctorId
INNER JOIN dbo.AppointmentStatus  ast ON ast.AppointmentStatusId = a.AppointmentStatusId
LEFT  JOIN dbo.Sex                sx  ON sx.SexId              = d.SexId;
GO

-- =============================================================================
-- PASO 2: Appointment_GetByClinic
-- Devuelve citas de una clínica para una fecha específica (o todas si @Date = NULL).
-- =============================================================================
IF OBJECT_ID('dbo.Appointment_GetByClinic', 'P') IS NOT NULL DROP PROCEDURE dbo.Appointment_GetByClinic;
GO

CREATE PROCEDURE dbo.Appointment_GetByClinic
    @ClinicId INT,
    @Date     DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        AppointmentId,
        ClinicId,
        ClinicName,
        PatientName,
        SpecialtyName,
        DoctorName,
        AppointmentDate,
        AppointmentTime,
        AppointmentStatusId,
        AppointmentStatusName,
        Symptoms
    FROM dbo.Appointment_Detail
    WHERE ClinicId = @ClinicId
      AND (@Date IS NULL OR AppointmentDay = @Date)
    ORDER BY AppointmentDate;
END
GO

-- =============================================================================
-- PASO 3: AppointmentStatus_GetAll
-- Devuelve todos los estatus disponibles para una cita.
-- =============================================================================
IF OBJECT_ID('dbo.AppointmentStatus_GetAll', 'P') IS NOT NULL DROP PROCEDURE dbo.AppointmentStatus_GetAll;
GO

CREATE PROCEDURE dbo.AppointmentStatus_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT AppointmentStatusId, AppointmentStatusName
    FROM   dbo.AppointmentStatus
    ORDER  BY AppointmentStatusId;
END
GO

-- =============================================================================
-- PASO 4: Appointment_UpdateStatus
-- Actualiza el estatus de una cita.
-- =============================================================================
IF OBJECT_ID('dbo.Appointment_UpdateStatus', 'P') IS NOT NULL DROP PROCEDURE dbo.Appointment_UpdateStatus;
GO

CREATE PROCEDURE dbo.Appointment_UpdateStatus
    @AppointmentId       INT,
    @AppointmentStatusId INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Appointment
    SET    AppointmentStatusId = @AppointmentStatusId
    WHERE  AppointmentId       = @AppointmentId;
END
GO

-- =============================================================================
-- PASO 5: Agregar columnas de detalle a la tabla Appointment
-- (idempotente — solo agrega si no existen)
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Appointment') AND name = 'PatientPhone')
BEGIN
    ALTER TABLE dbo.Appointment ADD
        PatientPhone           NVARCHAR(20)  NULL,
        PatientAddress         NVARCHAR(300) NULL,
        PatientStateId         INT           NULL,
        PatientBirthDate       DATE          NULL,
        ChildGuardianPhone     NVARCHAR(20)  NULL,
        ChildGuardianAddress   NVARCHAR(300) NULL,
        ChildGuardianStateId   INT           NULL,
        ChildGuardianBirthDate DATE          NULL,
        ChildGuardianSexId     INT           NULL;
END
GO

-- =============================================================================
-- PASO 6: Appointment_AddOrEdit — incluye nuevas columnas de detalle
-- =============================================================================
IF OBJECT_ID('dbo.Appointment_AddOrEdit', 'P') IS NOT NULL DROP PROCEDURE dbo.Appointment_AddOrEdit;
GO

CREATE PROCEDURE dbo.Appointment_AddOrEdit
    @AppointmentId          INT,
    @ClinicId               INT,
    @PatientTypeId          INT,
    @PatientAge             INT,
    @PatientIdNumber        INT,
    @ChildGuardianIdNumber  INT,
    @ChildGuardianName      NVARCHAR(200),
    @RelationshipId         INT,
    @PatientName            NVARCHAR(200),
    @SexId                  INT,
    @SpecialtyDoctorId      INT,
    @AppointmentDate        DATETIME,
    @Symptoms               NVARCHAR(500),
    @AppointmentStatusId    INT,
    @PatientPhone           NVARCHAR(20)  = NULL,
    @PatientAddress         NVARCHAR(300) = NULL,
    @PatientStateId         INT           = NULL,
    @PatientBirthDate       DATE          = NULL,
    @ChildGuardianPhone     NVARCHAR(20)  = NULL,
    @ChildGuardianAddress   NVARCHAR(300) = NULL,
    @ChildGuardianStateId   INT           = NULL,
    @ChildGuardianBirthDate DATE          = NULL,
    @ChildGuardianSexId     INT           = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF @AppointmentId = 0
    BEGIN
        INSERT INTO dbo.Appointment (
            ClinicId, PatientTypeId, PatientAge, PatientIdNumber,
            ChildGuardianIdNumber, ChildGuardianName, RelationshipId,
            PatientName, SexId, SpecialtyDoctorId, AppointmentDate,
            Symptoms, AppointmentStatusId,
            PatientPhone, PatientAddress, PatientStateId, PatientBirthDate,
            ChildGuardianPhone, ChildGuardianAddress, ChildGuardianStateId,
            ChildGuardianBirthDate, ChildGuardianSexId
        )
        VALUES (
            @ClinicId, @PatientTypeId, @PatientAge, @PatientIdNumber,
            @ChildGuardianIdNumber, @ChildGuardianName, @RelationshipId,
            @PatientName, @SexId, @SpecialtyDoctorId, @AppointmentDate,
            @Symptoms, @AppointmentStatusId,
            @PatientPhone, @PatientAddress, @PatientStateId, @PatientBirthDate,
            @ChildGuardianPhone, @ChildGuardianAddress, @ChildGuardianStateId,
            @ChildGuardianBirthDate, @ChildGuardianSexId
        );
        SELECT SCOPE_IDENTITY() AS AppointmentId;
    END
    ELSE
    BEGIN
        UPDATE dbo.Appointment
        SET ClinicId               = @ClinicId,
            PatientTypeId          = @PatientTypeId,
            PatientAge             = @PatientAge,
            PatientIdNumber        = @PatientIdNumber,
            ChildGuardianIdNumber  = @ChildGuardianIdNumber,
            ChildGuardianName      = @ChildGuardianName,
            RelationshipId         = @RelationshipId,
            PatientName            = @PatientName,
            SexId                  = @SexId,
            SpecialtyDoctorId      = @SpecialtyDoctorId,
            AppointmentDate        = @AppointmentDate,
            Symptoms               = @Symptoms,
            AppointmentStatusId    = @AppointmentStatusId,
            PatientPhone           = @PatientPhone,
            PatientAddress         = @PatientAddress,
            PatientStateId         = @PatientStateId,
            PatientBirthDate       = @PatientBirthDate,
            ChildGuardianPhone     = @ChildGuardianPhone,
            ChildGuardianAddress   = @ChildGuardianAddress,
            ChildGuardianStateId   = @ChildGuardianStateId,
            ChildGuardianBirthDate = @ChildGuardianBirthDate,
            ChildGuardianSexId     = @ChildGuardianSexId
        WHERE AppointmentId = @AppointmentId;
        SELECT @AppointmentId AS AppointmentId;
    END
END
GO

-- =============================================================================
-- PASO 7: Appointment_GetById — detalle completo de una cita
-- =============================================================================
IF OBJECT_ID('dbo.Appointment_GetById', 'P') IS NOT NULL DROP PROCEDURE dbo.Appointment_GetById;
GO

CREATE PROCEDURE dbo.Appointment_GetById
    @AppointmentId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        a.AppointmentId,
        a.ClinicId,
        c.ClinicName,
        a.PatientTypeId,
        a.PatientName,
        a.PatientAge,
        a.PatientIdNumber,
        sx_p.SexName                                                             AS PatientSexName,
        a.PatientPhone,
        a.PatientAddress,
        a.PatientBirthDate,
        st_p.StateName                                                           AS PatientStateName,
        a.ChildGuardianIdNumber,
        a.ChildGuardianName,
        r.RelationshipName,
        a.ChildGuardianPhone,
        a.ChildGuardianAddress,
        a.ChildGuardianBirthDate,
        st_g.StateName                                                           AS ChildGuardianStateName,
        sx_g.SexName                                                             AS ChildGuardianSexName,
        s.SpecialtyName,
        ISNULL(sx_d.DoctorAbbreviation + ' ', '') + d.DoctorName                AS DoctorName,
        a.AppointmentDate,
        LEFT(CONVERT(VARCHAR(8), CAST(a.AppointmentDate AS TIME), 108), 5)       AS AppointmentTime,
        a.Symptoms,
        a.AppointmentStatusId,
        ast.AppointmentStatusName
    FROM      dbo.Appointment        a
    INNER JOIN dbo.Clinic             c     ON c.ClinicId              = a.ClinicId
    INNER JOIN dbo.SpecialtyDoctor    sd    ON sd.SpecialtyDoctorId    = a.SpecialtyDoctorId
    INNER JOIN dbo.Specialty          s     ON s.SpecialtyId           = sd.SpecialtyId
    INNER JOIN dbo.Doctor             d     ON d.DoctorId              = sd.DoctorId
    INNER JOIN dbo.AppointmentStatus  ast   ON ast.AppointmentStatusId = a.AppointmentStatusId
    LEFT  JOIN dbo.Sex                sx_p  ON sx_p.SexId              = a.SexId
    LEFT  JOIN dbo.Sex                sx_d  ON sx_d.SexId              = d.SexId
    LEFT  JOIN dbo.Sex                sx_g  ON sx_g.SexId              = a.ChildGuardianSexId
    LEFT  JOIN dbo.Relationship       r     ON r.RelationshipId        = a.RelationshipId
    LEFT  JOIN dbo.State              st_p  ON st_p.StateId            = a.PatientStateId
    LEFT  JOIN dbo.State              st_g  ON st_g.StateId            = a.ChildGuardianStateId
    WHERE a.AppointmentId = @AppointmentId;
END
GO

-- =============================================================================
-- PASO 8: Agregar columna ReasonId a Appointment
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Appointment') AND name = 'ReasonId')
BEGIN
    ALTER TABLE dbo.Appointment ADD ReasonId INT NULL;
END
GO

-- =============================================================================
-- PASO 9: Appointment_AddOrEdit — agrega @ReasonId
-- =============================================================================
IF OBJECT_ID('dbo.Appointment_AddOrEdit', 'P') IS NOT NULL DROP PROCEDURE dbo.Appointment_AddOrEdit;
GO

CREATE PROCEDURE dbo.Appointment_AddOrEdit
    @AppointmentId          INT,
    @ClinicId               INT,
    @PatientTypeId          INT,
    @PatientAge             INT,
    @PatientIdNumber        INT,
    @ChildGuardianIdNumber  INT,
    @ChildGuardianName      NVARCHAR(200),
    @RelationshipId         INT,
    @PatientName            NVARCHAR(200),
    @SexId                  INT,
    @SpecialtyDoctorId      INT,
    @AppointmentDate        DATETIME,
    @Symptoms               NVARCHAR(500),
    @AppointmentStatusId    INT,
    @PatientPhone           NVARCHAR(20)  = NULL,
    @PatientAddress         NVARCHAR(300) = NULL,
    @PatientStateId         INT           = NULL,
    @PatientBirthDate       DATE          = NULL,
    @ChildGuardianPhone     NVARCHAR(20)  = NULL,
    @ChildGuardianAddress   NVARCHAR(300) = NULL,
    @ChildGuardianStateId   INT           = NULL,
    @ChildGuardianBirthDate DATE          = NULL,
    @ChildGuardianSexId     INT           = NULL,
    @ReasonId               INT           = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF @AppointmentId = 0
    BEGIN
        INSERT INTO dbo.Appointment (
            ClinicId, PatientTypeId, PatientAge, PatientIdNumber,
            ChildGuardianIdNumber, ChildGuardianName, RelationshipId,
            PatientName, SexId, SpecialtyDoctorId, AppointmentDate,
            Symptoms, AppointmentStatusId,
            PatientPhone, PatientAddress, PatientStateId, PatientBirthDate,
            ChildGuardianPhone, ChildGuardianAddress, ChildGuardianStateId,
            ChildGuardianBirthDate, ChildGuardianSexId, ReasonId
        )
        VALUES (
            @ClinicId, @PatientTypeId, @PatientAge, @PatientIdNumber,
            @ChildGuardianIdNumber, @ChildGuardianName, @RelationshipId,
            @PatientName, @SexId, @SpecialtyDoctorId, @AppointmentDate,
            @Symptoms, @AppointmentStatusId,
            @PatientPhone, @PatientAddress, @PatientStateId, @PatientBirthDate,
            @ChildGuardianPhone, @ChildGuardianAddress, @ChildGuardianStateId,
            @ChildGuardianBirthDate, @ChildGuardianSexId, @ReasonId
        );
        SELECT SCOPE_IDENTITY() AS AppointmentId;
    END
    ELSE
    BEGIN
        UPDATE dbo.Appointment
        SET ClinicId               = @ClinicId,
            PatientTypeId          = @PatientTypeId,
            PatientAge             = @PatientAge,
            PatientIdNumber        = @PatientIdNumber,
            ChildGuardianIdNumber  = @ChildGuardianIdNumber,
            ChildGuardianName      = @ChildGuardianName,
            RelationshipId         = @RelationshipId,
            PatientName            = @PatientName,
            SexId                  = @SexId,
            SpecialtyDoctorId      = @SpecialtyDoctorId,
            AppointmentDate        = @AppointmentDate,
            Symptoms               = @Symptoms,
            AppointmentStatusId    = @AppointmentStatusId,
            PatientPhone           = @PatientPhone,
            PatientAddress         = @PatientAddress,
            PatientStateId         = @PatientStateId,
            PatientBirthDate       = @PatientBirthDate,
            ChildGuardianPhone     = @ChildGuardianPhone,
            ChildGuardianAddress   = @ChildGuardianAddress,
            ChildGuardianStateId   = @ChildGuardianStateId,
            ChildGuardianBirthDate = @ChildGuardianBirthDate,
            ChildGuardianSexId     = @ChildGuardianSexId,
            ReasonId               = @ReasonId
        WHERE AppointmentId = @AppointmentId;
        SELECT @AppointmentId AS AppointmentId;
    END
END
GO

-- =============================================================================
-- PASO 10: Appointment_GetById — agrega JOIN a Reason y devuelve ReasonName
-- =============================================================================
IF OBJECT_ID('dbo.Appointment_GetById', 'P') IS NOT NULL DROP PROCEDURE dbo.Appointment_GetById;
GO

CREATE PROCEDURE dbo.Appointment_GetById
    @AppointmentId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        a.AppointmentId,
        a.ClinicId,
        c.ClinicName,
        a.PatientTypeId,
        a.PatientName,
        a.PatientAge,
        a.PatientIdNumber,
        sx_p.SexName                                                             AS PatientSexName,
        a.PatientPhone,
        a.PatientAddress,
        a.PatientBirthDate,
        st_p.StateName                                                           AS PatientStateName,
        a.ChildGuardianIdNumber,
        a.ChildGuardianName,
        r.RelationshipName,
        a.ChildGuardianPhone,
        a.ChildGuardianAddress,
        a.ChildGuardianBirthDate,
        st_g.StateName                                                           AS ChildGuardianStateName,
        sx_g.SexName                                                             AS ChildGuardianSexName,
        a.ReasonId,
        rsn.ReasonName,
        a.Symptoms,
        s.SpecialtyName,
        ISNULL(sx_d.DoctorAbbreviation + ' ', '') + d.DoctorName                AS DoctorName,
        a.AppointmentDate,
        LEFT(CONVERT(VARCHAR(8), CAST(a.AppointmentDate AS TIME), 108), 5)       AS AppointmentTime,
        a.AppointmentStatusId,
        ast.AppointmentStatusName
    FROM      dbo.Appointment        a
    INNER JOIN dbo.Clinic             c     ON c.ClinicId              = a.ClinicId
    INNER JOIN dbo.SpecialtyDoctor    sd    ON sd.SpecialtyDoctorId    = a.SpecialtyDoctorId
    INNER JOIN dbo.Specialty          s     ON s.SpecialtyId           = sd.SpecialtyId
    INNER JOIN dbo.Doctor             d     ON d.DoctorId              = sd.DoctorId
    INNER JOIN dbo.AppointmentStatus  ast   ON ast.AppointmentStatusId = a.AppointmentStatusId
    LEFT  JOIN dbo.Sex                sx_p  ON sx_p.SexId              = a.SexId
    LEFT  JOIN dbo.Sex                sx_d  ON sx_d.SexId              = d.SexId
    LEFT  JOIN dbo.Sex                sx_g  ON sx_g.SexId              = a.ChildGuardianSexId
    LEFT  JOIN dbo.Relationship       r     ON r.RelationshipId        = a.RelationshipId
    LEFT  JOIN dbo.State              st_p  ON st_p.StateId            = a.PatientStateId
    LEFT  JOIN dbo.State              st_g  ON st_g.StateId            = a.ChildGuardianStateId
    LEFT  JOIN dbo.Reason             rsn   ON rsn.ReasonId            = a.ReasonId
    WHERE a.AppointmentId = @AppointmentId;
END
GO

-- =============================================================================
-- PASO 11: Patient_AddOrEdit
-- =============================================================================
IF OBJECT_ID('dbo.Patient_AddOrEdit', 'P') IS NOT NULL DROP PROCEDURE dbo.Patient_AddOrEdit;
GO

CREATE PROCEDURE dbo.Patient_AddOrEdit
    @PatientId              INT,
    @PatientTypeId          INT,
    @PatientIdNumber        INT,
    @ChildGuardianIdNumber  INT,
    @ChildGuardianName      VARCHAR(50),
    @RelationshipId         INT,
    @PatientName            VARCHAR(120),
    @SexId                  INT,
    @PatientBirthdate       SMALLDATETIME,
    @PatientAddress         VARCHAR(150),
    @MunicipalityId         INT,
    @PatientMainPhone       INT
AS
BEGIN
    SET NOCOUNT ON;
    IF @PatientId = 0
    BEGIN
        INSERT INTO dbo.Patient (
            PatientTypeId, PatientIdNumber, ChildGuardianIdNumber, ChildGuardianName,
            RelationshipId, PatientName, SexId, PatientBirthdate, PatientAddress,
            MunicipalityId, PatientMainPhone
        )
        VALUES (
            @PatientTypeId, @PatientIdNumber, @ChildGuardianIdNumber, @ChildGuardianName,
            @RelationshipId, @PatientName, @SexId, @PatientBirthdate, @PatientAddress,
            @MunicipalityId, @PatientMainPhone
        );
        SELECT SCOPE_IDENTITY() AS PatientId;
    END
    ELSE
    BEGIN
        UPDATE dbo.Patient
        SET PatientTypeId         = @PatientTypeId,
            PatientIdNumber       = @PatientIdNumber,
            ChildGuardianIdNumber = @ChildGuardianIdNumber,
            ChildGuardianName     = @ChildGuardianName,
            RelationshipId        = @RelationshipId,
            PatientName           = @PatientName,
            SexId                 = @SexId,
            PatientBirthdate      = @PatientBirthdate,
            PatientAddress        = @PatientAddress,
            MunicipalityId        = @MunicipalityId,
            PatientMainPhone      = @PatientMainPhone
        WHERE PatientId = @PatientId;
        SELECT @PatientId AS PatientId;
    END
END
GO

-- =============================================================================
-- PASO 12: Agregar PatientId a Appointment_GetById
-- El C# AppointmentQuery.GetById() lee PatientId del reader.
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.Appointment_GetById
    @AppointmentId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        a.AppointmentId,
        a.ClinicId,
        c.ClinicName,
        a.PatientTypeId,
        a.PatientName,
        a.PatientAge,
        a.PatientIdNumber,
        a.SexId                                                                  AS PatientSexId,
        sx_p.SexName                                                             AS PatientSexName,
        a.PatientPhone,
        a.PatientAddress,
        a.PatientBirthDate,
        a.PatientStateId,
        st_p.StateName                                                           AS PatientStateName,
        a.ChildGuardianIdNumber,
        a.ChildGuardianName,
        r.RelationshipName,
        a.ChildGuardianPhone,
        a.ChildGuardianAddress,
        a.ChildGuardianBirthDate,
        a.ChildGuardianSexId,
        sx_g.SexName                                                             AS ChildGuardianSexName,
        a.ChildGuardianStateId,
        st_g.StateName                                                           AS ChildGuardianStateName,
        a.ReasonId,
        rsn.ReasonName,
        a.Symptoms,
        s.SpecialtyName,
        ISNULL(sx_d.DoctorAbbreviation + ' ', '') + d.DoctorName                AS DoctorName,
        a.AppointmentDate,
        LEFT(CONVERT(VARCHAR(8), CAST(a.AppointmentDate AS TIME), 108), 5)       AS AppointmentTime,
        a.AppointmentStatusId,
        ast.AppointmentStatusName,
        a.PatientId
    FROM      dbo.Appointment        a
    INNER JOIN dbo.Clinic             c     ON c.ClinicId              = a.ClinicId
    INNER JOIN dbo.SpecialtyDoctor    sd    ON sd.SpecialtyDoctorId    = a.SpecialtyDoctorId
    INNER JOIN dbo.Specialty          s     ON s.SpecialtyId           = sd.SpecialtyId
    INNER JOIN dbo.Doctor             d     ON d.DoctorId              = sd.DoctorId
    INNER JOIN dbo.AppointmentStatus  ast   ON ast.AppointmentStatusId = a.AppointmentStatusId
    LEFT  JOIN dbo.Sex                sx_p  ON sx_p.SexId              = a.SexId
    LEFT  JOIN dbo.Sex                sx_d  ON sx_d.SexId              = d.SexId
    LEFT  JOIN dbo.Sex                sx_g  ON sx_g.SexId              = a.ChildGuardianSexId
    LEFT  JOIN dbo.Relationship       r     ON r.RelationshipId        = a.RelationshipId
    LEFT  JOIN dbo.State              st_p  ON st_p.StateId            = a.PatientStateId
    LEFT  JOIN dbo.State              st_g  ON st_g.StateId            = a.ChildGuardianStateId
    LEFT  JOIN dbo.Reason             rsn   ON rsn.ReasonId            = a.ReasonId
    WHERE a.AppointmentId = @AppointmentId;
END
GO

-- =============================================================================
-- FIN DEL SCRIPT
-- =============================================================================
