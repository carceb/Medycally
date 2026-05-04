-- =============================================================================
-- Cleanup_TestData.sql
-- Elimina todos los datos de prueba: atenciones, citas, guardians y pacientes.
-- SOLO para entorno de desarrollo. NO ejecutar en producción.
-- Orden respeta las FK constraints.
-- =============================================================================

-- 1. MedicalAttention  →  depende de Appointment y Patient
DELETE FROM dbo.MedicalAttention;
GO

-- 2. Appointment  →  depende de SpecialtyDoctor y Clinic (no bloquea Patient)
DELETE FROM dbo.Appointment;
GO

-- 3. PatientGuardian  →  depende de Patient (ambas columnas)
DELETE FROM dbo.PatientGuardian;
GO

-- 4. Patient  →  ya sin dependencias activas
DELETE FROM dbo.Patient;
GO

-- Reiniciar contadores IDENTITY
DBCC CHECKIDENT ('dbo.MedicalAttention', RESEED, 0);
DBCC CHECKIDENT ('dbo.Appointment',      RESEED, 0);
DBCC CHECKIDENT ('dbo.PatientGuardian',  RESEED, 0);
DBCC CHECKIDENT ('dbo.Patient',          RESEED, 0);
GO
