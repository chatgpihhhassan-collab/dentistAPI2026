/**
 * 🎭 Playwright Configuration for Dentia E2E Test Suite
 * Configured for Desktop (1920x1080) and Mobile (iPhone 14 / 390x844) Viewports.
 */

import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './',
  timeout: 45000,
  expect: {
    timeout: 10000
  },
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: 1, // Deterministic sequential clinical execution
  reporter: [
    ['list'],
    ['html', { outputFolder: '../../playwright-report', open: 'never' }]
  ],
  use: {
    baseURL: process.env.E2E_BASE_URL || 'http://localhost:5173',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    actionTimeout: 10000,
    navigationTimeout: 15000
  },
  projects: [
    // 🖥️ Desktop Operatory Viewport
    {
      name: 'Desktop-Chrome-Operatory',
      use: {
        ...devices['Desktop Chrome'],
        viewport: { width: 1920, height: 1080 }
      }
    },
    // 📱 Mobile Viewport (Patient Portal & Responsive Layout)
    {
      name: 'Mobile-Patient-Portal',
      use: {
        ...devices['iPhone 14'],
        viewport: { width: 390, height: 844 },
        isMobile: true,
        hasTouch: true
      }
    }
  ],
  webServer: {
    command: 'npm run dev',
    port: 5173,
    reuseExistingServer: !process.env.CI,
    timeout: 30000
  }
});
