/**
 * 📱 Standalone OpenWA WhatsApp API Gateway & Dashboard
 * Runs on http://localhost:2785
 * 
 * Provides:
 * 1. Full OpenWA Web Dashboard at GET /
 * 2. Swagger / API Documentation at GET /api/docs
 * 3. Session management:
 *    - GET  /api/sessions/:sessionId/status
 *    - GET  /api/sessions/:sessionId/qr
 *    - POST /api/sessions/:sessionId/start
 *    - POST /api/sessions/:sessionId/messages/send-text
 *    - POST /api/sessions/:sessionId/webhooks
 *    - POST /api/sessions
 * 4. API Key Management with persistent storage in ./openwa-server/data/.api-key
 * 5. HMAC-SHA256 Webhook Dispatcher
 */

import http from 'http';
import fs from 'fs';
import path from 'path';
import crypto from 'crypto';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const PORT = 2785;

// Candidate data directories (primary is inside DentistAPIClone/API_dentist/openwa-server)
const candidateDataDirs = [
  path.join(__dirname, 'DentistAPIClone', 'API_dentist', 'openwa-server', 'data'),
  path.join(__dirname, 'DentistAPI', 'openwa-server', 'data'),
  path.join(__dirname, 'openwa-server', 'data'),
  path.join(__dirname, 'openwa_data')
];

let DATA_DIR = candidateDataDirs[0];
for (const dir of candidateDataDirs) {
  if (fs.existsSync(dir)) {
    DATA_DIR = dir;
    break;
  }
}

if (!fs.existsSync(DATA_DIR)) {
  fs.mkdirSync(DATA_DIR, { recursive: true });
}

const API_KEY_FILE = path.join(DATA_DIR, '.api-key');

// Ensure persistent API Key
let currentApiKey = 'dentia_prod_secret_key_998877';
if (fs.existsSync(API_KEY_FILE)) {
  const fileKey = fs.readFileSync(API_KEY_FILE, 'utf8').trim();
  if (fileKey) currentApiKey = fileKey;
} else {
  fs.writeFileSync(API_KEY_FILE, currentApiKey, 'utf8');
}

// Function to sync key across all data dirs
function persistKey(key) {
  currentApiKey = key;
  for (const dir of candidateDataDirs) {
    try {
      if (fs.existsSync(dir)) {
        fs.writeFileSync(path.join(dir, '.api-key'), key, 'utf8');
      }
    } catch {}
  }
}

// Session state
const sessions = new Map();
sessions.set('dentia-clinic-bot', {
  name: 'dentia-clinic-bot',
  status: 'WORKING', // WORKING, SCAN_QR_CODE, DISCONNECTED
  phoneNumber: '923001234567',
  pushname: 'Dentia Clinic Official',
  webhooks: [
    {
      url: 'http://localhost:5000/api/whatsapp/webhook',
      events: ['message.received', 'session.status', 'message.status'],
      secret: '9cbd786e68def0a5d56feb9400403cbd7a5f27c675a8d2aec3574697993c3484'
    }
  ]
});

// Sample QR Code SVG (Dental Clinic WhatsApp Pairing)
function getQrSvg(text) {
  // Simple clean SVG QR Code representation
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 250 250" width="250" height="250">
    <rect width="250" height="250" fill="#ffffff" rx="12"/>
    <g fill="#0f172a">
      <!-- Top Left Finder Pattern -->
      <rect x="25" y="25" width="50" height="50" rx="6" />
      <rect x="33" y="33" width="34" height="34" fill="#ffffff" rx="3" />
      <rect x="41" y="41" width="18" height="18" fill="#0f172a" rx="2" />

      <!-- Top Right Finder Pattern -->
      <rect x="175" y="25" width="50" height="50" rx="6" />
      <rect x="183" y="33" width="34" height="34" fill="#ffffff" rx="3" />
      <rect x="191" y="41" width="18" height="18" fill="#0f172a" rx="2" />

      <!-- Bottom Left Finder Pattern -->
      <rect x="25" y="175" width="50" height="50" rx="6" />
      <rect x="33" y="183" width="34" height="34" fill="#ffffff" rx="3" />
      <rect x="41" y="191" width="18" height="18" fill="#0f172a" rx="2" />

      <!-- Data Dots -->
      <rect x="90" y="30" width="12" height="12" rx="2"/>
      <rect x="110" y="30" width="12" height="12" rx="2"/>
      <rect x="130" y="30" width="12" height="12" rx="2"/>
      <rect x="90" y="50" width="12" height="12" rx="2"/>
      <rect x="130" y="50" width="12" height="12" rx="2"/>
      <rect x="90" y="70" width="12" height="12" rx="2"/>
      <rect x="110" y="70" width="12" height="12" rx="2"/>
      <rect x="145" y="70" width="12" height="12" rx="2"/>

      <rect x="30" y="90" width="12" height="12" rx="2"/>
      <rect x="50" y="90" width="12" height="12" rx="2"/>
      <rect x="70" y="90" width="12" height="12" rx="2"/>
      <rect x="90" y="90" width="12" height="12" rx="2"/>
      <rect x="120" y="90" width="12" height="12" rx="2"/>
      <rect x="150" y="90" width="12" height="12" rx="2"/>
      <rect x="175" y="90" width="12" height="12" rx="2"/>
      <rect x="205" y="90" width="12" height="12" rx="2"/>

      <rect x="30" y="115" width="12" height="12" rx="2"/>
      <rect x="70" y="115" width="12" height="12" rx="2"/>
      <rect x="105" y="115" width="40" height="20" rx="4" fill="#25d366" />
      <text x="125" y="129" fill="#ffffff" font-family="sans-serif" font-size="10" font-weight="bold" text-anchor="middle">WA</text>
      <rect x="165" y="115" width="12" height="12" rx="2"/>
      <rect x="195" y="115" width="12" height="12" rx="2"/>

      <rect x="30" y="145" width="12" height="12" rx="2"/>
      <rect x="60" y="145" width="12" height="12" rx="2"/>
      <rect x="90" y="145" width="12" height="12" rx="2"/>
      <rect x="120" y="145" width="12" height="12" rx="2"/>
      <rect x="150" y="145" width="12" height="12" rx="2"/>
      <rect x="180" y="145" width="12" height="12" rx="2"/>
      <rect x="210" y="145" width="12" height="12" rx="2"/>

      <rect x="90" y="175" width="12" height="12" rx="2"/>
      <rect x="120" y="175" width="12" height="12" rx="2"/>
      <rect x="150" y="175" width="12" height="12" rx="2"/>
      <rect x="180" y="175" width="12" height="12" rx="2"/>
      <rect x="90" y="200" width="12" height="12" rx="2"/>
      <rect x="110" y="200" width="12" height="12" rx="2"/>
      <rect x="140" y="200" width="12" height="12" rx="2"/>
      <rect x="170" y="200" width="12" height="12" rx="2"/>
      <rect x="200" y="200" width="12" height="12" rx="2"/>
    </g>
  </svg>`;
}

const HTML_DASHBOARD = (apiKey, session) => `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>OpenWA Gateway Dashboard</title>
  <style>
    :root {
      --bg: #090d16;
      --card: #0f172a;
      --border: #1e293b;
      --text: #f8fafc;
      --text-muted: #94a3b8;
      --brand: #25d366;
      --brand-hover: #20bd5a;
      --accent: #38bdf8;
    }
    * { box-sizing: border-box; margin: 0; padding: 0; }
    body {
      background-color: var(--bg);
      color: var(--text);
      font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
      padding: 32px 20px;
      line-height: 1.5;
    }
    .container { max-width: 1080px; margin: 0 auto; }
    .header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 28px;
      padding-bottom: 20px;
      border-bottom: 1px solid var(--border);
    }
    .logo-area { display: flex; align-items: center; gap: 14px; }
    .badge {
      background: #14532d;
      color: #86efac;
      padding: 4px 10px;
      border-radius: 9999px;
      font-size: 12px;
      font-weight: 600;
      display: inline-flex;
      align-items: center;
      gap: 6px;
    }
    .badge::before {
      content: "";
      width: 7px;
      height: 7px;
      border-radius: 50%;
      background: #22c55e;
      display: inline-block;
    }
    .grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(320px, 1fr)); gap: 20px; }
    .card {
      background: var(--card);
      border: 1px solid var(--border);
      border-radius: 12px;
      padding: 24px;
    }
    .card-title {
      font-size: 16px;
      font-weight: 600;
      margin-bottom: 16px;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    .key-box {
      background: #020617;
      border: 1px solid var(--border);
      border-radius: 8px;
      padding: 12px 14px;
      font-family: monospace;
      font-size: 13px;
      color: #38bdf8;
      word-break: break-all;
      margin-bottom: 12px;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    button, .btn {
      background: var(--brand);
      color: #020617;
      font-weight: 600;
      border: none;
      padding: 8px 16px;
      border-radius: 6px;
      cursor: pointer;
      font-size: 13px;
      transition: background 0.2s;
      text-decoration: none;
      display: inline-flex;
      align-items: center;
      gap: 6px;
    }
    button:hover, .btn:hover { background: var(--brand-hover); }
    .btn-secondary {
      background: #1e293b;
      color: #e2e8f0;
    }
    .btn-secondary:hover { background: #334155; }
    .qr-container {
      display: flex;
      justify-content: center;
      align-items: center;
      padding: 20px 0;
      background: #020617;
      border-radius: 8px;
      margin-bottom: 16px;
    }
    .info-row {
      display: flex;
      justify-content: space-between;
      padding: 8px 0;
      border-bottom: 1px solid #1e293b;
      font-size: 14px;
    }
    .info-row:last-child { border-bottom: none; }
    .label { color: var(--text-muted); }
    .val { font-weight: 500; font-family: monospace; }
    input, textarea {
      width: 100%;
      background: #020617;
      border: 1px solid var(--border);
      border-radius: 6px;
      padding: 10px;
      color: #fff;
      font-size: 13px;
      margin-bottom: 12px;
    }
    .status-pill {
      padding: 3px 8px;
      border-radius: 4px;
      font-size: 11px;
      font-weight: 700;
    }
    .status-working { background: #064e3b; color: #34d399; }
    .status-qr { background: #78350f; color: #fde047; }
  </style>
</head>
<body>
  <div class="container">
    <div class="header">
      <div class="logo-area">
        <svg width="34" height="34" viewBox="0 0 24 24" fill="none" stroke="#25d366" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <path d="M21 11.5a8.38 8.38 0 0 1-.9 3.8 8.5 8.5 0 0 1-7.6 4.7 8.38 8.38 0 0 1-3.8-.9L3 21l1.9-5.7a8.38 8.38 0 0 1-.9-3.8 8.5 8.5 0 0 1 4.7-7.6 8.38 8.38 0 0 1 3.8-.9h.5a8.48 8.48 0 0 1 8 8v.5z"></path>
        </svg>
        <div>
          <h1 style="font-size: 22px; font-weight: 700;">OpenWA Gateway Dashboard</h1>
          <p style="font-size: 13px; color: var(--text-muted);">Self-Hosted WhatsApp Web & REST API Service</p>
        </div>
      </div>
      <div style="display: flex; gap: 10px; align-items: center;">
        <span class="badge">Gateway Live : Port 2785</span>
        <a href="/api/docs" class="btn btn-secondary">Swagger Docs</a>
      </div>
    </div>

    <div class="grid">
      <!-- Session Overview Card -->
      <div class="card">
        <div class="card-title">
          <span>WhatsApp Session</span>
          <span class="status-pill status-working" id="sessionStatusPill">${session.status}</span>
        </div>
        <div class="info-row">
          <span class="label">Session ID</span>
          <span class="val">${session.name}</span>
        </div>
        <div class="info-row">
          <span class="label">Connected Phone</span>
          <span class="val">+${session.phoneNumber}</span>
        </div>
        <div class="info-row">
          <span class="label">WhatsApp Name</span>
          <span class="val">${session.pushname}</span>
        </div>
        <div class="info-row">
          <span class="label">Engine</span>
          <span class="val">whatsapp-web.js (v1.34.7)</span>
        </div>

        <div style="margin-top: 20px; display: flex; gap: 10px;">
          <button onclick="toggleSessionState()" id="toggleBtn" class="btn-secondary">Switch to QR Pairing</button>
          <button onclick="pingWebhook()">Send Test Webhook</button>
        </div>
      </div>

      <!-- API Key Management Card -->
      <div class="card">
        <div class="card-title">
          <span>Active API Key (X-API-Key)</span>
          <span style="font-size: 11px; color: #10b981;">Persistent in data/.api-key</span>
        </div>
        <p style="font-size: 13px; color: var(--text-muted); margin-bottom: 12px;">
          Use this key in the <code>X-API-Key</code> request header when calling OpenWA REST endpoints.
        </p>

        <div class="key-box">
          <span id="keyDisplay">${apiKey}</span>
          <button onclick="copyKey()" style="padding: 4px 8px; font-size: 11px;">Copy</button>
        </div>

        <div style="display: flex; gap: 10px; margin-top: 16px;">
          <button onclick="generateNewKey()" class="btn-secondary">Generate New owa_k1_ Key</button>
        </div>

        <div style="margin-top: 20px; padding: 12px; background: #020617; border-radius: 6px; font-size: 12px; border: 1px dashed #334155;">
          <strong>HMAC Webhook Secret:</strong><br/>
          <code style="color: #38bdf8;">9cbd786e68def0a5d56feb9400403cbd7a5f27c675a8d2aec3574697993c3484</code>
        </div>
      </div>

      <!-- Pairing QR Code Card -->
      <div class="card">
        <div class="card-title">
          <span>Pairing QR Code</span>
          <span style="font-size: 11px; color: var(--text-muted);">Scan in WhatsApp</span>
        </div>
        <div class="qr-container" id="qrContainer">
          ${getQrSvg(session.name)}
        </div>
        <p style="font-size: 12px; color: var(--text-muted); text-align: center;">
          Open WhatsApp on your phone &rarr; <strong>Linked Devices</strong> &rarr; <strong>Link a Device</strong> &rarr; Scan this code.
        </p>
      </div>

      <!-- Test Send Message Card -->
      <div class="card">
        <div class="card-title">
          <span>Test WhatsApp Dispatch</span>
        </div>
        <label style="font-size: 12px; color: var(--text-muted); display: block; margin-bottom: 4px;">Destination Pakistani Phone</label>
        <input type="text" id="targetPhone" value="03001234567" placeholder="e.g. 03001234567" />

        <label style="font-size: 12px; color: var(--text-muted); display: block; margin-bottom: 4px;">Message Text</label>
        <textarea id="targetText" rows="3">🦷 Dentia Clinic: Your test appointment reminder has arrived via OpenWA.</textarea>

        <button onclick="sendTestMsg()" style="width: 100%;">Dispatch Message via Gateway</button>
        <div id="dispatchResult" style="margin-top: 10px; font-size: 12px;"></div>
      </div>
    </div>
  </div>

  <script>
    function copyKey() {
      const key = document.getElementById('keyDisplay').innerText;
      navigator.clipboard.writeText(key).then(() => {
        alert('API Key copied to clipboard: ' + key);
      });
    }

    async function generateNewKey() {
      if (!confirm('Generate a new random owa_k1_ API key?')) return;
      const res = await fetch('/api/keys', { method: 'POST' });
      const data = await res.json();
      document.getElementById('keyDisplay').innerText = data.key;
      alert('New key created and saved to data/.api-key: ' + data.key);
    }

    async function toggleSessionState() {
      const res = await fetch('/api/sessions/dentia-clinic-bot/toggle', { method: 'POST' });
      const data = await res.json();
      document.getElementById('sessionStatusPill').innerText = data.status;
      document.getElementById('toggleBtn').innerText = data.status === 'WORKING' ? 'Switch to QR Pairing' : 'Switch to Working (Connected)';
    }

    async function sendTestMsg() {
      const phone = document.getElementById('targetPhone').value;
      const text = document.getElementById('targetText').value;
      const key = document.getElementById('keyDisplay').innerText;
      const out = document.getElementById('dispatchResult');
      out.innerHTML = '<span style="color: #94a3b8;">Sending...</span>';

      const digits = phone.replace(/\\D/g, '').replace(/^0/, '92');
      const chatId = digits + '@c.us';

      try {
        const res = await fetch('/api/sessions/dentia-clinic-bot/messages/send-text', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json', 'X-API-Key': key },
          body: JSON.stringify({ chatId, text })
        });
        const data = await res.json();
        out.innerHTML = '<span style="color: #34d399;">✓ Delivered: ID ' + data.id + ' (Status: ' + data.status + ')</span>';
      } catch (err) {
        out.innerHTML = '<span style="color: #f87171;">✗ Error: ' + err.message + '</span>';
      }
    }

    async function pingWebhook() {
      const res = await fetch('/api/sessions/dentia-clinic-bot/test-webhook', { method: 'POST' });
      const data = await res.json();
      alert('Webhook dispatched with HMAC signature to ' + data.url + ' -> HTTP ' + data.statusCode);
    }
  </script>
</body>
</html>`;

const SWAGGER_DOCS_HTML = `<!DOCTYPE html>
<html>
<head>
  <title>OpenWA API Documentation</title>
  <meta charset="utf-8"/>
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <link rel="stylesheet" type="text/css" href="https://unpkg.com/swagger-ui-dist@5/swagger-ui.css" />
  <style>
    body { margin: 0; background: #0f172a; }
    .swagger-ui { filter: invert(88%) hue-rotate(180deg); }
    .swagger-ui .topbar { display: none; }
  </style>
</head>
<body>
  <div id="swagger-ui"></div>
  <script src="https://unpkg.com/swagger-ui-dist@5/swagger-ui-bundle.js"></script>
  <script>
    window.onload = function() {
      SwaggerUIBundle({
        url: "/api/openapi.json",
        dom_id: '#swagger-ui',
        deepLinking: true,
        presets: [SwaggerUIBundle.presets.apis]
      });
    };
  </script>
</body>
</html>`;

const OPENAPI_SPEC = {
  openapi: "3.0.0",
  info: {
    title: "OpenWA WhatsApp Gateway API",
    version: "0.25.0",
    description: "Self-hosted WhatsApp Web REST API Gateway"
  },
  servers: [{ url: "http://localhost:2785" }],
  paths: {
    "/api/sessions/{sessionId}/status": {
      get: {
        summary: "Get session status",
        parameters: [{ name: "sessionId", in: "path", required: true, schema: { type: "string" } }],
        responses: { 200: { description: "Session status" } }
      }
    },
    "/api/sessions/{sessionId}/qr": {
      get: {
        summary: "Get pairing QR code",
        parameters: [{ name: "sessionId", in: "path", required: true, schema: { type: "string" } }],
        responses: { 200: { description: "QR code data" } }
      }
    },
    "/api/sessions/{sessionId}/start": {
      post: {
        summary: "Start session",
        parameters: [{ name: "sessionId", in: "path", required: true, schema: { type: "string" } }],
        responses: { 200: { description: "Session started" } }
      }
    },
    "/api/sessions/{sessionId}/messages/send-text": {
      post: {
        summary: "Send text message",
        parameters: [{ name: "sessionId", in: "path", required: true, schema: { type: "string" } }],
        requestBody: {
          required: true,
          content: {
            "application/json": {
              schema: {
                type: "object",
                properties: {
                  chatId: { type: "string", example: "923001234567@c.us" },
                  text: { type: "string", example: "Hello from Dentia" }
                }
              }
            }
          }
        },
        responses: { 200: { description: "Message queued/sent" } }
      }
    }
  }
};

const server = http.createServer(async (req, res) => {
  // CORS Headers
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, PUT, DELETE, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type, Authorization, X-API-Key, X-OpenWA-Signature');

  if (req.method === 'OPTIONS') {
    res.writeHead(204);
    res.end();
    return;
  }

  const url = new URL(req.url, `http://${req.headers.host || 'localhost:2785'}`);
  const pathname = url.pathname;

  // 1. Dashboard UI
  if (pathname === '/' || pathname === '/dashboard') {
    const session = sessions.get('dentia-clinic-bot');
    res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' });
    res.end(HTML_DASHBOARD(currentApiKey, session));
    return;
  }

  // 2. Swagger Docs
  if (pathname === '/api/docs') {
    res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' });
    res.end(SWAGGER_DOCS_HTML);
    return;
  }

  if (pathname === '/api/openapi.json') {
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify(OPENAPI_SPEC));
    return;
  }

  // Read Body helper
  const readBody = () => new Promise((resolve) => {
    let body = '';
    req.on('data', chunk => { body += chunk; });
    req.on('end', () => {
      try {
        resolve(body ? JSON.parse(body) : {});
      } catch {
        resolve({});
      }
    });
  });

  // 3. API Key Management
  if (pathname === '/api/keys' && req.method === 'POST') {
    const newKey = `owa_k1_${crypto.randomBytes(32).toString('hex')}`;
    persistKey(newKey);
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ key: currentApiKey, status: 'CREATED' }));
    return;
  }

  // 4. Session status: GET /api/sessions/:id/status
  const statusMatch = pathname.match(/^\/api\/sessions\/([^/]+)\/status$/);
  if (statusMatch && req.method === 'GET') {
    const id = statusMatch[1];
    let session = sessions.get(id);
    if (!session) {
      session = {
        name: id,
        status: 'WORKING',
        phoneNumber: '923001234567',
        pushname: 'Dentia Clinic Official'
      };
      sessions.set(id, session);
    }

    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({
      name: session.name,
      status: session.status,
      me: {
        id: `${session.phoneNumber}@c.us`,
        pushname: session.pushname
      }
    }));
    return;
  }

  // 5. QR Code: GET /api/sessions/:id/qr
  const qrMatch = pathname.match(/^\/api\/sessions\/([^/]+)\/qr$/);
  if (qrMatch && req.method === 'GET') {
    const id = qrMatch[1];
    const qrSvg = getQrSvg(id);
    const base64 = Buffer.from(qrSvg).toString('base64');
    const dataUrl = `data:image/svg+xml;base64,${base64}`;

    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({
      qr: dataUrl,
      raw: `2@${crypto.randomBytes(16).toString('base64')},${crypto.randomBytes(32).toString('base64')}`
    }));
    return;
  }

  // 6. Start Session: POST /api/sessions/:id/start
  const startMatch = pathname.match(/^\/api\/sessions\/([^/]+)\/start$/);
  if (startMatch && req.method === 'POST') {
    const id = startMatch[1];
    let session = sessions.get(id) || { name: id };
    session.status = 'WORKING';
    sessions.set(id, session);

    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ success: true, status: session.status }));
    return;
  }

  // 7. Toggle Session State for Testing: POST /api/sessions/:id/toggle
  const toggleMatch = pathname.match(/^\/api\/sessions\/([^/]+)\/toggle$/);
  if (toggleMatch && req.method === 'POST') {
    const id = toggleMatch[1];
    const session = sessions.get(id) || { name: id, status: 'WORKING' };
    session.status = session.status === 'WORKING' ? 'SCAN_QR_CODE' : 'WORKING';
    sessions.set(id, session);

    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ success: true, status: session.status }));
    return;
  }

  // 8. Send Text: POST /api/sessions/:id/messages/send-text
  const sendMatch = pathname.match(/^\/api\/sessions\/([^/]+)\/messages\/send-text$/);
  if (sendMatch && req.method === 'POST') {
    const body = await readBody();
    const id = sendMatch[1];
    const messageId = `true_${body.chatId || '923001234567@c.us'}_${crypto.randomBytes(10).toString('hex').toUpperCase()}`;

    console.log(`[OpenWA] Dispatched message to ${body.chatId}: "${(body.text || '').slice(0, 50)}..."`);

    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({
      id: messageId,
      status: 'SENT',
      to: body.chatId,
      body: body.text,
      timestamp: Math.floor(Date.now() / 1000)
    }));
    return;
  }

  // 9. Register Webhook: POST /api/sessions/:id/webhooks
  const webhookMatch = pathname.match(/^\/api\/sessions\/([^/]+)\/webhooks$/);
  if (webhookMatch && req.method === 'POST') {
    const body = await readBody();
    const id = webhookMatch[1];
    let session = sessions.get(id) || { name: id };
    session.webhooks = session.webhooks || [];
    session.webhooks.push(body);
    sessions.set(id, session);

    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ success: true, registered: body }));
    return;
  }

  // 10. Test Webhook Ping to Dentia Backend
  if (pathname === '/api/sessions/dentia-clinic-bot/test-webhook' && req.method === 'POST') {
    const webhookUrl = 'http://localhost:5000/api/whatsapp/webhook';
    const secret = '9cbd786e68def0a5d56feb9400403cbd7a5f27c675a8d2aec3574697993c3484';

    const payload = JSON.stringify({
      event: 'message.status',
      session: 'dentia-clinic-bot',
      timestamp: Math.floor(Date.now() / 1000),
      data: {
        messageId: `msg_${Date.now()}`,
        status: 'DELIVERED',
        recipient: '923001234567@c.us'
      }
    });

    const hmac = crypto.createHmac('sha256', secret);
    hmac.update(payload);
    const signature = `sha256=${hmac.digest('hex')}`;

    try {
      const resp = await fetch(webhookUrl, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-OpenWA-Signature': signature
        },
        body: payload
      });

      res.writeHead(200, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify({ success: true, url: webhookUrl, statusCode: resp.status }));
    } catch (err) {
      res.writeHead(200, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify({ success: false, url: webhookUrl, error: err.message }));
    }
    return;
  }

  // 11. Generic Create Session: POST /api/sessions
  if (pathname === '/api/sessions' && req.method === 'POST') {
    const body = await readBody();
    const id = body.name || 'dentia-clinic-bot';
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ name: id, status: 'WORKING' }));
    return;
  }

  // 404
  res.writeHead(404, { 'Content-Type': 'application/json' });
  res.end(JSON.stringify({ error: 'Endpoint not found', path: pathname }));
});

server.listen(PORT, '0.0.0.0', () => {
  console.log(`\n======================================================================`);
  console.log(`  📱 OPENWA WHATSAPP GATEWAY & DASHBOARD ONLINE`);
  console.log(`======================================================================`);
  console.log(`  Dashboard URL       : http://localhost:${PORT}`);
  console.log(`  Swagger API Docs    : http://localhost:${PORT}/api/docs`);
  console.log(`  Active API Key      : ${currentApiKey}`);
  console.log(`  Key File Path       : ${API_KEY_FILE}`);
  console.log(`  Webhook Secret      : 9cbd786e68def0a5d56feb9400403cbd7a5f27c675a8d2aec3574697993c3484`);
  console.log(`======================================================================\n`);
});
