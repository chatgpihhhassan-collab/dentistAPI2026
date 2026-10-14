/**
 * 🚀 Dentia Master QA Test Suite Runner
 * Runs the complete testing suite across all layers:
 * 1. Unit Tests (Validators, Billing Calculator, Date/Time Utilities)
 * 2. API Integration Tests (Auth, Patients, Appointments, Billing, RVG Uploads, Mocked AI)
 * 3. Component UI Form State Tests
 * 4. Non-Functional Tests (Performance, Concurrency, Accessibility)
 * 5. Security & Bug Regression Tests
 * 6. AI Prompt Engineering & Safety Evaluation
 */

import { spawn } from 'child_process';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const ROOT_DIR = path.resolve(__dirname, '..');

const colors = {
  reset: '\x1b[0m',
  bold: '\x1b[1m',
  green: '\x1b[32m',
  red: '\x1b[31m',
  cyan: '\x1b[36m',
  yellow: '\x1b[33m',
  magenta: '\x1b[35m',
  gray: '\x1b[90m'
};

function runCommand(command, args, cwd = ROOT_DIR) {
  return new Promise((resolve) => {
    console.log(`\n${colors.cyan}> Running: ${command} ${args.join(' ')}${colors.reset}`);
    const proc = spawn(command, args, { cwd, stdio: 'inherit', shell: true });
    proc.on('close', (code) => {
      resolve(code === 0);
    });
  });
}

async function main() {
  console.log(`\n${colors.bold}${colors.magenta}======================================================================${colors.reset}`);
  console.log(`${colors.bold}${colors.magenta}   🦷 DENTIA PRACTICE MANAGEMENT PLATFORM — COMPREHENSIVE QA RUNNER  ${colors.reset}`);
  console.log(`${colors.bold}${colors.magenta}======================================================================${colors.reset}`);

  const startTime = Date.now();
  const results = [];

  // Phase 1: Unit, API, Component, Non-Functional, and Regression Tests
  console.log(`\n${colors.bold}[PHASE 1] Running Unit, API, Component, Non-Functional & Regression Tests...${colors.reset}`);
  const phase1Pass = await runCommand('node', [
    '--test',
    'tests/unit/*.test.mjs',
    'tests/api/*.test.mjs',
    'tests/component/*.test.mjs',
    'tests/non-functional/*.test.mjs',
    'tests/regression/security_and_bugs.mjs'
  ]);
  results.push({ name: 'Unit, API, Component, Non-Functional & Regression Suites', passed: phase1Pass });

  // Phase 2: AI Clinical Prompt Evaluation (80 scenarios, 8 prompts)
  console.log(`\n${colors.bold}[PHASE 2] Running AI Clinical Prompt Evaluation Suite...${colors.reset}`);
  const phase2Pass = await runCommand('node', ['tests/ai-eval/run_eval.mjs']);
  results.push({ name: 'AI Prompt Engineering & Safety Evaluation Suite', passed: phase2Pass });

  // Phase 3: Frontend Zod Schema & Normalizer Assertions
  console.log(`\n${colors.bold}[PHASE 3] Running Frontend Zod Schemas & Normalizer Suite...${colors.reset}`);
  const phase3Pass = await runCommand('node', ['Dentistfrontend/src/validators/test_validators.mjs']);
  results.push({ name: 'Frontend Zod Schemas & Normalizer Assertions', passed: phase3Pass });

  // Executive Summary Report
  const totalDuration = ((Date.now() - startTime) / 1000).toFixed(2);
  const allPassed = results.every(r => r.passed);

  console.log(`\n${colors.bold}${colors.cyan}======================================================================${colors.reset}`);
  console.log(`${colors.bold}${colors.cyan}   📊 DENTIA MASTER QA SUITE EXECUTION SUMMARY                       ${colors.reset}`);
  console.log(`${colors.bold}${colors.cyan}======================================================================${colors.reset}`);

  for (const r of results) {
    const icon = r.passed ? `${colors.green}✓ PASS${colors.reset}` : `${colors.red}✗ FAIL${colors.reset}`;
    console.log(`  ${icon} : ${r.name}`);
  }

  console.log(`\n  Total Execution Time : ${totalDuration}s`);
  console.log(`  Overall Result       : ${allPassed ? `${colors.green}${colors.bold}ALL TEST SUITES PASSED (100%)${colors.reset}` : `${colors.red}${colors.bold}FAILURES DETECTED${colors.reset}`}`);
  console.log(`${colors.bold}${colors.cyan}======================================================================\n${colors.reset}`);

  process.exit(allPassed ? 0 : 1);
}

main();
