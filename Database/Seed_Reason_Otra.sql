-- =============================================================================
-- Seed_Reason_Otra.sql
-- Inserta una fila "Otra" en dbo.Reason por cada SpecialtyId que no la tenga.
-- Idempotente: se puede ejecutar múltiples veces sin duplicar.
-- =============================================================================

INSERT INTO dbo.Reason (ReasonName, SpecialtyId)
SELECT N'Otra', s.SpecialtyId
FROM   dbo.Specialty s
WHERE  NOT EXISTS (
    SELECT 1
    FROM   dbo.Reason r
    WHERE  r.SpecialtyId = s.SpecialtyId
      AND  r.ReasonName  = N'Otra'
);
GO

-- Verificación: muestra las filas "Otra" insertadas por especialidad
SELECT r.ReasonId, r.ReasonName, r.SpecialtyId, s.SpecialtyName
FROM   dbo.Reason   r
INNER JOIN dbo.Specialty s ON s.SpecialtyId = r.SpecialtyId
WHERE  r.ReasonName = N'Otra'
ORDER BY s.SpecialtyName;
GO
