-- ============================================================
-- ClinicSpecialtyFee: tabla y stored procedures
-- Ejecutar en SSMS (idempotente)
-- ============================================================

-- PASO 1: Tabla ClinicSpecialtyFee
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ClinicSpecialtyFee')
BEGIN
    CREATE TABLE dbo.ClinicSpecialtyFee (
        ClinicSpecialtyFeeId INT           IDENTITY(1,1) NOT NULL,
        ClinicId             INT           NOT NULL,
        SpecialtyId          INT           NOT NULL,
        FeeUSD               DECIMAL(10,2) NOT NULL,
        FeeVES               DECIMAL(18,2) NOT NULL,
        ExchangeRateUsed     DECIMAL(10,4) NOT NULL,
        UpdatedAt            DATETIME      NOT NULL DEFAULT GETDATE(),
        UpdatedByUserId      INT           NULL,
        CONSTRAINT PK_ClinicSpecialtyFee   PRIMARY KEY CLUSTERED (ClinicSpecialtyFeeId),
        CONSTRAINT UQ_ClinicSpecialtyFee   UNIQUE (ClinicId, SpecialtyId),
        CONSTRAINT FK_CSF_Clinic           FOREIGN KEY (ClinicId)    REFERENCES dbo.Clinic(ClinicId),
        CONSTRAINT FK_CSF_Specialty        FOREIGN KEY (SpecialtyId) REFERENCES dbo.Specialty(SpecialtyId)
    );
END
GO

-- PASO 2: Especialidades de la clinica con su tarifa (si existe)
-- Retorna todas las especialidades que ofrecen los medicos asignados
-- a la clinica, junto con la tarifa configurada (NULL si no se ha fijado).
CREATE OR ALTER PROCEDURE dbo.ClinicSpecialtyFee_GetByClinic
    @ClinicId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DISTINCT
        s.SpecialtyId,
        s.SpecialtyName,
        ISNULL(csf.ClinicSpecialtyFeeId, 0) AS ClinicSpecialtyFeeId,
        ISNULL(csf.FeeUSD, 0)               AS FeeUSD,
        ISNULL(csf.FeeVES, 0)               AS FeeVES,
        ISNULL(csf.ExchangeRateUsed, 0)     AS ExchangeRateUsed,
        csf.UpdatedAt
    FROM       dbo.Specialty          s
    INNER JOIN dbo.SpecialtyDoctor     sd  ON  sd.SpecialtyId = s.SpecialtyId
    INNER JOIN dbo.ClinicDoctor        cd  ON  cd.DoctorId    = sd.DoctorId
                                           AND cd.ClinicId    = @ClinicId
    LEFT JOIN  dbo.ClinicSpecialtyFee  csf ON  csf.SpecialtyId = s.SpecialtyId
                                           AND csf.ClinicId    = @ClinicId
    ORDER BY s.SpecialtyName;
END
GO

-- PASO 3: Upsert tarifa para una especialidad en una clinica
-- Retorna el ClinicSpecialtyFeeId resultante.
CREATE OR ALTER PROCEDURE dbo.ClinicSpecialtyFee_Save
    @ClinicId         INT,
    @SpecialtyId      INT,
    @FeeUSD           DECIMAL(10,2),
    @FeeVES           DECIMAL(18,2),
    @ExchangeRateUsed DECIMAL(10,4),
    @UpdatedByUserId  INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.ClinicSpecialtyFee
               WHERE ClinicId = @ClinicId AND SpecialtyId = @SpecialtyId)
    BEGIN
        UPDATE dbo.ClinicSpecialtyFee
        SET    FeeUSD           = @FeeUSD,
               FeeVES           = @FeeVES,
               ExchangeRateUsed = @ExchangeRateUsed,
               UpdatedAt        = GETDATE(),
               UpdatedByUserId  = @UpdatedByUserId
        WHERE  ClinicId    = @ClinicId
          AND  SpecialtyId = @SpecialtyId;

        SELECT ClinicSpecialtyFeeId
        FROM   dbo.ClinicSpecialtyFee
        WHERE  ClinicId = @ClinicId AND SpecialtyId = @SpecialtyId;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.ClinicSpecialtyFee
            (ClinicId, SpecialtyId, FeeUSD, FeeVES, ExchangeRateUsed, UpdatedByUserId)
        VALUES
            (@ClinicId, @SpecialtyId, @FeeUSD, @FeeVES, @ExchangeRateUsed, @UpdatedByUserId);

        SELECT CAST(SCOPE_IDENTITY() AS INT);
    END
END
GO
