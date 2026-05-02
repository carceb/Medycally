-- =============================================================================
-- Combina ClinicType.ClinicTypeName + Clinic.ClinicName en todos los lugares
-- donde se muestra el nombre de la clínica.
-- Ejecutar en SSMS sobre la BD de Medycally después de ClinicType_Setup.sql.
-- =============================================================================

-- =============================================================================
-- PASO 1: Clinic_GetAll — agrega ClinicTypeName al resultado
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.Clinic_GetAll
AS
BEGIN
    SET NOCOUNT ON;

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
    LEFT JOIN  dbo.Municipality  m   ON  m.MunicipalityId = c.MunicipalityId
    LEFT JOIN  dbo.State         s   ON  s.StateId        = m.StateId
    LEFT JOIN  dbo.ClinicType    ct  ON  ct.ClinicTypeId  = c.ClinicTypeId
    ORDER BY c.ClinicName;
END
GO

-- =============================================================================
-- PASO 2: Appointment_Detail VIEW
-- Combina ClinicTypeName + ClinicName en el campo ClinicName devuelto.
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
    a.Symptoms
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
-- PASO 3: Appointment_GetById — combina ClinicTypeName + ClinicName
-- (versión completa con PatientSexId, PatientStateId, ChildGuardianSexId,
--  ChildGuardianStateId añadidos en SPs_Patient.sql)
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
        ast.AppointmentStatusName
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
