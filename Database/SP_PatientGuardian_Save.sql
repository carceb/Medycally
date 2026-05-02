-- =============================================================================
-- PatientGuardian_Save — vincula representante y hereda dirección/municipio
-- Ejecutar en SSMS sobre la BD de Medycally.
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.PatientGuardian_Save
    @PatientId         INT,
    @GuardianPatientId INT,
    @RelationshipId    INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Insertar vínculo o actualizar parentesco si ya existía
    IF EXISTS (
        SELECT 1 FROM dbo.PatientGuardian
        WHERE PatientId = @PatientId AND GuardianPatientId = @GuardianPatientId
    )
        UPDATE dbo.PatientGuardian
        SET RelationshipId = @RelationshipId
        WHERE PatientId = @PatientId AND GuardianPatientId = @GuardianPatientId;
    ELSE
        INSERT INTO dbo.PatientGuardian (PatientId, GuardianPatientId, RelationshipId)
        VALUES (@PatientId, @GuardianPatientId, @RelationshipId);

    -- Heredar municipio y dirección del representante si el paciente no los tiene
    UPDATE p
    SET p.MunicipalityId = ISNULL(p.MunicipalityId, g.MunicipalityId),
        p.PatientAddress = ISNULL(p.PatientAddress, g.PatientAddress)
    FROM       dbo.Patient p
    JOIN       dbo.Patient g ON g.PatientId = @GuardianPatientId
    WHERE p.PatientId = @PatientId
      AND (p.MunicipalityId IS NULL OR p.PatientAddress IS NULL);
END
GO
