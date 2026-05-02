-- =============================================================================
-- SPs_MedicalAttention.sql
-- Módulo de Atención Médica: cola de pacientes, historial, diagnóstico y tratamiento.
-- Ejecutar en SSMS sobre la BD Medycally
-- =============================================================================

-- =============================================================================
-- PASO 1: Agregar estado "En Atención" a AppointmentStatus
-- Si la tabla usa IDENTITY ejecutar primero:
--   SET IDENTITY_INSERT dbo.AppointmentStatus ON
--   (y OFF al terminar)
-- Si NO usa IDENTITY, el INSERT simple funciona directamente.
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentStatus WHERE AppointmentStatusId = 4)
    INSERT INTO dbo.AppointmentStatus (AppointmentStatusId, AppointmentStatusName)
    VALUES (4, N'En Atención');
GO

-- =============================================================================
-- PASO 2: Agregar columna DoctorId a SecurityUser
-- Vincula un usuario del sistema con un registro de Médico.
-- NULL = usuario sin médico asociado (admin, recepcionista, etc.)
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.SecurityUser') AND name = 'DoctorId')
    ALTER TABLE dbo.SecurityUser ADD DoctorId INT NULL
        CONSTRAINT FK_SecurityUser_Doctor FOREIGN KEY REFERENCES dbo.Doctor(DoctorId);
GO

-- =============================================================================
-- PASO 3: Actualizar Security_UserLogin para devolver DoctorId
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.Security_UserLogin
    @UserEmail        VARCHAR(100),
    @UserPasswordHash VARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.SecurityUser
    SET    LastLoginAt = GETDATE()
    WHERE  UserEmail        = @UserEmail
      AND  UserPasswordHash = @UserPasswordHash
      AND  IsActive         = 1;

    SELECT
        u.SecurityUserId,
        u.UserName,
        u.UserEmail,
        u.UserIdNumber,
        u.SecurityRoleId,
        r.RoleName,
        r.RoleLevel,
        u.DoctorId
    FROM      dbo.SecurityUser u
    INNER JOIN dbo.SecurityRole r ON r.SecurityRoleId = u.SecurityRoleId
    WHERE u.UserEmail        = @UserEmail
      AND u.UserPasswordHash = @UserPasswordHash
      AND u.IsActive         = 1;
END
GO

-- =============================================================================
-- PASO 4: Actualizar SecurityUser_GetAll para incluir DoctorId y DoctorName
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.SecurityUser_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.SecurityUserId,
        u.UserName,
        u.UserEmail,
        u.UserIdNumber,
        u.SecurityRoleId,
        r.RoleName,
        CASE WHEN u.IsActive = 1 THEN 1 ELSE 2 END AS StatusId,
        u.DoctorId,
        d.DoctorName
    FROM       dbo.SecurityUser u
    INNER JOIN dbo.SecurityRole r ON r.SecurityRoleId = u.SecurityRoleId
    LEFT  JOIN dbo.Doctor       d ON d.DoctorId       = u.DoctorId
    ORDER BY   u.UserName;
END
GO

-- =============================================================================
-- PASO 5: Actualizar SecurityUser_AddOrEdit para aceptar @DoctorId
-- @PasswordHash = NULL → mantiene contraseña existente
-- @DoctorId     = NULL → sin médico vinculado
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.SecurityUser_AddOrEdit
    @SecurityUserId INT,
    @UserName       VARCHAR(100),
    @UserEmail      VARCHAR(100),
    @UserIdNumber   INT,
    @SecurityRoleId INT,
    @StatusId       INT,
    @PasswordHash   VARCHAR(256) = NULL,
    @DoctorId       INT          = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IsActive BIT = CASE WHEN @StatusId = 1 THEN 1 ELSE 0 END;

    IF @SecurityUserId = 0
    BEGIN
        INSERT INTO dbo.SecurityUser
            (SecurityRoleId, UserIdNumber, UserEmail, UserPasswordHash, UserName, IsActive, DoctorId)
        VALUES
            (@SecurityRoleId, @UserIdNumber, @UserEmail,
             ISNULL(@PasswordHash, ''), @UserName, @IsActive, @DoctorId);

        SELECT SCOPE_IDENTITY() AS SecurityUserId;
    END
    ELSE
    BEGIN
        UPDATE dbo.SecurityUser
        SET    UserName         = @UserName,
               UserEmail        = @UserEmail,
               UserIdNumber     = @UserIdNumber,
               SecurityRoleId   = @SecurityRoleId,
               IsActive         = @IsActive,
               DoctorId         = @DoctorId,
               UserPasswordHash = CASE WHEN @PasswordHash IS NOT NULL
                                       THEN @PasswordHash
                                       ELSE UserPasswordHash
                                   END
        WHERE  SecurityUserId = @SecurityUserId;

        SELECT @SecurityUserId AS SecurityUserId;
    END
END
GO

-- =============================================================================
-- PASO 6: Crear tabla MedicalAttention
-- Almacena el diagnóstico y tratamiento de cada atención médica.
-- AppointmentId referencia la cita; DoctorId se deriva de ella (también guardado aquí).
-- =============================================================================
IF OBJECT_ID('dbo.MedicalAttention', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MedicalAttention (
        AttentionId   INT            IDENTITY(1,1) NOT NULL,
        AppointmentId INT            NOT NULL,
        DoctorId      INT            NOT NULL,
        AttentionDate DATETIME       NOT NULL DEFAULT GETDATE(),
        Diagnosis     NVARCHAR(3000) NOT NULL,
        Treatment     NVARCHAR(3000) NOT NULL,
        Notes         NVARCHAR(1000) NULL,
        CONSTRAINT PK_MedicalAttention                PRIMARY KEY (AttentionId),
        CONSTRAINT FK_MedicalAttention_Appointment    FOREIGN KEY (AppointmentId) REFERENCES dbo.Appointment(AppointmentId),
        CONSTRAINT FK_MedicalAttention_Doctor         FOREIGN KEY (DoctorId)      REFERENCES dbo.Doctor(DoctorId),
        CONSTRAINT UQ_MedicalAttention_Appointment    UNIQUE      (AppointmentId)
    );
END
GO

-- =============================================================================
-- PASO 7: Appointment_GetConfirmedByClinic
-- Cola de pacientes para el módulo de Atención Médica.
-- Retorna citas con estatus Confirmado (2) o En Atención (4).
-- @DoctorId = NULL → todas los médicos; valor → filtrar por médico.
-- @Date     = NULL → hoy.
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.Appointment_GetConfirmedByClinic
    @ClinicId INT,
    @DoctorId INT  = NULL,
    @Date     DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TargetDate DATE = ISNULL(@Date, CAST(GETDATE() AS DATE));

    SELECT
        a.AppointmentId,
        a.PatientName,
        ISNULL(a.PatientIdNumber, 0)  AS PatientIdNumber,
        a.PatientAge,
        sd.DoctorId,
        ISNULL(sx.DoctorAbbreviation + N' ', N'') + d.DoctorName  AS DoctorName,
        sp.SpecialtyName,
        r.ReasonName,
        a.AppointmentDate,
        LEFT(CONVERT(VARCHAR(8), CAST(a.AppointmentDate AS TIME), 108), 5) AS AppointmentTime,
        a.AppointmentStatusId,
        ast.AppointmentStatusName,
        a.Symptoms
    FROM       dbo.Appointment       a
    INNER JOIN dbo.AppointmentStatus ast ON ast.AppointmentStatusId = a.AppointmentStatusId
    INNER JOIN dbo.SpecialtyDoctor   sd  ON sd.SpecialtyDoctorId    = a.SpecialtyDoctorId
    INNER JOIN dbo.Doctor            d   ON d.DoctorId              = sd.DoctorId
    INNER JOIN dbo.Specialty         sp  ON sp.SpecialtyId          = sd.SpecialtyId
    LEFT  JOIN dbo.Reason            r   ON r.ReasonId              = a.ReasonId
    LEFT  JOIN dbo.Sex               sx  ON sx.SexId                = d.SexId
    WHERE a.ClinicId             = @ClinicId
      AND CAST(a.AppointmentDate AS DATE) = @TargetDate
      AND a.AppointmentStatusId IN (2, 4)
      AND (@DoctorId IS NULL OR sd.DoctorId = @DoctorId)
    ORDER BY a.AppointmentDate;
END
GO

-- =============================================================================
-- PASO 8: MedicalAttention_Save
-- Inserta o actualiza un registro de atención médica.
-- DoctorId se deriva automáticamente de la cita (SpecialtyDoctor → Doctor).
-- AttentionId = 0 → INSERT; > 0 → UPDATE.
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.MedicalAttention_Save
    @AttentionId   INT,
    @AppointmentId INT,
    @Diagnosis     NVARCHAR(3000),
    @Treatment     NVARCHAR(3000),
    @Notes         NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @DoctorId INT;
    SELECT @DoctorId = sd.DoctorId
    FROM   dbo.Appointment     a
    INNER JOIN dbo.SpecialtyDoctor sd ON sd.SpecialtyDoctorId = a.SpecialtyDoctorId
    WHERE  a.AppointmentId = @AppointmentId;

    IF @AttentionId = 0
    BEGIN
        INSERT INTO dbo.MedicalAttention (AppointmentId, DoctorId, Diagnosis, Treatment, Notes)
        VALUES (@AppointmentId, @DoctorId, @Diagnosis, @Treatment, @Notes);
        SELECT CAST(SCOPE_IDENTITY() AS INT);
    END
    ELSE
    BEGIN
        UPDATE dbo.MedicalAttention
        SET    Diagnosis = @Diagnosis,
               Treatment = @Treatment,
               Notes     = @Notes
        WHERE  AttentionId = @AttentionId;
        SELECT @AttentionId;
    END
END
GO

-- =============================================================================
-- PASO 9: MedicalAttention_GetByPatient
-- Historia médica de un paciente identificado por su cédula.
-- Retorna todas las atenciones ordenadas de más reciente a más antigua.
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.MedicalAttention_GetByPatient
    @PatientIdNumber INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ma.AttentionId,
        ma.AppointmentId,
        ma.AttentionDate,
        ma.Diagnosis,
        ma.Treatment,
        ma.Notes,
        ISNULL(sx.DoctorAbbreviation + N' ', N'') + d.DoctorName  AS DoctorName,
        sp.SpecialtyName,
        a.AppointmentDate,
        a.Symptoms
    FROM       dbo.MedicalAttention  ma
    INNER JOIN dbo.Appointment       a   ON a.AppointmentId     = ma.AppointmentId
    INNER JOIN dbo.Doctor            d   ON d.DoctorId          = ma.DoctorId
    INNER JOIN dbo.SpecialtyDoctor   sd  ON sd.SpecialtyDoctorId = a.SpecialtyDoctorId
    INNER JOIN dbo.Specialty         sp  ON sp.SpecialtyId      = sd.SpecialtyId
    LEFT  JOIN dbo.Sex               sx  ON sx.SexId            = d.SexId
    WHERE  a.PatientIdNumber = @PatientIdNumber
    ORDER  BY ma.AttentionDate DESC;
END
GO

-- =============================================================================
-- PASO 10: MedicalAttention_GetByAppointment
-- Retorna la atención médica de una cita específica (para pre-llenar el formulario).
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.MedicalAttention_GetByAppointment
    @AppointmentId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ma.AttentionId,
        ma.AppointmentId,
        ma.AttentionDate,
        ma.Diagnosis,
        ma.Treatment,
        ma.Notes,
        ISNULL(sx.DoctorAbbreviation + N' ', N'') + d.DoctorName  AS DoctorName
    FROM       dbo.MedicalAttention  ma
    INNER JOIN dbo.Doctor            d  ON d.DoctorId = ma.DoctorId
    LEFT  JOIN dbo.Sex               sx ON sx.SexId   = d.SexId
    WHERE ma.AppointmentId = @AppointmentId;
END
GO

-- =============================================================================
-- PASO 11: Renombrar módulo "Medicos" / "Médicos" → "Atención Médica"
-- Cubre ambas variantes (con y sin tilde) y cualquier URL actual que no sea /Admin/
-- =============================================================================
UPDATE dbo.SecurityModule
SET    ModuleName = N'Atención Médica',
       ModuleUrl  = '/Medical/Index',
       ModuleIcon = 'fa-stethoscope'
WHERE  (ModuleName IN (N'Médicos', N'Medicos')
        OR ModuleUrl IN ('/Doctor', '/Doctor/Index'))
   AND (ModuleUrl IS NULL OR ModuleUrl NOT LIKE '/Admin/%');
GO

-- Si ningún registro existía con ese nombre/URL, insertar el módulo nuevo:
IF NOT EXISTS (SELECT 1 FROM dbo.SecurityModule WHERE ModuleUrl = '/Medical/Index')
BEGIN
    INSERT INTO dbo.SecurityModule (ModuleName, ModuleUrl, ModuleIcon, IsActive)
    VALUES (N'Atención Médica', '/Medical/Index', 'fa-stethoscope', 1);

    DECLARE @ModuleId INT = SCOPE_IDENTITY();

    INSERT INTO dbo.SecurityRoleModule (SecurityRoleId, SecurityModuleId, CanCreate, CanEdit, CanDelete)
    SELECT r.SecurityRoleId, @ModuleId, 1, 1, 1
    FROM   dbo.SecurityRole r
    WHERE  NOT EXISTS (
        SELECT 1 FROM dbo.SecurityRoleModule x
        WHERE  x.SecurityRoleId   = r.SecurityRoleId
          AND  x.SecurityModuleId = @ModuleId
    );
END
GO

-- =============================================================================
-- PASO 12: MedicalAttention_GetByGuardian
-- Historia médica de menores identificados por la cédula de su representante.
-- Se usa cuando PatientIdNumber = 0 (menores sin cédula propia).
-- Devuelve PatientName para identificar a cuál menor corresponde cada atención.
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.MedicalAttention_GetByGuardian
    @ChildGuardianIdNumber INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ma.AttentionId,
        ma.AppointmentId,
        ma.AttentionDate,
        ma.Diagnosis,
        ma.Treatment,
        ma.Notes,
        ISNULL(sx.DoctorAbbreviation + N' ', N'') + d.DoctorName  AS DoctorName,
        sp.SpecialtyName,
        a.AppointmentDate,
        a.Symptoms,
        a.PatientName
    FROM       dbo.MedicalAttention  ma
    INNER JOIN dbo.Appointment       a   ON a.AppointmentId      = ma.AppointmentId
    INNER JOIN dbo.Doctor            d   ON d.DoctorId           = ma.DoctorId
    INNER JOIN dbo.SpecialtyDoctor   sd  ON sd.SpecialtyDoctorId = a.SpecialtyDoctorId
    INNER JOIN dbo.Specialty         sp  ON sp.SpecialtyId       = sd.SpecialtyId
    LEFT  JOIN dbo.Sex               sx  ON sx.SexId             = d.SexId
    WHERE  a.ChildGuardianIdNumber = @ChildGuardianIdNumber
    ORDER  BY ma.AttentionDate DESC;
END
GO

-- =============================================================================
-- FIN DEL SCRIPT
-- =============================================================================
