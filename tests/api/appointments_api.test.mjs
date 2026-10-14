/**
 * 📅 API Integration Tests: Appointments Management & Business Rules
 * Tests 30-min collision prevention, operating hours (08:00–20:00), past booking checks, and status transitions.
 */

import { describe, it, beforeEach } from 'node:test';
import assert from 'node:assert/strict';

import { buildSyntheticAppointment } from '../factories/test_data_factories.mjs';

class MockAppointmentsRepository {
  constructor() {
    this.appointments = new Map();
  }

  insert(appointment) {
    this.appointments.set(appointment.appointmentId, { ...appointment });
    return appointment;
  }

  getById(id) {
    return this.appointments.get(id) || null;
  }

  update(id, updates) {
    const existing = this.appointments.get(id);
    if (!existing) return null;
    const updated = { ...existing, ...updates };
    this.appointments.set(id, updated);
    return updated;
  }

  countCollision(doctorId, preferredDate, thresholdMinutes = 30) {
    const targetMs = new Date(preferredDate).getTime();
    let collisionCount = 0;

    for (const appt of this.appointments.values()) {
      // Scoped to the same doctor (or any doctor if doctorId is null/0)
      if (doctorId && appt.doctorId && appt.doctorId !== doctorId) {
        continue;
      }

      // Ignore terminal canceled/no-show appointments
      if (['Cancelled', 'No-Show', 'Rejected'].includes(appt.status)) {
        continue;
      }

      const existingMs = new Date(appt.preferredDate).getTime();
      const diffMinutes = Math.abs(existingMs - targetMs) / (1000 * 60);

      if (diffMinutes < thresholdMinutes) {
        collisionCount++;
      }
    }

    return collisionCount;
  }
}

function processCreateAppointment(repo, payload) {
  if (!payload) {
    return { status: 400, error: "Payload required." };
  }

  const d = new Date(payload.preferredDate);
  if (isNaN(d.getTime())) {
    return { status: 400, field: "preferredDate", error: "Invalid date format." };
  }

  // 1. Past Date Check (-15 min grace)
  if (d.getTime() < Date.now() - 15 * 60 * 1000) {
    return {
      status: 400,
      field: "preferredDate",
      error: "Appointment date cannot be in the past."
    };
  }

  // 2. Operating Hours: 08:00 to 20:00
  const hour = d.getHours();
  if (hour < 8 || hour >= 20) {
    return {
      status: 400,
      field: "preferredDate",
      error: "Appointments can only be scheduled within clinic operating hours (08:00 AM to 08:00 PM)."
    };
  }

  // 3. Doctor Collision Check
  const collisions = repo.countCollision(payload.doctorId, payload.preferredDate, 30);
  if (collisions > 0) {
    return {
      status: 400,
      field: "preferredDate",
      error: "That time slot is already allocated for this doctor. Please choose another time."
    };
  }

  const newAppt = {
    ...payload,
    appointmentId: payload.appointmentId || Math.floor(Math.random() * 90000) + 10000,
    status: "Pending"
  };

  repo.insert(newAppt);
  return { status: 200, appointment: newAppt };
}

// State Transition Engine for Appointments
function processUpdateAppointmentStatus(repo, appointmentId, newStatus) {
  const existing = repo.getById(appointmentId);
  if (!existing) {
    return { status: 404, error: "Appointment not found." };
  }

  const allowedStatuses = ["Pending", "Confirmed", "In-Progress", "Completed", "Cancelled", "No-Show"];
  if (!allowedStatuses.includes(newStatus)) {
    return { status: 400, error: `Invalid status: ${newStatus}` };
  }

  // Business Rule: Terminal states (Completed, Cancelled, No-Show) cannot revert to Pending/Confirmed
  const terminalStates = ["Completed", "Cancelled", "No-Show"];
  if (terminalStates.includes(existing.status) && (newStatus === "Pending" || newStatus === "Confirmed")) {
    return {
      status: 400,
      error: `Invalid transition: A ${existing.status} appointment cannot be reverted back to ${newStatus}.`
    };
  }

  const updated = repo.update(appointmentId, { status: newStatus });
  return { status: 200, appointment: updated };
}

describe('API Integration: Appointment Booking & Collision Prevention', () => {
  let repo;
  beforeEach(() => {
    repo = new MockAppointmentsRepository();
  });

  it('should successfully book an appointment within operating hours (e.g. 10:00 AM)', () => {
    const tomorrow10Am = new Date();
    tomorrow10Am.setDate(tomorrow10Am.getDate() + 1);
    tomorrow10Am.setHours(10, 0, 0, 0);

    const appt = buildSyntheticAppointment({ doctorId: 1, preferredDate: tomorrow10Am.toISOString() });
    const res = processCreateAppointment(repo, appt);
    assert.equal(res.status, 200);
    assert.equal(res.appointment.status, "Pending");
  });

  it('should REJECT appointment booked outside operating hours (e.g. 05:00 AM)', () => {
    const earlyMorning = new Date();
    earlyMorning.setDate(earlyMorning.getDate() + 1);
    earlyMorning.setHours(5, 0, 0, 0);

    const appt = buildSyntheticAppointment({ preferredDate: earlyMorning.toISOString() });
    const res = processCreateAppointment(repo, appt);
    assert.equal(res.status, 400);
    assert.equal(res.error.includes("operating hours"), true);
  });

  it('should REJECT appointment booked in the past', () => {
    const past = new Date(Date.now() - 48 * 3600 * 1000);
    const appt = buildSyntheticAppointment({ preferredDate: past.toISOString() });
    const res = processCreateAppointment(repo, appt);
    assert.equal(res.status, 400);
    assert.equal(res.error.includes("past"), true);
  });

  it('should REJECT overlapping double-booking for the SAME doctor within 30 minutes', () => {
    const baseDate = new Date();
    baseDate.setDate(baseDate.getDate() + 2);
    baseDate.setHours(14, 0, 0, 0); // 02:00 PM

    const first = buildSyntheticAppointment({
      doctorId: 1,
      preferredDate: baseDate.toISOString()
    });
    assert.equal(processCreateAppointment(repo, first).status, 200);

    // Overlapping booking: 15 minutes later (02:15 PM) for Doctor 1
    const overlapDate = new Date(baseDate.getTime() + 15 * 60 * 1000);
    const second = buildSyntheticAppointment({
      doctorId: 1,
      preferredDate: overlapDate.toISOString()
    });
    const conflictRes = processCreateAppointment(repo, second);
    assert.equal(conflictRes.status, 400);
    assert.equal(conflictRes.error.includes("already allocated"), true);
  });

  it('should ALLOW concurrent booking at the exact same time for a DIFFERENT doctor', () => {
    const baseDate = new Date();
    baseDate.setDate(baseDate.getDate() + 2);
    baseDate.setHours(15, 0, 0, 0);

    // Doctor 1 booking
    const apptDoc1 = buildSyntheticAppointment({ doctorId: 1, preferredDate: baseDate.toISOString() });
    assert.equal(processCreateAppointment(repo, apptDoc1).status, 200);

    // Doctor 2 booking at exact same time
    const apptDoc2 = buildSyntheticAppointment({ doctorId: 2, preferredDate: baseDate.toISOString() });
    const resDoc2 = processCreateAppointment(repo, apptDoc2);
    assert.equal(resDoc2.status, 200); // Different doctor chairs are independent
  });
});

describe('API Integration: Appointment Status Transition Rules', () => {
  let repo;
  beforeEach(() => {
    repo = new MockAppointmentsRepository();
  });

  it('should follow natural forward status progression: Pending -> Confirmed -> In-Progress -> Completed', () => {
    const appt = buildSyntheticAppointment({ appointmentId: 501 });
    repo.insert(appt);

    assert.equal(processUpdateAppointmentStatus(repo, 501, "Confirmed").status, 200);
    assert.equal(processUpdateAppointmentStatus(repo, 501, "In-Progress").status, 200);
    assert.equal(processUpdateAppointmentStatus(repo, 501, "Completed").status, 200);
  });

  it('should REJECT reverting a Completed appointment back to Pending (APPT-BUG-01)', () => {
    const appt = buildSyntheticAppointment({ appointmentId: 502, status: "Completed" });
    repo.insert(appt);

    const res = processUpdateAppointmentStatus(repo, 502, "Pending");
    assert.equal(res.status, 400);
    assert.equal(res.error.includes("Invalid transition"), true);
  });

  it('should allow cancellation from Pending or Confirmed status', () => {
    const appt = buildSyntheticAppointment({ appointmentId: 503, status: "Confirmed" });
    repo.insert(appt);

    const res = processUpdateAppointmentStatus(repo, 503, "Cancelled");
    assert.equal(res.status, 200);
    assert.equal(res.appointment.status, "Cancelled");
  });
});
