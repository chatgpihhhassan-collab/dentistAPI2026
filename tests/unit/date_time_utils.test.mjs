/**
 * 🧪 Unit Tests: Clinical Date, Time & Working Hours Utilities
 * Tests operating hour boundaries (08:00 to 20:00), past booking prevention, leap years, and timezones.
 */

import { describe, it } from 'node:test';
import assert from 'node:assert/strict';

export function isWithinOperatingHours(dateInput) {
  if (!dateInput) return { isValid: false, reason: "Date is required." };
  const d = new Date(dateInput);
  if (isNaN(d.getTime())) return { isValid: false, reason: "Invalid date format." };

  const hour = d.getHours();
  // Operating hours: 08:00 AM to 08:00 PM (hour 8 through 19)
  if (hour < 8 || hour >= 20) {
    return {
      isValid: false,
      reason: `Requested time (${d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}) is outside operating hours (08:00 AM - 08:00 PM).`
    };
  }

  return { isValid: true, reason: null };
}

export function validateAppointmentNotPast(dateInput, graceMinutes = 15) {
  if (!dateInput) return { isValid: false, reason: "Date is required." };
  const d = new Date(dateInput);
  if (isNaN(d.getTime())) return { isValid: false, reason: "Invalid date format." };

  const cutoff = new Date(Date.now() - graceMinutes * 60 * 1000);
  if (d < cutoff) {
    return { isValid: false, reason: "Appointment date cannot be in the past." };
  }

  return { isValid: true, reason: null };
}

export function isSlotColliding(existingDateInput, newDateInput, thresholdMinutes = 30) {
  const d1 = new Date(existingDateInput).getTime();
  const d2 = new Date(newDateInput).getTime();
  if (isNaN(d1) || isNaN(d2)) return false;

  const diffMinutes = Math.abs(d1 - d2) / (1000 * 60);
  return diffMinutes < thresholdMinutes;
}

export function formatTimezoneDisplay(utcIsoString, timeZone = 'Asia/Karachi') {
  const d = new Date(utcIsoString);
  if (isNaN(d.getTime())) return '';
  return new Intl.DateTimeFormat('en-US', {
    timeZone,
    year: 'numeric',
    month: 'short',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    hour12: true
  }).format(d);
}

describe('Unit: Clinic Operating Hours Validation (08:00 - 20:00)', () => {
  it('should accept times squarely within operating hours (e.g. 10:30 AM, 02:15 PM, 07:45 PM)', () => {
    const d1 = new Date('2026-10-20T10:30:00');
    assert.equal(isWithinOperatingHours(d1).isValid, true);

    const d2 = new Date('2026-10-20T14:15:00');
    assert.equal(isWithinOperatingHours(d2).isValid, true);

    const d3 = new Date('2026-10-20T19:45:00');
    assert.equal(isWithinOperatingHours(d3).isValid, true);
  });

  it('should accept exactly 08:00 AM opening time', () => {
    const opening = new Date('2026-10-20T08:00:00');
    assert.equal(isWithinOperatingHours(opening).isValid, true);
  });

  it('should reject times prior to 08:00 AM (e.g. 07:59 AM, 04:00 AM, midnight)', () => {
    const early = new Date('2026-10-20T07:59:00');
    assert.equal(isWithinOperatingHours(early).isValid, false);

    const midnight = new Date('2026-10-20T00:30:00');
    assert.equal(isWithinOperatingHours(midnight).isValid, false);
  });

  it('should reject times at or after 08:00 PM (e.g. 20:00, 21:30)', () => {
    const closing = new Date('2026-10-20T20:00:00');
    assert.equal(isWithinOperatingHours(closing).isValid, false);

    const night = new Date('2026-10-20T21:30:00');
    assert.equal(isWithinOperatingHours(night).isValid, false);
  });
});

describe('Unit: Past Booking Prohibition & Grace Window', () => {
  it('should allow appointments scheduled in the future', () => {
    const future = new Date(Date.now() + 24 * 3600 * 1000);
    assert.equal(validateAppointmentNotPast(future).isValid, true);
  });

  it('should allow appointments scheduled within the 15-minute chairside intake grace window', () => {
    const fiveMinutesAgo = new Date(Date.now() - 5 * 60 * 1000);
    assert.equal(validateAppointmentNotPast(fiveMinutesAgo, 15).isValid, true);
  });

  it('should strictly reject appointments booked in the past beyond the grace window', () => {
    const yesterday = new Date(Date.now() - 24 * 3600 * 1000);
    const res = validateAppointmentNotPast(yesterday);
    assert.equal(res.isValid, false);
    assert.equal(res.reason, "Appointment date cannot be in the past.");
  });
});

describe('Unit: 30-Minute Appointment Collision Detector', () => {
  it('should detect collision for same-minute bookings', () => {
    const base = '2026-10-20T10:00:00Z';
    assert.equal(isSlotColliding(base, base), true);
  });

  it('should detect collision for bookings within 29 minutes of each other', () => {
    const t1 = '2026-10-20T10:00:00Z';
    const t2 = '2026-10-20T10:25:00Z';
    assert.equal(isSlotColliding(t1, t2, 30), true);
  });

  it('should allow bookings spaced 30 minutes or more apart', () => {
    const t1 = '2026-10-20T10:00:00Z';
    const t2 = '2026-10-20T10:30:00Z';
    assert.equal(isSlotColliding(t1, t2, 30), false);

    const t3 = '2026-10-20T11:15:00Z';
    assert.equal(isSlotColliding(t1, t3, 30), false);
  });
});

describe('Unit: Leap Year, Month-End Boundaries & Timezone Shifts', () => {
  it('should support leap day (Feb 29, 2028) correctly', () => {
    const leapDate = new Date('2028-02-29T10:00:00Z');
    assert.equal(leapDate.getUTCFullYear(), 2028);
    assert.equal(leapDate.getUTCMonth(), 1); // February is month 1
    assert.equal(leapDate.getUTCDate(), 29);
  });

  it('should accurately roll over month boundaries (Jan 31 -> Feb 1)', () => {
    const endOfMonth = new Date('2026-01-31T23:00:00Z');
    const nextDay = new Date(endOfMonth.getTime() + 2 * 3600 * 1000);
    assert.equal(nextDay.getUTCMonth(), 1);
    assert.equal(nextDay.getUTCDate(), 1);
  });

  it('should convert UTC times correctly to Pakistan Standard Time (PKT UTC+5)', () => {
    const utcString = '2026-10-20T05:00:00Z'; // 5 AM UTC = 10 AM PKT
    const pktFormatted = formatTimezoneDisplay(utcString, 'Asia/Karachi');
    assert.equal(pktFormatted.includes('10:00 AM'), true);
  });

  it('should convert UTC times correctly to New Zealand Daylight Time (NZDT UTC+13 in Oct)', () => {
    const utcString = '2026-10-20T01:00:00Z'; // 1 AM UTC = 2 PM NZDT (+13h in DST)
    const nzFormatted = formatTimezoneDisplay(utcString, 'Pacific/Auckland');
    assert.equal(nzFormatted.includes('02:00 PM'), true);
  });
});
