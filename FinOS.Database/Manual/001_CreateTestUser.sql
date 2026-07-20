-- ============================================================================
-- FinOS Database - Manual Script: Create Test User with Sample Data
-- Target: Microsoft SQL Server (SSMS)
-- Description: Creates a test user (test@finos.app) with sample accounts,
--              transactions, budget, goal, and loan for development/testing
-- WARNING:     This script is for development/testing ONLY. Do NOT run in
--              production. Uses known passwords for convenience.
-- ============================================================================

USE FinOS;
GO

SET NOCOUNT ON;
GO

-- ============================================================================
-- Variables
-- ============================================================================
DECLARE @TestEmail       NVARCHAR(256)  = N'test@finos.app';
DECLARE @PasswordHash    NVARCHAR(512)  = N'HASHED_TestPassword123!';  -- Placeholder hash
DECLARE @PasswordSalt    NVARCHAR(256)  = N'SALT_TestUser_2026';
DECLARE @TestUserId      BIGINT;
DECLARE @SavingsAccountId BIGINT;
DECLARE @CreditCardId    BIGINT;
DECLARE @CashAccountId   BIGINT;
DECLARE @SalaryCatId     BIGINT;
DECLARE @RentCatId       BIGINT;
DECLARE @GroceriesCatId  BIGINT;
DECLARE @DiningCatId     BIGINT;
DECLARE @TransportCatId  BIGINT;
DECLARE @UtilitiesCatId  BIGINT;
DECLARE @ShoppingCatId   BIGINT;
DECLARE @EntertainmentCatId BIGINT;
DECLARE @HealthCatId     BIGINT;
DECLARE @FreelanceCatId  BIGINT;
DECLARE @BudgetId        BIGINT;
DECLARE @GoalId          BIGINT;
DECLARE @LoanId          BIGINT;
DECLARE @Today           DATE = CAST(SYSUTCDATETIME() AS DATE);
DECLARE @CurrentMonth    DATE = DATEFROMPARTS(YEAR(SYSUTCDATETIME()), MONTH(SYSUTCDATETIME()), 1);

PRINT N'=================================================================';
PRINT N'  FinOS Test Data Generator';
PRINT N'  Email: ' + @TestEmail;
PRINT N'  Date:  ' + CONVERT(NVARCHAR(30), SYSUTCDATETIME(), 120);
PRINT N'=================================================================';

-- ============================================================================
-- Step 1: Create Test User
-- ============================================================================
PRINT N'';
PRINT N'>>> Creating test user...';

-- Check if user already exists
IF EXISTS (SELECT 1 FROM Security.Users WHERE Email = @TestEmail AND DeletedAt IS NULL)
BEGIN
    SELECT @TestUserId = Id FROM Security.Users WHERE Email = @TestEmail AND DeletedAt IS NULL;
    PRINT N'  User already exists with Id: ' + CAST(@TestUserId AS NVARCHAR(20));
    PRINT N'  Skipping user creation, will reuse existing user.';
END
ELSE
BEGIN
    INSERT INTO Security.Users
        (Email, PasswordHash, PasswordSalt, FirstName, LastName, PhoneNumber,
         IsActive, EmailVerified, Currency, TimeZone, Locale)
    VALUES
        (@TestEmail, @PasswordHash, @PasswordSalt, N'Test', N'User', N'+919876543210',
         1, 1, N'INR', N'Asia/Kolkata', N'en-IN');

    SET @TestUserId = SCOPE_IDENTITY();
    PRINT N'  Created user with Id: ' + CAST(@TestUserId AS NVARCHAR(20));

    -- Assign User role
    DECLARE @UserRoleId INT;
    SELECT @UserRoleId = Id FROM Security.Roles WHERE Name = N'User';
    IF @UserRoleId IS NOT NULL
    BEGIN
        INSERT INTO Security.UserRoles (UserId, RoleId) VALUES (@TestUserId, @UserRoleId);
        PRINT N'  Assigned User role.';
    END
END
GO

-- ============================================================================
-- Step 2: Create Accounts (Savings, Credit Card, Cash)
-- ============================================================================
PRINT N'';
PRINT N'>>> Creating accounts...';

DECLARE @TestUserId      BIGINT = (SELECT Id FROM Security.Users WHERE Email = N'test@finos.app' AND DeletedAt IS NULL);
DECLARE @SavingsTypeId   INT;
DECLARE @CreditCardTypeId INT;
DECLARE @CashTypeId      INT;
DECLARE @SavingsAccountId BIGINT;
DECLARE @CreditCardId     BIGINT;
DECLARE @CashAccountId    BIGINT;

-- Get account type IDs
SELECT @SavingsTypeId = Id FROM Core.AccountTypes WHERE Name = N'Savings';
SELECT @CreditCardTypeId = Id FROM Core.AccountTypes WHERE Name = N'CreditCard';
SELECT @CashTypeId = Id FROM Core.AccountTypes WHERE Name = N'Cash';

-- Savings Account (SBI)
IF NOT EXISTS (SELECT 1 FROM Core.Accounts WHERE UserId = @TestUserId AND Name = N'SBI Savings')
BEGIN
    INSERT INTO Core.Accounts
        (UserId, AccountTypeId, Name, InstitutionName, AccountNumber, Balance, Currency, Color, Icon, IsActive)
    VALUES
        (@TestUserId, @SavingsTypeId, N'SBI Savings', N'State Bank of India', N'XXXX1234', 150000.00, N'INR', N'#1E88E5', N'bank', 1);
    SET @SavingsAccountId = SCOPE_IDENTITY();
    PRINT N'  Created SBI Savings account (Id: ' + CAST(@SavingsAccountId AS NVARCHAR(20)) + N')';
END
ELSE
BEGIN
    SELECT @SavingsAccountId = Id FROM Core.Accounts WHERE UserId = @TestUserId AND Name = N'SBI Savings';
    PRINT N'  SBI Savings account already exists (Id: ' + CAST(@SavingsAccountId AS NVARCHAR(20)) + N')';
END

-- Credit Card (HDFC)
IF NOT EXISTS (SELECT 1 FROM Core.Accounts WHERE UserId = @TestUserId AND Name = N'HDFC Credit Card')
BEGIN
    INSERT INTO Core.Accounts
        (UserId, AccountTypeId, Name, InstitutionName, AccountNumber, Balance, CreditLimit, Currency, Color, Icon, IsIncludedInNetWorth, IsActive)
    VALUES
        (@TestUserId, @CreditCardTypeId, N'HDFC Credit Card', N'HDFC Bank', N'XXXX5678', -15000.00, 200000.00, N'INR', N'#E53935', N'credit-card', 0, 1);
    SET @CreditCardId = SCOPE_IDENTITY();
    PRINT N'  Created HDFC Credit Card account (Id: ' + CAST(@CreditCardId AS NVARCHAR(20)) + N')';
END
ELSE
BEGIN
    SELECT @CreditCardId = Id FROM Core.Accounts WHERE UserId = @TestUserId AND Name = N'HDFC Credit Card';
    PRINT N'  HDFC Credit Card already exists (Id: ' + CAST(@CreditCardId AS NVARCHAR(20)) + N')';
END

-- Cash
IF NOT EXISTS (SELECT 1 FROM Core.Accounts WHERE UserId = @TestUserId AND Name = N'Cash')
BEGIN
    INSERT INTO Core.Accounts
        (UserId, AccountTypeId, Name, Balance, Currency, Color, Icon, IsActive)
    VALUES
        (@TestUserId, @CashTypeId, N'Cash', 5000.00, N'INR', N'#43A047', N'cash', 1);
    SET @CashAccountId = SCOPE_IDENTITY();
    PRINT N'  Created Cash account (Id: ' + CAST(@CashAccountId AS NVARCHAR(20)) + N')';
END
ELSE
BEGIN
    SELECT @CashAccountId = Id FROM Core.Accounts WHERE UserId = @TestUserId AND Name = N'Cash';
    PRINT N'  Cash account already exists (Id: ' + CAST(@CashAccountId AS NVARCHAR(20)) + N')';
END
GO

-- ============================================================================
-- Step 3: Create Categories for Sample Transactions
-- ============================================================================
PRINT N'';
PRINT N'>>> Creating categories...';

DECLARE @TestUserId BIGINT = (SELECT Id FROM Security.Users WHERE Email = N'test@finos.app' AND DeletedAt IS NULL);

-- Income categories
IF NOT EXISTS (SELECT 1 FROM Core.Categories WHERE UserId = @TestUserId AND Name = N'Salary' AND Type = N'Income')
    INSERT INTO Core.Categories (UserId, Name, Type, Icon, Color, IsSystem, IsActive) VALUES (@TestUserId, N'Salary', N'Income', N'briefcase', N'#4CAF50', 0, 1);
IF NOT EXISTS (SELECT 1 FROM Core.Categories WHERE UserId = @TestUserId AND Name = N'Freelance Income' AND Type = N'Income')
    INSERT INTO Core.Categories (UserId, Name, Type, Icon, Color, IsSystem, IsActive) VALUES (@TestUserId, N'Freelance Income', N'Income', N'laptop', N'#8BC34A', 0, 1);

-- Expense categories
IF NOT EXISTS (SELECT 1 FROM Core.Categories WHERE UserId = @TestUserId AND Name = N'Rent' AND Type = N'Expense')
    INSERT INTO Core.Categories (UserId, Name, Type, Icon, Color, IsSystem, IsActive) VALUES (@TestUserId, N'Rent', N'Expense', N'home', N'#F44336', 0, 1);
IF NOT EXISTS (SELECT 1 FROM Core.Categories WHERE UserId = @TestUserId AND Name = N'Groceries' AND Type = N'Expense')
    INSERT INTO Core.Categories (UserId, Name, Type, Icon, Color, IsSystem, IsActive) VALUES (@TestUserId, N'Groceries', N'Expense', N'shopping-cart', N'#FF9800', 0, 1);
IF NOT EXISTS (SELECT 1 FROM Core.Categories WHERE UserId = @TestUserId AND Name = N'Dining Out' AND Type = N'Expense')
    INSERT INTO Core.Categories (UserId, Name, Type, Icon, Color, IsSystem, IsActive) VALUES (@TestUserId, N'Dining Out', N'Expense', N'silverware-fork-knife', N'#E91E63', 0, 1);
IF NOT EXISTS (SELECT 1 FROM Core.Categories WHERE UserId = @TestUserId AND Name = N'Transport' AND Type = N'Expense')
    INSERT INTO Core.Categories (UserId, Name, Type, Icon, Color, IsSystem, IsActive) VALUES (@TestUserId, N'Transport', N'Expense', N'car', N'#9C27B0', 0, 1);
IF NOT EXISTS (SELECT 1 FROM Core.Categories WHERE UserId = @TestUserId AND Name = N'Utilities' AND Type = N'Expense')
    INSERT INTO Core.Categories (UserId, Name, Type, Icon, Color, IsSystem, IsActive) VALUES (@TestUserId, N'Utilities', N'Expense', N'flash', N'#FFC107', 0, 1);
IF NOT EXISTS (SELECT 1 FROM Core.Categories WHERE UserId = @TestUserId AND Name = N'Shopping' AND Type = N'Expense')
    INSERT INTO Core.Categories (UserId, Name, Type, Icon, Color, IsSystem, IsActive) VALUES (@TestUserId, N'Shopping', N'Expense', N'store', N'#3F51B5', 0, 1);
IF NOT EXISTS (SELECT 1 FROM Core.Categories WHERE UserId = @TestUserId AND Name = N'Entertainment' AND Type = N'Expense')
    INSERT INTO Core.Categories (UserId, Name, Type, Icon, Color, IsSystem, IsActive) VALUES (@TestUserId, N'Entertainment', N'Expense', N'movie', N'#00BCD4', 0, 1);
IF NOT EXISTS (SELECT 1 FROM Core.Categories WHERE UserId = @TestUserId AND Name = N'Health' AND Type = N'Expense')
    INSERT INTO Core.Categories (UserId, Name, Type, Icon, Color, IsSystem, IsActive) VALUES (@TestUserId, N'Health', N'Expense', N'hospital-box', N'#009688', 0, 1);

PRINT N'  Categories created/verified.';
GO

-- ============================================================================
-- Step 4: Add Sample Transactions for Current Month
-- ============================================================================
PRINT N'';
PRINT N'>>> Creating sample transactions...';

DECLARE @TestUserId      BIGINT = (SELECT Id FROM Security.Users WHERE Email = N'test@finos.app' AND DeletedAt IS NULL);
DECLARE @SavingsAccountId BIGINT = (SELECT Id FROM Core.Accounts WHERE UserId = @TestUserId AND Name = N'SBI Savings');
DECLARE @CreditCardId     BIGINT = (SELECT Id FROM Core.Accounts WHERE UserId = @TestUserId AND Name = N'HDFC Credit Card');
DECLARE @CashAccountId    BIGINT = (SELECT Id FROM Core.Accounts WHERE UserId = @TestUserId AND Name = N'Cash');
DECLARE @SalaryCatId      BIGINT = (SELECT Id FROM Core.Categories WHERE UserId = @TestUserId AND Name = N'Salary');
DECLARE @FreelanceCatId   BIGINT = (SELECT Id FROM Core.Categories WHERE UserId = @TestUserId AND Name = N'Freelance Income');
DECLARE @RentCatId        BIGINT = (SELECT Id FROM Core.Categories WHERE UserId = @TestUserId AND Name = N'Rent');
DECLARE @GroceriesCatId   BIGINT = (SELECT Id FROM Core.Categories WHERE UserId = @TestUserId AND Name = N'Groceries');
DECLARE @DiningCatId      BIGINT = (SELECT Id FROM Core.Categories WHERE UserId = @TestUserId AND Name = N'Dining Out');
DECLARE @TransportCatId   BIGINT = (SELECT Id FROM Core.Categories WHERE UserId = @TestUserId AND Name = N'Transport');
DECLARE @UtilitiesCatId   BIGINT = (SELECT Id FROM Core.Categories WHERE UserId = @TestUserId AND Name = N'Utilities');
DECLARE @ShoppingCatId    BIGINT = (SELECT Id FROM Core.Categories WHERE UserId = @TestUserId AND Name = N'Shopping');
DECLARE @EntertainmentCatId BIGINT = (SELECT Id FROM Core.Categories WHERE UserId = @TestUserId AND Name = N'Entertainment');
DECLARE @HealthCatId      BIGINT = (SELECT Id FROM Core.Categories WHERE UserId = @TestUserId AND Name = N'Health');
DECLARE @CurrentMonth     DATE = DATEFROMPARTS(YEAR(SYSUTCDATETIME()), MONTH(SYSUTCDATETIME()), 1);
DECLARE @TxnCount         INT = 0;

-- Income: Salary (1st of month)
IF NOT EXISTS (SELECT 1 FROM Core.Transactions WHERE UserId = @TestUserId AND Description = N'Monthly Salary' AND TransactionDate = @CurrentMonth AND DeletedAt IS NULL)
BEGIN
    INSERT INTO Core.Transactions (UserId, AccountId, CategoryId, Type, Amount, Description, TransactionDate, Source)
    VALUES (@TestUserId, @SavingsAccountId, @SalaryCatId, N'Income', 120000.00, N'Monthly Salary', @CurrentMonth, N'Manual');
    SET @TxnCount = @TxnCount + 1;
END

-- Income: Freelance
IF NOT EXISTS (SELECT 1 FROM Core.Transactions WHERE UserId = @TestUserId AND Description = N'Freelance Project Payment' AND TransactionDate = DATEADD(DAY, 5, @CurrentMonth) AND DeletedAt IS NULL)
BEGIN
    INSERT INTO Core.Transactions (UserId, AccountId, CategoryId, Type, Amount, Description, TransactionDate, Source)
    VALUES (@TestUserId, @SavingsAccountId, @FreelanceCatId, N'Income', 25000.00, N'Freelance Project Payment', DATEADD(DAY, 5, @CurrentMonth), N'Manual');
    SET @TxnCount = @TxnCount + 1;
END

-- Expense: Rent (1st of month)
IF NOT EXISTS (SELECT 1 FROM Core.Transactions WHERE UserId = @TestUserId AND Description = N'Monthly Rent' AND TransactionDate = @CurrentMonth AND DeletedAt IS NULL)
BEGIN
    INSERT INTO Core.Transactions (UserId, AccountId, CategoryId, Type, Amount, Description, TransactionDate, MerchantName, Source)
    VALUES (@TestUserId, @SavingsAccountId, @RentCatId, N'Expense', 25000.00, N'Monthly Rent', @CurrentMonth, N'Landlord', N'Manual');
    SET @TxnCount = @TxnCount + 1;
END

-- Expense: Groceries (multiple)
IF NOT EXISTS (SELECT 1 FROM Core.Transactions WHERE UserId = @TestUserId AND Description = N'BigBasket Grocery Order' AND DeletedAt IS NULL)
BEGIN
    INSERT INTO Core.Transactions (UserId, AccountId, CategoryId, Type, Amount, Description, TransactionDate, MerchantName, Source)
    VALUES (@TestUserId, @CreditCardId, @GroceriesCatId, N'Expense', 4500.00, N'BigBasket Grocery Order', DATEADD(DAY, 3, @CurrentMonth), N'BigBasket', N'Manual');
    SET @TxnCount = @TxnCount + 1;
END

IF NOT EXISTS (SELECT 1 FROM Core.Transactions WHERE UserId = @TestUserId AND Description = N'Daily Groceries - Veggies & Fruits' AND DeletedAt IS NULL)
BEGIN
    INSERT INTO Core.Transactions (UserId, AccountId, CategoryId, Type, Amount, Description, TransactionDate, Source)
    VALUES (@TestUserId, @CashAccountId, @GroceriesCatId, N'Expense', 800.00, N'Daily Groceries - Veggies & Fruits', DATEADD(DAY, 7, @CurrentMonth), N'Manual');
    SET @TxnCount = @TxnCount + 1;
END

-- Expense: Dining Out
IF NOT EXISTS (SELECT 1 FROM Core.Transactions WHERE UserId = @TestUserId AND Description = N'Dinner at Mainland China' AND DeletedAt IS NULL)
BEGIN
    INSERT INTO Core.Transactions (UserId, AccountId, CategoryId, Type, Amount, Description, TransactionDate, MerchantName, Source)
    VALUES (@TestUserId, @CreditCardId, @DiningCatId, N'Expense', 2800.00, N'Dinner at Mainland China', DATEADD(DAY, 8, @CurrentMonth), N'Mainland China', N'Manual');
    SET @TxnCount = @TxnCount + 1;
END

IF NOT EXISTS (SELECT 1 FROM Core.Transactions WHERE UserId = @TestUserId AND Description = N'Swiggy Order' AND DeletedAt IS NULL)
BEGIN
    INSERT INTO Core.Transactions (UserId, AccountId, CategoryId, Type, Amount, Description, TransactionDate, MerchantName, Source)
    VALUES (@TestUserId, @CreditCardId, @DiningCatId, N'Expense', 650.00, N'Swiggy Order', DATEADD(DAY, 12, @CurrentMonth), N'Swiggy', N'Manual');
    SET @TxnCount = @TxnCount + 1;
END

-- Expense: Transport
IF NOT EXISTS (SELECT 1 FROM Core.Transactions WHERE UserId = @TestUserId AND Description = N'Uber Rides' AND DeletedAt IS NULL)
BEGIN
    INSERT INTO Core.Transactions (UserId, AccountId, CategoryId, Type, Amount, Description, TransactionDate, MerchantName, MerchantCategory, Source)
    VALUES (@TestUserId, @CashAccountId, @TransportCatId, N'Expense', 1500.00, N'Uber Rides', DATEADD(DAY, 4, @CurrentMonth), N'Uber', N'Transport', N'Manual');
    SET @TxnCount = @TxnCount + 1;
END

IF NOT EXISTS (SELECT 1 FROM Core.Transactions WHERE UserId = @TestUserId AND Description = N'Metro Card Recharge' AND DeletedAt IS NULL)
BEGIN
    INSERT INTO Core.Transactions (UserId, AccountId, CategoryId, Type, Amount, Description, TransactionDate, MerchantName, Source)
    VALUES (@TestUserId, @SavingsAccountId, @TransportCatId, N'Expense', 1000.00, N'Metro Card Recharge', DATEADD(DAY, 2, @CurrentMonth), N'DMRC', N'Manual');
    SET @TxnCount = @TxnCount + 1;
END

-- Expense: Utilities
IF NOT EXISTS (SELECT 1 FROM Core.Transactions WHERE UserId = @TestUserId AND Description = N'Electricity Bill' AND DeletedAt IS NULL)
BEGIN
    INSERT INTO Core.Transactions (UserId, AccountId, CategoryId, Type, Amount, Description, TransactionDate, MerchantName, Source)
    VALUES (@TestUserId, @SavingsAccountId, @UtilitiesCatId, N'Expense', 3500.00, N'Electricity Bill', DATEADD(DAY, 10, @CurrentMonth), N'BSES Rajdhani', N'Manual');
    SET @TxnCount = @TxnCount + 1;
END

IF NOT EXISTS (SELECT 1 FROM Core.Transactions WHERE UserId = @TestUserId AND Description = N'Mobile Recharge - Jio' AND DeletedAt IS NULL)
BEGIN
    INSERT INTO Core.Transactions (UserId, AccountId, CategoryId, Type, Amount, Description, TransactionDate, MerchantName, Source)
    VALUES (@TestUserId, @CreditCardId, @UtilitiesCatId, N'Expense', 299.00, N'Mobile Recharge - Jio', DATEADD(DAY, 1, @CurrentMonth), N'Reliance Jio', N'Manual');
    SET @TxnCount = @TxnCount + 1;
END

-- Expense: Shopping
IF NOT EXISTS (SELECT 1 FROM Core.Transactions WHERE UserId = @TestUserId AND Description = N'Amazon Order - Headphones' AND DeletedAt IS NULL)
BEGIN
    INSERT INTO Core.Transactions (UserId, AccountId, CategoryId, Type, Amount, Description, TransactionDate, MerchantName, Source)
    VALUES (@TestUserId, @CreditCardId, @ShoppingCatId, N'Expense', 3999.00, N'Amazon Order - Headphones', DATEADD(DAY, 6, @CurrentMonth), N'Amazon', N'Manual');
    SET @TxnCount = @TxnCount + 1;
END

-- Expense: Entertainment
IF NOT EXISTS (SELECT 1 FROM Core.Transactions WHERE UserId = @TestUserId AND Description = N'Netflix Subscription' AND DeletedAt IS NULL)
BEGIN
    INSERT INTO Core.Transactions (UserId, AccountId, CategoryId, Type, Amount, Description, TransactionDate, MerchantName, Source)
    VALUES (@TestUserId, @CreditCardId, @EntertainmentCatId, N'Expense', 649.00, N'Netflix Subscription', DATEADD(DAY, 5, @CurrentMonth), N'Netflix', N'Manual');
    SET @TxnCount = @TxnCount + 1;
END

-- Expense: Health
IF NOT EXISTS (SELECT 1 FROM Core.Transactions WHERE UserId = @TestUserId AND Description = N'Pharmacy - Medicines' AND DeletedAt IS NULL)
BEGIN
    INSERT INTO Core.Transactions (UserId, AccountId, CategoryId, Type, Amount, Description, TransactionDate, MerchantName, Source)
    VALUES (@TestUserId, @CashAccountId, @HealthCatId, N'Expense', 1200.00, N'Pharmacy - Medicines', DATEADD(DAY, 9, @CurrentMonth), N'Apollo Pharmacy', N'Manual');
    SET @TxnCount = @TxnCount + 1;
END

PRINT N'  Created ' + CAST(@TxnCount AS NVARCHAR(10)) + N' transactions.';
GO

-- ============================================================================
-- Step 5: Create a Budget with Categories
-- ============================================================================
PRINT N'';
PRINT N'>>> Creating sample budget...';

DECLARE @TestUserId      BIGINT = (SELECT Id FROM Security.Users WHERE Email = N'test@finos.app' AND DeletedAt IS NULL);
DECLARE @CurrentMonth    DATE = DATEFROMPARTS(YEAR(SYSUTCDATETIME()), MONTH(SYSUTCDATETIME()), 1);
DECLARE @BudgetId        BIGINT;
DECLARE @RentCatId       BIGINT = (SELECT Id FROM Core.Categories WHERE UserId = @TestUserId AND Name = N'Rent');
DECLARE @GroceriesCatId  BIGINT = (SELECT Id FROM Core.Categories WHERE UserId = @TestUserId AND Name = N'Groceries');
DECLARE @DiningCatId     BIGINT = (SELECT Id FROM Core.Categories WHERE UserId = @TestUserId AND Name = N'Dining Out');
DECLARE @TransportCatId  BIGINT = (SELECT Id FROM Core.Categories WHERE UserId = @TestUserId AND Name = N'Transport');
DECLARE @UtilitiesCatId  BIGINT = (SELECT Id FROM Core.Categories WHERE UserId = @TestUserId AND Name = N'Utilities');
DECLARE @ShoppingCatId   BIGINT = (SELECT Id FROM Core.Categories WHERE UserId = @TestUserId AND Name = N'Shopping');
DECLARE @EntertainmentCatId BIGINT = (SELECT Id FROM Core.Categories WHERE UserId = @TestUserId AND Name = N'Entertainment');
DECLARE @HealthCatId     BIGINT = (SELECT Id FROM Core.Categories WHERE UserId = @TestUserId AND Name = N'Health');

IF NOT EXISTS (
    SELECT 1 FROM Budget.Budgets
    WHERE UserId = @TestUserId
      AND Name = N'Monthly Budget'
      AND CAST(SYSUTCDATETIME() AS DATE) BETWEEN StartDate AND EndDate
      AND DeletedAt IS NULL
)
BEGIN
    INSERT INTO Budget.Budgets
        (UserId, Name, PeriodType, StartDate, EndDate, TotalBudgetAmount, Currency, AlertThresholdPct, IsActive)
    VALUES
        (@TestUserId, N'Monthly Budget', N'Monthly', @CurrentMonth, EOMONTH(@CurrentMonth), 70000.00, N'INR', 80.00, 1);

    SET @BudgetId = SCOPE_IDENTITY();

    -- Add budget categories
    INSERT INTO Budget.BudgetCategories (BudgetId, CategoryId, CustomLabel, AllocatedAmount, AlertThresholdPct, SortOrder)
    VALUES
        (@BudgetId, @RentCatId,        N'Rent',          25000.00, 90.00, 1),
        (@BudgetId, @GroceriesCatId,   N'Groceries',      8000.00, 85.00, 2),
        (@BudgetId, @DiningCatId,      N'Dining Out',     5000.00, 80.00, 3),
        (@BudgetId, @TransportCatId,   N'Transport',      5000.00, 80.00, 4),
        (@BudgetId, @UtilitiesCatId,   N'Utilities',      5000.00, 90.00, 5),
        (@BudgetId, @ShoppingCatId,    N'Shopping',       10000.00, 80.00, 6),
        (@BudgetId, @EntertainmentCatId, N'Entertainment', 3000.00, 80.00, 7),
        (@BudgetId, @HealthCatId,      N'Health',          5000.00, 90.00, 8),
        (@BudgetId, NULL,              N'Other',           4000.00, 80.00, 9);

    PRINT N'  Created Monthly Budget (₹70,000) with 9 categories.';
END
ELSE
BEGIN
    PRINT N'  Monthly Budget already exists for current month.';
END
GO

-- ============================================================================
-- Step 6: Create a Sample Goal (Emergency Fund)
-- ============================================================================
PRINT N'';
PRINT N'>>> Creating sample goal...';

DECLARE @TestUserId BIGINT = (SELECT Id FROM Security.Users WHERE Email = N'test@finos.app' AND DeletedAt IS NULL);
DECLARE @SavingsAccountId BIGINT = (SELECT Id FROM Core.Accounts WHERE UserId = @TestUserId AND Name = N'SBI Savings');
DECLARE @GoalId BIGINT;

IF NOT EXISTS (SELECT 1 FROM Goals.Goals WHERE UserId = @TestUserId AND Name = N'Emergency Fund' AND DeletedAt IS NULL)
BEGIN
    INSERT INTO Goals.Goals
        (UserId, Name, Description, Category, TargetAmount, CurrentAmount,
         MonthlyContribution, StartDate, TargetDate, Priority,
         LinkedAccountIds, Icon, Color, Status)
    VALUES
        (@TestUserId, N'Emergency Fund', N'6 months of expenses as emergency fund',
         N'Emergency', 500000.00, 200000.00,
         20000.00, DATEADD(MONTH, -6, SYSUTCDATETIME()),
         DATEADD(MONTH, 12, SYSUTCDATETIME()), N'High',
         N'[' + CAST(@SavingsAccountId AS NVARCHAR(20)) + N']',
         N'shield', N'#FF9800', N'InProgress');

    SET @GoalId = SCOPE_IDENTITY();
    PRINT N'  Created Emergency Fund goal (₹5,00,000 target, ₹2,00,000 saved).';
END
ELSE
BEGIN
    PRINT N'  Emergency Fund goal already exists.';
END
GO

-- ============================================================================
-- Step 7: Create a Sample Loan (Home Loan)
-- ============================================================================
PRINT N'';
PRINT N'>>> Creating sample loan...';

DECLARE @TestUserId      BIGINT = (SELECT Id FROM Security.Users WHERE Email = N'test@finos.app' AND DeletedAt IS NULL);
DECLARE @SavingsAccountId BIGINT = (SELECT Id FROM Core.Accounts WHERE UserId = @TestUserId AND Name = N'SBI Savings');
DECLARE @LoanId           BIGINT;
DECLARE @HomeLoanTypeId   INT;

SELECT @HomeLoanTypeId = Id FROM Loan.LoanTypes WHERE Name = N'HomeLoan';

IF @HomeLoanTypeId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Loan.Loans WHERE UserId = @TestUserId AND LenderName = N'SBI Home Loan' AND DeletedAt IS NULL)
    BEGIN
        INSERT INTO Loan.Loans
            (UserId, LoanTypeId, AccountId, LenderName, LoanAccountNumber,
             PrincipalAmount, OutstandingPrincipal, InterestRate, InterestType,
             TenureMonths, RemainingTenureMonths, EMI, EMIDayOfMonth,
             StartDate, MaturityDate, ProcessingFee, IsPrepaymentAllowed,
             TotalInterestPayable, TotalAmountPayable, NextEMIDate, Status)
        VALUES
            (@TestUserId, @HomeLoanTypeId, @SavingsAccountId, N'SBI Home Loan', N'SBIHL001234',
             5000000.00, 4500000.00, 8.50, N'Fixed',
             240, 216, 43361.00, 5,
             DATEFROMPARTS(2024, 1, 5), DATEFROMPARTS(2044, 1, 5), 10000.00, 1,
             5406640.00, 10406640.00, DATEFROMPARTS(YEAR(SYSUTCDATETIME()), MONTH(SYSUTCDATETIME()), 5), N'Active');

        SET @LoanId = SCOPE_IDENTITY();
        PRINT N'  Created Home Loan (₹50,00,000 @ 8.5%, 20 years, EMI ₹43,361).';
    END
    ELSE
    BEGIN
        PRINT N'  Home Loan already exists.';
    END
END
ELSE
BEGIN
    PRINT N'  WARNING: HomeLoan type not found in Loan.LoanTypes. Run seed data first.';
END
GO

-- ============================================================================
-- Summary
-- ============================================================================
PRINT N'';
PRINT N'=================================================================';
PRINT N'  Test Data Generation Complete!';
PRINT N'  Email:    test@finos.app';
PRINT N'  Password: TestPassword123! (use app hash in production)';
PRINT N'  ';
PRINT N'  Created:';
PRINT N'    - 1 User (test@finos.app)';
PRINT N'    - 3 Accounts (SBI Savings, HDFC Credit Card, Cash)';
PRINT N'    - 11 Categories (2 Income, 9 Expense)';
PRINT N'    - 14 Transactions (salary, rent, groceries, dining, etc.)';
PRINT N'    - 1 Budget (₹70,000/month with 9 categories)';
PRINT N'    - 1 Goal (Emergency Fund ₹5L, ₹2L saved)';
PRINT N'    - 1 Loan (Home Loan ₹50L @ 8.5%, 20yr)';
PRINT N'=================================================================';
GO
