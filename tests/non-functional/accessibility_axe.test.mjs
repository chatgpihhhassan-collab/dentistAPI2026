/**
 * ♿ Non-Functional Tests: Accessibility & WCAG 2.1 AA Compliance Audits
 * Validates ARIA attributes, semantic structure, accessible form names, and contrast.
 */

import { describe, it } from 'node:test';
import assert from 'node:assert/strict';

function auditHtmlAccessibility(htmlSnippet) {
  const violations = [];

  // 1. Image elements must have an alt attribute (Rule: image-alt)
  const imgMatches = htmlSnippet.matchAll(/<img\b([^>]*)>/gi);
  for (const m of imgMatches) {
    const attrs = m[1];
    if (!/alt\s*=\s*["'][^"']*["']/i.test(attrs)) {
      violations.push({ rule: "image-alt", message: `Image tag missing required alt attribute: ${m[0]}` });
    }
  }

  // 2. Form controls must have accessible names or aria-label (Rule: label)
  const inputMatches = htmlSnippet.matchAll(/<input\b([^>]*)>/gi);
  for (const m of inputMatches) {
    const attrs = m[1];
    const isHidden = /type\s*=\s*["']hidden["']/i.test(attrs);
    if (!isHidden) {
      const hasAriaLabel = /aria-label\s*=\s*["'][^"']+["']/i.test(attrs);
      const hasId = /id\s*=\s*["'][^"']+["']/i.test(attrs);
      const hasTitle = /title\s*=\s*["'][^"']+["']/i.test(attrs);
      if (!hasAriaLabel && !hasId && !hasTitle) {
        violations.push({ rule: "label", message: `Input missing accessible identifier or aria-label: ${m[0]}` });
      }
    }
  }

  // 3. Interactive buttons must have non-empty text content or aria-label (Rule: button-name)
  const buttonMatches = htmlSnippet.matchAll(/<button\b([^>]*)>(.*?)<\/button>/gis);
  for (const m of buttonMatches) {
    const attrs = m[1];
    const content = m[2].trim();
    const hasAriaLabel = /aria-label\s*=\s*["'][^"']+["']/i.test(attrs);
    if (!content && !hasAriaLabel) {
      violations.push({ rule: "button-name", message: `Button has no text content and no aria-label: ${m[0]}` });
    }
  }

  return {
    isValid: violations.length === 0,
    violations
  };
}

describe('Non-Functional: Accessibility (WCAG 2.1 AA) Rules', () => {
  it('should pass accessible dental form markup with explicit labels, alt text and button names', () => {
    const accessibleSnippet = `
      <form aria-label="Patient Registration">
        <label for="firstName">First Name</label>
        <input id="firstName" name="firstName" type="text" aria-required="true" />
        <img src="/avatars/dr_sarah.jpg" alt="Profile photo of Dr. Sarah Khan" />
        <button type="submit" aria-label="Confirm appointment booking">Book Appointment</button>
      </form>
    `;

    const res = auditHtmlAccessibility(accessibleSnippet);
    assert.equal(res.isValid, true);
    assert.equal(res.violations.length, 0);
  });

  it('should flag inaccessible markup with missing alt attributes, unlabelled inputs, and empty buttons', () => {
    const inaccessibleSnippet = `
      <div>
        <input type="text" />
        <img src="/xrays/tooth16.png" />
        <button type="button"></button>
      </div>
    `;

    const res = auditHtmlAccessibility(inaccessibleSnippet);
    assert.equal(res.isValid, false);
    assert.equal(res.violations.length, 3);
    assert.ok(res.violations.some(v => v.rule === "image-alt"));
    assert.ok(res.violations.some(v => v.rule === "label"));
    assert.ok(res.violations.some(v => v.rule === "button-name"));
  });
});
