-- ============================================================================
-- FinOS Database - Seed Data: Sample Gold Price History
-- Target: Microsoft SQL Server (SSMS)
-- Description: Inserts sample 24K gold price data (INR per 10 grams) for the
--              last 12 months into Investment.GoldPriceHistory.
--              Prices approximate Indian market rates (MCIndia / IBJA).
--              Current date assumed: March 2026.
-- ============================================================================

USE FinOS;
GO

PRINT N'Inserting Investment.GoldPriceHistory (24K, last 12 months)...';

-- Insert gold prices idempotently (avoid duplicates by PriceDate+GoldType)
MERGE INTO Investment.GoldPriceHistory AS Target
USING (
    VALUES
        (CONVERT(DATE, N'2025-03-15', 126), N'24K', 73550.00),
        (CONVERT(DATE, N'2025-04-15', 126), N'24K', 74820.00),
        (CONVERT(DATE, N'2025-05-15', 126), N'24K', 76100.00),
        (CONVERT(DATE, N'2025-06-15', 126), N'24K', 75480.00),
        (CONVERT(DATE, N'2025-07-15', 126), N'24K', 76950.00),
        (CONVERT(DATE, N'2025-08-15', 126), N'24K', 78320.00),
        (CONVERT(DATE, N'2025-09-15', 126), N'24K', 79650.00),
        (CONVERT(DATE, N'2025-10-15', 126), N'24K', 80250.00),
        (CONVERT(DATE, N'2025-11-15', 126), N'24K', 81800.00),
        (CONVERT(DATE, N'2025-12-15', 126), N'24K', 81050.00),
        (CONVERT(DATE, N'2026-01-15', 126), N'24K', 82400.00),
        (CONVERT(DATE, N'2026-02-15', 126), N'24K', 83750.00)
) AS Source(PriceDate, GoldType, PricePer10g)
ON Target.PriceDate = Source.PriceDate AND Target.GoldType = Source.GoldType
WHEN NOT MATCHED BY TARGET THEN
    INSERT (PriceDate, GoldType, PricePer10g, CreatedAt)
    VALUES (Source.PriceDate, Source.GoldType, Source.PricePer10g, SYSUTCDATETIME());
GO

PRINT N'Gold price history seeded successfully.';
GO
