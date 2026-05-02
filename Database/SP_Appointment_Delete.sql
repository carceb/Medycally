-- =============================================================================
-- Eliminar cita del dashboard
-- Ejecutar en SSMS sobre la BD de Medycally.
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.Appointment_Delete
    @AppointmentId INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.Appointment WHERE AppointmentId = @AppointmentId;
END
GO
