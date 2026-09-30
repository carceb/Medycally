-- =============================================================================
-- SP: SecurityUser_GetByUser
-- Devuelve la lista de usuarios visibles para el usuario logueado.
--
-- Reglas:
--   - Si IsSuperAdmin = 1 → ve a TODOS los usuarios (sin filtro).
--   - En otro caso:
--       * Excluye usuarios cuyo rol tenga IsSuperAdmin = 1.
--       * Incluye al propio usuario logueado.
--       * Incluye usuarios que comparten al menos una clínica con el logueado:
--           - vía SecurityUserClinic (asignación manual), o
--           - vía ClinicDoctor (cuando ese usuario es médico).
--       * Las clínicas accesibles del logueado se calculan también desde
--         SecurityUserClinic + ClinicDoctor (cuando él mismo tiene DoctorId).
--
-- Idempotente: CREATE OR ALTER.
-- =============================================================================

CREATE OR ALTER PROCEDURE dbo.SecurityUser_GetByUser
    @SecurityUserId INT,
    @IsSuperAdmin   BIT,
    @DoctorId       INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- ── SuperAdmin: ve a todos ──────────────────────────────────────────────
    IF @IsSuperAdmin = 1
    BEGIN
        SELECT
            u.SecurityUserId,
            u.UserName,
            u.UserEmail,
            u.UserIdNumber,
            u.SecurityRoleId,
            r.RoleName,
            r.IsSuperAdmin,
            CASE WHEN u.IsActive = 1 THEN 1 ELSE 2 END AS StatusId,
            u.IsActivated,
            u.DoctorId,
            d.DoctorName
        FROM       dbo.SecurityUser u
        INNER JOIN dbo.SecurityRole r ON r.SecurityRoleId = u.SecurityRoleId
        LEFT  JOIN dbo.Doctor       d ON d.DoctorId       = u.DoctorId
        ORDER BY u.UserName;
        RETURN;
    END

    -- ── Usuario regular: scoping por clínica ────────────────────────────────
    DECLARE @MyClinics TABLE (ClinicId INT PRIMARY KEY);

    INSERT INTO @MyClinics (ClinicId)
    SELECT DISTINCT ClinicId
    FROM (
        SELECT suc.ClinicId
        FROM   dbo.SecurityUserClinic suc
        WHERE  suc.SecurityUserId = @SecurityUserId
        UNION
        SELECT cd.ClinicId
        FROM   dbo.ClinicDoctor cd
        WHERE  @DoctorId IS NOT NULL
          AND  cd.DoctorId = @DoctorId
    ) src;

    SELECT DISTINCT
        u.SecurityUserId,
        u.UserName,
        u.UserEmail,
        u.UserIdNumber,
        u.SecurityRoleId,
        r.RoleName,
        r.IsSuperAdmin,
        CASE WHEN u.IsActive = 1 THEN 1 ELSE 2 END AS StatusId,
        u.IsActivated,
        u.DoctorId,
        d.DoctorName
    FROM       dbo.SecurityUser u
    INNER JOIN dbo.SecurityRole r ON r.SecurityRoleId = u.SecurityRoleId
    LEFT  JOIN dbo.Doctor       d ON d.DoctorId       = u.DoctorId
    WHERE  r.IsSuperAdmin = 0
      AND  (
            -- Siempre se ve a sí mismo
            u.SecurityUserId = @SecurityUserId
            OR
            -- Usuarios regulares con clínica compartida (vía SecurityUserClinic)
            EXISTS (
                SELECT 1
                FROM   dbo.SecurityUserClinic suc
                INNER JOIN @MyClinics mc ON mc.ClinicId = suc.ClinicId
                WHERE  suc.SecurityUserId = u.SecurityUserId
            )
            OR
            -- Médicos cuyas clínicas (vía ClinicDoctor) coinciden con las del logueado
            EXISTS (
                SELECT 1
                FROM   dbo.ClinicDoctor cd
                INNER JOIN @MyClinics    mc ON mc.ClinicId = cd.ClinicId
                WHERE  u.DoctorId IS NOT NULL
                  AND  cd.DoctorId = u.DoctorId
            )
      )
    ORDER BY u.UserName;
END
GO

PRINT 'OK — dbo.SecurityUser_GetByUser creado/actualizado.';
