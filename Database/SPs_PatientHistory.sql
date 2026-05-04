-- =============================================================================
-- Historia Médica del Paciente — Antecedentes estáticos
-- Ejecutar en SSMS sobre la BD de Medycally.
-- =============================================================================

-- PASO 1: Crear tabla PatientHistory (1:1 con Patient)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PatientHistory' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.PatientHistory (
        PatientHistoryId       INT IDENTITY(1,1) NOT NULL,
        PatientId              INT NOT NULL,

        -- Antecedentes Médicos Personales
        HasHypertension        BIT NOT NULL DEFAULT 0,
        HasDiabetes            BIT NOT NULL DEFAULT 0,
        HasHeartDisease        BIT NOT NULL DEFAULT 0,
        HasAsthmaRespiratory   BIT NOT NULL DEFAULT 0,
        HasThyroidDisease      BIT NOT NULL DEFAULT 0,
        HasRenalDisease        BIT NOT NULL DEFAULT 0,
        HasAnxietyDepression   BIT NOT NULL DEFAULT 0,
        HasCancer              BIT NOT NULL DEFAULT 0,
        CancerDetail           NVARCHAR(200) NULL,
        PreviousSurgeries      NVARCHAR(500) NULL,
        RecentHospitalizations NVARCHAR(500) NULL,

        -- Alergias
        DrugAllergies          NVARCHAR(300) NULL,
        FoodAllergies          NVARCHAR(300) NULL,
        OtherAllergies         NVARCHAR(300) NULL,

        -- Medicamentos Actuales
        CurrentMedications     NVARCHAR(1000) NULL,

        -- Antecedentes Familiares
        FamilyDiabetes         BIT NOT NULL DEFAULT 0,
        FamilyCancer           BIT NOT NULL DEFAULT 0,
        FamilyHeartDisease     BIT NOT NULL DEFAULT 0,
        FamilyHypertension     BIT NOT NULL DEFAULT 0,
        FamilyOthers           NVARCHAR(300) NULL,

        -- Hábitos y Estilo de Vida
        -- SmokingStatus: 0=Nunca, 1=Ex-fumador, 2=Activo
        SmokingStatus          TINYINT NOT NULL DEFAULT 0,
        SmokingDailyAmount     NVARCHAR(50) NULL,
        -- AlcoholStatus: 0=Nunca, 1=Ocasional, 2=Frecuente
        AlcoholStatus          TINYINT NOT NULL DEFAULT 0,
        -- PhysicalActivity: 0=Sedentario, 1=Moderado, 2=Activo
        PhysicalActivity       TINYINT NOT NULL DEFAULT 0,
        DrugUse                BIT NOT NULL DEFAULT 0,

        -- Revisión por Sistemas
        HasWeightLoss          BIT NOT NULL DEFAULT 0,
        HasFrequentFever       BIT NOT NULL DEFAULT 0,
        HasHeadache            BIT NOT NULL DEFAULT 0,
        HasVisionHearingChange BIT NOT NULL DEFAULT 0,
        HasBreathingDifficulty BIT NOT NULL DEFAULT 0,

        -- Metadatos
        LastUpdated            DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedByUserId        INT NULL,

        CONSTRAINT PK_PatientHistory        PRIMARY KEY (PatientHistoryId),
        CONSTRAINT UQ_PatientHistory_Patient UNIQUE (PatientId),
        CONSTRAINT FK_PatientHistory_Patient FOREIGN KEY (PatientId) REFERENCES dbo.Patient(PatientId)
    );
    PRINT 'Tabla PatientHistory creada.';
END
ELSE
    PRINT 'Tabla PatientHistory ya existe.';
GO

-- PASO 2: PatientHistory_GetByPatientId
CREATE OR ALTER PROCEDURE dbo.PatientHistory_GetByPatientId
    @PatientId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        PatientHistoryId,
        PatientId,
        HasHypertension,
        HasDiabetes,
        HasHeartDisease,
        HasAsthmaRespiratory,
        HasThyroidDisease,
        HasRenalDisease,
        HasAnxietyDepression,
        HasCancer,
        CancerDetail,
        PreviousSurgeries,
        RecentHospitalizations,
        DrugAllergies,
        FoodAllergies,
        OtherAllergies,
        CurrentMedications,
        FamilyDiabetes,
        FamilyCancer,
        FamilyHeartDisease,
        FamilyHypertension,
        FamilyOthers,
        SmokingStatus,
        SmokingDailyAmount,
        AlcoholStatus,
        PhysicalActivity,
        DrugUse,
        HasWeightLoss,
        HasFrequentFever,
        HasHeadache,
        HasVisionHearingChange,
        HasBreathingDifficulty,
        LastUpdated,
        UpdatedByUserId
    FROM dbo.PatientHistory
    WHERE PatientId = @PatientId;
END
GO

-- PASO 3: PatientHistory_Save (upsert)
CREATE OR ALTER PROCEDURE dbo.PatientHistory_Save
    @PatientId              INT,
    @HasHypertension        BIT,
    @HasDiabetes            BIT,
    @HasHeartDisease        BIT,
    @HasAsthmaRespiratory   BIT,
    @HasThyroidDisease      BIT,
    @HasRenalDisease        BIT,
    @HasAnxietyDepression   BIT,
    @HasCancer              BIT,
    @CancerDetail           NVARCHAR(200)  = NULL,
    @PreviousSurgeries      NVARCHAR(500)  = NULL,
    @RecentHospitalizations NVARCHAR(500)  = NULL,
    @DrugAllergies          NVARCHAR(300)  = NULL,
    @FoodAllergies          NVARCHAR(300)  = NULL,
    @OtherAllergies         NVARCHAR(300)  = NULL,
    @CurrentMedications     NVARCHAR(1000) = NULL,
    @FamilyDiabetes         BIT,
    @FamilyCancer           BIT,
    @FamilyHeartDisease     BIT,
    @FamilyHypertension     BIT,
    @FamilyOthers           NVARCHAR(300)  = NULL,
    @SmokingStatus          TINYINT,
    @SmokingDailyAmount     NVARCHAR(50)   = NULL,
    @AlcoholStatus          TINYINT,
    @PhysicalActivity       TINYINT,
    @DrugUse                BIT,
    @HasWeightLoss          BIT,
    @HasFrequentFever       BIT,
    @HasHeadache            BIT,
    @HasVisionHearingChange BIT,
    @HasBreathingDifficulty BIT,
    @UpdatedByUserId        INT            = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.PatientHistory WHERE PatientId = @PatientId)
    BEGIN
        UPDATE dbo.PatientHistory SET
            HasHypertension        = @HasHypertension,
            HasDiabetes            = @HasDiabetes,
            HasHeartDisease        = @HasHeartDisease,
            HasAsthmaRespiratory   = @HasAsthmaRespiratory,
            HasThyroidDisease      = @HasThyroidDisease,
            HasRenalDisease        = @HasRenalDisease,
            HasAnxietyDepression   = @HasAnxietyDepression,
            HasCancer              = @HasCancer,
            CancerDetail           = @CancerDetail,
            PreviousSurgeries      = @PreviousSurgeries,
            RecentHospitalizations = @RecentHospitalizations,
            DrugAllergies          = @DrugAllergies,
            FoodAllergies          = @FoodAllergies,
            OtherAllergies         = @OtherAllergies,
            CurrentMedications     = @CurrentMedications,
            FamilyDiabetes         = @FamilyDiabetes,
            FamilyCancer           = @FamilyCancer,
            FamilyHeartDisease     = @FamilyHeartDisease,
            FamilyHypertension     = @FamilyHypertension,
            FamilyOthers           = @FamilyOthers,
            SmokingStatus          = @SmokingStatus,
            SmokingDailyAmount     = @SmokingDailyAmount,
            AlcoholStatus          = @AlcoholStatus,
            PhysicalActivity       = @PhysicalActivity,
            DrugUse                = @DrugUse,
            HasWeightLoss          = @HasWeightLoss,
            HasFrequentFever       = @HasFrequentFever,
            HasHeadache            = @HasHeadache,
            HasVisionHearingChange = @HasVisionHearingChange,
            HasBreathingDifficulty = @HasBreathingDifficulty,
            LastUpdated            = GETDATE(),
            UpdatedByUserId        = @UpdatedByUserId
        WHERE PatientId = @PatientId;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.PatientHistory (
            PatientId,
            HasHypertension, HasDiabetes, HasHeartDisease, HasAsthmaRespiratory,
            HasThyroidDisease, HasRenalDisease, HasAnxietyDepression, HasCancer,
            CancerDetail, PreviousSurgeries, RecentHospitalizations,
            DrugAllergies, FoodAllergies, OtherAllergies, CurrentMedications,
            FamilyDiabetes, FamilyCancer, FamilyHeartDisease, FamilyHypertension, FamilyOthers,
            SmokingStatus, SmokingDailyAmount, AlcoholStatus, PhysicalActivity, DrugUse,
            HasWeightLoss, HasFrequentFever, HasHeadache, HasVisionHearingChange, HasBreathingDifficulty,
            LastUpdated, UpdatedByUserId
        )
        VALUES (
            @PatientId,
            @HasHypertension, @HasDiabetes, @HasHeartDisease, @HasAsthmaRespiratory,
            @HasThyroidDisease, @HasRenalDisease, @HasAnxietyDepression, @HasCancer,
            @CancerDetail, @PreviousSurgeries, @RecentHospitalizations,
            @DrugAllergies, @FoodAllergies, @OtherAllergies, @CurrentMedications,
            @FamilyDiabetes, @FamilyCancer, @FamilyHeartDisease, @FamilyHypertension, @FamilyOthers,
            @SmokingStatus, @SmokingDailyAmount, @AlcoholStatus, @PhysicalActivity, @DrugUse,
            @HasWeightLoss, @HasFrequentFever, @HasHeadache, @HasVisionHearingChange, @HasBreathingDifficulty,
            GETDATE(), @UpdatedByUserId
        );
    END

    SELECT PatientHistoryId FROM dbo.PatientHistory WHERE PatientId = @PatientId;
END
GO
