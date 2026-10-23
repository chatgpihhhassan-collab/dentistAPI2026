-- ============================================================================
-- DENTIA CLINICAL DATABASE MIGRATION: TREATMENT TIMER & SINGLE-TREATMENT AUDIT
-- Date: 2026-10-23
-- Description:
-- 1. Adds TreatmentDurationSeconds, TreatmentDurationFormatted, TreatmentStartedAt,
--    and TreatmentEndedAt to [dentist].[Appointments] for recording treatment session time.
-- 2. Ensures [dentist].[TreatmentItems] has TransitionNotes & ClosedReason columns.
-- 3. Creates/verifies indexes for rapid lookup by PatientID & ToothNumber.
-- ============================================================================

SET NOCOUNT ON;

-- 1. Add Treatment Timer Columns to [dentist].[Appointments]
IF COL_LENGTH('dentist.Appointments', 'TreatmentDurationSeconds') IS NULL
BEGIN
    ALTER TABLE [dentist].[Appointments]
    ADD [TreatmentDurationSeconds] INT NULL CONSTRAINT DF_Appointments_TreatmentDurationSeconds DEFAULT (0);
    PRINT 'Added TreatmentDurationSeconds column to [dentist].[Appointments]';
END
ELSE
BEGIN
    PRINT 'TreatmentDurationSeconds already exists on [dentist].[Appointments]';
END;

IF COL_LENGTH('dentist.Appointments', 'TreatmentDurationFormatted') IS NULL
BEGIN
    ALTER TABLE [dentist].[Appointments]
    ADD [TreatmentDurationFormatted] NVARCHAR(50) NULL;
    PRINT 'Added TreatmentDurationFormatted column to [dentist].[Appointments]';
END
ELSE
BEGIN
    PRINT 'TreatmentDurationFormatted already exists on [dentist].[Appointments]';
END;

IF COL_LENGTH('dentist.Appointments', 'TreatmentStartedAt') IS NULL
BEGIN
    ALTER TABLE [dentist].[Appointments]
    ADD [TreatmentStartedAt] DATETIME2 NULL;
    PRINT 'Added TreatmentStartedAt column to [dentist].[Appointments]';
END
ELSE
BEGIN
    PRINT 'TreatmentStartedAt already exists on [dentist].[Appointments]';
END;

IF COL_LENGTH('dentist.Appointments', 'TreatmentEndedAt') IS NULL
BEGIN
    ALTER TABLE [dentist].[Appointments]
    ADD [TreatmentEndedAt] DATETIME2 NULL;
    PRINT 'Added TreatmentEndedAt column to [dentist].[Appointments]';
END
ELSE
BEGIN
    PRINT 'TreatmentEndedAt already exists on [dentist].[Appointments]';
END;

-- 2. Ensure [dentist].[TreatmentItems] has TransitionNotes / ClosedReason
IF COL_LENGTH('dentist.TreatmentItems', 'ClosedReason') IS NULL
BEGIN
    ALTER TABLE [dentist].[TreatmentItems]
    ADD [ClosedReason] NVARCHAR(MAX) NULL;
    PRINT 'Added ClosedReason column to [dentist].[TreatmentItems]';
END;

IF COL_LENGTH('dentist.TreatmentItems', 'TransitionNotes') IS NULL
BEGIN
    ALTER TABLE [dentist].[TreatmentItems]
    ADD [TransitionNotes] NVARCHAR(MAX) NULL;
    PRINT 'Added TransitionNotes column to [dentist].[TreatmentItems]';
END;

-- 3. Nonclustered Index for single active treatment validation
IF EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_TreatmentItems_Patient_Tooth_Status' 
      AND object_id = OBJECT_ID('dentist.TreatmentItems')
)
BEGIN
    DROP INDEX [IX_TreatmentItems_Patient_Tooth_Status] ON [dentist].[TreatmentItems];
END;

CREATE NONCLUSTERED INDEX [IX_TreatmentItems_Patient_Tooth_Status]
ON [dentist].[TreatmentItems] ([PatientID], [ToothNumber], [Status])
WHERE [ToothNumber] > 0 AND [Status] = 'in_progress';
PRINT 'Created index IX_TreatmentItems_Patient_Tooth_Status';

PRINT 'Migration completed successfully.';
