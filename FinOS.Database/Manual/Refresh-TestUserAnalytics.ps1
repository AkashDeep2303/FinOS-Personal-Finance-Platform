param(
  [long]$UserId = 1,
  [int]$Year = 2026
)

$ErrorActionPreference = 'Stop'
$query = @"
EXEC Analytics.sp_CalculateNetWorth @UserId = $UserId;
EXEC Analytics.sp_GenerateMonthlyAggregates @UserId = $UserId, @Year = $Year, @Month = 0;
EXEC Analytics.sp_CalculateFinancialScore @UserId = $UserId;
SELECT 'NetWorthSnapshots' AS Dataset, COUNT(*) AS Records FROM Analytics.NetWorthSnapshots WHERE UserId = $UserId
UNION ALL SELECT 'MonthlyAggregates', COUNT(*) FROM Analytics.MonthlyAggregates WHERE UserId = $UserId
UNION ALL SELECT 'FinancialScore', COUNT(*) FROM Analytics.FinancialScore WHERE UserId = $UserId;
"@
docker exec finos-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'CHANGE_ME_SQL_PASSWORD' -d FinOS -C -b -Q $query