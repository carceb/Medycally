-- =============================================================================
-- Asignación multi-clínica para usuarios (SecurityUserClinic)
-- Idempotente. Ejecutar en SSMS sobre la BD de Medycally.
-- =============================================================================

-- =============================================================================
-- PASO 1: Tabla SecurityUserClinic (si no existe ya creada por scripts previos)
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SecurityUserClinic')
BEGIN
    CREATE TABLE dbo.SecurityUserClinic
    (
        SecurityUserId INT NOT NULL,
        ClinicId       INT NOT NULL,
        CONSTRAINT PK_SecurityUserClinic PRIMARY KEY (SecurityUserId, ClinicId),
        CONSTRAINT FK_SecurityUserClinic_User   FOREIGN KEY (SecurityUserId) REFERENCES dbo.SecurityUser (SecurityUserId),
        CONSTRAINT FK_SecurityUserClinic_Clinic FOREIGN KEY (ClinicId)       REFERENCES dbo.Clinic       (ClinicId)
    );
END
GO

-- =============================================================================
-- PASO 2: SecurityUserClinic_GetByUser
-- Devuelve, para un usuario dado, todas las clínicas con un flag IsAssigned.
-- Útil para poblar el modal de asignación (lista paginada con checkboxes).
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.SecurityUserClinic_GetByUser
    @SecurityUserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        c.ClinicId,
        ISNULL(ct.ClinicTypeName + N' ', N'') + c.ClinicName AS ClinicName,
        ISNULL(s.StateName, '')                              AS StateName,
        CASE WHEN suc.ClinicId IS NULL THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS IsAssigned
    FROM       dbo.Clinic              c
    LEFT JOIN  dbo.ClinicType          ct  ON ct.ClinicTypeId   = c.ClinicTypeId
    LEFT JOIN  dbo.Municipality        m   ON m.MunicipalityId  = c.MunicipalityId
    LEFT JOIN  dbo.State               s   ON s.StateId         = m.StateId
    LEFT JOIN  dbo.SecurityUserClinic  suc ON suc.ClinicId      = c.ClinicId
                                          AND suc.SecurityUserId = @SecurityUserId
    WHERE c.StatusId = 1
    ORDER BY c.ClinicName;
END
GO

-- =============================================================================
-- PASO 3: SecurityUserClinic_Save
-- Reemplaza las asignaciones de clínicas del usuario con la lista recibida.
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.SecurityUserClinic_Save
    @SecurityUserId INT,
    @ClinicIds      VARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.SecurityUserClinic WHERE SecurityUserId = @SecurityUserId;

    IF @ClinicIds IS NOT NULL AND LEN(TRIM(@ClinicIds)) > 0
    BEGIN
        INSERT INTO dbo.SecurityUserClinic (SecurityUserId, ClinicId)
        SELECT DISTINCT @SecurityUserId, TRY_CAST(TRIM(value) AS INT)
        FROM   STRING_SPLIT(@ClinicIds, ',')
        WHERE  TRY_CAST(TRIM(value) AS INT) IS NOT NULL;
    END
END
GO

-- =============================================================================
-- PASO 4: Clinic_GetByUser
-- Devuelve la lista de clínicas accesibles para un usuario.
--   - SuperAdmin (IsSuperAdmin = 1) → todas las clínicas (mismo shape que Clinic_GetAll)
--   - Resto → UNION de SecurityUserClinic + (si DoctorId no null) ClinicDoctor
-- Mantiene el mismo conjunto de columnas que Clinic_GetAll para que el DAL
-- existente lo consuma sin cambios.
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.Clinic_GetByUser
    @SecurityUserId INT,
    @IsSuperAdmin   BIT,
    @DoctorId       INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @IsSuperAdmin = 1
    BEGIN
        SELECT
            c.ClinicId,
            c.ClinicRif,
            c.ClinicTypeId,
            ct.ClinicTypeName,
            c.ClinicGroupId,
            c.ClinicName,
            c.MunicipalityId,
            ISNULL(m.MunicipalityName, '')  AS MunicipalityName,
            ISNULL(m.StateId, 0)            AS StateId,
            ISNULL(s.StateName,  '')        AS StateName,
            c.ClinicAddress,
            c.ClinicPhones,
            c.GoogleMapsUrl,
            ISNULL(c.Latitude,   0)         AS Latitude,
            ISNULL(c.Longitude,  0)         AS Longitude,
            c.RepresentativeName,
            c.LandingPage,
            c.ClinicDateCreated,
            c.StatusId
        FROM       dbo.Clinic        c
        LEFT JOIN  dbo.Municipality  m   ON m.MunicipalityId = c.MunicipalityId
        LEFT JOIN  dbo.State         s   ON s.StateId        = m.StateId
        LEFT JOIN  dbo.ClinicType    ct  ON ct.ClinicTypeId  = c.ClinicTypeId
        ORDER BY c.ClinicName;
        RETURN;
    END

    SELECT
        c.ClinicId,
        c.ClinicRif,
        c.ClinicTypeId,
        ct.ClinicTypeName,
        c.ClinicGroupId,
        c.ClinicName,
        c.MunicipalityId,
        ISNULL(m.MunicipalityName, '')  AS MunicipalityName,
        ISNULL(m.StateId, 0)            AS StateId,
        ISNULL(s.StateName,  '')        AS StateName,
        c.ClinicAddress,
        c.ClinicPhones,
        c.GoogleMapsUrl,
        ISNULL(c.Latitude,   0)         AS Latitude,
        ISNULL(c.Longitude,  0)         AS Longitude,
        c.RepresentativeName,
        c.LandingPage,
        c.ClinicDateCreated,
        c.StatusId
    FROM dbo.Clinic c
    LEFT JOIN  dbo.Municipality  m   ON m.MunicipalityId = c.MunicipalityId
    LEFT JOIN  dbo.State         s   ON s.StateId        = m.StateId
    LEFT JOIN  dbo.ClinicType    ct  ON ct.ClinicTypeId  = c.ClinicTypeId
    WHERE c.ClinicId IN (
        SELECT ClinicId FROM dbo.SecurityUserClinic WHERE SecurityUserId = @SecurityUserId
        UNION
        SELECT ClinicId FROM dbo.ClinicDoctor       WHERE @DoctorId IS NOT NULL AND DoctorId = @DoctorId
    )
    ORDER BY c.ClinicName;
END
GO
