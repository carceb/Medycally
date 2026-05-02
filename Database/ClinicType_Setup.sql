-- =============================================================================
-- ClinicType_Setup.sql
-- Crea la tabla ClinicType y su SP de consulta
-- Ejecutar en SSMS sobre la BD Medycally
-- =============================================================================

-- PASO 1: Tabla ClinicType
IF OBJECT_ID('dbo.ClinicType', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ClinicType (
        ClinicTypeId   INT IDENTITY(1,1) PRIMARY KEY,
        ClinicTypeName NVARCHAR(50) NOT NULL
    );
    INSERT INTO dbo.ClinicType (ClinicTypeName) VALUES (N'Clínica'), (N'Hospital'), (N'Consultorio');
END
GO

-- PASO 2: SP ClinicType_GetAll
IF OBJECT_ID('dbo.ClinicType_GetAll', 'P') IS NOT NULL DROP PROCEDURE dbo.ClinicType_GetAll;
GO

CREATE PROCEDURE dbo.ClinicType_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ClinicTypeId, ClinicTypeName FROM dbo.ClinicType ORDER BY ClinicTypeId;
END
GO

-- =============================================================================
-- FIN DEL SCRIPT
-- =============================================================================
