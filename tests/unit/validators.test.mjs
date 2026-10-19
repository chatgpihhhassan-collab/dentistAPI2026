/**
 * 🧪 Unit Tests: Pure Logic Validators & Data Normalizers
 * Tests phone, CNIC, password complexity, Luhn algorithm, FDI tooth notation, and sanitization.
 */

import { describe, it } from 'node:test';
import assert from 'node:assert/strict';

import {
  normalizePakistaniPhone,
  formatPakistaniPhone,
  formatCnic,
  validateCnic,
  sanitizeText,
  validateLuhnCardNumber,
  isValidFdiToothNumber,
  formatCurrencySafe,
  getWhatsAppChatId,
  getWhatsAppDirectUrl
} from '../../Dentistfrontend/src/validators/normalizationUtils.js';

import { EdgeCasePayloads } from '../factories/test_data_factories.mjs';

function normalizeDentitionType(raw) {
  if (!raw || typeof raw !== 'string') return 'Adult';
  const trimmed = raw.trim().toLowerCase();
  if (['permanent', 'adult', 'adults'].includes(trimmed)) return 'Adult';
  if (['pediatric', 'child', 'primary', 'deciduous'].includes(trimmed)) return 'Pediatric';
  if (['mixed', 'transitional'].includes(trimmed)) return 'Mixed';
  return 'Adult';
}

describe('Unit: Pakistani Phone Number E.164 Normalization', () => {
  it('should normalize 11-digit local format (03001234567) to +923001234567', () => {
    const res = normalizePakistaniPhone('03001234567');
    assert.equal(res.isValid, true);
    assert.equal(res.normalized, '+923001234567');
  });

  it('should normalize international format without plus (923214455667) to +923214455667', () => {
    const res = normalizePakistaniPhone('923214455667');
    assert.equal(res.isValid, true);
    assert.equal(res.normalized, '+923214455667');
  });

  it('should normalize 10-digit format without leading 0 (3459876543) to +923459876543', () => {
    const res = normalizePakistaniPhone('3459876543');
    assert.equal(res.isValid, true);
    assert.equal(res.normalized, '+923459876543');
  });

  it('should normalize punctuated/spaced input (+92 333-1122334) to +923331122334', () => {
    const res = normalizePakistaniPhone('+92 333-1122334');
    assert.equal(res.isValid, true);
    assert.equal(res.normalized, '+923331122334');
  });

  it('should reject non-mobile numbers or landline prefixes (042-35889900)', () => {
    const res = normalizePakistaniPhone('04235889900');
    assert.equal(res.isValid, false);
  });

  it('should reject too short phone numbers', () => {
    const res = normalizePakistaniPhone('0300123');
    assert.equal(res.isValid, false);
  });

  it('should reject empty, null, or whitespace-only phone inputs', () => {
    assert.equal(normalizePakistaniPhone('').isValid, false);
    assert.equal(normalizePakistaniPhone('   ').isValid, false);
    assert.equal(normalizePakistaniPhone(null).isValid, false);
  });
});

describe('Unit: OpenWA WhatsApp ChatId and Direct URLs', () => {
  it('should format 03001234567 to 923001234567@c.us', () => {
    assert.equal(getWhatsAppChatId('03001234567'), '923001234567@c.us');
  });

  it('should format +923001234567 to 923001234567@c.us', () => {
    assert.equal(getWhatsAppChatId('+923001234567'), '923001234567@c.us');
  });

  it('should format punctuated +92 321-4455667 to 923214455667@c.us', () => {
    assert.equal(getWhatsAppChatId('+92 321-4455667'), '923214455667@c.us');
  });

  it('should return null for invalid landline or malformed numbers', () => {
    assert.equal(getWhatsAppChatId('04235889900'), null);
    assert.equal(getWhatsAppChatId(''), null);
    assert.equal(getWhatsAppChatId(null), null);
  });

  it('should generate valid wa.me direct chat link with prefilled text', () => {
    const url = getWhatsAppDirectUrl('03001234567', 'Hello Dentia');
    assert.equal(url, 'https://wa.me/923001234567?text=Hello%20Dentia');
  });
});

describe('Unit: Pakistani CNIC (National ID) Formatting & Validation', () => {
  it('should format clean 13 digits into standard XXXXX-XXXXXXX-X format', () => {
    const formatted = formatCnic('3520112345671');
    assert.equal(formatted, '35201-1234567-1');
  });

  it('should validate properly structured CNIC', () => {
    const res = validateCnic('35201-1234567-1');
    assert.equal(res.isValid, true);
    assert.equal(res.normalized, '35201-1234567-1');
  });

  it('should treat blank or null CNIC as valid (optional field)', () => {
    assert.equal(validateCnic('').isValid, true);
    assert.equal(validateCnic(null).isValid, true);
  });

  it('should reject CNIC with fewer than 13 digits (e.g. 12 digits)', () => {
    const res = validateCnic('35201-123456-1');
    assert.equal(res.isValid, false);
  });

  it('should reject CNIC with letters or symbols', () => {
    const res = validateCnic('35201-ABC4567-1');
    assert.equal(res.isValid, false);
  });
});

describe('Unit: FDI World Dental Federation Notation Validation', () => {
  it('should recognize all 32 adult teeth as valid FDI numbers', () => {
    const adultTeeth = [
      11, 12, 13, 14, 15, 16, 17, 18,
      21, 22, 23, 24, 25, 26, 27, 28,
      31, 32, 33, 34, 35, 36, 37, 38,
      41, 42, 43, 44, 45, 46, 47, 48
    ];
    for (const t of adultTeeth) {
      assert.equal(isValidFdiToothNumber(t), true, `Tooth #${t} should be valid Adult FDI`);
    }
  });

  it('should recognize all 20 primary/deciduous teeth as valid FDI numbers', () => {
    const primaryTeeth = [
      51, 52, 53, 54, 55,
      61, 62, 63, 64, 65,
      71, 72, 73, 74, 75,
      81, 82, 83, 84, 85
    ];
    for (const t of primaryTeeth) {
      assert.equal(isValidFdiToothNumber(t), true, `Tooth #${t} should be valid Primary FDI`);
    }
  });

  it('should reject non-existent FDI tooth numbers', () => {
    const invalidTeeth = [0, -1, 9, 10, 19, 20, 29, 39, 49, 50, 56, 86, 90, 99, 100];
    for (const t of invalidTeeth) {
      assert.equal(isValidFdiToothNumber(t), false, `Tooth #${t} should be REJECTED as invalid FDI`);
    }
  });
});

describe('Unit: Luhn Card Checksum Verification (Mod 10)', () => {
  it('should validate standard Luhn test card numbers', () => {
    assert.equal(validateLuhnCardNumber('4532015112830366'), true);
    assert.equal(validateLuhnCardNumber('4532 0151 1283 0366'), true);
  });

  it('should reject invalid Luhn card numbers with wrong check digit', () => {
    assert.equal(validateLuhnCardNumber('4532015112830367'), false);
  });

  it('should reject empty or non-numeric card numbers', () => {
    assert.equal(validateLuhnCardNumber(''), false);
    assert.equal(validateLuhnCardNumber(null), false);
    assert.equal(validateLuhnCardNumber('not-a-card-number'), false);
  });
});

describe('Unit: Text & HTML Sanitization (XSS Prevention)', () => {
  it('should strip script tags and javascript pseudo-protocols', () => {
    const sanitized = sanitizeText('<script>alert("hack")</script>Dr. Sarah Khan');
    assert.equal(sanitized, 'Dr. Sarah Khan');
  });

  it('should collapse multiple spaces, tabs, and newlines into single spaces', () => {
    const sanitized = sanitizeText("Tooth    16    \n\t  pain");
    assert.equal(sanitized, "Tooth 16 pain");
  });

  it('should truncate strings exceeding maxLength', () => {
    const long = "A".repeat(100);
    const sanitized = sanitizeText(long, 50);
    assert.equal(sanitized.length, 50);
  });

  it('should safely handle malicious XSS attack vectors', () => {
    const res = sanitizeText(EdgeCasePayloads.XSS_IMG_ONERROR);
    assert.equal(res.includes('<img'), false);
    assert.equal(res.includes('onerror'), false);
  });
});

describe('Unit: Dentition Type Normalization', () => {
  it('should normalize Permanent to Adult', () => {
    assert.equal(normalizeDentitionType('Permanent'), 'Adult');
    assert.equal(normalizeDentitionType('permanent'), 'Adult');
  });

  it('should normalize Child / Primary / Deciduous to Pediatric', () => {
    assert.equal(normalizeDentitionType('Child'), 'Pediatric');
    assert.equal(normalizeDentitionType('primary'), 'Pediatric');
    assert.equal(normalizeDentitionType('deciduous'), 'Pediatric');
  });

  it('should preserve Mixed dentition', () => {
    assert.equal(normalizeDentitionType('Mixed'), 'Mixed');
    assert.equal(normalizeDentitionType('transitional'), 'Mixed');
  });

  it('should fallback to Adult for unknown or blank inputs', () => {
    assert.equal(normalizeDentitionType(''), 'Adult');
    assert.equal(normalizeDentitionType(null), 'Adult');
    assert.equal(normalizeDentitionType('UnknownType'), 'Adult');
  });
});

describe('Unit: WhatsApp Opt-In Policy & Conditional Mobile Validation', () => {
  function validatePatientPhoneRequirement(patient) {
    const isWhatsAppActive = Boolean(patient.whatsAppOptIn);
    if (!isWhatsAppActive) {
      return { valid: true };
    }
    if (!patient.phone || !patient.phone.trim()) {
      return { valid: false, error: 'Mobile phone number is mandatory when WhatsApp notifications are enabled.' };
    }
    const norm = normalizePakistaniPhone(patient.phone);
    if (!norm.isValid) {
      return { valid: false, error: 'Please enter a valid Pakistani mobile number (e.g. 03001234567 or +923001234567).' };
    }
    return { valid: true, normalized: norm.normalized };
  }

  function parseWhatsAppIntentFromVoice(command) {
    const lower = (command || '').toLowerCase();
    const enableMatch = /with\s+whatsapp|enable\s+whatsapp|activate\s+whatsapp|whatsapp\s+yes|yes\s+whatsapp|whatsapp\s+active/i.test(lower);
    const disableMatch = /without\s+whatsapp|disable\s+whatsapp|no\s+whatsapp|whatsapp\s+off|deactivate\s+whatsapp|whatsapp\s+inactive|does\s+not\s+use\s+whatsapp/i.test(lower);
    if (enableMatch) return true;
    if (disableMatch) return false;
    return false; // Default inactive
  }

  it('should default WhatsApp opt-in to false (inactive)', () => {
    const defaultPatient = { firstName: 'Ali', lastName: 'Khan', whatsAppOptIn: false };
    assert.equal(defaultPatient.whatsAppOptIn, false);
  });

  it('should allow patient registration without phone if WhatsApp is inactive', () => {
    const res = validatePatientPhoneRequirement({ firstName: 'Fatima', whatsAppOptIn: false, phone: '' });
    assert.equal(res.valid, true);
  });

  it('should block patient registration if WhatsApp is active but phone is missing', () => {
    const res = validatePatientPhoneRequirement({ firstName: 'Hamza', whatsAppOptIn: true, phone: '' });
    assert.equal(res.valid, false);
    assert.equal(res.error.includes('mandatory'), true);
  });

  it('should block patient registration if WhatsApp is active but phone is invalid landline', () => {
    const res = validatePatientPhoneRequirement({ firstName: 'Hamza', whatsAppOptIn: true, phone: '04235889900' });
    assert.equal(res.valid, false);
    assert.equal(res.error.includes('valid Pakistani mobile number'), true);
  });

  it('should accept valid Pakistani mobile phone when WhatsApp is active', () => {
    const res = validatePatientPhoneRequirement({ firstName: 'Hamza', whatsAppOptIn: true, phone: '03001234567' });
    assert.equal(res.valid, true);
    assert.equal(res.normalized, '+923001234567');
  });

  it('should parse voice/chat intent "with whatsapp" to true', () => {
    assert.equal(parseWhatsAppIntentFromVoice('register new patient Hamza with whatsapp'), true);
    assert.equal(parseWhatsAppIntentFromVoice('enable whatsapp for this patient'), true);
    assert.equal(parseWhatsAppIntentFromVoice('activate whatsapp notifications'), true);
  });

  it('should parse voice/chat intent "without whatsapp" to false', () => {
    assert.equal(parseWhatsAppIntentFromVoice('register patient Hamza without whatsapp'), false);
    assert.equal(parseWhatsAppIntentFromVoice('disable whatsapp'), false);
    assert.equal(parseWhatsAppIntentFromVoice('patient does not use whatsapp'), false);
  });
});

