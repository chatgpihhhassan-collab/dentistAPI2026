---
title: "Dentia Dental Platform — Obsidian Master Hub & Knowledge Base"
date_created: 2026-09-28
last_updated: 2026-09-28
tags:
  - dentia/hub
  - dentia/architecture
  - dentia/clinical
  - dentia/database
  - dentia/ai
  - dentia/security
  - dentia/portal
aliases:
  - Dentia Hub
  - Master MOC
  - Dentia Wiki
---

# 🦷 DENTIA Clinical & System Knowledge Base (Obsidian Hub)

> [!INFO] **Welcome to the DENTIA Knowledge Graph**
> This repository is fully configured as a native **Obsidian Vault**.
> Every clinical specialty, architecture document, SQL migration, AI vision pipeline, and security audit is connected using bi-directional `[[wikilinks]]`.
>
> 💡 **Quick Tip in Obsidian:** Press `Ctrl + G` to open the **Interactive Graph View** or open [[DENTIA_SYSTEM_ARCHITECTURE.canvas]] for the zoomable system whiteboard!

---

## 🗺️ Interactive System Architecture Canvas
- 🎨 **Visual Whiteboard Map:** [[DENTIA_SYSTEM_ARCHITECTURE.canvas]] *(Zoom in/out to explore Frontend, Backend, AI Engines, Hardware bridges & Database connections)*

---

## 🏗️ 1. Core Architecture & System Foundations
*High-level architecture, user experience, caching, session security, and directory rendering.*

- [[DENTIA_FULL_FUNCTIONALITY_CLINICAL_AND_SYSTEM_MANUAL]] — Complete end-to-end master manual of all workflows, controllers, and pages.
- [[PROJECT_DOCUMENTATION]] — Architectural layers, tech stack (.NET 8 + React 19 + Vite), and repository layout.
- [[README]] — Quickstart guide, local environment configuration, and startup instructions.
- [[DESKTOP_UI_UX_DESIGN_REVIEW_REPORT]] — Desktop UI/UX benchmarks, responsive layout specs, and color schemes.
- [[DIRECTORY_PAGE_LOADING_AND_READY_STATE_ARCHITECTURE]] — Instant directory hydration, skeleton loading, and query optimization.
- [[PERFORMANCE_OPTIMIZATION_ARCHITECTURE_DIRECTORY_AND_CHART]] — 60 FPS odontogram rendering, memoization, and API debouncing.
- [[IDLE_SESSION_TIMEOUT_ARCHITECTURE]] — HIPAA-compliant idle session timeout, auto-lock modal, and token refresh.
- [[JAW_IMAGES_BACKGROUND_PRELOAD_ARCHITECTURE]] — Instant odontogram jaw switching with background texture preloading.

---

## 🩺 2. Clinical Specialties & Odontogram Suite
*Tooth-specific charting, specialty surgical planning, orthodontic suites, and PDF report engines.*

- [[CLINICAL_SPECIALTIES_EXPANSION_AND_SQL_GUIDE]] — **🔩 Implant Planning (D6010), 🔬 Biopsy Pathology (D7286), ✨ Clear Aligners (D8080)** full clinical specs, modals, voice dictation, and SQL schema.
- [[ORTHO_OCCLUSION_TMJ_SPECIFICATION]] — Angle's Class I/II/III occlusion, overjet, overbite, crossbite, and TMJ clicking/crepitus diagnosis.
- [[ORTHO_TMJ_AGE_COHORT_CLINICAL_SPECIFICATION]] — Pediatric vs Adult orthodontic cohorts, mixed dentition analysis, and retention protocols.
- [[ORTHO_TMJ_SAVE_BEHAVIOR_AND_SYSTEM_AUDIT]] — Audit trail and persistence engine for orthodontic & TMJ exams.
- [[MULTI_CONDITION_TEETH_ARCHITECTURE_AND_SQL_GUIDE]] — Surface-level condition stacking (MOD restorations, recurrent caries, crowns).
- [[COMPREHENSIVE_PATIENT_DENTAL_REPORT_PDF_SPECIFICATION]] — Multi-page clinical PDF report generator with tooth maps and AI notes.
- [[missing-clinical-fields]] — Comprehensive dental chart audit and clinical field gap analysis.

---

## 🗄️ 3. Database Schema & SQL Migrations
*SQL Server schemas, entity relationships, billing schedules, and table structures.*

- [[DATABASE_SCHEMA_DENTIST]] — Complete master database schema and table relational dictionary.
- `CLINICAL_FIELDS_EXPANSION_MIGRATION.sql` — Schema migration for Implant Planning, Biopsy Requisitions, and Clear Aligners.
- `SEED_ALL_15_CATEGORIES_PROCEDURES.sql` — 135+ verified CDT clinical procedures and categories.
- `DOCTOR_FEE_SCHEDULES_AND_TREATMENT_BILLING.sql` — Fee schedules, doctor commission percentages, and tax calculations.
- `PATIENT_PORTAL_AND_BILLING_MIGRATION.sql` — Patient authentication credentials and self-service billing records.
- `PATIENT_DOCTOR_TREATMENT_AND_INVOICE_SYNC_MIGRATION.sql` — Two-way sync tables between doctor chart and patient invoice.
- `DENTIA_IMAGING_AND_VOICE_AI_MIGRATION.sql` — Tables for radiograph metadata, voice notes, and AI findings.

---

## 🤖 4. AI Dental Scribe, Vision & Hardware Ingestion
*Speech-to-text clinical transcription, Groq vision x-ray detection, and hardware LAN scanner bridge.*

- [[DENTIA_IMAGING_AI_PIPELINE_SPECIFICATION]] — High-throughput Groq Vision and LLaVA imaging ingestion architecture.
- [[DENTIA_IMAGING_XRAYS_AI_CHART_WORKFLOW_SPECIFICATION]] — Automated caries/periapical pathology detection with odontogram overlays.
- [[DENTIA_Imaging_AI_Implementation_Prompts]] — Clinically validated system prompts for dental radiograph analysis.
- [[ANAM_AI_INTEGRATION_AND_EVALUATION_GUIDE]] — Real-time interactive digital human consultation avatar evaluation.
- [[SOREDEX_DIGORA_OPTIME_ETHERNET_INTEGRATION]] — Reverse-engineered TCP/IP LAN bridge for **Soredex Digora Optime** dental phosphor plate scanners.
- `patient_26_mic_transcription_and_gemini_logs.txt` — Real-world voice dictation benchmarks and Gemini AI response logs.

---

## 🔒 5. Security, Pentesting & Resilience
*Penetration testing, rate limiting, magic-byte upload validation, and vulnerability remediation.*

- [[SECURITY_HARDENING_RATE_LIMITING_AND_FILEUPLOAD_PLAN]] — ASP.NET Core IP rate limiting, magic-byte MIME validation, and security headers.
- `run_strix_pentest.ps1` — Automated runner for Strix AI autonomous penetration testing engine.
- `strix_security_config.yaml` — DAST configuration, target endpoints, rate limits, and scan depth.
- `test_api_security.ps1` — Automated smoke test verifying rate limit HTTP 429 triggers and auth fuzzing.
- [[FULL_PROJECT_QA_AUDIT_REPORT]] — Complete security and quality assurance verification report.

---

## 👥 6. Patient Portal, Notion Sync & Billing Hub
*Self-service patient dashboard, online appointment booking, and Notion calendar sync.*

- [[DOCTOR_PROFILE_HISTORY_AND_SUPERADMIN_SPECIFICATION]] — Complete doctor profiles, experience, hospital affiliations, patient selection & superadmin controls.
- `DOCTOR_PROFILES_AND_PATIENT_PORTAL_EXPANSION.sql` — Schema migration for doctor profiles, work experience, and verified clinic seed data.
- [[NOTION_APPOINTMENT_INTEGRATION_GUIDE]] — Notion Calendar 2-way real-time synchronization guide and Kanban scheduler.
- [[PATIENT_PORTAL_ARCHITECTURE_AND_UI_UX_SPECIFICATION]] — Patient portal architecture, health maps, and invoice views.
- [[PATIENT_PORTAL_AND_DOCTOR_TREATMENT_SYNC_MANUAL]] — Synchronization flow between doctor treatment plans and patient access.
- [[DOCTOR_PER_PATIENT_INVOICE_AND_TREATMENT_REPORT_SPECIFICATION]] — Financial reports and itemized billing specifications.
- [[TREATMENT_PRICING_AND_INVOICING_SPECIFICATION]] — Multi-tier procedure pricing algorithms.
- [[PATIENT_PROFILE_IMAGE_SPECIFICATION]] — Secure profile image upload, compression, and display rules.

---

## 🚀 7. 15-Step Master Implementation Journey
*Chronological roadmap of the Dentia platform implementation milestones.*

| Step | Topic | Specification |
| :--- | :--- | :--- |
| **01** | Backend Authentication & Models | [[STEP_1_BACKEND_AUTHENTICATION_AND_MODELS]] |
| **02** | Backend Portal & Billing APIs | [[STEP_2_BACKEND_PORTAL_AND_BILLING_APIS]] |
| **03** | Frontend Auth & Portal Routing | [[STEP_3_FRONTEND_AUTH_AND_PORTAL_ROUTING]] |
| **04** | Patient Dashboard & Health Map | [[STEP_4_PATIENT_DASHBOARD_AND_HEALTH_MAP]] |
| **05** | Appointments Hub & Booking | [[STEP_5_APPOINTMENTS_HUB_AND_BOOKING]] |
| **06** | Clinical Reports & X-Rays | [[STEP_6_CLINICAL_REPORTS_AND_XRAYS]] |
| **07** | Billing Online & Cash Payments | [[STEP_7_BILLING_ONLINE_AND_CASH_PAYMENTS]] |
| **08** | End-to-End Verification Report | [[STEP_8_END_TO_END_VERIFICATION_REPORT]] |
| **09** | Imaging & VoiceStudio DB Schema | [[STEP_9_IMAGING_AND_VOICESTUDIO_DATABASE_SCHEMA]] |
| **10** | Backend Imaging Ingestion & Groq Vision | [[STEP_10_BACKEND_IMAGING_INGESTION_AND_GROQ_VISION]] |
| **11** | Backend VoiceStudio API Service | [[STEP_11_BACKEND_VOICESTUDIO_API_SERVICE]] |
| **12** | AI Findings Review & Safety Gateway | [[STEP_12_BACKEND_AI_FINDINGS_REVIEW_AND_SAFETY_GATEWAY]] |
| **13** | Frontend Camera Capture & Gallery | [[STEP_13_FRONTEND_CAMERA_CAPTURE_AND_GALLERY]] |
| **14** | Frontend Odontogram AI Overlay & Voice Post-Op | [[STEP_14_FRONTEND_ODONTOGRAM_AI_OVERLAY_AND_VOICE_POSTOP]] |
| **15** | Imaging & Voice E2E Verification Report | [[STEP_15_IMAGING_AND_VOICE_E2E_VERIFICATION_REPORT]] |

---

## ⚡ Obsidian Keyboard Shortcuts for This Vault
- **`Ctrl + O`** — Quick Switcher: Type any document title to jump to it instantly.
- **`Ctrl + G`** — Graph View: Visualize connections between all clinical, architectural, and database documents.
- **`Ctrl + Click`** on any `[[Link]]` — Open the linked document in a new split tab.
- **`Alt + Click`** — Preview document without leaving your current note.
