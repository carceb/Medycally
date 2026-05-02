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
