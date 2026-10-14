/**
 * ⚡ Non-Functional Tests: Performance, Burst Concurrency & Response-Time Assertions
 * Asserts sub-millisecond execution for critical validators and simulates concurrency bursts.
 */

import { describe, it } from 'node:test';
import assert from 'node:assert/strict';

import {
  normalizePakistaniPhone,
  formatCnic,
  validateLuhnCardNumber,
  isValidFdiToothNumber
} from '../../Dentistfrontend/src/validators/normalizationUtils.js';

import { calculateInvoiceTotals, computeBalance } from '../unit/billing_calculator.test.mjs';
import { isSlotColliding } from '../unit/date_time_utils.test.mjs';

describe('Non-Functional: Response-Time Assertions (< 5ms per operation)', () => {
  it('should normalize Pakistani phone numbers in < 1ms', () => {
    const start = performance.now();
    for (let i = 0; i < 100; i++) {
      normalizePakistaniPhone('03001234567');
    }
    const elapsed = performance.now() - start;
    const avgMs = elapsed / 100;
    assert.ok(avgMs < 1.0, `Average phone normalization time (${avgMs.toFixed(3)}ms) exceeded 1ms threshold.`);
  });

  it('should validate Luhn credit card checksum in < 0.5ms', () => {
    const start = performance.now();
    for (let i = 0; i < 100; i++) {
      validateLuhnCardNumber('4532015112830366');
    }
    const elapsed = performance.now() - start;
    const avgMs = elapsed / 100;
    assert.ok(avgMs < 0.5, `Average Luhn check time (${avgMs.toFixed(3)}ms) exceeded 0.5ms threshold.`);
  });

  it('should compute complex 10-item invoice totals with tax in < 1ms', () => {
    const items = Array.from({ length: 10 }, (_, i) => ({
      unitPrice: (i + 1) * 1500.00,
      discount: 100.00,
      quantity: 1
    }));

    const start = performance.now();
    for (let i = 0; i < 50; i++) {
      calculateInvoiceTotals(items, 500.00, 15);
    }
    const elapsed = performance.now() - start;
    const avgMs = elapsed / 50;
    assert.ok(avgMs < 1.0, `Average invoice calculation time (${avgMs.toFixed(3)}ms) exceeded 1ms threshold.`);
  });
});

describe('Non-Functional: High-Volume Concurrency Burst Simulation', () => {
  it('should handle a burst of 500 concurrent appointment collision checks in < 50ms', async () => {
    const baseDate = '2026-10-20T10:00:00Z';
    const testDates = Array.from({ length: 500 }, (_, i) =>
      new Date(new Date(baseDate).getTime() + (i * 60 * 1000)).toISOString()
    );

    const start = performance.now();
    // Simulate 500 parallel incoming appointment checks
    const results = await Promise.all(
      testDates.map(d => Promise.resolve(isSlotColliding(baseDate, d, 30)))
    );
    const elapsed = performance.now() - start;

    assert.equal(results.length, 500);
    // First 29 items collide, 30th onwards do not
    assert.equal(results[0], true);
    assert.equal(results[30], false);
    assert.ok(elapsed < 50.0, `Concurrency burst (${elapsed.toFixed(2)}ms) exceeded 50ms threshold.`);
  });
});
