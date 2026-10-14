/**
 * 🏥 API Integration Tests: Patients Management, Duplicate Prevention & Edge Cases
 * Tests patient validation, duplicate detection, SQL/XSS resilience, Urdu/multilingual text, and pagination.
 */

import { describe, it, beforeEach } from 'node:test';
import assert from 'node:assert/strict';

import { buildSyntheticPatient, EdgeCasePayloads } from '../factories/test_data_factories.mjs';
import {
  normalizePakistaniPhone,
  validateCnic,
  sanitizeText
} from '../../Dentistfrontend/src/validators/normalizationUtils.js';

// Simulated Patients Service mirroring PatientsController validation & repository logic
class MockPatientsRepository {
  constructor() {
    this.patients = new Map();
  }

  findDuplicate(doctorId, firstName, lastName, phone, dob) {
    for (const p of this.patients.values()) {
      if (p.doctorId === doctorId &&
          p.firstName.toLowerCase() === firstName.toLowerCase() &&
          p.lastName.toLowerCase() === lastName.toLowerCase() &&
          p.phone === phone &&
          p.dob === dob) {
        return p;
      }
    }
    return null;
  }

  insert(patient) {
    this.patients.set(patient.patientID, patient);
    return patient;
  }

  query({ page = 1, limit = 20, search = '' }) {
    // Normalization of pagination bounds
    const safePage = Math.max(1, parseInt(page, 10) || 1);
    const safeLimit = Math.min(100, Math.max(1, parseInt(limit, 10) || 20));

    let all = Array.from(this.patients.values());
    if (search && search.trim()) {
      const q = search.trim().toLowerCase();
      // Safe parameterized-like search
      all = all.filter(p =>
        p.firstName.toLowerCase().includes(q) ||
        p.lastName.toLowerCase().includes(q) ||
        (p.phone && p.phone.includes(q))
      );
    }

    const start = (safePage - 1) * safeLimit;
    const pageItems = all.slice(start, start + safeLimit);

    return {
      items: pageItems,
      totalCount: all.length,
      page: safePage,
      limit: safeLimit,
      totalPages: Math.ceil(all.length / safeLimit)
    };
  }
}

function processCreatePatient(repo, payload) {
  if (!payload) {
    return { status: 400, errors: { payload: "Patient payload cannot be empty." } };
  }

  const errors = {};

  // First Name validation
  const firstName = sanitizeText(payload.firstName, 50);
  if (!firstName || firstName.length < 2) {
    errors.firstName = "First name must be at least 2 characters.";
  }

  // Last Name validation
  const lastName = sanitizeText(payload.lastName, 50);
  if (!lastName || lastName.length < 2) {
    errors.lastName = "Last name must be at least 2 characters.";
  }

  // Phone normalization
  const phoneRes = normalizePakistaniPhone(payload.phone);
  if (!phoneRes.isValid) {
    errors.phone = phoneRes.error || "Invalid Pakistani phone number.";
  }

  // CNIC validation (if provided)
  if (payload.cnic) {
    const cnicRes = validateCnic(payload.cnic);
    if (!cnicRes.isValid) {
      errors.cnic = cnicRes.error;
    }
  }

  // DOB validation
  if (!payload.dob) {
    errors.dob = "Date of Birth is required.";
  }

  if (Object.keys(errors).length > 0) {
    return { status: 400, errors };
  }

  // Duplicate check
  const duplicate = repo.findDuplicate(
    payload.doctorId || 1,
    firstName,
    lastName,
    phoneRes.normalized,
    payload.dob
  );

  if (duplicate) {
    return {
      status: 409,
      error: `An account for '${firstName} ${lastName}' is already registered with Reference #${duplicate.referenceNumber}.`,
      existingReferenceNumber: duplicate.referenceNumber
    };
  }

  const newPatient = {
    ...payload,
    firstName,
    lastName,
    phone: phoneRes.normalized,
    address: sanitizeText(payload.address, 500),
    medicalHistory: sanitizeText(payload.medicalHistory, 2000)
  };

  repo.insert(newPatient);
  return { status: 201, patient: newPatient };
}

describe('API Integration: Patient Creation & Validation', () => {
  let repo;
  beforeEach(() => {
    repo = new MockPatientsRepository();
  });

  it('should successfully create a new patient with sanitized fields and normalized phone', () => {
    const raw = buildSyntheticPatient({
      firstName: "  Muhammad  ",
      lastName: "Ali",
      phone: "03001234567",
      cnic: "35201-1234567-1"
    });

    const res = processCreatePatient(repo, raw);
    assert.equal(res.status, 201);
    assert.equal(res.patient.firstName, "Muhammad");
    assert.equal(res.patient.phone, "+923001234567");
  });

  it('should REJECT payload with missing or short names (< 2 chars)', () => {
    const raw = buildSyntheticPatient({ firstName: "A", lastName: "" });
    const res = processCreatePatient(repo, raw);
    assert.equal(res.status, 400);
    assert.ok(res.errors.firstName);
    assert.ok(res.errors.lastName);
  });

  it('should REJECT payload with invalid landline phone number', () => {
    const raw = buildSyntheticPatient({ phone: "04235889900" });
    const res = processCreatePatient(repo, raw);
    assert.equal(res.status, 400);
    assert.ok(res.errors.phone);
  });
});

describe('API Integration: Duplicate Patient Detection', () => {
  let repo;
  beforeEach(() => {
    repo = new MockPatientsRepository();
  });

  it('should return 409 Conflict when submitting an identical patient duplicate', () => {
    const patient1 = buildSyntheticPatient({
      doctorId: 1,
      firstName: "Fatima",
      lastName: "Zahra",
      phone: "03215556677",
      dob: "1994-08-12"
    });

    const firstRes = processCreatePatient(repo, patient1);
    assert.equal(firstRes.status, 201);

    // Duplicate submit
    const duplicatePayload = buildSyntheticPatient({
      doctorId: 1,
      firstName: "fatima", // Case-insensitive matching
      lastName: "ZAHRA",
      phone: "03215556677",
      dob: "1994-08-12"
    });

    const secondRes = processCreatePatient(repo, duplicatePayload);
    assert.equal(secondRes.status, 409);
    assert.equal(secondRes.error.includes("already registered"), true);
  });
});

describe('API Integration: Security & Edge Case Payloads', () => {
  let repo;
  beforeEach(() => {
    repo = new MockPatientsRepository();
  });

  it('should sanitize XSS script payloads in medical history and address', () => {
    const p = buildSyntheticPatient({
      medicalHistory: EdgeCasePayloads.XSS_SCRIPT_TAG + " Penicillin allergy",
      address: EdgeCasePayloads.XSS_IMG_ONERROR + " House 42"
    });

    const res = processCreatePatient(repo, p);
    assert.equal(res.status, 201);
    assert.equal(res.patient.medicalHistory.includes("<script"), false);
    assert.equal(res.patient.medicalHistory, "Penicillin allergy");
    assert.equal(res.patient.address.includes("<img"), false);
    assert.equal(res.patient.address, "House 42");
  });

  it('should truncate buffer overflow strings exceeding max schema bounds (10,000+ chars)', () => {
    const p = buildSyntheticPatient({
      medicalHistory: EdgeCasePayloads.VERY_LONG_STRING_10K
    });

    const res = processCreatePatient(repo, p);
    assert.equal(res.status, 201);
    assert.equal(res.patient.medicalHistory.length, 2000); // Truncated to 2000 max
  });

  it('should preserve Urdu script and Roman Urdu clinical notes accurately without encoding failure', () => {
    const p = buildSyntheticPatient({
      medicalHistory: EdgeCasePayloads.URDU_NASTALIQ,
      address: "اسلام آباد"
    });

    const res = processCreatePatient(repo, p);
    assert.equal(res.status, 201);
    assert.equal(res.patient.medicalHistory, EdgeCasePayloads.URDU_NASTALIQ);
    assert.equal(res.patient.address, "اسلام آباد");
  });

  it('should safely neutralize SQL injection vectors in patient search', () => {
    repo.insert(buildSyntheticPatient({ firstName: "Tariq", lastName: "Mehmood" }));

    // Attacker query
    const result = repo.query({ search: EdgeCasePayloads.SQLI_AUTH_BYPASS });
    // Should safely return 0 matches rather than dumping all table rows
    assert.equal(result.items.length, 0);
  });

  it('should safely normalize boundary pagination values (page=0, page=-5, limit=5000)', () => {
    for (let i = 0; i < 25; i++) {
      repo.insert(buildSyntheticPatient({ patientID: 2000 + i, firstName: `Patient_${i}` }));
    }

    const pZero = repo.query({ page: 0, limit: 10 });
    assert.equal(pZero.page, 1); // Normalized to 1
    assert.equal(pZero.items.length, 10);

    const pOverLimit = repo.query({ page: 1, limit: 5000 });
    assert.equal(pOverLimit.limit, 100); // Clamped to 100 max

    const pPastEnd = repo.query({ page: 999, limit: 10 });
    assert.equal(pPastEnd.items.length, 0); // Clean empty list beyond end
  });
});
