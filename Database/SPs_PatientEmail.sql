-- =============================================================================
-- SPs_PatientEmail.sql
-- Agrega PatientEmail al flujo de pacientes y citas.
--
-- Pre-requisito (ya ejecutado por el usuario):
--   ALTER TABLE dbo.Patient     ADD PatientEmail        VARCHAR(150) NOT NULL;
--   ALTER TABLE dbo.Appointment ADD PatientEmail        VARCHAR(150) NULL;
--   ALTER TABLE dbo.Appointment ADD ChildGuardianEmail  VARCHAR(150) NULL;
--
-- Ejecutar en SSMS sobre la BD Medycally.
-- =============================================================================

-- =============================================================================
-- PASO 1: Vista Patient_GetByIdNumber — incluir PatientEmail
-- =============================================================================
IF OBJECT_ID('dbo.Patient_GetByIdNumber', 'V') IS NOT NULL DROP VIEW dbo.Patient_GetByIdNumber;
GO

CREATE VIEW dbo.Patient_GetByIdNumber AS
SELECT
    p.PatientId,
    p.PatientIdNumber,
    p.PatientName,
    p.PatientEmail,
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
-- PASO 2: Patient_AddOrEdit — acepta @PatientEmail y lo persiste
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.Patient_AddOrEdit
    @PatientId        INT,
    @PatientIdNumber  INT            = NULL,
    @PatientName      NVARCHAR(150),
    @PatientEmail     VARCHAR(150)   = NULL,
    @SexId            INT,
    @PatientBirthdate DATE           = NULL,
    @PatientAddress   NVARCHAR(300)  = NULL,
    @MunicipalityId   INT            = 0,
    @PatientMainPhone BIGINT         = NULL,
    @IsGuardianOnly   BIT            = 0
AS
BEGIN
    SET NOCOUNT ON;

    -- PatientEmail es NOT NULL en la tabla — si llega NULL desde el cliente,
    -- guardamos cadena vacía para no romper el INSERT/UPDATE.
    SET @PatientEmail = ISNULL(@PatientEmail, '');

    IF @PatientId = 0 AND @PatientIdNumber IS NOT NULL
        SELECT @PatientId = PatientId FROM dbo.Patient WHERE PatientIdNumber = @PatientIdNumber;

    IF ISNULL(@PatientId, 0) = 0
    BEGIN
        INSERT INTO dbo.Patient
            (PatientIdNumber, PatientName, PatientEmail, SexId, PatientBirthdate,
             PatientAddress, MunicipalityId, PatientMainPhone, IsGuardianOnly)
        VALUES
            (@PatientIdNumber, @PatientName, @PatientEmail, @SexId, @PatientBirthdate,
             @PatientAddress, NULLIF(@MunicipalityId, 0), @PatientMainPhone, @IsGuardianOnly);
        SELECT CAST(SCOPE_IDENTITY() AS INT);
    END
    ELSE
    BEGIN
        UPDATE dbo.Patient
        SET PatientIdNumber  = ISNULL(@PatientIdNumber, PatientIdNumber),
            PatientName      = @PatientName,
            -- Si llega vacío, conserva el email existente
            PatientEmail     = CASE WHEN LEN(@PatientEmail) = 0 THEN PatientEmail ELSE @PatientEmail END,
            SexId            = @SexId,
            PatientBirthdate = @PatientBirthdate,
            PatientAddress   = @PatientAddress,
            MunicipalityId   = NULLIF(@MunicipalityId, 0),
            PatientMainPhone = @PatientMainPhone,
            IsGuardianOnly   = CASE WHEN IsGuardianOnly = 0 THEN 0 ELSE @IsGuardianOnly END
        WHERE PatientId = @PatientId;
        SELECT @PatientId;
    END
END
GO

-- =============================================================================
-- PASO 3: Patient_GetAll — incluir PatientEmail en el resultado
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
        p.PatientEmail,
        p.SexId,
        sx.SexName,
        p.PatientBirthdate,
        CASE
            WHEN p.PatientBirthdate IS NULL THEN 0
            ELSE DATEDIFF(YEAR, p.PatientBirthdate, GETDATE())
                 - CASE WHEN DATEADD(YEAR, DATEDIFF(YEAR, p.PatientBirthdate, GETDATE()), p.PatientBirthdate) > GETDATE()
                        THEN 1 ELSE 0 END
        END                                                                  AS Age,
        ISNULL(p.PatientMainPhone, (
            SELECT TOP 1 g.PatientMainPhone
            FROM   dbo.PatientGuardian pg2
            JOIN   dbo.Patient         g ON g.PatientId = pg2.GuardianPatientId
            WHERE  pg2.PatientId = p.PatientId
              AND  g.PatientMainPhone IS NOT NULL
        ))                                                                   AS PatientMainPhone,
        CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.PatientGuardian pg3
                               WHERE pg3.PatientId = p.PatientId)
                  THEN 1 ELSE 0 END AS BIT)                                  AS HasGuardian,
        p.PatientAddress,
        p.MunicipalityId,
        ISNULL(m.MunicipalityName, '')                                       AS MunicipalityName,
        ISNULL(m.StateId, 0)                                                 AS StateId,
        ISNULL(s.StateName, '')                                              AS StateName,
        (SELECT COUNT(*) FROM dbo.PatientGuardian pg
         WHERE pg.PatientId = p.PatientId
            OR pg.GuardianPatientId = p.PatientId)                           AS FamilyCount
    FROM      dbo.Patient       p
    LEFT JOIN dbo.Sex           sx ON sx.SexId         = p.SexId
    LEFT JOIN dbo.Municipality  m  ON m.MunicipalityId = p.MunicipalityId
    LEFT JOIN dbo.State         s  ON s.StateId        = m.StateId
    WHERE p.IsGuardianOnly = 0
      AND (
           @Search IS NULL
        OR p.PatientName    LIKE '%' + @Search + '%'
        OR p.PatientEmail   LIKE '%' + @Search + '%'
        OR CAST(p.PatientIdNumber AS VARCHAR(20)) LIKE '%' + @Search + '%'
      )
    ORDER BY p.PatientName;
END
GO

-- =============================================================================
-- PASO 4: Patient_GetFamily — incluir PatientEmail tanto en guardian como dependent
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.Patient_GetFamily
    @PatientId INT
AS
BEGIN
    SET NOCOUNT ON;
    -- Representantes del paciente
    SELECT
        'guardian'                       AS Role,
        g.PatientId,
        g.PatientIdNumber,
        g.PatientName,
        g.PatientEmail,
        g.SexId,
        sx.SexName,
        pg.RelationshipId,
        r.RelationshipName,
        g.PatientMainPhone,
        g.PatientBirthdate,
        g.PatientAddress,
        ISNULL(mu.MunicipalityId, 0)     AS MunicipalityId,
        ISNULL(mu.MunicipalityName, '')  AS MunicipalityName,
        ISNULL(mu.StateId, 0)            AS StateId,
        ISNULL(st.StateName, '')         AS StateName
    FROM       dbo.PatientGuardian  pg
    JOIN       dbo.Patient          g   ON  g.PatientId        = pg.GuardianPatientId
    LEFT JOIN  dbo.Sex              sx  ON  sx.SexId           = g.SexId
    LEFT JOIN  dbo.Relationship     r   ON  r.RelationshipId   = pg.RelationshipId
    LEFT JOIN  dbo.Municipality     mu  ON  mu.MunicipalityId  = g.MunicipalityId
    LEFT JOIN  dbo.State            st  ON  st.StateId         = mu.StateId
    WHERE pg.PatientId = @PatientId
    UNION ALL
    -- Dependientes
    SELECT
        'dependent'                      AS Role,
        d.PatientId,
        d.PatientIdNumber,
        d.PatientName,
        d.PatientEmail,
        d.SexId,
        sx.SexName,
        pg.RelationshipId,
        r.RelationshipName,
        d.PatientMainPhone,
        d.PatientBirthdate,
        d.PatientAddress,
        ISNULL(mu.MunicipalityId, 0)     AS MunicipalityId,
        ISNULL(mu.MunicipalityName, '')  AS MunicipalityName,
        ISNULL(mu.StateId, 0)            AS StateId,
        ISNULL(st.StateName, '')         AS StateName
    FROM       dbo.PatientGuardian  pg
    JOIN       dbo.Patient          d   ON  d.PatientId        = pg.PatientId
    LEFT JOIN  dbo.Sex              sx  ON  sx.SexId           = d.SexId
    LEFT JOIN  dbo.Relationship     r   ON  r.RelationshipId   = pg.RelationshipId
    LEFT JOIN  dbo.Municipality     mu  ON  mu.MunicipalityId  = d.MunicipalityId
    LEFT JOIN  dbo.State            st  ON  st.StateId         = mu.StateId
    WHERE pg.GuardianPatientId = @PatientId;
END
GO

-- =============================================================================
-- PASO 5: Appointment_AddOrEdit — acepta @PatientEmail y @ChildGuardianEmail
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
    @PatientEmail           VARCHAR(150)  = NULL,
    @PatientAddress         NVARCHAR(300) = NULL,
    @PatientStateId         INT           = NULL,
    @PatientBirthDate       DATE          = NULL,
    @ChildGuardianPhone     NVARCHAR(20)  = NULL,
    @ChildGuardianEmail     VARCHAR(150)  = NULL,
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
            PatientPhone, PatientEmail, PatientAddress, PatientStateId, PatientBirthDate,
            ChildGuardianPhone, ChildGuardianEmail, ChildGuardianAddress, ChildGuardianStateId,
            ChildGuardianBirthDate, ChildGuardianSexId, ReasonId, PatientId
        )
        VALUES (
            @ClinicId, @PatientTypeId, @PatientAge, @PatientIdNumber,
            @ChildGuardianIdNumber, @ChildGuardianName, @RelationshipId,
            @PatientName, @SexId, @SpecialtyDoctorId, @AppointmentDate,
            @Symptoms, @AppointmentStatusId,
            @PatientPhone, @PatientEmail, @PatientAddress, @PatientStateId, @PatientBirthDate,
            @ChildGuardianPhone, @ChildGuardianEmail, @ChildGuardianAddress, @ChildGuardianStateId,
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
            PatientEmail           = @PatientEmail,
            PatientAddress         = @PatientAddress,
            PatientStateId         = @PatientStateId,
            PatientBirthDate       = @PatientBirthDate,
            ChildGuardianPhone     = @ChildGuardianPhone,
            ChildGuardianEmail     = @ChildGuardianEmail,
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
-- PASO 6: Appointment_GetById — devolver emails para pre-llenar modal de registro
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
        a.PatientEmail,
        a.PatientAddress,
        a.PatientBirthDate,
        a.PatientStateId,
        st_p.StateName       AS PatientStateName,
        a.ChildGuardianIdNumber,
        a.ChildGuardianName,
        r.RelationshipName,
        a.ChildGuardianPhone,
        a.ChildGuardianEmail,
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
-- FIN DEL SCRIPT
-- =============================================================================
