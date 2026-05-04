-- =============================================================================
-- Fix: Diferenciar representantes de pacientes
-- Ejecutar en SSMS sobre la BD de Medycally.
-- =============================================================================

-- PASO 1: Hacer nullable columnas opcionales
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Patient') AND name = 'PatientBirthdate' AND is_nullable = 0)
    ALTER TABLE dbo.Patient ALTER COLUMN PatientBirthdate DATE NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Patient') AND name = 'PatientAddress' AND is_nullable = 0)
    ALTER TABLE dbo.Patient ALTER COLUMN PatientAddress NVARCHAR(300) NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Patient') AND name = 'MunicipalityId' AND is_nullable = 0)
    ALTER TABLE dbo.Patient ALTER COLUMN MunicipalityId INT NULL;
GO

-- PASO 2: Agregar columna IsGuardianOnly (0 = paciente, 1 = solo representante)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Patient') AND name = 'IsGuardianOnly'
)
    ALTER TABLE dbo.Patient ADD IsGuardianOnly BIT NOT NULL DEFAULT 0;
GO

-- PASO 3: Patient_GetAll — solo devuelve pacientes (IsGuardianOnly = 0)
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
        CASE
            WHEN p.PatientBirthdate IS NULL THEN 0
            ELSE DATEDIFF(YEAR, p.PatientBirthdate, GETDATE())
                 - CASE WHEN DATEADD(YEAR, DATEDIFF(YEAR, p.PatientBirthdate, GETDATE()), p.PatientBirthdate) > GETDATE()
                        THEN 1 ELSE 0 END
        END                                                          AS Age,
        p.PatientMainPhone,
        p.PatientAddress,
        p.MunicipalityId,
        ISNULL(m.MunicipalityName, '')                               AS MunicipalityName,
        ISNULL(m.StateId, 0)                                         AS StateId,
        ISNULL(s.StateName, '')                                      AS StateName,
        (SELECT COUNT(*) FROM dbo.PatientGuardian pg
         WHERE pg.PatientId = p.PatientId
            OR pg.GuardianPatientId = p.PatientId)                   AS FamilyCount
    FROM      dbo.Patient       p
    LEFT JOIN dbo.Sex           sx ON sx.SexId         = p.SexId
    LEFT JOIN dbo.Municipality  m  ON m.MunicipalityId = p.MunicipalityId
    LEFT JOIN dbo.State         s  ON s.StateId        = m.StateId
    WHERE p.IsGuardianOnly = 0
      AND (
           @Search IS NULL
        OR p.PatientName LIKE '%' + @Search + '%'
        OR CAST(p.PatientIdNumber AS VARCHAR(20)) LIKE '%' + @Search + '%'
      )
    ORDER BY p.PatientName;
END
GO

-- PASO 4: Patient_AddOrEdit — upsert por cédula + IsGuardianOnly + Birthdate nullable
CREATE OR ALTER PROCEDURE dbo.Patient_AddOrEdit
    @PatientId        INT,
    @PatientIdNumber  INT            = NULL,
    @PatientName      NVARCHAR(150),
    @SexId            INT,
    @PatientBirthdate DATE           = NULL,
    @PatientAddress   NVARCHAR(300)  = NULL,
    @MunicipalityId   INT            = 0,
    @PatientMainPhone BIGINT         = NULL,
    @IsGuardianOnly   BIT            = 0
AS
BEGIN
    SET NOCOUNT ON;

    -- Upsert: si no se pasó PatientId, buscar por cédula para evitar duplicados
    IF @PatientId = 0 AND @PatientIdNumber IS NOT NULL
        SELECT @PatientId = PatientId FROM dbo.Patient WHERE PatientIdNumber = @PatientIdNumber;

    IF ISNULL(@PatientId, 0) = 0
    BEGIN
        INSERT INTO dbo.Patient
            (PatientIdNumber, PatientName, SexId, PatientBirthdate,
             PatientAddress, MunicipalityId, PatientMainPhone, IsGuardianOnly)
        VALUES
            (@PatientIdNumber, @PatientName, @SexId, @PatientBirthdate,
             @PatientAddress, NULLIF(@MunicipalityId, 0), @PatientMainPhone, @IsGuardianOnly);
        SELECT CAST(SCOPE_IDENTITY() AS INT);
    END
    ELSE
    BEGIN
        UPDATE dbo.Patient
        SET PatientIdNumber  = ISNULL(@PatientIdNumber, PatientIdNumber),
            PatientName      = @PatientName,
            SexId            = @SexId,
            PatientBirthdate = @PatientBirthdate,
            PatientAddress   = @PatientAddress,
            MunicipalityId   = NULLIF(@MunicipalityId, 0),
            PatientMainPhone = @PatientMainPhone,
            -- No degradar un paciente real (0) a solo-representante (1)
            IsGuardianOnly   = CASE WHEN IsGuardianOnly = 0 THEN 0 ELSE @IsGuardianOnly END
        WHERE PatientId = @PatientId;
        SELECT @PatientId;
    END
END
GO

-- PASO 5: Marcar registros existentes que son solo representantes
-- Un representante existente aparece en PatientGuardian.GuardianPatientId
-- pero nunca como paciente directo (PatientGuardian.PatientId).
UPDATE dbo.Patient
SET IsGuardianOnly = 1
WHERE PatientId IN (
    SELECT DISTINCT GuardianPatientId FROM dbo.PatientGuardian
)
AND PatientId NOT IN (
    SELECT DISTINCT PatientId FROM dbo.PatientGuardian
);
GO

-- =============================================================================
-- PASO 6: Fix PatientMainPhone — asegurar que el SP use BIGINT
--         Ejecutar si el teléfono no se guarda correctamente (núms venezolanos
--         de 11 dígitos > INT MAX = 2.147.483.647).
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.Patient_AddOrEdit
    @PatientId        INT,
    @PatientIdNumber  INT            = NULL,
    @PatientName      NVARCHAR(150),
    @SexId            INT,
    @PatientBirthdate DATE           = NULL,
    @PatientAddress   NVARCHAR(300)  = NULL,
    @MunicipalityId   INT            = 0,
    @PatientMainPhone BIGINT         = NULL,
    @IsGuardianOnly   BIT            = 0
AS
BEGIN
    SET NOCOUNT ON;

    IF @PatientId = 0 AND @PatientIdNumber IS NOT NULL
        SELECT @PatientId = PatientId FROM dbo.Patient WHERE PatientIdNumber = @PatientIdNumber;

    IF ISNULL(@PatientId, 0) = 0
    BEGIN
        INSERT INTO dbo.Patient
            (PatientIdNumber, PatientName, SexId, PatientBirthdate,
             PatientAddress, MunicipalityId, PatientMainPhone, IsGuardianOnly)
        VALUES
            (@PatientIdNumber, @PatientName, @SexId, @PatientBirthdate,
             @PatientAddress, NULLIF(@MunicipalityId, 0), @PatientMainPhone, @IsGuardianOnly);
        SELECT CAST(SCOPE_IDENTITY() AS INT);
    END
    ELSE
    BEGIN
        UPDATE dbo.Patient
        SET PatientIdNumber  = ISNULL(@PatientIdNumber, PatientIdNumber),
            PatientName      = @PatientName,
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
-- PASO 7: Patient_GetAll — teléfono del representante como fallback para menores
--         Si el menor no tiene PatientMainPhone propio, devuelve el teléfono
--         de su representante (primer representante con teléfono registrado).
--         Ejecutar en SSMS.
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
        OR CAST(p.PatientIdNumber AS VARCHAR(20)) LIKE '%' + @Search + '%'
      )
    ORDER BY p.PatientName;
END
GO

-- =============================================================================
-- PASO 8: Patient_GetFamily — incluye datos completos (teléfono, fechas,
--         dirección, estado, municipio, SexId, RelationshipId) para poder
--         pre-llenar el modal de edición de menores.
--         Ejecutar en SSMS.
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
