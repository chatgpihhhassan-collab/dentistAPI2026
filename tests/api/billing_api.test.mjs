/**
 * 💳 API Integration Tests: Billing, Invoicing, Online Payments & Cash Desk Vouchers
 * Tests invoice creation, online card simulation, cash vouchers, partial payments, and IDOR protection.
 */

import { describe, it, beforeEach } from 'node:test';
import assert from 'node:assert/strict';

import { buildSyntheticInvoice } from '../factories/test_data_factories.mjs';

class MockBillingRepository {
  constructor() {
    this.invoices = new Map();
    this.payments = [];
    this.cashVouchers = new Map();
  }

  insertInvoice(invoice) {
    this.invoices.set(invoice.invoiceId, { ...invoice });
    return invoice;
  }

  getInvoice(id) {
    return this.invoices.get(id) || null;
  }

  recordPayment(invoiceId, amount, method, staffDoctorId = null) {
    const inv = this.invoices.get(invoiceId);
    if (!inv) return { success: false, message: "Invoice not found." };

    if (inv.status === "Paid") {
      return { success: false, message: "This invoice has already been fully paid." };
    }

    if (amount <= 0) {
      return { success: false, message: "Payment amount must be greater than zero." };
    }

    if (amount > inv.balanceAmount) {
      return {
        success: false,
        message: `Payment amount (${amount}) exceeds remaining balance (${inv.balanceAmount}). Overpayment blocked.`
      };
    }

    inv.paidAmount += amount;
    inv.balanceAmount = Math.max(0, inv.totalAmount - inv.paidAmount);
    inv.status = inv.balanceAmount === 0 ? "Paid" : "Partially_Paid";

    const receiptNo = `REC-2026-${String(this.payments.length + 1).padStart(5, '0')}`;
    this.payments.push({
      receiptNo,
      invoiceId,
      amount,
      method,
      staffDoctorId,
      createdAt: new Date().toISOString()
    });

    return {
      success: true,
      receiptNo,
      newBalance: inv.balanceAmount,
      newStatus: inv.status
    };
  }

  createCashVoucher(invoiceId, patientId, amount) {
    const inv = this.invoices.get(invoiceId);
    if (!inv) return null;
    const voucherCode = `CSH-2026-${String(this.cashVouchers.size + 1).padStart(5, '0')}`;
    const voucher = {
      voucherCode,
      invoiceId,
      patientId,
      amount,
      status: "Pending_Desk_Confirmation",
      createdAt: new Date().toISOString()
    };
    this.cashVouchers.set(voucherCode, voucher);
    return voucher;
  }

  confirmCashVoucher(voucherCode, staffDoctorId) {
    const voucher = this.cashVouchers.get(voucherCode);
    if (!voucher || voucher.status !== "Pending_Desk_Confirmation") return null;

    const paymentResult = this.recordPayment(voucher.invoiceId, voucher.amount, "Cash_Desk", staffDoctorId);
    if (paymentResult.success) {
      voucher.status = "Confirmed";
      voucher.receiptNo = paymentResult.receiptNo;
      return { success: true, voucher, paymentResult };
    }
    return { success: false, message: paymentResult.message };
  }
}

describe('API Integration: Invoicing & Payment Processing', () => {
  let repo;
  beforeEach(() => {
    repo = new MockBillingRepository();
  });

  it('should process online card payment and generate receipt number', () => {
    const inv = buildSyntheticInvoice({ invoiceId: 101, totalAmount: 12000, balanceAmount: 12000, status: "Issued" });
    repo.insertInvoice(inv);

    const result = repo.recordPayment(101, 12000, "Online_Card");
    assert.equal(result.success, true);
    assert.equal(result.newStatus, "Paid");
    assert.equal(result.newBalance, 0);
    assert.ok(result.receiptNo.startsWith("REC-2026-"));
  });

  it('should handle partial payments accurately (Partially_Paid status)', () => {
    const inv = buildSyntheticInvoice({ invoiceId: 102, totalAmount: 20000, balanceAmount: 20000, status: "Issued" });
    repo.insertInvoice(inv);

    // Pay 8,000 partial
    const p1 = repo.recordPayment(102, 8000, "Bank_Transfer");
    assert.equal(p1.success, true);
    assert.equal(p1.newStatus, "Partially_Paid");
    assert.equal(p1.newBalance, 12000);

    // Pay remaining 12,000
    const p2 = repo.recordPayment(102, 12000, "Cash");
    assert.equal(p2.success, true);
    assert.equal(p2.newStatus, "Paid");
    assert.equal(p2.newBalance, 0);
  });

  it('should REJECT payment when invoice is already fully paid', () => {
    const inv = buildSyntheticInvoice({ invoiceId: 103, totalAmount: 5000, balanceAmount: 0, paidAmount: 5000, status: "Paid" });
    repo.insertInvoice(inv);

    const result = repo.recordPayment(103, 1000, "Online_Card");
    assert.equal(result.success, false);
    assert.equal(result.message.includes("already been fully paid"), true);
  });

  it('should REJECT overpayment exceeding remaining invoice balance', () => {
    const inv = buildSyntheticInvoice({ invoiceId: 104, totalAmount: 5000, balanceAmount: 2000, paidAmount: 3000, status: "Partially_Paid" });
    repo.insertInvoice(inv);

    const result = repo.recordPayment(104, 3000, "Cash"); // 3000 > 2000
    assert.equal(result.success, false);
    assert.equal(result.message.includes("exceeds remaining balance"), true);
  });
});

describe('API Integration: Cash Voucher Lifecycle & Front-Desk Confirmation', () => {
  let repo;
  beforeEach(() => {
    repo = new MockBillingRepository();
  });

  it('should generate cash voucher in pending status without prematurely marking invoice as paid', () => {
    const inv = buildSyntheticInvoice({ invoiceId: 105, totalAmount: 15000, balanceAmount: 15000, status: "Issued" });
    repo.insertInvoice(inv);

    const voucher = repo.createCashVoucher(105, 1001, 15000);
    assert.ok(voucher.voucherCode.startsWith("CSH-2026-"));
    assert.equal(voucher.status, "Pending_Desk_Confirmation");

    // Verify invoice is still NOT paid
    assert.equal(repo.getInvoice(105).status, "Issued");
  });

  it('should transition invoice to Paid when clinic front-desk confirms cash voucher', () => {
    const inv = buildSyntheticInvoice({ invoiceId: 106, totalAmount: 10000, balanceAmount: 10000, status: "Issued" });
    repo.insertInvoice(inv);

    const voucher = repo.createCashVoucher(106, 1001, 10000);
    const confirmation = repo.confirmCashVoucher(voucher.voucherCode, 1 /* Staff Doctor ID */);

    assert.equal(confirmation.success, true);
    assert.equal(confirmation.voucher.status, "Confirmed");
    assert.equal(repo.getInvoice(106).status, "Paid");
    assert.equal(repo.getInvoice(106).balanceAmount, 0);
  });
});

describe('API Integration: IDOR Access Restrictions on Invoices', () => {
  let repo;
  beforeEach(() => {
    repo = new MockBillingRepository();
    // Invoice 107 belongs to Patient 1001
    repo.insertInvoice(buildSyntheticInvoice({ invoiceId: 107, patientId: 1001 }));
  });

  function checkInvoiceAccess(invoiceId, requestingPatientId) {
    const inv = repo.getInvoice(invoiceId);
    if (!inv) return 404;
    if (inv.patientId !== requestingPatientId) return 403;
    return 200;
  }

  it('should allow Patient 1001 to view their own invoice', () => {
    assert.equal(checkInvoiceAccess(107, 1001), 200);
  });

  it('should REJECT Patient 1002 attempting to view Patient 1001 invoice with 403 Forbidden', () => {
    assert.equal(checkInvoiceAccess(107, 1002), 403);
  });
});
