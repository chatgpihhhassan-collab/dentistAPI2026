import test from 'node:test';
import assert from 'node:assert/strict';

// In-Memory Mock of Session Tracking and Diagnostic Auto-Billing Engine
class MockTreatmentSessionService {
  constructor() {
    this.sessions = [];
    this.treatmentItems = [];
    this.doctorFeeSchedules = {
      1: { consultation: 5000, rvg_xray: 2500, opg_xray: 8500 },
      2: { consultation: 4000, rvg_xray: 2000, opg_xray: 7500 }
    };
  }

  ensureConsultationFee(patientId, doctorId = 1) {
    const existing = this.treatmentItems.find(
      i => i.patientId === patientId && (i.category === 'consultation' || i.procedureCode === 'D0150')
    );
    if (existing) {
      return { added: false, fee: existing.finalPrice, itemId: existing.id, message: 'Consultation fee already exists.' };
    }

    const fee = this.doctorFeeSchedules[doctorId]?.consultation || 5000;
    const newItem = {
      id: this.treatmentItems.length + 1,
      patientId,
      doctorId,
      name: 'Dental Consultation & Diagnosis',
      category: 'consultation',
      procedureCode: 'D0150',
      toothNo: 0,
      estimatedPrice: fee,
      finalPrice: fee,
      status: 'accepted',
      source: 'auto_consultation'
    };
    this.treatmentItems.push(newItem);
    return { added: true, fee, itemId: newItem.id, message: `Consultation fee (Rs ${fee.toLocaleString()}) added.` };
  }

  startSession(patientId, doctorId = 1, appointmentId = null) {
    // Check if active session exists
    let session = this.sessions.find(s => s.patientId === patientId && s.status === 'active');
    let isNew = false;

    if (!session) {
      isNew = true;
      const startedAt = new Date();
      session = {
        sessionId: this.sessions.length + 1,
        patientId,
        doctorId,
        appointmentId,
        startedAt,
        endedAt: null,
        durationSeconds: 0,
        formattedDuration: '00:00',
        status: 'active',
        actionCount: 1,
        summaryNotes: 'Chairside clinical session started',
        actionLogs: [
          {
            id: `act_${Date.now()}_start`,
            timestamp: startedAt.toISOString(),
            elapsedSeconds: 0,
            actionType: 'session_started',
            title: 'Session Started',
            description: `Chairside treatment encounter started by Doctor #${doctorId}.`
          }
        ]
      };
      this.sessions.push(session);
    }

    // Auto-ensure consultation fee
    const consultResult = this.ensureConsultationFee(patientId, doctorId);
    if (consultResult.added) {
      session.actionLogs.push({
        id: `act_${Date.now()}_consult`,
        timestamp: new Date().toISOString(),
        elapsedSeconds: 1,
        actionType: 'consultation_billed',
        title: 'Consultation Fee Added',
        description: `Added Dental Consultation Fee (Rs ${consultResult.fee.toLocaleString()}) to Treatment Plan.`
      });
      session.actionCount = session.actionLogs.length;
    }

    return {
      success: true,
      isNew,
      sessionId: session.sessionId,
      startedAt: session.startedAt,
      status: session.status,
      consultationFeeAdded: consultResult.added,
      consultationFee: consultResult.fee
    };
  }

  logAction(sessionId, actionType, title, description, elapsedSeconds = 0) {
    const session = this.sessions.find(s => s.sessionId === sessionId);
    if (!session) throw new Error('Session not found');

    const newLog = {
      id: `act_${Date.now()}_${Math.random().toString(36).substr(2, 4)}`,
      timestamp: new Date().toISOString(),
      elapsedSeconds,
      actionType,
      title,
      description
    };
    session.actionLogs.push(newLog);
    session.actionCount = session.actionLogs.length;
    return newLog;
  }

  endSession(sessionId, durationSeconds, formattedDuration, summaryNotes = 'Encounter completed') {
    const session = this.sessions.find(s => s.sessionId === sessionId);
    if (!session) throw new Error('Session not found');

    session.endedAt = new Date();
    session.durationSeconds = durationSeconds;
    session.formattedDuration = formattedDuration;
    session.status = 'completed';
    session.summaryNotes = summaryNotes;

    session.actionLogs.push({
      id: `act_${Date.now()}_completed`,
      timestamp: session.endedAt.toISOString(),
      elapsedSeconds: durationSeconds,
      actionType: 'session_completed',
      title: 'Session Completed',
      description: `Session concluded. Duration: ${formattedDuration}.`
    });
    session.actionCount = session.actionLogs.length;

    return {
      success: true,
      sessionId: session.sessionId,
      formattedDuration,
      durationSeconds,
      actionCount: session.actionCount
    };
  }

  getLatestSession(patientId) {
    const patientSessions = this.sessions.filter(s => s.patientId === patientId);
    if (patientSessions.length === 0) {
      return { hasSession: false, durationSeconds: 0, formattedDuration: '00:00' };
    }
    const latest = patientSessions[patientSessions.length - 1];
    return {
      hasSession: true,
      sessionId: latest.sessionId,
      patientId: latest.patientId,
      doctorId: latest.doctorId,
      durationSeconds: latest.durationSeconds,
      formattedDuration: latest.formattedDuration,
      status: latest.status,
      actionCount: latest.actionCount,
      actionLogs: latest.actionLogs
    };
  }
}

// Category matching helper identical to TreatmentPlanTab.jsx
function matchesCategory(item, catUpper) {
  if (!catUpper || catUpper === 'ALL') return true;
  const cat = (item.category || '').toLowerCase();
  const name = (item.name || item.procedureName || '').toLowerCase();
  const code = (item.procedureCode || '').toUpperCase();
  const sel = catUpper.toLowerCase();

  if (cat === sel) return true;
  if (sel === 'braces' && (cat.includes('ortho') || cat.includes('brace') || name.includes('brace') || code.startsWith('D8'))) return true;
  if (sel === 'whitening' && (cat.includes('whiten') || cat.includes('bleach') || name.includes('whiten') || name.includes('bleach'))) return true;
  if (sel === 'cavity' && (cat.includes('cavity') || cat.includes('restor') || name.includes('cavity') || name.includes('composite') || name.includes('amalgam') || code.startsWith('D21') || code.startsWith('D23'))) return true;
  if (sel === 'consultation' && (cat.includes('consult') || name.includes('consult') || name.includes('eval') || code === 'D0150' || code === 'D0120')) return true;
  if ((sel === 'xray' || sel === 'x-ray') && (cat.includes('xray') || cat.includes('x-ray') || cat.includes('radio') || name.includes('x-ray') || name.includes('radiograph') || code === 'D0220' || code === 'D0330')) return true;
  return false;
}

// ==========================================
// TEST SUITE: CHAIRSIDE SESSIONS & ACTION LOGS
// ==========================================

test('Feature: Auto-Billing Consultation Fee on Session Start for New Patient', () => {
  const service = new MockTreatmentSessionService();
  const newPatientId = 1055; // Sadaf Imran

  // Treatment plan is initially empty
  assert.equal(service.treatmentItems.filter(i => i.patientId === newPatientId).length, 0);

  // Doctor starts session
  const startResult = service.startSession(newPatientId, 1);
  assert.equal(startResult.success, true);
  assert.equal(startResult.consultationFeeAdded, true);
  assert.equal(startResult.consultationFee, 5000);

  // Consultation item must now be present in Treatment Plan
  const items = service.treatmentItems.filter(i => i.patientId === newPatientId);
  assert.equal(items.length, 1);
  assert.equal(items[0].category, 'consultation');
  assert.equal(items[0].procedureCode, 'D0150');
  assert.equal(items[0].finalPrice, 5000);
});

test('Feature: Idempotency of Consultation Fee Auto-Billing', () => {
  const service = new MockTreatmentSessionService();
  const patientId = 1055;

  service.startSession(patientId, 1);
  assert.equal(service.treatmentItems.filter(i => i.patientId === patientId).length, 1);

  // Resuming or starting session again does NOT double-add consultation fee
  const secondStart = service.startSession(patientId, 1);
  assert.equal(secondStart.consultationFeeAdded, false);
  assert.equal(service.treatmentItems.filter(i => i.patientId === patientId).length, 1);
});

test('Feature: Detailed Action Logging During Active Treatment Session', () => {
  const service = new MockTreatmentSessionService();
  const patientId = 1055;

  const startRes = service.startSession(patientId, 1);
  const sessionId = startRes.sessionId;

  // Doctor performs chairside actions
  service.logAction(sessionId, 'tooth_diagnosed', 'Tooth #19 Diagnosed', 'Diagnosed Deep Occlusal Caries on Tooth #19', 120);
  service.logAction(sessionId, 'radiograph_acquired', 'Nano-Pix RVG Captured', 'Acquired digital radiograph on Tooth #19', 300);
  service.logAction(sessionId, 'procedure_added', 'Added Composite Restoration', 'Added Composite Posterior #19 (Rs 4,500)', 480);

  // End session
  const endRes = service.endSession(sessionId, 1122, '18m 42s', 'Completed composite restoration on tooth #19');
  assert.equal(endRes.success, true);
  assert.equal(endRes.formattedDuration, '18m 42s');
  assert.equal(endRes.durationSeconds, 1122);

  // Verify full action history
  const latest = service.getLatestSession(patientId);
  assert.equal(latest.hasSession, true);
  assert.equal(latest.status, 'completed');
  assert.equal(latest.formattedDuration, '18m 42s');
  assert.ok(latest.actionLogs.length >= 5);

  const actionTypes = latest.actionLogs.map(l => l.actionType);
  assert.ok(actionTypes.includes('session_started'));
  assert.ok(actionTypes.includes('consultation_billed'));
  assert.ok(actionTypes.includes('tooth_diagnosed'));
  assert.ok(actionTypes.includes('radiograph_acquired'));
  assert.ok(actionTypes.includes('procedure_added'));
  assert.ok(actionTypes.includes('session_completed'));
});

test('Feature: Category Tab Matching and Accurate Counter Separation', () => {
  const sampleItems = [
    { category: 'consultation', name: 'Dental Consultation & Diagnosis', procedureCode: 'D0150', finalPrice: 5000 },
    { category: 'xray', name: 'Digital Intraoral RVG Radiograph', procedureCode: 'D0220', finalPrice: 2500 },
    { category: 'orthodontics', name: 'Comprehensive Braces Alignment', procedureCode: 'D8080', finalPrice: 120000 },
    { category: 'whitening', name: 'Professional Teeth Bleaching', procedureCode: 'D9972', finalPrice: 30000 },
    { category: 'restorative', name: 'Posterior Composite 2-Surface', procedureCode: 'D2392', finalPrice: 6500 }
  ];

  // Test category matching for standard category tabs
  assert.equal(matchesCategory(sampleItems[0], 'CONSULTATION'), true);
  assert.equal(matchesCategory(sampleItems[0], 'X-RAY'), false);

  assert.equal(matchesCategory(sampleItems[1], 'X-RAY'), true);
  assert.equal(matchesCategory(sampleItems[1], 'CONSULTATION'), false);

  assert.equal(matchesCategory(sampleItems[2], 'BRACES'), true);
  assert.equal(matchesCategory(sampleItems[3], 'WHITENING'), true);
  assert.equal(matchesCategory(sampleItems[4], 'CAVITY'), true);

  // Counters
  const consultCount = sampleItems.filter(i => matchesCategory(i, 'CONSULTATION')).length;
  const xrayCount = sampleItems.filter(i => matchesCategory(i, 'X-RAY')).length;
  const bracesCount = sampleItems.filter(i => matchesCategory(i, 'BRACES')).length;
  const whiteningCount = sampleItems.filter(i => matchesCategory(i, 'WHITENING')).length;
  const cavityCount = sampleItems.filter(i => matchesCategory(i, 'CAVITY')).length;

  assert.equal(consultCount, 1);
  assert.equal(xrayCount, 1);
  assert.equal(bracesCount, 1);
  assert.equal(whiteningCount, 1);
  assert.equal(cavityCount, 1);
});
