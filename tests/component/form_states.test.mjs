/**
 * 🖥️ Component & Form State Tests: UI States across Clinical & Patient Portal Forms
 * Verifies loading, empty, error, success, validation messages, and disabled button behaviors.
 */

import { describe, it } from 'node:test';
import assert from 'node:assert/strict';

import {
  normalizePakistaniPhone,
  validateCnic,
  validateLuhnCardNumber
} from '../../Dentistfrontend/src/validators/normalizationUtils.js';

// State Machine Models simulating React Component Form Hooks
class AuthFormStateModel {
  constructor() {
    this.username = '';
    this.password = '';
    this.isLoading = false;
    this.errorMessage = null;
    this.fieldErrors = {};
    this.isSuccess = false;
  }

  setUsername(val) {
    this.username = val;
    delete this.fieldErrors.username;
  }

  setPassword(val) {
    this.password = val;
    delete this.fieldErrors.password;
  }

  get isSubmitDisabled() {
    return this.isLoading || !this.username.trim() || !this.password;
  }

  async submit(authApiStub) {
    this.fieldErrors = {};
    this.errorMessage = null;

    if (!this.username.trim()) this.fieldErrors.username = "Username is required.";
    if (!this.password) this.fieldErrors.password = "Password is required.";

    if (Object.keys(this.fieldErrors).length > 0) return false;

    this.isLoading = true;
    try {
      const res = await authApiStub(this.username, this.password);
      if (res.ok) {
        this.isSuccess = true;
        this.isLoading = false;
        return true;
      } else {
        this.errorMessage = res.message || "Invalid credentials.";
        this.isLoading = false;
        return false;
      }
    } catch (err) {
      this.errorMessage = "Network error. Please try again.";
      this.isLoading = false;
      return false;
    }
  }
}

class NewPatientFormStateModel {
  constructor() {
    this.firstName = '';
    this.lastName = '';
    this.phone = '';
    this.dob = '';
    this.cnic = '';
    this.gender = 'male';
    this.isLoading = false;
    this.fieldErrors = {};
    this.serverError = null;
    this.isSuccess = false;
  }

  validate() {
    const errors = {};
    if (!this.firstName.trim() || this.firstName.trim().length < 2) {
      errors.firstName = "First name must be at least 2 characters.";
    }
    if (!this.lastName.trim() || this.lastName.trim().length < 2) {
      errors.lastName = "Last name must be at least 2 characters.";
    }
    const phoneCheck = normalizePakistaniPhone(this.phone);
    if (!phoneCheck.isValid) {
      errors.phone = phoneCheck.error || "Invalid Pakistani phone.";
    }
    if (!this.dob) {
      errors.dob = "Date of Birth is required.";
    }
    if (this.cnic) {
      const cnicCheck = validateCnic(this.cnic);
      if (!cnicCheck.isValid) errors.cnic = cnicCheck.error;
    }
    this.fieldErrors = errors;
    return Object.keys(errors).length === 0;
  }

  get isSubmitDisabled() {
    return this.isLoading || !this.firstName.trim() || !this.lastName.trim() || !this.phone.trim();
  }

  async submit(createApiStub) {
    this.serverError = null;
    if (!this.validate()) return false;

    this.isLoading = true;
    try {
      const res = await createApiStub(this);
      this.isLoading = false;
      if (res.ok) {
        this.isSuccess = true;
        return true;
      } else {
        this.serverError = res.message || "Failed to create patient.";
        return false;
      }
    } catch {
      this.isLoading = false;
      this.serverError = "Network failure.";
      return false;
    }
  }
}

class PaymentModalStateModel {
  constructor(invoiceBalance) {
    this.invoiceBalance = invoiceBalance;
    this.paymentMethod = 'Online_Card';
    this.amount = invoiceBalance;
    this.cardNumber = '';
    this.cardExpiry = '';
    this.cardCvc = '';
    this.isLoading = false;
    this.error = null;
    this.isSuccess = false;
  }

  setAmount(val) {
    this.amount = parseFloat(val) || 0;
    this.error = null;
  }

  validate() {
    if (this.amount <= 0) {
      this.error = "Payment amount must be greater than zero.";
      return false;
    }
    if (this.amount > this.invoiceBalance) {
      this.error = `Payment amount exceeds invoice balance of ${this.invoiceBalance}.`;
      return false;
    }
    if (this.paymentMethod === 'Online_Card') {
      if (!validateLuhnCardNumber(this.cardNumber)) {
        this.error = "Invalid credit card number.";
        return false;
      }
    }
    return true;
  }

  get isPayButtonDisabled() {
    return this.isLoading || this.amount <= 0 || (this.paymentMethod === 'Online_Card' && !this.cardNumber.trim());
  }
}

describe('Component State: Clinician Auth Form States', () => {
  it('should disable submit button when form is empty', () => {
    const form = new AuthFormStateModel();
    assert.equal(form.isSubmitDisabled, true);
  });

  it('should enable submit button once username and password are provided', () => {
    const form = new AuthFormStateModel();
    form.setUsername('dr.sarah');
    form.setPassword('SecurePassword123!');
    assert.equal(form.isSubmitDisabled, false);
  });

  it('should set loading state and disable submit button during active submission', async () => {
    const form = new AuthFormStateModel();
    form.setUsername('dr.sarah');
    form.setPassword('SecurePassword123!');

    // Stub that delays response
    let resolveApi;
    const delayStub = () => new Promise(r => { resolveApi = r; });

    const submitPromise = form.submit(delayStub);
    assert.equal(form.isLoading, true);
    assert.equal(form.isSubmitDisabled, true);

    resolveApi({ ok: true });
    await submitPromise;
    assert.equal(form.isLoading, false);
    assert.equal(form.isSuccess, true);
  });

  it('should transition to error state with validation message on failed login', async () => {
    const form = new AuthFormStateModel();
    form.setUsername('dr.sarah');
    form.setPassword('wrongpassword');

    const failStub = async () => ({ ok: false, message: "Invalid username or password." });
    const success = await form.submit(failStub);

    assert.equal(success, false);
    assert.equal(form.isSuccess, false);
    assert.equal(form.errorMessage, "Invalid username or password.");
  });
});

describe('Component State: New Patient Intake Form States', () => {
  it('should display field errors and block submission when required fields are missing', async () => {
    const form = new NewPatientFormStateModel();
    form.firstName = "A"; // Too short
    form.lastName = "";   // Missing
    form.phone = "12345"; // Invalid phone

    const ok = form.validate();
    assert.equal(ok, false);
    assert.ok(form.fieldErrors.firstName);
    assert.ok(form.fieldErrors.lastName);
    assert.ok(form.fieldErrors.phone);
    assert.ok(form.fieldErrors.dob);
  });

  it('should clear validation errors and submit successfully when all fields valid', async () => {
    const form = new NewPatientFormStateModel();
    form.firstName = "Kamran";
    form.lastName = "Akmal";
    form.phone = "03001234567";
    form.dob = "1990-04-15";

    const createStub = async () => ({ ok: true });
    const success = await form.submit(createStub);

    assert.equal(success, true);
    assert.equal(form.isSuccess, true);
    assert.equal(Object.keys(form.fieldErrors).length, 0);
  });
});

describe('Component State: Patient Billing Checkout Modal States', () => {
  it('should block payment and display error when user enters amount exceeding balance', () => {
    const modal = new PaymentModalStateModel(10000.00);
    modal.setAmount(15000.00); // 15000 > 10000

    const valid = modal.validate();
    assert.equal(valid, false);
    assert.equal(modal.error.includes("exceeds invoice balance"), true);
  });

  it('should block payment when card number fails Luhn checksum', () => {
    const modal = new PaymentModalStateModel(5000.00);
    modal.cardNumber = "4532015112830367"; // Bad check digit

    const valid = modal.validate();
    assert.equal(valid, false);
    assert.equal(modal.error.includes("Invalid credit card number"), true);
  });

  it('should allow valid payment with compliant card and amount', () => {
    const modal = new PaymentModalStateModel(5000.00);
    modal.setAmount(5000.00);
    modal.cardNumber = "4532015112830366"; // Valid Luhn

    const valid = modal.validate();
    assert.equal(valid, true);
    assert.equal(modal.error, null);
    assert.equal(modal.isPayButtonDisabled, false);
  });
});
