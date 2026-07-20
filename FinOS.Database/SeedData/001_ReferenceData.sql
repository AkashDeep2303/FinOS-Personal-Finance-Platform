-- ============================================================================
-- FinOS Database - Seed Data: Reference / Lookup Data
-- Target: Microsoft SQL Server (SSMS)
-- Description: Inserts system-level reference data required for the application
--              to function: AccountTypes, Categories, InvestmentTypes, LoanTypes,
--              NotificationTypes, and GoalTemplates.
-- ============================================================================

USE FinOS;
GO

-- ---------------------------------------------------------------------------
-- 1. Core.AccountTypes
-- ---------------------------------------------------------------------------
PRINT N'Inserting Core.AccountTypes...';

-- Insert only missing account types (idempotent)
MERGE INTO Core.AccountTypes AS Target
USING (
    VALUES
        (N'Savings', N'piggy-bank', 1),
        (N'Current', N'bank', 0),
        (N'CreditCard', N'credit-card', 0),
        (N'Loan', N'hand-coins', 0),
        (N'Investment', N'trending-up', 0),
        (N'Cash', N'banknote', 1),
        (N'Wallet', N'wallet', 0),
        (N'EPF', N'shield-check', 0),
        (N'PPF', N'shield', 0),
        (N'NPS', N'retirement', 0)
) AS Source(Name, Icon, IsDefault)
ON Target.Name = Source.Name
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Name, Icon, IsDefault, CreatedAt)
    VALUES (Source.Name, Source.Icon, Source.IsDefault, SYSUTCDATETIME());
GO

-- ---------------------------------------------------------------------------
-- 2. Core.Categories (system categories: UserId=NULL, IsSystem=1)
--    Hierarchy: Type-level parents -> main categories -> sub-categories
--    We use a table variable to capture parent IDs so children can reference them.
-- ---------------------------------------------------------------------------

PRINT N'Inserting Core.Categories...';

-- =========================================================================
-- 2a. Income categories (no sub-categories)
-- =========================================================================
INSERT INTO Core.Categories
    (UserId, ParentId, Name, Type, Icon, Color, BudgetAmount, IsSystem, IsActive, SortOrder, CreatedAt, UpdatedAt)
VALUES
    (NULL, NULL, N'Salary',          N'Income', N'briefcase',      N'#4CAF50', NULL, 1, 1,  1, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (NULL, NULL, N'Freelance',       N'Income', N'laptop',         N'#4CAF50', NULL, 1, 1,  2, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (NULL, NULL, N'Business',        N'Income', N'store',          N'#4CAF50', NULL, 1, 1,  3, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (NULL, NULL, N'Rental',          N'Income', N'home',           N'#4CAF50', NULL, 1, 1,  4, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (NULL, NULL, N'Dividend',        N'Income', N'bar-chart-2',    N'#4CAF50', NULL, 1, 1,  5, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (NULL, NULL, N'Interest',        N'Income', N'percent',        N'#4CAF50', NULL, 1, 1,  6, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (NULL, NULL, N'Gift',            N'Income', N'gift',           N'#4CAF50', NULL, 1, 1,  7, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (NULL, NULL, N'Refund',          N'Income', N'refresh-cw',     N'#4CAF50', NULL, 1, 1,  8, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (NULL, NULL, N'Other Income',    N'Income', N'plus-circle',    N'#4CAF50', NULL, 1, 1,  9, SYSUTCDATETIME(), SYSUTCDATETIME());
GO

-- =========================================================================
-- 2b. Expense categories (some have sub-categories)
--     First insert parent-level categories, capture their IDs, then insert
--     children that need a ParentId.
-- =========================================================================

-- Table variable to map category name -> generated Id for parent categories
DECLARE @ExpenseParentIds TABLE
(
    Name NVARCHAR(100),
    Id   BIGINT PRIMARY KEY
);

-- Insert top-level Expense categories (no ParentId)
INSERT INTO Core.Categories
    (UserId, ParentId, Name, Type, Icon, Color, BudgetAmount, IsSystem, IsActive, SortOrder, CreatedAt, UpdatedAt)
OUTPUT INSERTED.Name, INSERTED.Id INTO @ExpenseParentIds
VALUES
    (NULL, NULL, N'Rent',               N'Expense', N'home',           N'#F44336', NULL, 1, 1, 10, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (NULL, NULL, N'Groceries',           N'Expense', N'shopping-cart',  N'#F44336', NULL, 1, 1, 11, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (NULL, NULL, N'Dining Out',          N'Expense', N'utensils',       N'#F44336', NULL, 1, 1, 12, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (NULL, NULL, N'Utilities',           N'Expense', N'zap',            N'#F44336', NULL, 1, 1, 13, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (NULL, NULL, N'Internet',            N'Expense', N'wifi',           N'#F44336', NULL, 1, 1, 14, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (NULL, NULL, N'Mobile Recharge',     N'Expense', N'smartphone',     N'#F44336', NULL, 1, 1, 15, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (NULL, NULL, N'Transport',           N'Expense', N'navigation',     N'#F44336', NULL, 1, 1, 16, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (NULL, NULL, N'Insurance',           N'Expense', N'shield',         N'#F44336', NULL, 1, 1, 17, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (NULL, NULL, N'Medical',             N'Expense', N'heart-pulse',    N'#F44336', NULL, 1, 1, 18, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (NULL, NULL, N'Education',           N'Expense', N'book-open',      N'#F44336', NULL, 1, 1, 19, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (NULL, NULL, N'Shopping',            N'Expense', N'shopping-bag',   N'#F44336', NULL, 1, 1, 20, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (NULL, NULL, N'Entertainment',       N'Expense', N'film',           N'#F44336', NULL, 1, 1, 21, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (NULL, NULL, N'Personal Care',       N'Expense', N'smile',          N'#F44336', NULL, 1, 1, 22, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (NULL, NULL, N'Gym',                 N'Expense', N'dumbbell',       N'#F44336', NULL, 1, 1, 23, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (NULL, NULL, N'Travel',              N'Expense', N'plane',          N'#F44336', NULL, 1, 1, 24, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (NULL, NULL, N'Gifts & Donations',   N'Expense', N'heart',          N'#F44336', NULL, 1, 1, 25, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (NULL, NULL, N'Home Maintenance',    N'Expense', N'tool',           N'#F44336', NULL, 1, 1, 26, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (NULL, NULL, N'EMI',                 N'Expense', N'file-text',      N'#F44336', NULL, 1, 1, 27, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (NULL, NULL, N'Child Care',          N'Expense', N'baby',           N'#F44336', NULL, 1, 1, 28, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (NULL, NULL, N'Pet Care',            N'Expense', N'paw-print',      N'#F44336', NULL, 1, 1, 29, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (NULL, NULL, N'Other Expense',       N'Expense', N'minus-circle',   N'#F44336', NULL, 1, 1, 30, SYSUTCDATETIME(), SYSUTCDATETIME());

-- Insert sub-categories under 'Utilities'
INSERT INTO Core.Categories
    (UserId, ParentId, Name, Type, Icon, Color, BudgetAmount, IsSystem, IsActive, SortOrder, CreatedAt, UpdatedAt)
SELECT
    NULL, P.Id, N'Electricity', N'Expense', N'zap', N'#FF9800', NULL, 1, 1, 131, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM @ExpenseParentIds P WHERE P.Name = N'Utilities'
UNION ALL
SELECT
    NULL, P.Id, N'Gas', N'Expense', N'flame', N'#FF9800', NULL, 1, 1, 132, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM @ExpenseParentIds P WHERE P.Name = N'Utilities'
UNION ALL
SELECT
    NULL, P.Id, N'Water', N'Expense', N'droplet', N'#FF9800', NULL, 1, 1, 133, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM @ExpenseParentIds P WHERE P.Name = N'Utilities';

-- Insert sub-categories under 'Transport'
INSERT INTO Core.Categories
    (UserId, ParentId, Name, Type, Icon, Color, BudgetAmount, IsSystem, IsActive, SortOrder, CreatedAt, UpdatedAt)
SELECT
    NULL, P.Id, N'Fuel', N'Expense', N'fuel', N'#2196F3', NULL, 1, 1, 161, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM @ExpenseParentIds P WHERE P.Name = N'Transport'
UNION ALL
SELECT
    NULL, P.Id, N'Cab', N'Expense', N'car', N'#2196F3', NULL, 1, 1, 162, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM @ExpenseParentIds P WHERE P.Name = N'Transport'
UNION ALL
SELECT
    NULL, P.Id, N'Metro', N'Expense', N'train-front', N'#2196F3', NULL, 1, 1, 163, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM @ExpenseParentIds P WHERE P.Name = N'Transport'
UNION ALL
SELECT
    NULL, P.Id, N'Bus', N'Expense', N'bus', N'#2196F3', NULL, 1, 1, 164, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM @ExpenseParentIds P WHERE P.Name = N'Transport';

-- Insert sub-categories under 'Insurance'
INSERT INTO Core.Categories
    (UserId, ParentId, Name, Type, Icon, Color, BudgetAmount, IsSystem, IsActive, SortOrder, CreatedAt, UpdatedAt)
SELECT
    NULL, P.Id, N'Health Insurance', N'Expense', N'heart-pulse', N'#9C27B0', NULL, 1, 1, 171, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM @ExpenseParentIds P WHERE P.Name = N'Insurance'
UNION ALL
SELECT
    NULL, P.Id, N'Life Insurance', N'Expense', N'heart', N'#9C27B0', NULL, 1, 1, 172, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM @ExpenseParentIds P WHERE P.Name = N'Insurance'
UNION ALL
SELECT
    NULL, P.Id, N'Vehicle Insurance', N'Expense', N'car', N'#9C27B0', NULL, 1, 1, 173, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM @ExpenseParentIds P WHERE P.Name = N'Insurance';

-- Insert sub-categories under 'Shopping'
INSERT INTO Core.Categories
    (UserId, ParentId, Name, Type, Icon, Color, BudgetAmount, IsSystem, IsActive, SortOrder, CreatedAt, UpdatedAt)
SELECT
    NULL, P.Id, N'Clothes', N'Expense', N'shirt', N'#E91E63', NULL, 1, 1, 201, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM @ExpenseParentIds P WHERE P.Name = N'Shopping'
UNION ALL
SELECT
    NULL, P.Id, N'Electronics', N'Expense', N'monitor', N'#E91E63', NULL, 1, 1, 202, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM @ExpenseParentIds P WHERE P.Name = N'Shopping'
UNION ALL
SELECT
    NULL, P.Id, N'Home', N'Expense', N'lamp', N'#E91E63', NULL, 1, 1, 203, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM @ExpenseParentIds P WHERE P.Name = N'Shopping';

-- Insert sub-categories under 'Entertainment'
INSERT INTO Core.Categories
    (UserId, ParentId, Name, Type, Icon, Color, BudgetAmount, IsSystem, IsActive, SortOrder, CreatedAt, UpdatedAt)
SELECT
    NULL, P.Id, N'Movies', N'Expense', N'film', N'#00BCD4', NULL, 1, 1, 211, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM @ExpenseParentIds P WHERE P.Name = N'Entertainment'
UNION ALL
SELECT
    NULL, P.Id, N'Subscriptions', N'Expense', N'repeat', N'#00BCD4', NULL, 1, 1, 212, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM @ExpenseParentIds P WHERE P.Name = N'Entertainment'
UNION ALL
SELECT
    NULL, P.Id, N'Games', N'Expense', N'gamepad-2', N'#00BCD4', NULL, 1, 1, 213, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM @ExpenseParentIds P WHERE P.Name = N'Entertainment';

-- =========================================================================
-- 2c. Transfer categories (no sub-categories)
-- =========================================================================
INSERT INTO Core.Categories
    (UserId, ParentId, Name, Type, Icon, Color, BudgetAmount, IsSystem, IsActive, SortOrder, CreatedAt, UpdatedAt)
VALUES
    (NULL, NULL, N'Account Transfer',     N'Transfer', N'arrow-left-right', N'#607D8B', NULL, 1, 1, 31, SYSUTCDATETIME(), SYSUTCDATETIME()),
    (NULL, NULL, N'Credit Card Payment',  N'Transfer', N'credit-card',     N'#607D8B', NULL, 1, 1, 32, SYSUTCDATETIME(), SYSUTCDATETIME());
GO

-- ---------------------------------------------------------------------------
-- 3. Investment.InvestmentTypes
-- ---------------------------------------------------------------------------
PRINT N'Inserting Investment.InvestmentTypes...';

-- Insert investment types idempotently
MERGE INTO Investment.InvestmentTypes AS Target
USING (
    VALUES
        (N'MutualFund', N'Equity', N'trending-up', 0, 1),
        (N'Stock', N'Equity', N'bar-chart-2', 0, 2),
        (N'FD', N'Debt', N'lock', 0, 3),
        (N'Gold', N'Gold', N'circle-dot', 0, 4),
        (N'Crypto', N'Crypto', N'bitcoin', 0, 5),
        (N'EPF', N'Debt', N'shield-check', 1, 6),
        (N'PPF', N'Debt', N'shield', 1, 7),
        (N'NPS', N'Mixed', N'retirement', 1, 8),
        (N'RealEstate', N'RealEstate', N'building', 0, 9),
        (N'Bond', N'Debt', N'file-text', 0, 10),
        (N'RD', N'Debt', N'calendar', 0, 11),
        (N'SovereignGoldBond', N'Gold', N'circle-dot', 1, 12)
) AS Source(Name, AssetClass, Icon, IsTaxSaving, SortOrder)
ON Target.Name = Source.Name
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Name, AssetClass, Icon, IsTaxSaving, SortOrder)
    VALUES (Source.Name, Source.AssetClass, Source.Icon, Source.IsTaxSaving, Source.SortOrder);
GO

-- ---------------------------------------------------------------------------
-- 4. Loan.LoanTypes
-- ---------------------------------------------------------------------------
PRINT N'Inserting Loan.LoanTypes...';

-- Insert loan types idempotently
MERGE INTO Loan.LoanTypes AS Target
USING (
    VALUES
        (N'HomeLoan', N'home', 1),
        (N'CarLoan', N'car', 2),
        (N'PersonalLoan', N'user', 3),
        (N'EducationLoan', N'graduation-cap', 4),
        (N'CreditCard', N'credit-card', 5),
        (N'GoldLoan', N'circle-dot', 6),
        (N'LoanAgainstProperty', N'building', 7),
        (N'BusinessLoan', N'store', 8)
) AS Source(Name, Icon, SortOrder)
ON Target.Name = Source.Name
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Name, Icon, SortOrder)
    VALUES (Source.Name, Source.Icon, Source.SortOrder);
GO

-- ---------------------------------------------------------------------------
-- 5. Notifications.NotificationTypes
-- ---------------------------------------------------------------------------
PRINT N'Inserting Notifications.NotificationTypes...';

-- Insert notification types idempotently
MERGE INTO Notifications.NotificationTypes AS Target
USING (
    VALUES
        (N'Login', N'Alert when a new login is detected on your account', N'Security', 1),
        (N'PasswordChange', N'Alert when your password is changed', N'Security', 1),
        (N'Threshold', N'Alert when spending reaches a budget threshold percentage', N'Budget', 1),
        (N'Overspent', N'Alert when a budget category is overspent', N'Budget', 1),
        (N'EMIReminder', N'Reminder before an EMI payment is due', N'Loan', 1),
        (N'EMIOverdue', N'Alert when an EMI payment is overdue', N'Loan', 1),
        (N'SIPReminder', N'Reminder before an upcoming SIP installment', N'Investment', 1),
        (N'PriceAlert', N'Alert when an investment price crosses a specified threshold', N'Investment', 1),
        (N'MilestoneReached', N'Alert when a financial goal milestone is reached', N'Goal', 1),
        (N'TargetDateNear', N'Alert when a goal target date is approaching', N'Goal', 1),
        (N'FeatureUpdate', N'Notification about new features and improvements', N'System', 1),
        (N'Maintenance', N'Notification about scheduled maintenance windows', N'System', 1)
) AS Source(Name, Description, Category, IsEnabled)
ON Target.Name = Source.Name
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Name, Description, Category, IsEnabled)
    VALUES (Source.Name, Source.Description, Source.Category, Source.IsEnabled);
GO

-- ---------------------------------------------------------------------------
-- 6. Goals.GoalTemplates
-- ---------------------------------------------------------------------------
PRINT N'Inserting Goals.GoalTemplates...';

INSERT INTO Goals.GoalTemplates
    (Name, Description, Category, SuggestedAmount, SuggestedMonths, Icon, Color, SortOrder)
VALUES
    (N'Emergency Fund',
     N'Build a safety net covering 6 months of essential expenses to protect against unexpected financial shocks.',
     N'Emergency', 300000.00,  12, N'shield',       N'#F44336',  1),

    (N'Retirement Corpus',
     N'Accumulate a retirement fund to maintain your lifestyle and achieve financial independence post-retirement.',
     N'Retirement', 5000000.00, 240, N'sunset',      N'#FF9800',  2),

    (N'Home Purchase',
     N'Save for the down payment and additional costs for purchasing your dream home.',
     N'Purchase', 1500000.00,  60, N'home',          N'#4CAF50',  3),

    (N'Car Purchase',
     N'Save up to buy a new or pre-owned vehicle, including insurance and registration costs.',
     N'Purchase', 500000.00,   36, N'car',           N'#2196F3',  4),

    (N'Dream Vacation',
     N'Plan and save for that perfect getaway — flights, stay, experiences, and all.',
     N'Travel', 150000.00,    12, N'plane',          N'#00BCD4',  5),

    (N'Child Education',
     N'Build an education fund for your child''s school, college, or higher studies abroad.',
     N'Education', 2500000.00, 180, N'graduation-cap', N'#9C27B0', 6),

    (N'Wedding',
     N'Save for wedding expenses including venue, catering, attire, and celebrations.',
     N'Wedding', 1000000.00,  36, N'heart',          N'#E91E63',  7),

    (N'Debt Payoff',
     N'Create a structured plan to pay off outstanding debts — credit cards, personal loans, etc.',
     N'Debt', NULL,           24, N'check-circle',    N'#607D8B',  8),

    (N'Laptop Purchase',
     N'Save for a new laptop for work, study, or creative projects.',
     N'Purchase', 80000.00,    6, N'laptop',          N'#795548',  9),

    (N'Festival Shopping',
     N'Set aside a budget for festive season purchases — gifts, clothes, sweets, and decorations.',
     N'Purchase', 30000.00,     3, N'sparks',          N'#FF5722', 10);
GO

PRINT N'Reference data seeded successfully.';
GO
