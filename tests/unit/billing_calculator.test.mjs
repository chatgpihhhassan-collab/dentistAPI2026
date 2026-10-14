/**
 * 🧪 Unit Tests: Dental Billing & Currency-Safe Calculations
 * Tests financial calculations, tax, discounts, negative balance prevention, and currency rounding.
 */

import { describe, it } from 'node:test';
import assert from 'node:assert/strict';

export function calculateInvoiceTotals(items = [], discountVal = 0, taxRate = 0) {
  // Sum item amounts using integer cents to prevent IEEE-754 floating point distortion
  let subtotalCents = 0;
  for (const item of items) {
    const itemPriceCents = Math.round(Number(item.unitPrice || 0) * 100);
    const itemDiscountCents = Math.round(Number(item.discount || 0) * 100);
    const qty = Number(item.quantity || 1);
    const itemNet = Math.max(0, (itemPriceCents - itemDiscountCents) * qty);
    subtotalCents += itemNet;
  }

  const globalDiscountCents = Math.round(Number(discountVal || 0) * 100);
  const netBeforeTaxCents = Math.max(0, subtotalCents - globalDiscountCents);
  const taxCents = Math.round(netBeforeTaxCents * (Number(taxRate || 0) / 100));
  const totalCents = netBeforeTaxCents + taxCents;

  return {
    subtotal: subtotalCents / 100,
    discount: globalDiscountCents / 100,
    tax: taxCents / 100,
    total: totalCents / 100
  };
}

export function computeBalance(totalAmount, paidAmount) {
  const totalCents = Math.round(Number(totalAmount || 0) * 100);
  const paidCents = Math.round(Number(paidAmount || 0) * 100);
  const balanceCents = totalCents - paidCents;

  return {
    balance: Math.max(0, balanceCents / 100),
    isOverpaid: balanceCents < 0,
    overpaymentAmount: balanceCents < 0 ? Math.abs(balanceCents / 100) : 0,
    isFullyPaid: balanceCents <= 0
  };
}

export function formatCurrencyAmount(amount, currency = 'PKR') {
  const num = Number(amount || 0);
  const curr = currency.toUpperCase();
  if (curr === 'PKR') {
    return `Rs ${num.toLocaleString('en-PK', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
  }
  if (curr === 'USD') return `$${num.toFixed(2)} USD`;
  if (curr === 'NZD') return `$${num.toFixed(2)} NZD`;
  if (curr === 'GBP') return `£${num.toFixed(2)}`;
  if (curr === 'EUR') return `€${num.toFixed(2)}`;
  return `${num.toFixed(2)} ${curr}`;
}

export function validateInvoiceSequenceNumber(invNo) {
  if (!invNo || typeof invNo !== 'string') return false;
  return /^INV-\d{4}-\d{5}$/.test(invNo.trim());
}

export function validateReceiptSequenceNumber(recNo) {
  if (!recNo || typeof recNo !== 'string') return false;
  return /^(REC|CSH)-\d{4}-\d{5}$/.test(recNo.trim());
}

describe('Unit: Currency-Safe Dental Invoicing Arithmetic', () => {
  it('should eliminate floating-point representation errors (0.1 + 0.2 = 0.30)', () => {
    const items = [
      { unitPrice: 0.10, quantity: 1 },
      { unitPrice: 0.20, quantity: 1 }
    ];
    const totals = calculateInvoiceTotals(items, 0, 0);
    assert.equal(totals.subtotal, 0.30);
    assert.equal(totals.total, 0.30);
  });

  it('should accurately compute multi-procedure invoice with discounts and 15% GST', () => {
    const items = [
      { unitPrice: 15000.00, discount: 1000.00, quantity: 1 }, // D3330 Molar RCT = 14,000
      { unitPrice: 8500.00, discount: 500.00, quantity: 2 }     // D2392 Composite = 16,000
    ];
    // Subtotal: 14000 + 16000 = 30000. Global discount: 2000 => 28000. 15% GST = 4200. Total = 32200.
    const totals = calculateInvoiceTotals(items, 2000.00, 15);
    assert.equal(totals.subtotal, 30000.00);
    assert.equal(totals.discount, 2000.00);
    assert.equal(totals.tax, 4200.00);
    assert.equal(totals.total, 32200.00);
  });

  it('should prevent invoice total from becoming negative even if discount exceeds subtotal', () => {
    const items = [{ unitPrice: 2000.00, quantity: 1 }];
    const totals = calculateInvoiceTotals(items, 5000.00, 10);
    assert.equal(totals.total, 0);
    assert.equal(totals.tax, 0);
  });
});

describe('Unit: Balance Computation & Overpayment Invariant Guard', () => {
  it('should compute remaining balance on partial payments correctly', () => {
    const { balance, isFullyPaid, isOverpaid } = computeBalance(10000.00, 4500.00);
    assert.equal(balance, 5500.00);
    assert.equal(isFullyPaid, false);
    assert.equal(isOverpaid, false);
  });

  it('should mark invoice as fully paid when payment equals total', () => {
    const { balance, isFullyPaid } = computeBalance(7500.00, 7500.00);
    assert.equal(balance, 0.00);
    assert.equal(isFullyPaid, true);
  });

  it('should detect overpayment without allowing balance to become negative', () => {
    const { balance, isOverpaid, overpaymentAmount } = computeBalance(5000.00, 6000.00);
    assert.equal(balance, 0.00); // Balance invariant: never negative
    assert.equal(isOverpaid, true);
    assert.equal(overpaymentAmount, 1000.00);
  });

  it('should handle zero and null payments gracefully', () => {
    const { balance, isFullyPaid } = computeBalance(8000.00, 0);
    assert.equal(balance, 8000.00);
    assert.equal(isFullyPaid, false);
  });
});

describe('Unit: Multi-Currency Formatter Validation', () => {
  it('should format Pakistani Rupee (PKR) with Rs prefix and comma thousands', () => {
    const formatted = formatCurrencyAmount(125000.50, 'PKR');
    assert.equal(formatted.includes('Rs'), true);
    assert.equal(formatted.includes('125,000.50'), true);
  });

  it('should format New Zealand Dollars (NZD)', () => {
    assert.equal(formatCurrencyAmount(345.20, 'NZD'), '$345.20 NZD');
  });

  it('should format US Dollars (USD)', () => {
    assert.equal(formatCurrencyAmount(95.00, 'USD'), '$95.00 USD');
  });

  it('should format British Pounds (GBP)', () => {
    assert.equal(formatCurrencyAmount(220.75, 'GBP'), '£220.75');
  });

  it('should format Euros (EUR)', () => {
    assert.equal(formatCurrencyAmount(180.30, 'EUR'), '€180.30');
  });
});

describe('Unit: Sequential Invoice and Receipt Number Integrity', () => {
  it('should validate valid invoice format (INV-YYYY-XXXXX)', () => {
    assert.equal(validateInvoiceSequenceNumber('INV-2026-00124'), true);
    assert.equal(validateInvoiceSequenceNumber('INV-2025-99999'), true);
  });

  it('should reject malformed invoice numbers', () => {
    assert.equal(validateInvoiceSequenceNumber('INV-26-124'), false);
    assert.equal(validateInvoiceSequenceNumber('INVOICE-001'), false);
    assert.equal(validateInvoiceSequenceNumber('INV-2026-ABCD'), false);
    assert.equal(validateInvoiceSequenceNumber(''), false);
  });

  it('should validate valid receipt and cash voucher formats', () => {
    assert.equal(validateReceiptSequenceNumber('REC-2026-00452'), true);
    assert.equal(validateReceiptSequenceNumber('CSH-2026-00891'), true);
  });

  it('should reject malformed receipt numbers', () => {
    assert.equal(validateReceiptSequenceNumber('REC-999'), false);
    assert.equal(validateReceiptSequenceNumber('CSH_2026_01'), false);
  });
});
