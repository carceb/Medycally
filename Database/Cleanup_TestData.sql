-- =============================================================================
-- Cleanup_TestData.sql
-- Elimina todos los datos de prueba: atenciones médicas, historia, citas,
-- guardians y pacientes.
-- Conserva: Doctor, SecurityUser, SecurityModule, SecurityRole, Clinic y catálogos.
-- SOLO para entorno de desarrollo. NO ejecutar en producción.
-- Orden respeta las FK constraints.
-- =============================================================================

-- 1. MedicalAttention  →  referencia Appointment, Patient, Doctor y Reason
DELETE FROM dbo.MedicalAttention;
GO

-- 2. PatientHistory  →  referencia Patient (1:1)
DELETE FROM dbo.PatientHistory;
GO

-- 3. Appointment  →  referencia SpecialtyDoctor, Clinic y Patient (FK nullable)
DELETE FROM dbo.Appointment;
GO

-- 4. PatientGuardian  →  referencia Patient (dos columnas)
DELETE FROM dbo.PatientGuardian;
GO

-- 5. Patient  →  ya sin dependencias activas
DELETE FROM dbo.Patient;
GO

-- Reiniciar contadores IDENTITY (IDs arrancan de 1 en el próximo INSERT)
DBCC CHECKIDENT ('dbo.MedicalAttention', RESEED, 0);
DBCC CHECKIDENT ('dbo.PatientHistory',   RESEED, 0);
DBCC CHECKIDENT ('dbo.Appointment',      RESEED, 0);
DBCC CHECKIDENT ('dbo.PatientGuardian',  RESEED, 0);
DBCC CHECKIDENT ('dbo.Patient',          RESEED, 0);
GO
