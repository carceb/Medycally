-- ============================================================
-- ExchangeRate: tabla y stored procedures
-- Ejecutar en SSMS (idempotente)
-- ============================================================

-- PASO 1: Tabla ExchangeRate
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ExchangeRate')
BEGIN
    CREATE TABLE ExchangeRate (
        CurrencyCode CHAR(3)       NOT NULL,
        Rate         DECIMAL(10,4) NOT NULL,
        FetchedAt    DATETIME      NOT NULL DEFAULT GETDATE(),
        CONSTRAINT PK_ExchangeRate PRIMARY KEY (CurrencyCode)
    );
END
GO

-- PASO 2: Obtener todas las tasas
CREATE OR ALTER PROCEDURE ExchangeRate_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CurrencyCode, Rate, FetchedAt
    FROM   ExchangeRate;
END
GO

-- PASO 3: Upsert tasa por codigo de moneda
CREATE OR ALTER PROCEDURE ExchangeRate_Save
    @CurrencyCode CHAR(3),
    @Rate         DECIMAL(10,4)
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM ExchangeRate WHERE CurrencyCode = @CurrencyCode)
        UPDATE ExchangeRate
        SET    Rate = @Rate, FetchedAt = GETDATE()
        WHERE  CurrencyCode = @CurrencyCode;
    ELSE
        INSERT INTO ExchangeRate (CurrencyCode, Rate)
        VALUES (@CurrencyCode, @Rate);
END
GO
