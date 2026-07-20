# FinOS Database

SQL Server database project for **FinOS** — an Indian personal finance management application. Contains all schemas, stored procedures, views, seed data, agent jobs, and maintenance scripts.

---

## Project Overview

FinOS helps users manage their personal finances with support for:

- **Accounts & Transactions** — Multi-account tracking (Savings, Credit Card, Cash, Wallet)
- **Budgets** — Category-level budgets with overspend alerts
- **Investments** — MF, Stocks, FD, Gold, EPF, PPF, NPS with SIP processing and XIRR
- **Loans** — Home, Car, Personal, Education loans with EMI schedules and prepayment simulation
- **Goals** — Emergency funds, retirement, travel, wedding savings tracking
- **Analytics** — Net worth snapshots, financial scores (0-1000), monthly aggregates
- **AI Assistant** — Conversational financial queries with feedback tracking
- **Notifications** — Multi-channel alerts (In-App, Email, Push, SMS)
- **Subscriptions** — Auto-detected recurring subscriptions from transactions
- **Data Import** — CSV, Excel, Bank Statement imports with error tracking

All monetary values use `DECIMAL(18,2)` with **INR** as the default currency.

---

## Folder Structure

```
FinOS.Database/
├── Schema/                          -- Database schema scripts (run first)
│   ├── 001_CreateDatabase.sql           Database, filegroups, schemas
│   ├── 002_Security_Schema.sql          Users, Roles, AuditLog, Tokens
│   ├── 003_Core_Finance_Schema.sql      Accounts, Categories, Transactions, Tags, RecurringSchedules
│   ├── 004_Budget_Schema.sql            Budgets, BudgetCategories, BudgetAlerts, SavingsRules
│   ├── 005_Investment_Schema.sql        Portfolios, Holdings, SIPs, EPF, GoldPriceHistory
│   ├── 006_Loan_Schema.sql              Loans, EMISchedule, Prepayments, Simulations
│   ├── 007_Goals_Analytics_Schema.sql   Goals, NetWorthSnapshots, FinancialScore, MonthlyAggregates
│   └── 008_AI_Notifications_Schema.sql  AIConversations, AIMessages, Notifications, Subscriptions, Imports
│
├── StoredProcedures/               -- Business logic stored procedures
│   ├── Security_sp.sql                  User management, auth, audit
│   ├── Core_sp.sql                      Transactions, accounts, recurring schedules, subscription detection
│   ├── Budget_sp.sql                    Budget CRUD, spending tracking, alerts
│   ├── Investment_sp.sql                Holdings, SIPs, EPF, XIRR, portfolio summary
│   ├── Loan_sp.sql                      Loans, EMI schedules, prepayment simulation/execution
│   └── Goals_sp.sql                     Goals, contributions, projections
│
├── Views/                          -- Read-only views for dashboards and reports
│   ├── Dashboard_Views.sql
│   ├── Analytics_Views.sql
│   ├── Budget_Views.sql
│   ├── Investment_Views.sql
│   ├── Loan_Views.sql
│   └── Admin_Views.sql
│
├── SeedData/                       -- Reference and sample data
│   ├── 001_ReferenceData.sql            Account types, investment types, loan types, notification types
│   ├── 002_RolesAndPermissions.sql       User/Admin roles
│   └── 003_SampleGoldPrices.sql          Historical gold price data
│
├── Jobs/                           -- SQL Server Agent job definitions
│   ├── JobExecutionLog.sql              Logging infrastructure (table, SP, view)
│   ├── Job_RecurringTransactions.sql    Daily 6:00 AM IST — recurring transactions, SIPs, overdue EMIs
│   ├── Job_DailyAnalytics.sql           Daily 2:00 AM IST — net worth, aggregates, budget alerts
│   ├── Job_WeeklyMaintenance.sql        Sunday 3:00 AM IST — financial scores, index rebuild, data purge
│   └── Job_MonthlyProcessing.sql        1st monthly 1:00 AM IST — goal archive, EPF interest, snapshots
│
└── Manual/                         -- Manual / maintenance scripts
    ├── 001_CreateTestUser.sql           Test user with sample data
    ├── 002_DataMigration_v1_to_v2.sql   Migration template with rollback
    ├── 003_BackupAndRestore.sql         Backup/restore procedures
    ├── 004_IndexMaintenance.sql         Index maintenance SPs
    └── 005_DataPurge.sql                Data purge SPs
```

---

## Execution Order for Schema Scripts

Run the schema scripts in numerical order. Each script is idempotent (uses `IF NOT EXISTS` checks).

```sql
-- Step 1: Create database and filegroups
:run Schema/001_CreateDatabase.sql

-- Step 2: Security & authentication tables
:run Schema/002_Security_Schema.sql

-- Step 3: Core finance tables (depends on Security.Users)
:run Schema/003_Core_Finance_Schema.sql

-- Step 4: Budget tables (depends on Core.Categories, Core.Accounts)
:run Schema/004_Budget_Schema.sql

-- Step 5: Investment tables (depends on Core.Accounts)
:run Schema/005_Investment_Schema.sql

-- Step 6: Loan tables (depends on Core.Accounts)
:run Schema/006_Loan_Schema.sql

-- Step 7: Goals & Analytics tables (depends on Security.Users, Core)
:run Schema/007_Goals_Analytics_Schema.sql

-- Step 8: AI, Notifications, Subscriptions, Import tables
:run Schema/008_AI_Notifications_Schema.sql
```

---

## How to Run Seed Data

After creating all schemas, run the seed data scripts to populate reference data:

```sql
-- 1. Reference data (account types, investment types, loan types, notification types)
:run SeedData/001_ReferenceData.sql

-- 2. Roles and permissions
:run SeedData/002_RolesAndPermissions.sql

-- 3. Sample gold prices (optional, for testing)
:run SeedData/003_SampleGoldPrices.sql
```

> **Note:** Seed data scripts are also idempotent. They check for existing data before inserting.

---

## How to Deploy Stored Procedures

Deploy stored procedures after schemas and seed data are in place. The order does not matter much since SPs reference existing tables, but the recommended order is:

```sql
:run StoredProcedures/Security_sp.sql
:run StoredProcedures/Core_sp.sql
:run StoredProcedures/Budget_sp.sql
:run StoredProcedures/Investment_sp.sql
:run StoredProcedures/Loan_sp.sql
:run StoredProcedures/Goals_sp.sql
:run StoredProcedures/Analytics_sp.sql
```

> Each SP script uses `DROP PROCEDURE IF EXISTS` followed by `CREATE PROCEDURE`, making them safe to re-run.

---

## How to Deploy Views

Views depend on both schemas and stored procedures (some views reference SP results). Deploy after SPs:

```sql
:run Views/Dashboard_Views.sql
:run Views/Analytics_Views.sql
:run Views/Budget_Views.sql
:run Views/Investment_Views.sql
:run Views/Loan_Views.sql
:run Views/Admin_Views.sql
```

---

## How to Set Up SQL Server Agent Jobs

### Prerequisites

1. SQL Server Agent service must be running
2. Deploy the **JobExecutionLog** infrastructure first:

```sql
:run Jobs/JobExecutionLog.sql
```

3. Ensure all referenced stored procedures are deployed (Core, Investment, Analytics, Budget, Loan SPs)

### Deploy Jobs

Run each job script on the `msdb` database. Each script is idempotent (deletes existing job before recreating).

```sql
-- 1. Daily at 6:00 AM IST: Recurring transactions, SIPs, overdue EMI checks
:run Jobs/Job_RecurringTransactions.sql

-- 2. Daily at 2:00 AM IST: Net worth, monthly aggregates, budget alerts
:run Jobs/Job_DailyAnalytics.sql

-- 3. Weekly Sunday 3:00 AM IST: Financial scores, index rebuild, statistics, data purge
:run Jobs/Job_WeeklyMaintenance.sql

-- 4. Monthly on 1st at 1:00 AM IST: Goal archive, EPF interest, price updates, snapshots
:run Jobs/Job_MonthlyProcessing.sql
```

### Job Schedule Summary

| Job | Schedule | IST Time | UTC Time | Key Steps |
|-----|----------|----------|----------|-----------|
| RecurringTransactions | Daily | 6:00 AM | 00:30 | Recurring txns, SIPs, Overdue EMIs |
| DailyAnalytics | Daily | 2:00 AM | 20:30 (prev day) | Net worth, Aggregates, Budget alerts, Subscription detection (Sundays) |
| WeeklyMaintenance | Weekly (Sunday) | 3:00 AM | 21:30 (Sat) | Financial scores, Index rebuild, Stats update, Data purge |
| MonthlyProcessing | Monthly (1st) | 1:00 AM | 19:30 (prev day) | Goal archive, EPF interest, Price updates, AI purge, Snapshots |

### Monitoring Job Execution

```sql
-- View recent job execution history
SELECT * FROM dbo.vw_JobExecutionHistory;

-- Get job execution summary for last 7 days
EXEC dbo.sp_GetJobExecutionSummary @DaysBack = 7;

-- Check for recent failures
SELECT * FROM dbo.vw_JobExecutionHistory WHERE IsRecentFailure = 1;
```

---

## Notes on Manual Scripts

### 001_CreateTestUser.sql

Creates a test user (`test@finos.app`) with complete sample data for development and testing:

- 1 user with known credentials
- 3 accounts (SBI Savings ₹1.5L, HDFC Credit Card ₹15K outstanding, Cash ₹5K)
- 11 categories (2 Income, 9 Expense)
- 14 transactions for the current month
- 1 budget (₹70,000/month with 9 categories)
- 1 goal (Emergency Fund ₹5L, ₹2L saved)
- 1 loan (Home Loan ₹50L @ 8.5%, 20 years)

> **WARNING:** For development/testing ONLY. Never run in production.

### 002_DataMigration_v1_to_v2.sql

Template for schema migrations between versions:

- Runs within a transaction (all-or-nothing)
- Idempotent (checks if changes already applied)
- Includes commented rollback section at the bottom
- Covers: new columns, new tables, column modifications, data transformations, new indexes

> **IMPORTANT:** Test on staging before production. Always have a backup.

### 003_BackupAndRestore.sql

Backup and restore procedures with:

- Full backup with timestamp naming and compression
- Differential backup (changes since last full)
- Transaction log backup (for point-in-time recovery)
- Point-in-time restore (full → differential → log chain → STOPAT)
- Backup integrity verification (RESTORE VERIFYONLY)
- Automated backup health check

> Scripts are provided as commented templates. Uncomment and adjust paths for your environment.

### 004_IndexMaintenance.sql

Five stored procedures for index maintenance:

| SP | Purpose |
|----|---------|
| `dbo.sp_IdentifyFragmentedIndexes` | Lists indexes with fragmentation > threshold |
| `dbo.sp_RebuildReorganizeIndexes` | Rebuilds (>30%) or reorganizes (>10%) fragmented indexes |
| `dbo.sp_UpdateAllStatistics` | Updates statistics on all FinOS tables |
| `dbo.sp_IdentifyMissingIndexes` | Recommends new indexes from DMV data |
| `dbo.sp_IdentifyUnusedIndexes` | Finds indexes with little or no usage |

Usage:
```sql
-- Find fragmented indexes
EXEC dbo.sp_IdentifyFragmentedIndexes @MinFragmentationPct = 15.0;

-- Rebuild/reorganize indexes
EXEC dbo.sp_RebuildReorganizeIndexes @OnlineRebuild = 1;

-- Update statistics
EXEC dbo.sp_UpdateAllStatistics @FullScan = 1;

-- Find missing indexes
EXEC dbo.sp_IdentifyMissingIndexes;

-- Find unused indexes
EXEC dbo.sp_IdentifyUnusedIndexes;
```

### 005_DataPurge.sql

Seven stored procedures for data retention management:

| SP | Default Retention | Purpose |
|----|-------------------|---------|
| `dbo.sp_PurgeOldAuditLogs` | 90 days | Security.AuditLog |
| `dbo.sp_PurgeOldNotifications` | 180 days (read only) | Notifications.Notifications, Budget.BudgetAlerts |
| `dbo.sp_PurgeExpiredTokens` | 30 days | Security.RefreshTokens, Security.PasswordResetTokens |
| `dbo.sp_PurgeOldAIConversations` | 365 days | AI.AIConversations, AI.AIMessages |
| `dbo.sp_PurgeCompletedGoals` | 6 months | Goals.Goals (archive + soft-delete) |
| `dbo.sp_PurgeOldImportBatches` | 30 days | Import.ImportBatches, Import.ImportErrors |
| `dbo.sp_MasterPurge` | — | Calls all above with configurable retention |

Usage:
```sql
-- Dry run (report only, no data deleted)
EXEC dbo.sp_MasterPurge @DryRun = 1;

-- Run with default retention periods
EXEC dbo.sp_MasterPurge;

-- Run with custom retention
EXEC dbo.sp_MasterPurge
    @AuditLogRetentionDays = 60,
    @NotificationRetentionDays = 90,
    @AIConversationRetentionDays = 180;

-- Individual purge
EXEC dbo.sp_PurgeOldAuditLogs @RetentionDays = 60;
EXEC dbo.sp_PurgeOldNotifications @RetentionDays = 90, @PurgeUnread = 0;
```

---

## Technical Notes

- **Database:** Microsoft SQL Server 2019+
- **Filegroups:** `FinOS_Data` (data), `FinOS_Index` (indexes), `FinOS_Log` (transaction log)
- **Schemas:** Security, Core, Budget, Investment, Loan, Goals, Analytics, AI, Notifications, Subscriptions, Import, dbo
- **All timestamps** use `DATETIME2` with `SYSUTCDATETIME()` (UTC). IST conversions done at application/display layer.
- **Soft deletes** via `DeletedAt` column (NULL = active)
- **Currency:** INR by default, multi-currency support via `Currency`, `ExchangeRate`, `OriginalAmount`, `OriginalCurrency` columns
