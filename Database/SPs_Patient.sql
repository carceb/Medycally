-- =============================================================================
-- SPs_Patient.sql
-- Reestructura Patient + nueva tabla PatientGuardian
-- Ejecutar en SSMS sobre la BD Medycally
-- =============================================================================

-- =============================================================================
-- PASO 1: Reestructurar tabla Patient
--         Eliminar campos del representante y ajustar tipos
-- =============================================================================

-- 1a) PatientIdNumber pasa a NULL (menores sin cédula)
ALTER TABLE dbo.Patient ALTER COLUMN PatientIdNumber INT NULL;
GO

-- 1b) PatientMainPhone a BIGINT (11 dígitos Venezuela)
ALTER TABLE dbo.Patient ALTER COLUMN PatientMainPhone BIGINT NULL;
GO

-- 1c) Eliminar DEFAULT CONSTRAINTS conocidos antes de borrar las columnas
IF OBJECT_ID('DF_Patient_PatientTypeId',         'D') IS NOT NULL ALTER TABLE dbo.Patient DROP CONSTRAINT DF_Patient_PatientTypeId;
GO
IF OBJECT_ID('DF_Patient_ChildGuardianIdNumber', 'D') IS NOT NULL ALTER TABLE dbo.Patient DROP CONSTRAINT DF_Patient_ChildGuardianIdNumber;
GO
IF OBJECT_ID('DF_Patient_ChildGuardianName',     'D') IS NOT NULL ALTER TABLE dbo.Patient DROP CONSTRAINT DF_Patient_ChildGuardianName;
GO
IF OBJECT_ID('DF_Patient_RelationshipId',        'D') IS NOT NULL ALTER TABLE dbo.Patient DROP CONSTRAINT DF_Patient_RelationshipId;
GO

-- 1d) Eliminar columnas de representante
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Patient') AND name='PatientTypeId')
    ALTER TABLE dbo.Patient DROP COLUMN PatientTypeId;
GO
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Patient') AND name='ChildGuardianIdNumber')
    ALTER TABLE dbo.Patient DROP COLUMN ChildGuardianIdNumber;
GO
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Patient') AND name='ChildGuardianName')
    ALTER TABLE dbo.Patient DROP COLUMN ChildGuardianName;
GO
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Patient') AND name='RelationshipId')
    ALTER TABLE dbo.Patient DROP COLUMN RelationshipId;
GO

-- =============================================================================
-- PASO 2: Tabla PatientGuardian
--         Vincula un menor (PatientId) con su representante (GuardianPatientId),
--         ambos son registros en la tabla Patient.
-- =============================================================================
IF OBJECT_ID('dbo.PatientGuardian', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PatientGuardian (
        PatientGuardianId  INT IDENTITY(1,1) PRIMARY KEY,
        PatientId          INT NOT NULL REFERENCES dbo.Patient(PatientId),
        GuardianPatientId  INT NOT NULL REFERENCES dbo.Patient(PatientId),
        RelationshipId     INT NOT NULL REFERENCES dbo.Relationship(RelationshipId),
        CONSTRAINT UQ_PatientGuardian UNIQUE (PatientId, GuardianPatientId)
    );
END
GO

-- =============================================================================
-- PASO 3: Vista Patient_GetByIdNumber (actualizada para nueva estructura)
-- =============================================================================
IF OBJECT_ID('dbo.Patient_GetByIdNumber', 'V') IS NOT NULL DROP VIEW dbo.Patient_GetByIdNumber;
GO

CREATE VIEW dbo.Patient_GetByIdNumber AS
SELECT
    p.PatientId,
    p.PatientIdNumber,
    p.PatientName,
    p.SexId,
    sx.SexName,
    p.PatientBirthdate,
    p.PatientAddress,
    p.MunicipalityId,
    m.MunicipalityName,
    m.StateId,
    st.StateName,
    p.PatientMainPhone
FROM      dbo.Patient      p
LEFT JOIN dbo.Sex          sx ON sx.SexId          = p.SexId
LEFT JOIN dbo.Municipality m  ON m.MunicipalityId  = p.MunicipalityId
LEFT JOIN dbo.State        st ON st.StateId        = m.StateId;
GO

-- =============================================================================
-- PASO 4: Patient_AddOrEdit (simplificado, sin campos de representante)
--         Lógica de upsert:
--         - Si @PatientId = 0 y @PatientIdNumber no es NULL → busca por cédula,
--           actualiza si existe o inserta si no.
--         - Si @PatientId = 0 y @PatientIdNumber es NULL → siempre inserta (menor sin cédula).
--         - Si @PatientId > 0 → actualiza ese registro.
-- =============================================================================
IF OBJECT_ID('dbo.Patient_AddOrEdit', 'P') IS NOT NULL DROP PROCEDURE dbo.Patient_AddOrEdit;
GO

CREATE PROCEDURE dbo.Patient_AddOrEdit
    @PatientId        INT,
    @PatientIdNumber  INT           = NULL,
    @PatientName      VARCHAR(120),
    @SexId            INT,
    @PatientBirthdate SMALLDATETIME,
    @PatientAddress   VARCHAR(150)  = NULL,
    @MunicipalityId   INT,
    @PatientMainPhone BIGINT        = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @PatientId = 0 AND @PatientIdNumber IS NOT NULL
        SELECT @PatientId = PatientId FROM dbo.Patient WHERE PatientIdNumber = @PatientIdNumber;

    IF ISNULL(@PatientId, 0) = 0
    BEGIN
        INSERT INTO dbo.Patient (PatientIdNumber, PatientName, SexId, PatientBirthdate,
                                  PatientAddress, MunicipalityId, PatientMainPhone)
        VALUES (@PatientIdNumber, @PatientName, @SexId, @PatientBirthdate,
                @PatientAddress, @MunicipalityId, @PatientMainPhone);
        SELECT SCOPE_IDENTITY() AS PatientId;
    END
    ELSE
    BEGIN
        UPDATE dbo.Patient
        SET PatientIdNumber  = ISNULL(@PatientIdNumber, PatientIdNumber),
            PatientName      = @PatientName,
            SexId            = @SexId,
            PatientBirthdate = @PatientBirthdate,
            PatientAddress   = @PatientAddress,
            MunicipalityId   = @MunicipalityId,
            PatientMainPhone = @PatientMainPhone
        WHERE PatientId = @PatientId;
        SELECT @PatientId AS PatientId;
    END
END
GO

-- =============================================================================
-- PASO 5: PatientGuardian_Save
--         Inserta o actualiza el vínculo entre menor y representante.
-- =============================================================================
IF OBJECT_ID('dbo.PatientGuardian_Save', 'P') IS NOT NULL DROP PROCEDURE dbo.PatientGuardian_Save;
GO

CREATE PROCEDURE dbo.PatientGuardian_Save
    @PatientId         INT,
    @GuardianPatientId INT,
    @RelationshipId    INT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM dbo.PatientGuardian
               WHERE PatientId = @PatientId AND GuardianPatientId = @GuardianPatientId)
        UPDATE dbo.PatientGuardian
        SET RelationshipId = @RelationshipId
        WHERE PatientId = @PatientId AND GuardianPatientId = @GuardianPatientId;
    ELSE
        INSERT INTO dbo.PatientGuardian (PatientId, GuardianPatientId, RelationshipId)
        VALUES (@PatientId, @GuardianPatientId, @RelationshipId);
END
GO

-- =============================================================================
-- PASO 6: Appointment_GetById — agrega PatientSexId, PatientStateId,
--         ChildGuardianSexId, ChildGuardianStateId para pre-llenar el
--         formulario de registro de paciente.
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
-- FIN DEL SCRIPT
-- =============================================================================
