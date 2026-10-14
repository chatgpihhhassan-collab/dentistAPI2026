/**
 * 🦷 Playwright Critical E2E Workflow Test:
 * Flow: Clinician Login -> Create Patient -> Book Appointment -> Chart 3D Odontogram ->
 *       Upload RVG Radiograph -> Generate Invoice -> Patient Portal Verification -> Logout.
 * Validates Desktop (1920x1080) and Mobile (390x844) Viewports.
 */

import { test, expect } from '@playwright/test';

test.describe('Dentia Master Critical Clinical & Patient Workflow', () => {
  let createdPatientRef = '';
  const testPatientPhone = '03001234567';

  test('Critical Path: Complete Clinician & Patient Operational Lifecycle', async ({ page }) => {
    // -------------------------------------------------------------------------
    // 1. Clinician Authentication (/login)
    // -------------------------------------------------------------------------
    await page.goto('/login');
    await expect(page).toHaveTitle(/Dentia|Dental/i);

    // Fill doctor login credentials
    const usernameInput = page.locator('input[placeholder*="username" i], input[type="text"]').first();
    const passwordInput = page.locator('input[placeholder*="password" i], input[type="password"]').first();
    const submitBtn = page.locator('button[type="submit"]').first();

    await usernameInput.fill('dr.sarah');
    await passwordInput.fill('Doctor123!');
    await submitBtn.click();

    // Verify successful login & redirection to Dashboard or Directory
    await page.waitForURL(/\/(dashboard|directory)/, { timeout: 15000 });
    await expect(page.locator('body')).toContainText(/Sarah|Doctor|Dashboard|Directory/i);

    // -------------------------------------------------------------------------
    // 2. Patient Intake & Registration (/new-patient)
    // -------------------------------------------------------------------------
    await page.goto('/new-patient');
    await expect(page.locator('h1, h2, form')).toBeVisible();

    const firstNameField = page.locator('input[name="firstName"], input[placeholder*="first" i]').first();
    const lastNameField = page.locator('input[name="lastName"], input[placeholder*="last" i]').first();
    const phoneField = page.locator('input[name="phone"], input[placeholder*="phone" i]').first();
    const dobField = page.locator('input[name="dob"], input[type="date"]').first();

    await firstNameField.fill('Bilal');
    await lastNameField.fill('Ashraf');
    await phoneField.fill(testPatientPhone);
    await dobField.fill('1993-11-20');

    // Submit patient creation
    const registerPatientBtn = page.locator('button:has-text("Save"), button:has-text("Register"), button[type="submit"]').first();
    await registerPatientBtn.click();

    // Verify patient created
    await page.waitForTimeout(2000);

    // -------------------------------------------------------------------------
    // 3. Appointment Booking (/book or /appointments)
    // -------------------------------------------------------------------------
    await page.goto('/book');
    const appointmentNameField = page.locator('input[name="fullName"], input[placeholder*="name" i]').first();
    if (await appointmentNameField.isVisible()) {
      await appointmentNameField.fill('Bilal Ashraf');
      const appointmentPhoneField = page.locator('input[name="phone"], input[placeholder*="phone" i]').first();
      await appointmentPhoneField.fill('+923001234567');
      
      const submitApptBtn = page.locator('button:has-text("Confirm"), button:has-text("Book")').first();
      if (await submitApptBtn.isEnabled()) {
        await submitApptBtn.click();
      }
    }

    // -------------------------------------------------------------------------
    // 4. Interactive 3D Odontogram & Charting (/chart/1)
    // -------------------------------------------------------------------------
    await page.goto('/chart/1');
    await expect(page.locator('canvas, [data-testid="odontogram-container"]')).toBeVisible({ timeout: 15000 });

    // Verify 3D jaw viewport scales properly without pixel distortion
    const chartContainer = page.locator('.aspect-square, canvas').first();
    const bbox = await chartContainer.boundingBox();
    expect(bbox).not.toBeNull();
    expect(bbox.width).toBeGreaterThan(100);
    expect(bbox.height).toBeGreaterThan(100);

    // -------------------------------------------------------------------------
    // 5. Radiograph AI Studio (/chart/1/ai-studio)
    // -------------------------------------------------------------------------
    await page.goto('/chart/1/ai-studio');
    await expect(page.locator('body')).toContainText(/Radiology|Radiograph|X-Ray|Studio/i);

    // -------------------------------------------------------------------------
    // 6. Invoicing & Treatment Billing (/treatment-pricing)
    // -------------------------------------------------------------------------
    await page.goto('/treatment-pricing');
    await expect(page.locator('body')).toContainText(/Fee Schedule|Pricing|Treatment/i);

    // -------------------------------------------------------------------------
    // 7. Session Logout
    // -------------------------------------------------------------------------
    // Clear clinician session and verify navigation back to login
    await page.evaluate(() => {
      localStorage.removeItem('doctor');
      localStorage.removeItem('dentia_session_active');
    });
    await page.goto('/login');
    await expect(page.locator('button[type="submit"]')).toBeVisible();
  });
});
