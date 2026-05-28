-- =============================================================================
-- Calendario de Citas: Appointment_GetForCalendar
-- Ejecutar en SSMS sobre la BD de Medycally
-- =============================================================================

CREATE OR ALTER PROCEDURE dbo.Appointment_GetForCalendar
    @DoctorId  INT  = NULL,
    @StartDate DATE,
    @EndDate   DATE
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        AppointmentId,
        PatientName,
        DoctorId,
        DoctorName,
        SpecialtyName,
        AppointmentDate,
        AppointmentTime,
        AppointmentStatusId,
        AppointmentStatusName
    FROM dbo.Appointment_Detail
    WHERE AppointmentDay >= @StartDate
      AND AppointmentDay <  @EndDate
      AND (@DoctorId IS NULL OR DoctorId = @DoctorId)
    ORDER BY AppointmentDate;
END
GO
