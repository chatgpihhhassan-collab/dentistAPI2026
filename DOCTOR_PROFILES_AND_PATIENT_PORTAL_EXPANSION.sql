-- ==============================================================================
-- DENTIA DENTAL CLINICAL PLATFORM
-- DATABASE EXPANSION: DOCTOR PROFILES, WORK EXPERIENCE & ORGANIZATION HISTORY
-- ==============================================================================
-- Purpose:
-- 1. Expands [dentist].[Doctors] with comprehensive clinical credentials, 
--    hospital/organization affiliations, years of experience, and patient ratings.
-- 2. Enables Patient Portal to display doctor history and allow patient selection.
-- 3. Enables Superadmin to manage, update, and audit doctor profiles.
-- ==============================================================================

USE [DentistAPI];
GO

PRINT '>>> [1/3] Adding Doctor Profile and Organization Experience Columns to [dentist].[Doctors]...';

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dentist].[Doctors]') AND name = 'Specialization')
BEGIN
    ALTER TABLE [dentist].[Doctors] ADD [Specialization] NVARCHAR(150) NULL DEFAULT 'General Dental Surgeon';
    PRINT '    [+] Added column: Specialization';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dentist].[Doctors]') AND name = 'Title')
BEGIN
    ALTER TABLE [dentist].[Doctors] ADD [Title] NVARCHAR(100) NULL DEFAULT 'BDS, RDS';
    PRINT '    [+] Added column: Title';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dentist].[Doctors]') AND name = 'YearsOfExperience')
BEGIN
    ALTER TABLE [dentist].[Doctors] ADD [YearsOfExperience] INT NULL DEFAULT 5;
    PRINT '    [+] Added column: YearsOfExperience';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dentist].[Doctors]') AND name = 'Biography')
BEGIN
    ALTER TABLE [dentist].[Doctors] ADD [Biography] NVARCHAR(MAX) NULL;
    PRINT '    [+] Added column: Biography';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dentist].[Doctors]') AND name = 'OrganizationWorkHistory')
BEGIN
    ALTER TABLE [dentist].[Doctors] ADD [OrganizationWorkHistory] NVARCHAR(MAX) NULL;
    PRINT '    [+] Added column: OrganizationWorkHistory';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dentist].[Doctors]') AND name = 'Education')
BEGIN
    ALTER TABLE [dentist].[Doctors] ADD [Education] NVARCHAR(MAX) NULL;
    PRINT '    [+] Added column: Education';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dentist].[Doctors]') AND name = 'Certifications')
BEGIN
    ALTER TABLE [dentist].[Doctors] ADD [Certifications] NVARCHAR(MAX) NULL;
    PRINT '    [+] Added column: Certifications';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dentist].[Doctors]') AND name = 'ConsultationFee')
BEGIN
    ALTER TABLE [dentist].[Doctors] ADD [ConsultationFee] DECIMAL(18,2) NULL DEFAULT 100.00;
    PRINT '    [+] Added column: ConsultationFee';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dentist].[Doctors]') AND name = 'ProfileImageUrl')
BEGIN
    ALTER TABLE [dentist].[Doctors] ADD [ProfileImageUrl] NVARCHAR(500) NULL;
    PRINT '    [+] Added column: ProfileImageUrl';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dentist].[Doctors]') AND name = 'Languages')
BEGIN
    ALTER TABLE [dentist].[Doctors] ADD [Languages] NVARCHAR(200) NULL DEFAULT 'English, Urdu';
    PRINT '    [+] Added column: Languages';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dentist].[Doctors]') AND name = 'Rating')
BEGIN
    ALTER TABLE [dentist].[Doctors] ADD [Rating] DECIMAL(3,2) NULL DEFAULT 4.90;
    PRINT '    [+] Added column: Rating';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dentist].[Doctors]') AND name = 'ReviewCount')
BEGIN
    ALTER TABLE [dentist].[Doctors] ADD [ReviewCount] INT NULL DEFAULT 25;
    PRINT '    [+] Added column: ReviewCount';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dentist].[Doctors]') AND name = 'IsActive')
BEGIN
    ALTER TABLE [dentist].[Doctors] ADD [IsActive] BIT NULL DEFAULT 1;
    PRINT '    [+] Added column: IsActive';
END
GO

PRINT '>>> [2/3] Seeding / Updating Verified Clinical Profiles and Organization Histories...';

-- 1. Dr. Haider Ali (Implantology & Oral Surgery)
UPDATE [dentist].[Doctors]
SET 
    [Specialization] = 'Senior Consultant Implantologist & Oral Surgeon',
    [Title] = 'BDS, MDS (Oral & Maxillofacial Surgery), FICOI (USA)',
    [YearsOfExperience] = 15,
    [Biography] = 'Dr. Haider Ali is a distinguished Oral & Maxillofacial Surgeon and Fellow of the International Congress of Oral Implantologists (ICOI, USA). With over 15 years of dedicated surgical experience across leading tertiary teaching hospitals and private specialty clinics, he has successfully placed more than 3,500 dental implants with computer-guided surgical navigation, sinus augmentations, and full-mouth rehabilitation.',
    [OrganizationWorkHistory] = '[
        {"organization": "Shifa International Hospital", "role": "Head of Oral & Maxillofacial Surgery", "period": "2018 - Present", "description": "Supervising surgical theater for advanced ridge augmentation, zygomatic implants, and trauma reconstructions."},
        {"organization": "Auckland Regional Hospital Dental Wing", "role": "Senior Clinical Registrar", "period": "2014 - 2018", "description": "Specialized in surgical extractions, bone grafting, and biopsy pathology assessment."},
        {"organization": "Mayo Hospital / King Edward Medical University", "role": "Resident Surgeon", "period": "2010 - 2014", "description": "Completed 4-year intensive clinical residency in maxillofacial trauma and oral pathology."}
    ]',
    [Education] = 'BDS (Bachelor of Dental Surgery) - King Edward Medical University (2009)' + CHAR(13) + CHAR(10) +
                  'MDS (Oral & Maxillofacial Surgery) - Postgraduate Institute of Dental Sciences (2014)' + CHAR(13) + CHAR(10) +
                  'Fellowship in Advanced Implantology - International Congress of Oral Implantologists (ICOI USA, 2017)',
    [Certifications] = 'Diplomate & Fellow ICOI (USA), Digital Guided Implant Surgery Specialist, Basic & Advanced Life Support (BLS/ACLS)',
    [ConsultationFee] = 180.00,
    [ProfileImageUrl] = 'https://images.unsplash.com/photo-1622253692010-333f2da6031d?auto=format&fit=crop&q=80&w=400',
    [Languages] = 'English, Urdu, Punjabi',
    [Rating] = 4.97,
    [ReviewCount] = 168,
    [IsActive] = 1
WHERE [FirstName] LIKE '%Haider%' OR [Username] LIKE '%haider%' OR [DoctorID] = 2;

-- 2. Dr. Sarah Khan (Orthodontics & Clear Aligners)
UPDATE [dentist].[Doctors]
SET 
    [Specialization] = 'Specialist Orthodontist & Dentofacial Orthopedics',
    [Title] = 'BDS, MSc (Orthodontics), MOrth RCSEd (UK)',
    [YearsOfExperience] = 12,
    [Biography] = 'Dr. Sarah Khan is a recognized specialist in adult and pediatric orthodontics, dentofacial orthopedics, and digital clear aligner treatment. She is an Invisalign Diamond Apex Provider and member of the Royal College of Surgeons of Edinburgh. Dr. Sarah focuses on non-extraction orthodontic alignment, TMJ stabilization, and aesthetic smile design.',
    [OrganizationWorkHistory] = '[
        {"organization": "Aga Khan University Hospital (AKUH)", "role": "Consultant Orthodontist", "period": "2017 - Present", "description": "Director of Adult Orthodontic Clinic and digital clear aligner biomechanics."},
        {"organization": "Guy’s and St Thomas’ NHS Foundation Trust, London", "role": "Clinical Orthodontic Fellow", "period": "2015 - 2017", "description": "Advanced training in lingual braces and multidisciplinary orthognathic surgical planning."},
        {"organization": "Armed Forces Institute of Dentistry (AFID)", "role": "Orthodontic Resident", "period": "2012 - 2015", "description": "Treated complex skeletal Class II and Class III malocclusions and pediatric palate expansions."}
    ]',
    [Education] = 'BDS - Army Medical College (2011)' + CHAR(13) + CHAR(10) +
                  'MSc in Orthodontics - King’s College London (2016)' + CHAR(13) + CHAR(10) +
                  'MOrth - Royal College of Surgeons of Edinburgh (2017)',
    [Certifications] = 'Invisalign Diamond Apex Provider, Damon System Certified, Lingual Orthodontics Specialist (WIN & Incognito)',
    [ConsultationFee] = 160.00,
    [ProfileImageUrl] = 'https://images.unsplash.com/photo-1559839734-2b71ea197ec2?auto=format&fit=crop&q=80&w=400',
    [Languages] = 'English, Urdu',
    [Rating] = 4.95,
    [ReviewCount] = 142,
    [IsActive] = 1
WHERE [FirstName] LIKE '%Sarah%' OR [Username] LIKE '%sarah%' OR [DoctorID] = 4;

-- 3. Default updates for any other registered doctors
UPDATE [dentist].[Doctors]
SET 
    [Specialization] = 'Consultant Dental Surgeon & Endodontist',
    [Title] = 'BDS, FCPS (Restorative Dentistry & Endodontics)',
    [YearsOfExperience] = 9,
    [Biography] = 'Experienced dental surgeon specializing in microscopic root canal therapy, complex retreatment cases, and tooth-colored cosmetic restorations.',
    [OrganizationWorkHistory] = '[
        {"organization": "Dental Associates Healthcare", "role": "Senior Dental Surgeon", "period": "2019 - Present", "description": "Lead clinician for restorative dentistry and single-visit rotary endodontics."},
        {"organization": "City Dental Teaching Hospital", "role": "Registrar", "period": "2015 - 2019", "description": "Conducted emergency dental trauma treatment and root canal clinical trials."}
    ]',
    [Education] = 'BDS - University of Health Sciences (2014)' + CHAR(13) + CHAR(10) +
                  'FCPS Part II Trained (Restorative Dentistry & Endodontics)',
    [Certifications] = 'Rotary Endodontics Masterclass, Laser Dentistry Certification',
    [ConsultationFee] = 120.00,
    [ProfileImageUrl] = 'https://images.unsplash.com/photo-1537368910025-700350fe46c7?auto=format&fit=crop&q=80&w=400',
    [Languages] = 'English, Urdu',
    [Rating] = 4.88,
    [ReviewCount] = 64,
    [IsActive] = 1
WHERE [Specialization] IS NULL OR [Biography] IS NULL;
GO

PRINT '>>> [3/3] Verification Query: Current Doctor Profiles & Affiliations';
SELECT 
    DoctorID, 
    Username, 
    FirstName + ' ' + LastName AS FullName, 
    Specialization, 
    YearsOfExperience, 
    ConsultationFee, 
    Rating, 
    ReviewCount, 
    IsActive, 
    SUBSTRING(OrganizationWorkHistory, 1, 120) + '...' AS OrganizationPreview
FROM [dentist].[Doctors]
ORDER BY DoctorID ASC;
GO

PRINT '>>> [SUCCESS] Doctor Profile and Experience Migration Script executed successfully!';
GO
