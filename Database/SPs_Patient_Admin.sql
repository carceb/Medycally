-- =============================================================================
-- Administración de Pacientes — SPs para el módulo de gestión
-- Ejecutar en SSMS sobre la BD de Medycally.
-- =============================================================================

-- =============================================================================
-- PASO 1: Patient_GetAll — lista todos los pacientes con datos de display
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.Patient_GetAll
    @Search VARCHAR(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        p.PatientId,
        p.PatientIdNumber,
        p.PatientName,
        p.SexId,
        sx.SexName,
        p.PatientBirthdate,
        DATEDIFF(YEAR, p.PatientBirthdate, GETDATE())
            - CASE WHEN DATEADD(YEAR, DATEDIFF(YEAR, p.PatientBirthdate, GETDATE()), p.PatientBirthdate) > GETDATE()
                   THEN 1 ELSE 0 END                     AS Age,
        p.PatientMainPhone,
        p.PatientAddress,
        p.MunicipalityId,
        ISNULL(m.MunicipalityName, '')                   AS MunicipalityName,
        ISNULL(m.StateId, 0)                             AS StateId,
        ISNULL(s.StateName, '')                          AS StateName,
        (SELECT COUNT(*) FROM dbo.PatientGuardian pg
         WHERE pg.PatientId = p.PatientId
            OR pg.GuardianPatientId = p.PatientId)       AS FamilyCount
    FROM      dbo.Patient       p
    LEFT JOIN dbo.Sex           sx ON sx.SexId          = p.SexId
    LEFT JOIN dbo.Municipality  m  ON m.MunicipalityId  = p.MunicipalityId
    LEFT JOIN dbo.State         s  ON s.StateId         = m.StateId
    WHERE @Search IS NULL
       OR p.PatientName    LIKE '%' + @Search + '%'
       OR CAST(p.PatientIdNumber AS VARCHAR(20)) LIKE '%' + @Search + '%'
    ORDER BY p.PatientName;
END
GO

-- =============================================================================
-- PASO 2: Patient_GetFamily — representantes y dependientes de un paciente
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.Patient_GetFamily
    @PatientId INT
AS
BEGIN
    SET NOCOUNT ON;
    -- Representantes del paciente
    SELECT
        'guardian'          AS Role,
        g.PatientId,
        g.PatientIdNumber,
        g.PatientName,
        sx.SexName,
        r.RelationshipName
    FROM       dbo.PatientGuardian  pg
    JOIN       dbo.Patient          g   ON  g.PatientId        = pg.GuardianPatientId
    LEFT JOIN  dbo.Sex              sx  ON  sx.SexId           = g.SexId
    LEFT JOIN  dbo.Relationship     r   ON  r.RelationshipId   = pg.RelationshipId
    WHERE pg.PatientId = @PatientId
    UNION ALL
    -- Dependientes (pacientes donde esta persona es representante)
    SELECT
        'dependent'         AS Role,
        d.PatientId,
        d.PatientIdNumber,
        d.PatientName,
        sx.SexName,
        r.RelationshipName
    FROM       dbo.PatientGuardian  pg
    JOIN       dbo.Patient          d   ON  d.PatientId        = pg.PatientId
    LEFT JOIN  dbo.Sex              sx  ON  sx.SexId           = d.SexId
    LEFT JOIN  dbo.Relationship     r   ON  r.RelationshipId   = pg.RelationshipId
    WHERE pg.GuardianPatientId = @PatientId;
END
GO

-- =============================================================================
-- PASO 3: Patient_Delete — elimina paciente y sus vínculos familiares
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.Patient_Delete
    @PatientId INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.PatientGuardian
    WHERE PatientId = @PatientId OR GuardianPatientId = @PatientId;
    DELETE FROM dbo.Patient WHERE PatientId = @PatientId;
END
GO

-- =============================================================================
-- PASO 4: PatientGuardian_Delete — desvincula representante de dependiente
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.PatientGuardian_Delete
    @PatientId         INT,
    @GuardianPatientId INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.PatientGuardian
    WHERE PatientId = @PatientId AND GuardianPatientId = @GuardianPatientId;
END
GO
