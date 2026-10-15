# Dentia Clinical AI UX Guidelines & Architecture Manual

> **Version:** 2.0.0  
> **Target Audience:** Frontend Engineers, Product Designers, Dental Scribes, Clinical Informatics  
> **Scope:** Web Application ("Dentia"), Chairside Copilot ("Jarvis"), Ambient Dental Notes, Radiograph Vision Studio, Patient Self-Service Portal.

---

## 1. Core Mission & Philosophy

Dentia is a chairside clinical operating system. Dentists use Dentia while examining patients, wearing nitrile gloves, managing tight 15-minute appointment slots, and handling legal electronic health records (EHR).

Clinical AI features exist to **assist and accelerate** clinical decision-making—**never to automate legal dental records without clinician oversight**.

---

## 2. The 7 Dental UX Principles

### Principle 1: Chairside Intuitive
- **Zero-Friction Interaction**: Minimal clicks for repetitive tasks. Frequent clinical commands should be executable hands-free via voice, spacebar / dental foot-pedal, or one click.
- **Large Ergonomic Touch Targets**: All interactive buttons chairside must have a minimum target of **44 × 44 pixels** to accommodate touchscreens and gloved use.
- **Progressive Disclosure**: High-level summaries first (e.g., "3 findings detected"), expandable into tooth-by-tooth details upon clinician tap.

### Principle 2: Consistent Patterns
Every AI feature in Dentia must adhere to the standard AI component system:
1. **Header / Attribution**: `<AIBadgeAndActions />` with medical safety disclaimer, confidence level (High / Mod / Low %), and data source drawer.
2. **Loading Feedback**: `<AILoadingState />` with live step preview, elapsed timer, and immediate **Cancel** button.
3. **Actionable Error Fallback**: `<AIErrorFallback />` with plain-language explanation, preserved user inputs, and "Retry" / "Write Manually" buttons.
4. **Empty State Inspiration**: `<AIEmptyStateCard />` with 3–4 clickable clinical suggestion chips.

### Principle 3: Reliable Fallbacks & Zero Lost Work
- **No Blocking Alerts**: Browser `alert()` or `confirm()` popups freeze the entire application and can crash audio recording. **They are strictly forbidden.** Use non-blocking inline alerts, toasts, or accessible modals.
- **Draft Persistence**: In-progress dictations, unparsed transcripts, and newly drafted SOAP notes must automatically persist to `localStorage`. If an operatory computer is refreshed or network drops, unsaved work is auto-restored.
- **Input Preservation**: If an AI request fails, user dictation is **never** cleared. A 1-click "Copy Dictation" button is always offered.

### Principle 4: Trust & Control (Clinical Safety Mandate)
- **Zero Unauthorized Auto-Writes**: AI vision and speech models can hallucinate. **No AI finding may write to `[dentist].[TeethState]` or generate invoices without itemized clinician confirmation.**
- **Gated Review Modals**: Radiograph findings and automated odontogram suggestions must use `<AIFindingsConfirmModal />` where the dentist reviews tooth numbers, condition badges, and CDT codes, selecting or deselecting individual items before saving.
- **Undo AI Auto-Fill**: In forms like `NewPatientPage`, an undo banner allows reverting AI field extractions with one click.

### Principle 5: WCAG 2.1 AA Accessibility
- Minimum color contrast ratio of **4.5:1** for body text and **3:1** for large text/icons.
- Full keyboard operability (Tab navigation, Escape to close modals, Enter to confirm).
- Accessible `aria-live="polite"` regions for live captions, speech recognition, and error announcements.

### Principle 6: Perceived Performance
- Provide visual feedback within **100ms** of any user interaction.
- Show elapsed timers and streaming status updates on operations exceeding 3 seconds.
- Provide a responsive **Cancel** button linked to an `AbortController` signal on all AI requests.

### Principle 7: Frictionless Onboarding
- Provide clear suggestions without AI jargon.
- Use clickable prompt chips for common consultations (e.g., "Periodic Oral Evaluation", "Composite Restoration Tooth 16").
- Offer a minimal keyboard/foot-pedal cheatsheet accessible via `?` or command modal.

---

## 3. Dental Terminology & Wording Standards

| Concept | Correct Term | Forbidden / Avoided Terms | Rationale |
| :--- | :--- | :--- | :--- |
| **Planned Care** | **Treatment Plan** | *Treatment Cart, Shopping Cart, Bucket* | Dentistry is medical healthcare, not retail e-commerce. |
| **Tooth Notation (Clinical)** | **FDI Two-Digit (#11–#48)** with Universal (#1–#32) reference | *Single digits without arch context* | International ISO/FDI dental standard avoids fatal extraction mistakes. |
| **Tooth Notation (Patient)** | **Plain Names** (e.g. *Upper Right First Molar (#3)*) | *Raw CDT codes or isolated numbers* | Patients cannot interpret "Tooth 16 MOD D2391". |
| **Tooth Decay** | **Caries / Enamel Demineralization** (Doctor) &bull; **Cavity / Tooth Decay** (Patient) | *Rot, Hole, Dirty Tooth* | Professional and reassuring terminology. |
| **AI Status** | **Advisory Finding / Suggestion** | *Diagnostic Truth, AI Diagnosis* | AI is legally an assistive tool; only the licensed dentist diagnoses. |
| **Currency** | Standard clinic currency (**PKR / USD**) formatted per clinic profile | *Mismatched currency symbols ($ for PKR)* | Avoids patient billing disputes and invoicing errors. |

---

## 4. Standard API Contracts

### Standard Success Response Envelope
```json
{
  "success": true,
  "data": {
    "summary": "Clinical SOAP note drafted...",
    "detectedFindings": [
      {
        "toothKey": "16",
        "condition": "Caries",
        "surface": "Occlusal",
        "confidence": 0.92,
        "cdtCode": "D2391"
      }
    ]
  },
  "meta": {
    "promptId": "clinical-note-summarizer",
    "version": "2.0.0",
    "model": "gemini-3.1-flash-lite",
    "confidence": 0.92,
    "latencyMs": 1420
  }
}
```

### Standard Error Response Envelope
```json
{
  "success": false,
  "error": {
    "code": "TIMEOUT_ERROR",
    "message": "Gateway timeout: Gemini inference exceeded 25000ms",
    "userMessage": "The AI analysis took longer than expected. You can retry or complete documentation manually.",
    "status": 504
  },
  "meta": {
    "promptId": "radiograph-caries-detector",
    "version": "2.0.0"
  }
}
```

### Error Code Taxonomy
- `TIMEOUT_ERROR`: Request exceeded SLA (default 25s).
- `OFFLINE`: Browser or sensor disconnected from clinic network.
- `RATE_LIMITED`: HTTP 429 high load; exponential backoff retry active.
- `VALIDATION_ERROR`: Missing mandatory clinical identifiers or invalid tooth key.
- `SAFETY_BLOCKED`: Content triggered medical safety filter.
- `SERVER_ERROR`: Upstream network or internal 500 error; inputs preserved.
- `ABORTED`: User clicked "Cancel" chairside.

---

## 5. Developer Guide: How to Add a New AI Feature in Dentia

Follow these 5 steps when introducing any new AI-powered capability:

### Step 1: Use `aiClient` for Network Calls
Import `aiClient` from `src/services/ai/aiClient.js`:
```javascript
import { aiClient } from '../services/ai/aiClient';

const abortController = new AbortController();

const res = await aiClient.post('/api/ai/my-endpoint', payload, {
  signal: abortController.signal,
  timeoutMs: 20000,
  meta: { feature: 'my-feature', promptId: 'my-prompt-id' }
});

if (!res.success) {
  // Use res.error.userMessage directly in UI
  setErrorMessage(res.error.userMessage);
}
```

### Step 2: Implement Skeleton & Cancelable Loading State
Never show a blank screen or plain spinner. Use `<AILoadingState />`:
```jsx
{isLoading && (
  <AILoadingState
    title="Analyzing Study"
    text="Dentia AI is processing records..."
    onCancel={() => abortController.abort()}
  />
)}
```

### Step 3: Implement the Friendly Error Fallback
Never display raw JSON error strings or stack traces:
```jsx
{error && (
  <AIErrorFallback
    error={error}
    preservedInput={userInputText}
    onRetry={handleExecuteAI}
    onContinueManual={() => setError(null)}
  />
)}
```

### Step 4: Display Output with Guardrail Badge & Feedback
Wrap all generated output with `<AIBadgeAndActions />`:
```jsx
<AIBadgeAndActions
  promptId="my-prompt-id"
  version="2.0.0"
  feature="clinical"
  confidence={result.meta?.confidence || 0.9}
  basedOn="Current bitewing radiograph & periodontal probe measurements"
  textToCopy={result.text}
  onRegenerate={handleExecuteAI}
  showFeedback={true}
/>
```

### Step 5: Gated Review Before Legal EHR / Chart Writes
If the feature produces tooth diagnoses, pathology findings, or treatment items, **never write directly to the database**. Present findings in `<AIFindingsConfirmModal />`:
```jsx
<AIFindingsConfirmModal
  isOpen={showReviewModal}
  findings={detectedFindings}
  onClose={() => setShowReviewModal(false)}
  onConfirm={async (approvedFindings) => {
    await saveToDentalChart(approvedFindings);
  }}
/>
```

---

## 6. Verification Checklist for New AI PRs
- [ ] No `alert()`, `confirm()`, or unhandled `window.prompt()` calls.
- [ ] Large touch targets (>=44px) on primary actions.
- [ ] Unsaved dictation or form state is saved to `localStorage`.
- [ ] Tooth notation follows FDI standard with Universal secondary notation.
- [ ] AI error messages are plain language and actionable.
- [ ] "Cancel" button aborts the network request.
- [ ] Clinician has explicit checkbox review before chart records are committed.
