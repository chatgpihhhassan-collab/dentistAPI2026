-- =====================================================================================
-- 🦷 DENTIA CLINICAL ECOSYSTEM: DOCTOR FEE SCHEDULES & TREATMENT BILLING SCHEMA
-- Database Engine : Microsoft SQL Server 2019 / 2022 / Azure SQL Database
-- Target Database : DentistAPI
-- Schema          : dentist
-- Author          : Dentia Clinical Architecture Team
-- Date            : September 2026
-- Description     :
--   Complete, idempotent DDL and DML migration script for:
--     1. [dentist].[DoctorFeeSchedules]  - Clinician-specific procedure rates and currency
--     2. [dentist].[Invoices]             - Patient master treatment bills & ledger
--     3. [dentist].[InvoiceItems]         - Itemized procedure line items & tooth mapping
--     4. [dentist].[Payments]             - Dual payment gateway (Online Card + Cash Vouchers)
--     5. Stored Procedures & Triggers    - Automated invoice balance & status calculation
--     6. Production Standard Seed Data   - 12 ADA CDT procedures for NZD and PKR clinicians
-- =====================================================================================

USE [DentistAPI];
GO

-- -------------------------------------------------------------------------------------
-- 0. SCHEMA VERIFICATION
-- -------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'dentist')
BEGIN
    EXEC('CREATE SCHEMA dentist');
    PRINT '>> [SUCCESS] Schema [dentist] created.';
END
ELSE
BEGIN
    PRINT '>> [INFO] Schema [dentist] already exists.';
END
GO


-- =====================================================================================
-- 1. TABLE: [dentist].[DoctorFeeSchedules]
--    Stores clinician-specific procedure fees, standard chair times, and clinic currency.
-- =====================================================================================
PRINT '>> Step 1: Provisioning [dentist].[DoctorFeeSchedules]...';

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = 'DoctorFeeSchedules' AND s.name = 'dentist')
BEGIN
    CREATE TABLE [dentist].[DoctorFeeSchedules] (
        [FeeScheduleID]     INT IDENTITY(1,1) NOT NULL,
        [DoctorID]          INT NOT NULL,
        [Currency]          NVARCHAR(10) NOT NULL CONSTRAINT DF_DoctorFeeSchedules_Currency DEFAULT 'NZD', -- 'NZD', 'PKR', 'USD', 'GBP', 'EUR', 'AUD'
        [ProcedureCode]     NVARCHAR(50) NOT NULL,                                                           -- e.g. 'D0120', 'D2391'
        [ProcedureName]     NVARCHAR(255) NOT NULL,
        [Category]          NVARCHAR(100) NOT NULL,                                                          -- Preventative, Restorative, Orthodontic, etc.
        [EstimatedDuration] NVARCHAR(50) NOT NULL CONSTRAINT DF_DoctorFeeSchedules_Duration DEFAULT '45 mins',
        [StandardFee]       DECIMAL(18,2) NOT NULL,
        [Description]       NVARCHAR(MAX) NULL,
        [IsActive]          BIT NOT NULL CONSTRAINT DF_DoctorFeeSchedules_IsActive DEFAULT 1,
        [CreatedAt]         DATETIME2 NOT NULL CONSTRAINT DF_DoctorFeeSchedules_CreatedAt DEFAULT SYSUTCDATETIME(),
        [UpdatedAt]         DATETIME2 NOT NULL CONSTRAINT DF_DoctorFeeSchedules_UpdatedAt DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_DoctorFeeSchedules PRIMARY KEY CLUSTERED ([FeeScheduleID] ASC),
        CONSTRAINT FK_DoctorFeeSchedules_Doctors FOREIGN KEY ([DoctorID]) 
            REFERENCES [dentist].[Doctors] ([DoctorID]) ON DELETE CASCADE
    );

    PRINT '   + [dentist].[DoctorFeeSchedules] table created successfully.';
END
ELSE
BEGIN
    PRINT '   + [dentist].[DoctorFeeSchedules] table already exists.';
END
GO

-- Indices for Fast Lookup
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DoctorFeeSchedules_Doctor_Active' AND object_id = OBJECT_ID('dentist.DoctorFeeSchedules'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DoctorFeeSchedules_Doctor_Active
    ON [dentist].[DoctorFeeSchedules] ([DoctorID], [IsActive])
    INCLUDE ([Currency], [ProcedureCode], [StandardFee], [ProcedureName], [Category]);
    PRINT '   + Created Index IX_DoctorFeeSchedules_Doctor_Active';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DoctorFeeSchedules_Doctor_Code' AND object_id = OBJECT_ID('dentist.DoctorFeeSchedules'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_DoctorFeeSchedules_Doctor_Code
    ON [dentist].[DoctorFeeSchedules] ([DoctorID], [ProcedureCode]);
    PRINT '   + Created Unique Index IX_DoctorFeeSchedules_Doctor_Code';
END
GO


-- =====================================================================================
-- 2. TABLE: [dentist].[Invoices]
--    Patient master financial bills, tracking totals, settlements, balances, and due dates.
-- =====================================================================================
PRINT '>> Step 2: Provisioning [dentist].[Invoices]...';

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = 'Invoices' AND s.name = 'dentist')
BEGIN
    CREATE TABLE [dentist].[Invoices] (
        [InvoiceID]       BIGINT IDENTITY(1,1) NOT NULL,
        [InvoiceNumber]   NVARCHAR(50) NOT NULL,                                                           -- e.g. 'INV-2026-000261'
        [PatientID]       INT NOT NULL,
        [DoctorID]        INT NULL,
        [AppointmentID]   INT NULL,
        [IssueDate]       DATE NOT NULL CONSTRAINT DF_Invoices_IssueDate DEFAULT CAST(GETDATE() AS DATE),
        [DueDate]         DATE NOT NULL CONSTRAINT DF_Invoices_DueDate DEFAULT DATEADD(DAY, 14, CAST(GETDATE() AS DATE)),
        [SubTotal]        DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoices_SubTotal DEFAULT 0.00,
        [TaxAmount]       DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoices_TaxAmount DEFAULT 0.00,
        [DiscountAmount]  DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoices_DiscountAmount DEFAULT 0.00,
        [TotalAmount]     DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoices_TotalAmount DEFAULT 0.00,
        [PaidAmount]      DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoices_PaidAmount DEFAULT 0.00,
        [BalanceAmount]   AS ([TotalAmount] - [PaidAmount]) PERSISTED,                                    -- Computed column
        [Status]          NVARCHAR(30) NOT NULL CONSTRAINT DF_Invoices_Status DEFAULT 'Unpaid',           -- 'Unpaid', 'Partially Paid', 'Paid', 'Pending Cash Settlement', 'Cancelled'
        [Currency]        NVARCHAR(10) NOT NULL CONSTRAINT DF_Invoices_Currency DEFAULT 'NZD',
        [Notes]           NVARCHAR(MAX) NULL,
        [CreatedAt]       DATETIME2 NOT NULL CONSTRAINT DF_Invoices_CreatedAt DEFAULT SYSUTCDATETIME(),
        [UpdatedAt]       DATETIME2 NOT NULL CONSTRAINT DF_Invoices_UpdatedAt DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_Invoices PRIMARY KEY CLUSTERED ([InvoiceID] ASC),
        CONSTRAINT UQ_Invoices_InvoiceNumber UNIQUE ([InvoiceNumber]),
        CONSTRAINT FK_Invoices_Patients FOREIGN KEY ([PatientID]) 
            REFERENCES [dentist].[Patients] ([PatientID]) ON DELETE CASCADE,
        CONSTRAINT FK_Invoices_Doctors FOREIGN KEY ([DoctorID]) 
            REFERENCES [dentist].[Doctors] ([DoctorID]) ON DELETE SET NULL
    );

    PRINT '   + [dentist].[Invoices] table created successfully.';
END
ELSE
BEGIN
    PRINT '   + [dentist].[Invoices] table already exists.';
END
GO

-- Indices for High-Throughput Ledger Queries
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Invoices_PatientID' AND object_id = OBJECT_ID('dentist.Invoices'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Invoices_PatientID
    ON [dentist].[Invoices] ([PatientID], [IssueDate] DESC)
    INCLUDE ([InvoiceNumber], [TotalAmount], [PaidAmount], [BalanceAmount], [Status], [Currency]);
    PRINT '   + Created Index IX_Invoices_PatientID';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Invoices_Status_DueDate' AND object_id = OBJECT_ID('dentist.Invoices'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Invoices_Status_DueDate
    ON [dentist].[Invoices] ([Status], [DueDate]);
    PRINT '   + Created Index IX_Invoices_Status_DueDate';
END
GO


-- =====================================================================================
-- 3. TABLE: [dentist].[InvoiceItems]
--    Itemized procedures attached to each invoice, with tooth number and line totals.
-- =====================================================================================
PRINT '>> Step 3: Provisioning [dentist].[InvoiceItems]...';

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = 'InvoiceItems' AND s.name = 'dentist')
BEGIN
    CREATE TABLE [dentist].[InvoiceItems] (
        [InvoiceItemID] BIGINT IDENTITY(1,1) NOT NULL,
        [InvoiceID]     BIGINT NOT NULL,
        [ProcedureCode] NVARCHAR(50) NULL,
        [Description]   NVARCHAR(255) NOT NULL,
        [ToothNumber]   INT NULL,                                                                        -- FDI 11-48 notation if tooth-specific
        [Quantity]      INT NOT NULL CONSTRAINT DF_InvoiceItems_Qty DEFAULT 1,
        [UnitPrice]     DECIMAL(18,2) NOT NULL,
        [TotalPrice]    AS ([Quantity] * [UnitPrice]) PERSISTED,                                         -- Computed column
        [CreatedAt]     DATETIME2 NOT NULL CONSTRAINT DF_InvoiceItems_CreatedAt DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_InvoiceItems PRIMARY KEY CLUSTERED ([InvoiceItemID] ASC),
        CONSTRAINT FK_InvoiceItems_Invoices FOREIGN KEY ([InvoiceID]) 
            REFERENCES [dentist].[Invoices] ([InvoiceID]) ON DELETE CASCADE
    );

    PRINT '   + [dentist].[InvoiceItems] table created successfully.';
END
ELSE
BEGIN
    PRINT '   + [dentist].[InvoiceItems] table already exists.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_InvoiceItems_InvoiceID' AND object_id = OBJECT_ID('dentist.InvoiceItems'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_InvoiceItems_InvoiceID
    ON [dentist].[InvoiceItems] ([InvoiceID]);
    PRINT '   + Created Index IX_InvoiceItems_InvoiceID';
END
GO


-- =====================================================================================
-- 4. TABLE: [dentist].[Payments]
--    Financial transactions supporting both instant Online Card checkouts & Cash Vouchers.
-- =====================================================================================
PRINT '>> Step 4: Provisioning [dentist].[Payments]...';

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = 'Payments' AND s.name = 'dentist')
BEGIN
    CREATE TABLE [dentist].[Payments] (
        [PaymentID]            BIGINT IDENTITY(1,1) NOT NULL,
        [PaymentReceiptNo]     NVARCHAR(50) NOT NULL,                                                    -- 'REC-2026-08129' or 'CSH-2026-04921'
        [InvoiceID]            BIGINT NOT NULL,
        [PatientID]            INT NOT NULL,
        [Amount]               DECIMAL(18,2) NOT NULL,
        [PaymentMethod]        NVARCHAR(30) NOT NULL,                                                    -- 'Online_Card', 'Cash', 'Insurance', 'Bank_Transfer'
        [PaymentStatus]        NVARCHAR(30) NOT NULL CONSTRAINT DF_Payments_Status DEFAULT 'Completed',  -- 'Pending', 'Completed', 'Failed', 'Refunded'
        [TransactionReference] NVARCHAR(100) NULL,                                                       -- Stripe charge ID / Gateway reference
        [PaymentGateway]       NVARCHAR(50) NULL,                                                        -- 'Stripe', 'PayFast', 'ClinicCashDesk'
        [CashVoucherCode]      NVARCHAR(50) NULL,                                                        -- Clinic check-in redemption voucher code
        [ReceivedByDoctorID]   INT NULL,                                                                 -- Receptionist / Doctor who confirmed cash
        [PaymentDate]          DATETIME2 NOT NULL CONSTRAINT DF_Payments_PaymentDate DEFAULT SYSUTCDATETIME(),
        [Notes]                NVARCHAR(MAX) NULL,

        CONSTRAINT PK_Payments PRIMARY KEY CLUSTERED ([PaymentID] ASC),
        CONSTRAINT UQ_Payments_ReceiptNo UNIQUE ([PaymentReceiptNo]),
        CONSTRAINT FK_Payments_Invoices FOREIGN KEY ([InvoiceID]) 
            REFERENCES [dentist].[Invoices] ([InvoiceID]) ON DELETE CASCADE,
        CONSTRAINT FK_Payments_Patients FOREIGN KEY ([PatientID]) 
            REFERENCES [dentist].[Patients] ([PatientID]),
        CONSTRAINT FK_Payments_Doctors FOREIGN KEY ([ReceivedByDoctorID]) 
            REFERENCES [dentist].[Doctors] ([DoctorID])
    );

    PRINT '   + [dentist].[Payments] table created successfully.';
END
ELSE
BEGIN
    PRINT '   + [dentist].[Payments] table already exists.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Payments_InvoiceID' AND object_id = OBJECT_ID('dentist.Payments'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Payments_InvoiceID
    ON [dentist].[Payments] ([InvoiceID]);
    PRINT '   + Created Index IX_Payments_InvoiceID';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Payments_PatientID' AND object_id = OBJECT_ID('dentist.Payments'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Payments_PatientID
    ON [dentist].[Payments] ([PatientID], [PaymentDate] DESC);
    PRINT '   + Created Index IX_Payments_PatientID';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Payments_CashVoucherCode' AND object_id = OBJECT_ID('dentist.Payments'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Payments_CashVoucherCode
    ON [dentist].[Payments] ([CashVoucherCode])
    WHERE [CashVoucherCode] IS NOT NULL;
    PRINT '   + Created Index IX_Payments_CashVoucherCode';
END
GO


-- =====================================================================================
-- 5. STORED PROCEDURES: PAYMENT PROCESSING & CASH VOUCHER RECONCILIATION
-- =====================================================================================
PRINT '>> Step 5: Provisioning Transactional Stored Procedures...';
GO

-- Procedure: Process Invoice Payment (Online Card or Initial Cash Voucher)
CREATE OR ALTER PROCEDURE [dentist].[usp_ProcessInvoicePayment]
    @InvoiceID             BIGINT,
    @PatientID             INT,
    @Amount                DECIMAL(18,2),
    @PaymentMethod         NVARCHAR(30),
    @TransactionReference  NVARCHAR(100) = NULL,
    @PaymentGateway        NVARCHAR(50) = NULL,
    @ReceivedByDoctorID    INT = NULL,
    @Notes                 NVARCHAR(MAX) = NULL,
    @GeneratedReceiptNo    NVARCHAR(50) OUTPUT,
    @NewInvoiceStatus      NVARCHAR(30) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;
    BEGIN TRY
        -- 1. Validate Invoice existence
        DECLARE @TotalAmt DECIMAL(18,2), @CurrentPaid DECIMAL(18,2);
        SELECT @TotalAmt = TotalAmount, @CurrentPaid = PaidAmount
        FROM [dentist].[Invoices] WITH (UPDLOCK, ROWLOCK)
        WHERE InvoiceID = @InvoiceID AND PatientID = @PatientID;

        IF @TotalAmt IS NULL
        BEGIN
            THROW 50001, 'Invoice not found or patient does not have access permissions.', 1;
        END;

        -- 2. Generate Unique Receipt / Voucher Number
        DECLARE @Prefix NVARCHAR(10) = CASE WHEN @PaymentMethod = 'Cash' THEN 'CSH-' ELSE 'REC-' END;
        DECLARE @RandomPart INT = CAST(RAND() * 90000 + 10000 AS INT);
        SET @GeneratedReceiptNo = @Prefix + CAST(YEAR(GETDATE()) AS NVARCHAR(4)) + '-' + RIGHT('00000' + CAST(@InvoiceID AS NVARCHAR(10)), 5) + CAST(@RandomPart AS NVARCHAR(5));

        DECLARE @PayStatus NVARCHAR(30) = CASE WHEN @PaymentMethod = 'Cash' THEN 'Pending' ELSE 'Completed' END;
        DECLARE @VoucherCode NVARCHAR(50) = CASE WHEN @PaymentMethod = 'Cash' THEN @GeneratedReceiptNo ELSE NULL END;

        -- 3. Insert Payment Record
        INSERT INTO [dentist].[Payments] (
            PaymentReceiptNo, InvoiceID, PatientID, Amount, PaymentMethod,
            PaymentStatus, TransactionReference, PaymentGateway, CashVoucherCode,
            ReceivedByDoctorID, PaymentDate, Notes
        )
        VALUES (
            @GeneratedReceiptNo, @InvoiceID, @PatientID, @Amount, @PaymentMethod,
            @PayStatus, @TransactionReference, @PaymentGateway, @VoucherCode,
            @ReceivedByDoctorID, SYSUTCDATETIME(), @Notes
        );

        -- 4. If Completed (Online Card payment), update invoice balances immediately
        IF @PayStatus = 'Completed'
        BEGIN
            DECLARE @NewPaid DECIMAL(18,2) = @CurrentPaid + @Amount;
            SET @NewInvoiceStatus = CASE WHEN @NewPaid >= @TotalAmt THEN 'Paid' ELSE 'Partially Paid' END;

            UPDATE [dentist].[Invoices]
            SET PaidAmount = @NewPaid,
                Status = @NewInvoiceStatus,
                UpdatedAt = SYSUTCDATETIME()
            WHERE InvoiceID = @InvoiceID;
        END
        ELSE
        BEGIN
            -- For Cash voucher, status marks pending settlement
            SET @NewInvoiceStatus = 'Pending Cash Settlement';

            UPDATE [dentist].[Invoices]
            SET Status = @NewInvoiceStatus,
                UpdatedAt = SYSUTCDATETIME()
            WHERE InvoiceID = @InvoiceID;
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO
PRINT '   + Created/Updated [dentist].[usp_ProcessInvoicePayment]';
GO

-- Procedure: Confirm Cash Payment at Front Desk Desk Reception
CREATE OR ALTER PROCEDURE [dentist].[usp_ConfirmCashPaymentAtFrontDesk]
    @CashVoucherCode   NVARCHAR(50),
    @StaffDoctorID     INT,
    @ConfirmedAmount   DECIMAL(18,2) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;
    BEGIN TRY
        DECLARE @PaymentID BIGINT, @InvoiceID BIGINT, @PayAmount DECIMAL(18,2), @Status NVARCHAR(30);

        SELECT @PaymentID = PaymentID, @InvoiceID = InvoiceID, @PayAmount = Amount, @Status = PaymentStatus
        FROM [dentist].[Payments] WITH (UPDLOCK, ROWLOCK)
        WHERE CashVoucherCode = @CashVoucherCode;

        IF @PaymentID IS NULL
        BEGIN
            THROW 50002, 'Invalid or expired Cash Voucher Code.', 1;
        END

        IF @Status = 'Completed'
        BEGIN
            THROW 50003, 'This cash voucher has already been settled and completed.', 1;
        END

        DECLARE @FinalAmount DECIMAL(18,2) = ISNULL(@ConfirmedAmount, @PayAmount);

        -- Mark payment completed
        UPDATE [dentist].[Payments]
        SET PaymentStatus = 'Completed',
            Amount = @FinalAmount,
            ReceivedByDoctorID = @StaffDoctorID,
            Notes = ISNULL(Notes, '') + ' [Settled in-clinic by Staff ID ' + CAST(@StaffDoctorID AS NVARCHAR(10)) + ' at ' + CONVERT(NVARCHAR(30), GETDATE(), 120) + ']'
        WHERE PaymentID = @PaymentID;

        -- Update Invoice balance
        UPDATE [dentist].[Invoices]
        SET PaidAmount = PaidAmount + @FinalAmount,
            Status = CASE WHEN (PaidAmount + @FinalAmount) >= TotalAmount THEN 'Paid' ELSE 'Partially Paid' END,
            UpdatedAt = SYSUTCDATETIME()
        WHERE InvoiceID = @InvoiceID;

        COMMIT TRANSACTION;

        SELECT 
            P.PaymentReceiptNo,
            P.CashVoucherCode,
            P.Amount,
            P.PaymentStatus,
            I.InvoiceNumber,
            I.Status AS InvoiceStatus,
            I.PaidAmount,
            I.BalanceAmount
        FROM [dentist].[Payments] P
        JOIN [dentist].[Invoices] I ON P.InvoiceID = I.InvoiceID
        WHERE P.PaymentID = @PaymentID;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO
PRINT '   + Created/Updated [dentist].[usp_ConfirmCashPaymentAtFrontDesk]';
GO


-- =====================================================================================
-- 6. DEFAULT SEED DATA: MULTI-CURRENCY CLINICIAN FEE SCHEDULES
--    Seeds default ADA procedure fee schedules for active clinicians (PKR & NZD).
-- =====================================================================================
PRINT '>> Step 6: Seeding default ADA procedure catalogs and clinician fee schedules...';

-- Dr. Jhangir Ahmed (ID 2 - PKR Currency Default)
IF EXISTS (SELECT 1 FROM [dentist].[Doctors] WHERE DoctorID = 2)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [dentist].[DoctorFeeSchedules] WHERE DoctorID = 2)
    BEGIN
        INSERT INTO [dentist].[DoctorFeeSchedules] (DoctorID, Currency, ProcedureCode, ProcedureName, Category, EstimatedDuration, StandardFee, Description)
        VALUES
        (2, 'PKR', 'D0120', 'Periodic Oral Health Checkup & Exam', 'Preventative', '30 mins', 4500.00, 'Visual exam, periodontal probe screening, soft tissue inspection.'),
        (2, 'PKR', 'D1110', 'Ultrasonic Scaling & Prophylaxis Polish', 'Preventative', '45 mins', 4000.00, 'Supragingival tartar removal, plaque scaling, and enamel polish.'),
        (2, 'PKR', 'D0140', 'Emergency Triage & Acute Toothache', 'Emergency', '30 mins', 7800.00, 'Urgent diagnostic exam for acute pain, broken tooth, or swelling.'),
        (2, 'PKR', 'D2391', 'Cavity Composite Restoration (1 Surface)', 'Restorative', '45 mins', 9750.00, 'Tooth-colored aesthetic composite resin filling with rubber dam.'),
        (2, 'PKR', 'D2392', 'Cavity Composite Restoration (2 Surfaces)', 'Restorative', '60 mins', 12000.00, 'Multi-surface composite restoration with contour matrix.'),
        (2, 'PKR', 'D2740', 'Full Monolithic Zirconia Crown', 'Restorative', '60 mins', 42250.00, 'CAD/CAM precision-milled monolithic zirconia dental crown.'),
        (2, 'PKR', 'D3330', 'Molar Root Canal Therapy (3 Canals)', 'Endodontic', '90 mins', 29250.00, 'Rotary nickel-titanium canal instrumentation and obturation.'),
        (2, 'PKR', 'D4341', 'Periodontal Deep Scaling & Root Planing', 'Periodontal', '60 mins', 10400.00, 'Subgingival deep root scaling per quadrant for pocket management.'),
        (2, 'PKR', 'D8080', 'Orthodontic & Clear Aligner Consultation', 'Orthodontic', '30 mins', 12000.00, 'Digital intraoral 3D scan, aligner staging, and fit review.'),
        (2, 'PKR', 'D9972', 'In-Clinic Laser Cosmetic Teeth Whitening', 'Cosmetic', '45 mins', 16250.00, 'In-office hydrogen peroxide laser bleaching & shade match.'),
        (2, 'PKR', 'D7140', 'Simple Routine Tooth Extraction', 'Oral Surgery', '45 mins', 8500.00, 'Atraumatic forceps extraction, hemostatic socket pack, post-op care.'),
        (2, 'PKR', 'D7210', 'Surgical Impaction Wisdom Tooth Removal', 'Oral Surgery', '60 mins', 18200.00, 'Surgical flap elevation, bone guttering, and resorbable sutures.');

        PRINT '   + Seeded 12 PKR procedures for Doctor 2 (Dr. Jhangir Ahmed).';
    END
END
GO

-- Dr. Ahmed Khan & Dr. Sarah Jenkins (ID 3 & 4 - NZD Currency Default)
DECLARE @DocID INT;
DECLARE doc_nzd_cursor CURSOR FOR 
    SELECT DoctorID FROM [dentist].[Doctors] WHERE DoctorID IN (3, 4);

OPEN doc_nzd_cursor;
FETCH NEXT FROM doc_nzd_cursor INTO @DocID;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [dentist].[DoctorFeeSchedules] WHERE DoctorID = @DocID)
    BEGIN
        INSERT INTO [dentist].[DoctorFeeSchedules] (DoctorID, Currency, ProcedureCode, ProcedureName, Category, EstimatedDuration, StandardFee, Description)
        VALUES
        (@DocID, 'NZD', 'D0120', 'Periodic Oral Health Checkup & Exam', 'Preventative', '30 mins', 45.00, 'Visual exam, periodontal probe screening, soft tissue inspection.'),
        (@DocID, 'NZD', 'D1110', 'Ultrasonic Scaling & Prophylaxis Polish', 'Preventative', '45 mins', 40.00, 'Supragingival tartar removal, plaque scaling, and enamel polish.'),
        (@DocID, 'NZD', 'D0140', 'Emergency Triage & Acute Toothache', 'Emergency', '30 mins', 120.00, 'Urgent diagnostic exam for acute pain, broken tooth, or swelling.'),
        (@DocID, 'NZD', 'D2391', 'Cavity Composite Restoration (1 Surface)', 'Restorative', '45 mins', 150.00, 'Tooth-colored aesthetic composite resin filling with rubber dam.'),
        (@DocID, 'NZD', 'D2392', 'Cavity Composite Restoration (2 Surfaces)', 'Restorative', '60 mins', 185.00, 'Multi-surface composite restoration with contour matrix.'),
        (@DocID, 'NZD', 'D2740', 'Full Monolithic Zirconia Crown', 'Restorative', '60 mins', 650.00, 'CAD/CAM precision-milled monolithic zirconia dental crown.'),
        (@DocID, 'NZD', 'D3330', 'Molar Root Canal Therapy (3 Canals)', 'Endodontic', '90 mins', 450.00, 'Rotary nickel-titanium canal instrumentation and obturation.'),
        (@DocID, 'NZD', 'D4341', 'Periodontal Deep Scaling & Root Planing', 'Periodontal', '60 mins', 160.00, 'Subgingival deep root scaling per quadrant for pocket management.'),
        (@DocID, 'NZD', 'D8080', 'Orthodontic & Clear Aligner Consultation', 'Orthodontic', '30 mins', 180.00, 'Digital intraoral 3D scan, aligner staging, and fit review.'),
        (@DocID, 'NZD', 'D9972', 'In-Clinic Laser Cosmetic Teeth Whitening', 'Cosmetic', '45 mins', 250.00, 'In-office hydrogen peroxide laser bleaching & shade match.'),
        (@DocID, 'NZD', 'D7140', 'Simple Routine Tooth Extraction', 'Oral Surgery', '45 mins', 130.00, 'Atraumatic forceps extraction, hemostatic socket pack, post-op care.'),
        (@DocID, 'NZD', 'D7210', 'Surgical Impaction Wisdom Tooth Removal', 'Oral Surgery', '60 mins', 280.00, 'Surgical flap elevation, bone guttering, and resorbable sutures.');

        PRINT '   + Seeded 12 NZD procedures for Doctor ' + CAST(@DocID AS NVARCHAR(10)) + '.';
    END

    FETCH NEXT FROM doc_nzd_cursor INTO @DocID;
END

CLOSE doc_nzd_cursor;
DEALLOCATE doc_nzd_cursor;
GO

PRINT '>> [COMPLETE] Doctor Fee Schedules and Billing Migration executed successfully!';
GO
