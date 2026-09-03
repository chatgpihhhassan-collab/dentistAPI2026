# 🦷 Multi-Condition Dental Charting System — Architecture & SQL Specification

## 📋 Executive Overview
In clinical dentistry, a single tooth frequently presents with **multiple co-existing conditions, treatments, and appliances**.
For instance:
* **Tooth #16** can simultaneously have:
  1. **Pathologic Mobility (Grade II)** *(Periodontics)*
  2. **Mesio-Occlusal Composite Inlay** *(Restorative)*
  3. **45° Mesiopalatal Axial Rotation** *(Orthodontic / Developmental)*
  4. **Bonded Orthodontic Bracket** *(Orthodontic Appliance)*

This document details the complete end-to-end architecture covering the **SQL Server Database Schema**, **C# ASP.NET Core API**, **React/Three.js Frontend Rendering**, **Animation Management**, and **UI/UX Design**.

---

## 🗄️ 1. SQL Server Database Changes & Migration Scripts

### Approach Comparison:
* **Option A (Recommended — JSON Document Column):** Stores rich structured condition objects inside `[dentist].[TeethState].[ConditionsJson]` while maintaining indexing and relational integrity with the patient table.
* **Option B (Normalized Relational Tables):** Uses a dedicated 1-to-Many `[dentist].[ToothConditions]` table.

---

### 🔹 Migration Script: Option A (JSON-Backed Multi-Condition Column)

Execute this script in SQL Server Management Studio (SSMS) or via Dapper migration:

```sql
USE [DentistDB];
GO

-- 1. Ensure dentist schema exists
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'dentist')
BEGIN
    EXEC('CREATE SCHEMA dentist');
END
GO

-- 2. Create or Update [dentist].[TeethState] Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TeethState' AND schema_id = SCHEMA_ID('dentist'))
BEGIN
    CREATE TABLE [dentist].[TeethState] (
        [TeethStateID] INT IDENTITY(1,1) PRIMARY KEY,
        [PatientID] INT NOT NULL,
        [ToothNumber] INT NOT NULL,
        [PrimaryStatus] NVARCHAR(100) NOT NULL DEFAULT 'Healthy',
        [Specialty] VARCHAR(50) NOT NULL DEFAULT 'General', -- 'Pathology', 'Restorative', 'Endodontics', 'Periodontics', 'Implantology', 'Orthodontics', 'Oral Surgery'
        [AffectedZone] NVARCHAR(150) NULL,                  -- e.g. 'Tooth #3 Buccal (B) Cervical Margin / Class V'
        [Color] VARCHAR(20) NOT NULL DEFAULT '#10B981',
        [RotationDeg] INT NOT NULL DEFAULT 0,
        [Comments] NVARCHAR(MAX) NULL,
        [ConditionsJson] NVARCHAR(MAX) NULL, -- Stores array of conditions: [{type, specialty, zone, grade, surface, color, date}]
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [FK_TeethState_Patients] FOREIGN KEY ([PatientID]) REFERENCES [dentist].[Patients]([PatientID]) ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX [IX_TeethState_Patient_Tooth] ON [dentist].[TeethState] ([PatientID], [ToothNumber]);
END
ELSE
BEGIN
    -- Add ConditionsJson if missing
    IF COL_LENGTH('dentist.TeethState', 'ConditionsJson') IS NULL
    BEGIN
        ALTER TABLE [dentist].[TeethState] ADD [ConditionsJson] NVARCHAR(MAX) NULL;
    END

    -- Add RotationDeg if missing
    IF COL_LENGTH('dentist.TeethState', 'RotationDeg') IS NULL
    BEGIN
        ALTER TABLE [dentist].[TeethState] ADD [RotationDeg] INT NOT NULL DEFAULT 0;
    END

    -- Add PrimaryStatus if missing
    IF COL_LENGTH('dentist.TeethState', 'PrimaryStatus') IS NULL
    BEGIN
        ALTER TABLE [dentist].[TeethState] ADD [PrimaryStatus] NVARCHAR(100) NOT NULL DEFAULT 'Healthy';
    END
END
GO

-- 3. JSON Validity Constraint Check (Ensures clean structured JSON)
ALTER TABLE [dentist].[TeethState]
ADD CONSTRAINT [CK_TeethState_ConditionsJson] 
CHECK ([ConditionsJson] IS NULL OR ISJSON([ConditionsJson]) = 1);
GO
```

---

### 🔹 Migration Script: Option B (Normalized 1-to-Many Relational Tables)

If strict normalization is preferred:

```sql
USE [DentistDB];
GO

-- 1. [dentist].[ToothConditions] (1 Tooth -> Many Clinical Conditions)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ToothConditions' AND schema_id = SCHEMA_ID('dentist'))
BEGIN
    CREATE TABLE [dentist].[ToothConditions] (
        [ConditionID] INT IDENTITY(1,1) PRIMARY KEY,
        [PatientID] INT NOT NULL,
        [ToothNumber] INT NOT NULL,
        [Category] VARCHAR(50) NOT NULL,       -- 'Restorative', 'Periodontal', 'Endodontic', 'Orthodontic', 'Prosthodontic', 'Implant'
        [ConditionType] VARCHAR(100) NOT NULL,  -- 'Composite', 'Caries', 'Mobility', 'RCT', 'Implant', 'Bracket', 'Rotation'
        [SurfaceCode] VARCHAR(20) NULL,        -- 'O', 'MO', 'DO', 'MOD', 'B', 'L', 'Class V'
        [SeverityOrGrade] VARCHAR(50) NULL,    -- 'Grade I', 'Grade II', 'Grade III', '3mm bone loss'
        [RotationAngle] INT NULL DEFAULT 0,     -- e.g. 45, 90
        [HexColor] VARCHAR(20) NOT NULL DEFAULT '#10B981',
        [ClinicalNote] NVARCHAR(MAX) NULL,
        [DiagnosedByDoctorID] INT NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [IsActive] BIT NOT NULL DEFAULT 1,
        CONSTRAINT [FK_ToothConditions_Patients] FOREIGN KEY ([PatientID]) REFERENCES [dentist].[Patients]([PatientID]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_ToothConditions_Patient_Tooth] ON [dentist].[ToothConditions] ([PatientID], [ToothNumber], [IsActive]);
END
GO
```

---

## 💻 2. C# ASP.NET Core Backend Architecture (`DentistAPI`)

### A. C# Models & DTOs (`DentistAPI/Models/DentalChartModels.cs`)

```csharp
using System;
using System.Collections.Generic;

namespace DentistAPI.Models
{
    public class ToothConditionItem
    {
        public string Type { get; set; } = string.Empty;       // "mobility", "composite", "rotation", "implant", "bracket", "caries", "rct"
        public string? Surface { get; set; }                   // "MO", "DO", "MOD", "Facial", "Lingual", "Occlusal"
        public string? Grade { get; set; }                     // "Grade II", "3mm bone loss"
        public int? RotationDeg { get; set; }                  // 45
        public string? Material { get; set; }                  // "Zirconia", "Titanium", "Composite Resin"
        public string Color { get; set; } = "#2563EB";
        public string Description { get; set; } = string.Empty;
        public DateTime DiagnosedDate { get; set; } = DateTime.UtcNow;
    }

    public class ToothMultiStateDto
    {
        public int ToothNumber { get; set; }
        public string PrimaryStatus { get; set; } = "Healthy";
        public string Color { get; set; } = "#10B981";
        public int RotationDeg { get; set; } = 0;
        public string Comments { get; set; } = string.Empty;
        public List<ToothConditionItem> Conditions { get; set; } = new();
    }

    public class BulkTeethMultiUpdateRequest
    {
        public int PatientId { get; set; }
        public List<ToothMultiStateDto> Updates { get; set; } = new();
    }
}
```

### B. Repository Implementation (`DentistAPI/Repositories/DentalRepository.cs`)

```csharp
public async Task<int> SaveBulkTeethMultiAsync(int patientId, List<ToothMultiStateDto> updates)
{
    using var connection = CreateConnection();
    string mergeSql = @"
        MERGE [dentist].[TeethState] AS target
        USING (SELECT @PatientID AS PatientID, @ToothNumber AS ToothNumber) AS source
        ON (target.PatientID = source.PatientID AND target.ToothNumber = source.ToothNumber)
        WHEN MATCHED THEN
            UPDATE SET 
                PrimaryStatus = @PrimaryStatus,
                Color = @Color,
                RotationDeg = @RotationDeg,
                Comments = @Comments,
                ConditionsJson = @ConditionsJson,
                UpdatedAt = SYSUTCDATETIME()
        WHEN NOT MATCHED THEN
            INSERT (PatientID, ToothNumber, PrimaryStatus, Color, RotationDeg, Comments, ConditionsJson, UpdatedAt)
            VALUES (@PatientID, @ToothNumber, @PrimaryStatus, @Color, @RotationDeg, @Comments, @ConditionsJson, SYSUTCDATETIME());";

    int count = 0;
    foreach (var item in updates)
    {
        string conditionsJson = System.Text.Json.JsonSerializer.Serialize(item.Conditions);
        count += await connection.ExecuteAsync(mergeSql, new
        {
            PatientID = patientId,
            ToothNumber = item.ToothNumber,
            PrimaryStatus = item.PrimaryStatus,
            Color = item.Color,
            RotationDeg = item.RotationDeg,
            Comments = item.Comments,
            ConditionsJson = conditionsJson
        });
    }
    return count;
}
```

---

## 🎨 3. Frontend Multi-Layer 3D Canvas Architecture (`ThreeDentalJawArch.jsx`)

Instead of mutual exclusion (`if ... else if`), the Canvas Generator uses **Additive Multi-Layer Canvas Compositing**:

```javascript
/**
 * Procedural Multi-Layer Clinical Canvas Generator
 * Renders multiple stacked clinical restorations and appliances simultaneously
 */
function createClinicalOverlayCanvas(toothData, toothComments, toothNum, isMaxilla) {
  const canvas = document.createElement('canvas');
  canvas.width = 128;
  canvas.height = 128;
  const ctx = canvas.getContext('2d');
  ctx.clearRect(0, 0, 128, 128);

  const full = `${(toothData.status || '')} ${toothComments}`.toLowerCase();

  const isImplant = full.includes('implant');
  const isOrthodontic = full.includes('bracket') || full.includes('orthodontic');
  const isCaries = full.includes('caries') || full.includes('decay') || full.includes('cavity');
  const isComposite = full.includes('composite') || full.includes('fill');
  const isRCT = full.includes('canal') || full.includes('rct') || full.includes('pulpitis');
  const isMobility = full.includes('mobility') && !full.includes('grade 0');

  let hasDrawnSomething = false;

  // 🔹 LAYER 1: BASE ENAMEL & RESTORATIVE CORE
  if (isImplant) {
    // 1. Solid Zirconia Porcelain Base
    ctx.save();
    ctx.fillStyle = '#FFFFFF';
    ctx.beginPath();
    ctx.arc(64, 64, 46, 0, Math.PI * 2);
    ctx.fill();

    // Glowing Medical Teal Aura Ring (#0E8A80)
    ctx.strokeStyle = '#0E8A80';
    ctx.lineWidth = 6;
    ctx.shadowColor = 'rgba(14, 138, 128, 0.9)';
    ctx.shadowBlur = 10;
    ctx.stroke();

    // Central Titanium Abutment & Golden Hex Screw
    ctx.shadowBlur = 0;
    ctx.fillStyle = '#475569';
    ctx.beginPath();
    ctx.arc(64, 64, 26, 0, Math.PI * 2);
    ctx.fill();

    ctx.fillStyle = '#0F172A';
    ctx.beginPath();
    ctx.arc(64, 64, 14, 0, Math.PI * 2);
    ctx.fill();

    ctx.fillStyle = '#F59E0B';
    ctx.beginPath();
    for (let i = 0; i < 6; i++) {
      const a = (i * Math.PI) / 3;
      const hx = 64 + 9 * Math.cos(a);
      const hy = 64 + 9 * Math.sin(a);
      if (i === 0) ctx.moveTo(hx, hy);
      else ctx.lineTo(hx, hy);
    }
    ctx.closePath();
    ctx.fill();
    ctx.restore();
    hasDrawnSomething = true;
  }

  if (isComposite && !isImplant) {
    // 2. Tooth-colored composite filling with blue border
    ctx.save();
    ctx.fillStyle = '#F4EFEA';
    ctx.strokeStyle = '#2563EB';
    ctx.lineWidth = 5;
    ctx.beginPath();
    ctx.ellipse(64, 64, 28, 20, 0, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();
    ctx.restore();
    hasDrawnSomething = true;
  }

  if (isCaries && !isImplant) {
    // 3. Dark caries cavity lesion
    ctx.save();
    ctx.fillStyle = '#5C2C16';
    ctx.beginPath();
    ctx.ellipse(64, 64, 22, 16, 0, 0, Math.PI * 2);
    ctx.fill();
    ctx.restore();
    hasDrawnSomething = true;
  }

  if (isRCT && !isImplant) {
    // 4. Purple canal access with gold apex core
    ctx.save();
    ctx.fillStyle = '#7C3AED';
    ctx.beginPath();
    ctx.arc(64, 64, 16, 0, Math.PI * 2);
    ctx.fill();
    ctx.fillStyle = '#F59E0B';
    ctx.beginPath();
    ctx.arc(64, 64, 7, 0, Math.PI * 2);
    ctx.fill();
    ctx.restore();
    hasDrawnSomething = true;
  }

  // 🔹 LAYER 2: FACIAL HARDWARE & ORTHODONTICS
  if (isOrthodontic) {
    // Titanium Bracket & Blue Archwire Slot
    ctx.save();
    ctx.fillStyle = '#CBD5E1';
    ctx.strokeStyle = '#475569';
    ctx.lineWidth = 2.5;
    ctx.fillRect(44, 44, 40, 40);
    ctx.strokeRect(44, 44, 40, 40);

    ctx.fillStyle = '#0284C7';
    ctx.fillRect(30, 60, 68, 8);
    ctx.restore();
    hasDrawnSomething = true;
  }

  // 🔹 LAYER 3: PERIODONTAL MOBILITY AURAS
  if (isMobility) {
    // Amber pulsing periodontal aura
    ctx.save();
    ctx.strokeStyle = '#F59E0B';
    ctx.lineWidth = 4;
    ctx.setLineDash([5, 4]);
    ctx.beginPath();
    ctx.arc(64, 64, 52, 0, Math.PI * 2);
    ctx.stroke();
    ctx.restore();
    hasDrawnSomething = true;
  }

  if (!hasDrawnSomething) return null;

  const texture = new THREE.CanvasTexture(canvas);
  texture.needsUpdate = true;
  return texture;
}
```

---

## 🎬 4. Animation Management Matrix

| Condition | Animation Technology | Visual Effect |
| :--- | :--- | :--- |
| **Axial Malposition / Rotation** | Three.js `rotateZ(deg)` + CSS `animate-spin [duration:8s]` | Dashed orbital ring spinning around the tooth |
| **Pathologic Mobility** | Periodontal `animate-pulse` / ping aura | Pulsing amber wave indicating micro-mobility |
| **Active Caries Lesion** | CSS Heartbeat `animate-pulse` | Red glowing alert pulse in the cavity pit |
| **Dental Implant & Zirconia Crown** | Static High-Contrast Teal Aura (`#0E8A80`) | Pristine porcelain white with titanium hex-screw |
| **Doctor Hover & Click** | Three.js Raycaster Spotlight | Smooth Z-Elevation (`z: +0.25`) with bright cyan beacon |

---

## 🌈 5. Color Palette System

| Clinical Domain | Hex Color | Role / Condition |
| :--- | :--- | :--- |
| **🔴 Pathology** | `#EF4444` | Active Caries, Cavities, Fractures |
| **🟣 Endodontics** | `#7C3AED` | Root Canal (RCT), Pulpitis, Post & Core |
| **🔵 Restorative** | `#2563EB` | Composite Fillings, Inlays, Sealants |
| **⚪ Amalgam** | `#475569` | Silver Amalgam Restorations |
| **🟡 Periodontics** | `#F59E0B` | Mobility (Grade I/II/III), Bone Loss |
| **🔩 Implantology** | `#0E8A80` | Titanium Implants, Screw-Retained Crowns |
| **🩵 Orthodontics** | `#0284C7` | Orthodontic Brackets, Archwires |
| **🟢 Healthy / Sound** | `#10B981` | Intact Enamel, Physiological Mobility (Grade 0) |

---

## 📱 6. Executive UI/UX Design

1. **32-Tooth Observations Directory (`ChartPage.jsx`)**:
   * Each card displays stacked condition badges, full untruncated notes, and authentic occlusal thumbnails.
2. **Floating Viewport HUD (`ThreeDentalJawArch.jsx`)**:
   * Clamped glassmorphic card (`fixed z-50`) that never clips outside the screen.
3. **Dedicated Jaw Card Header Banners**:
   * `🔵 MAXILLA (UPPER JAW) · 16 Teeth` & `🔵 MANDIBLE (LOWER JAW) · 16 Teeth` banners with zero vertical overlap.
