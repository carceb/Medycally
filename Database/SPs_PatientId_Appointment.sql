-- =============================================================================
-- SPs_PatientId_Appointment.sql
-- Vincula Appointment con Patient mediante PatientId.
-- Ejecutar en SSMS sobre la BD Medycally.
-- =============================================================================

-- =============================================================================
-- PASO 1: Agregar columna PatientId a Appointment
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Appointment') AND name = 'PatientId')
BEGIN
    ALTER TABLE dbo.Appointment ADD PatientId INT NULL
        CONSTRAINT FK_Appointment_Patient FOREIGN KEY REFERENCES dbo.Patient(PatientId);
END
GO

-- =============================================================================
-- PASO 2: Appointment_SetPatientId
-- Vincula una cita con un paciente registrado.
-- Llamado desde el dashboard al registrar un paciente, y desde el wizard
-- cuando el paciente ya existía en la BD.
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.Appointment_SetPatientId
    @AppointmentId INT,
    @PatientId     INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Appointment
    SET    PatientId = @PatientId
    WHERE  AppointmentId = @AppointmentId;
END
GO

-- =============================================================================
-- PASO 3: Appointment_AddOrEdit — acepta @PatientId opcional
-- Preserva PatientId existente si @PatientId IS NULL (UPDATE).
-- En INSERT: guarda @PatientId si se provee (wizard con paciente existente).
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.Appointment_AddOrEdit
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
    @ReasonId               INT           = NULL,
    @PatientId              INT           = NULL
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
            ChildGuardianBirthDate, ChildGuardianSexId, ReasonId, PatientId
        )
        VALUES (
            @ClinicId, @PatientTypeId, @PatientAge, @PatientIdNumber,
            @ChildGuardianIdNumber, @ChildGuardianName, @RelationshipId,
            @PatientName, @SexId, @SpecialtyDoctorId, @AppointmentDate,
            @Symptoms, @AppointmentStatusId,
            @PatientPhone, @PatientAddress, @PatientStateId, @PatientBirthDate,
            @ChildGuardianPhone, @ChildGuardianAddress, @ChildGuardianStateId,
            @ChildGuardianBirthDate, @ChildGuardianSexId, @ReasonId, @PatientId
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
            ReasonId               = @ReasonId,
            PatientId              = ISNULL(@PatientId, PatientId)
        WHERE AppointmentId = @AppointmentId;
        SELECT @AppointmentId AS AppointmentId;
    END
END
GO

-- =============================================================================
-- PASO 4: Appointment_Detail VIEW — agrega PatientId
-- =============================================================================
CREATE OR ALTER VIEW dbo.Appointment_Detail AS
SELECT
    a.AppointmentId,
    a.ClinicId,
    ISNULL(ct.ClinicTypeName + N' ', N'') + c.ClinicName AS ClinicName,
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
    a.Symptoms,
    a.PatientId,
    CAST(CASE WHEN a.PatientId IS NOT NULL THEN 1 ELSE 0 END AS BIT)  AS IsRegistered
FROM      dbo.Appointment        a
INNER JOIN dbo.Clinic             c   ON  c.ClinicId             = a.ClinicId
LEFT  JOIN dbo.ClinicType         ct  ON  ct.ClinicTypeId        = c.ClinicTypeId
INNER JOIN dbo.SpecialtyDoctor    sd  ON  sd.SpecialtyDoctorId   = a.SpecialtyDoctorId
INNER JOIN dbo.Specialty          s   ON  s.SpecialtyId          = sd.SpecialtyId
INNER JOIN dbo.Doctor             d   ON  d.DoctorId             = sd.DoctorId
INNER JOIN dbo.AppointmentStatus  ast ON  ast.AppointmentStatusId = a.AppointmentStatusId
LEFT  JOIN dbo.Sex                sx  ON  sx.SexId               = d.SexId;
GO

-- =============================================================================
-- PASO 5: Appointment_GetByClinic — agrega IsRegistered al resultado
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.Appointment_GetByClinic
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
        Symptoms,
        IsRegistered
    FROM dbo.Appointment_Detail
    WHERE ClinicId = @ClinicId
      AND (@Date IS NULL OR AppointmentDay = @Date)
    ORDER BY AppointmentDate;
END
GO

-- =============================================================================
-- PASO 6: Appointment_GetById — agrega PatientId al resultado
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.Appointment_GetById
    @AppointmentId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        a.AppointmentId,
        a.ClinicId,
        ISNULL(ct.ClinicTypeName + N' ', N'') + c.ClinicName AS ClinicName,
        a.PatientTypeId,
        a.PatientName,
        a.PatientAge,
        a.PatientIdNumber,
        a.SexId              AS PatientSexId,
        sx_p.SexName         AS PatientSexName,
        a.PatientPhone,
        a.PatientAddress,
        a.PatientBirthDate,
        a.PatientStateId,
        st_p.StateName       AS PatientStateName,
        a.ChildGuardianIdNumber,
        a.ChildGuardianName,
        r.RelationshipName,
        a.ChildGuardianPhone,
        a.ChildGuardianAddress,
        a.ChildGuardianBirthDate,
        a.ChildGuardianSexId,
        sx_g.SexName         AS ChildGuardianSexName,
        a.ChildGuardianStateId,
        st_g.StateName       AS ChildGuardianStateName,
        a.ReasonId,
        rsn.ReasonName,
        a.Symptoms,
        s.SpecialtyName,
        ISNULL(sx_d.DoctorAbbreviation + ' ', '') + d.DoctorName AS DoctorName,
        a.AppointmentDate,
        LEFT(CONVERT(VARCHAR(8), CAST(a.AppointmentDate AS TIME), 108), 5) AS AppointmentTime,
        a.AppointmentStatusId,
        ast.AppointmentStatusName,
        a.PatientId
    FROM      dbo.Appointment        a
    INNER JOIN dbo.Clinic             c     ON  c.ClinicId              = a.ClinicId
    LEFT  JOIN dbo.ClinicType         ct    ON  ct.ClinicTypeId         = c.ClinicTypeId
    INNER JOIN dbo.SpecialtyDoctor    sd    ON  sd.SpecialtyDoctorId    = a.SpecialtyDoctorId
    INNER JOIN dbo.Specialty          s     ON  s.SpecialtyId           = sd.SpecialtyId
    INNER JOIN dbo.Doctor             d     ON  d.DoctorId              = sd.DoctorId
    INNER JOIN dbo.AppointmentStatus  ast   ON  ast.AppointmentStatusId = a.AppointmentStatusId
    LEFT  JOIN dbo.Sex                sx_p  ON  sx_p.SexId              = a.SexId
    LEFT  JOIN dbo.Sex                sx_d  ON  sx_d.SexId              = d.SexId
    LEFT  JOIN dbo.Sex                sx_g  ON  sx_g.SexId              = a.ChildGuardianSexId
    LEFT  JOIN dbo.Relationship       r     ON  r.RelationshipId        = a.RelationshipId
    LEFT  JOIN dbo.State              st_p  ON  st_p.StateId            = a.PatientStateId
    LEFT  JOIN dbo.State              st_g  ON  st_g.StateId            = a.ChildGuardianStateId
    LEFT  JOIN dbo.Reason             rsn   ON  rsn.ReasonId            = a.ReasonId
    WHERE a.AppointmentId = @AppointmentId;
END
GO

-- =============================================================================
-- PASO 7: Appointment_GetConfirmedByClinic — solo pacientes registrados
-- Solo muestra citas donde el paciente ya fue registrado en la tabla Patient.
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.Appointment_GetConfirmedByClinic
    @ClinicId INT,
    @DoctorId INT  = NULL,
    @Date     DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TargetDate DATE = ISNULL(@Date, CAST(GETDATE() AS DATE));

    SELECT
        a.AppointmentId,
        a.PatientName,
        ISNULL(a.PatientIdNumber, 0)  AS PatientIdNumber,
        a.PatientAge,
        sd.DoctorId,
        ISNULL(sx.DoctorAbbreviation + N' ', N'') + d.DoctorName  AS DoctorName,
        sp.SpecialtyName,
        r.ReasonName,
        a.AppointmentDate,
        LEFT(CONVERT(VARCHAR(8), CAST(a.AppointmentDate AS TIME), 108), 5) AS AppointmentTime,
        a.AppointmentStatusId,
        ast.AppointmentStatusName,
        a.Symptoms
    FROM       dbo.Appointment       a
    INNER JOIN dbo.AppointmentStatus ast ON ast.AppointmentStatusId = a.AppointmentStatusId
    INNER JOIN dbo.SpecialtyDoctor   sd  ON sd.SpecialtyDoctorId    = a.SpecialtyDoctorId
    INNER JOIN dbo.Doctor            d   ON d.DoctorId              = sd.DoctorId
    INNER JOIN dbo.Specialty         sp  ON sp.SpecialtyId          = sd.SpecialtyId
    LEFT  JOIN dbo.Reason            r   ON r.ReasonId              = a.ReasonId
    LEFT  JOIN dbo.Sex               sx  ON sx.SexId                = d.SexId
    WHERE a.ClinicId             = @ClinicId
      AND CAST(a.AppointmentDate AS DATE) = @TargetDate
      AND a.AppointmentStatusId IN (2, 4)
      AND a.PatientId            IS NOT NULL
      AND (@DoctorId IS NULL OR sd.DoctorId = @DoctorId)
    ORDER BY a.AppointmentDate;
END
GO

-- =============================================================================
-- FIN DEL SCRIPT
-- =============================================================================
