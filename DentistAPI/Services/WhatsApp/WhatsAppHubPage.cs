using System;

namespace DentistAPI.Services
{
    /// <summary>
    /// Generates a standalone, responsive, single-page clinical WhatsApp Hub and Gateway console.
    /// Served directly by ASP.NET Core on IIS for /whatsapp and /api/whatsapp/hub with zero server setup.
    /// </summary>
    public static class WhatsAppHubPage
    {
        public static string Render()
        {
            return @"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <title>Dentia — WhatsApp Clinical Hub & Gateway Dashboard</title>
  <link rel=""preconnect"" href=""https://fonts.googleapis.com"">
  <link rel=""preconnect"" href=""https://fonts.gstatic.com"" crossorigin>
  <link href=""https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@400;500;600;700;800&family=JetBrains+Mono:wght@400;500&display=swap"" rel=""stylesheet"">
  <style>
    :root {
      --bg: #0b1120;
      --card-bg: #131c31;
      --card-border: #1e293b;
      --card-hover: #1e2942;
      --primary: #10b981;
      --primary-hover: #059669;
      --cyan: #06b6d4;
      --text-main: #f8fafc;
      --text-muted: #94a3b8;
      --text-dim: #64748b;
      --danger: #ef4444;
      --warning: #f59e0b;
      --success: #10b981;
    }
    * { box-sizing: border-box; margin: 0; padding: 0; }
    body {
      font-family: 'Plus Jakarta Sans', -apple-system, BlinkMacSystemFont, sans-serif;
      background: radial-gradient(circle at 50% 0%, #1e293b 0%, #0b1120 70%);
      color: var(--text-main);
      min-height: 100vh;
      padding: 24px 16px;
      line-height: 1.5;
    }
    .container { max-width: 1200px; margin: 0 auto; }
    header {
      display: flex;
      flex-wrap: wrap;
      justify-content: space-between;
      align-items: center;
      gap: 16px;
      padding-bottom: 24px;
      border-bottom: 1px solid var(--card-border);
      margin-bottom: 24px;
    }
    .brand { display: flex; align-items: center; gap: 14px; }
    .brand-icon {
      width: 48px;
      height: 48px;
      border-radius: 12px;
      background: linear-gradient(135deg, #10b981, #06b6d4);
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 24px;
      box-shadow: 0 8px 20px rgba(16, 185, 129, 0.25);
    }
    .brand h1 { font-size: 22px; font-weight: 800; letter-spacing: -0.5px; }
    .brand p { font-size: 13px; color: var(--text-muted); }
    .status-badge {
      display: inline-flex;
      align-items: center;
      gap: 8px;
      padding: 6px 14px;
      border-radius: 9999px;
      font-size: 13px;
      font-weight: 600;
      background: rgba(16, 185, 129, 0.12);
      color: #34d399;
      border: 1px solid rgba(16, 185, 129, 0.3);
    }
    .pulse-dot {
      width: 8px;
      height: 8px;
      border-radius: 50%;
      background: #10b981;
      box-shadow: 0 0 10px #10b981;
      animation: pulse 2s infinite;
    }
    @keyframes pulse { 0% { opacity: 0.4; } 50% { opacity: 1; } 100% { opacity: 0.4; } }

    .banner {
      background: linear-gradient(135deg, rgba(16, 185, 129, 0.1), rgba(6, 182, 212, 0.08));
      border: 1px solid rgba(16, 185, 129, 0.25);
      border-radius: 14px;
      padding: 16px 20px;
      margin-bottom: 24px;
      display: flex;
      align-items: flex-start;
      gap: 14px;
    }
    .banner-icon { font-size: 22px; flex-shrink: 0; margin-top: 2px; }
    .banner-content h3 { font-size: 15px; font-weight: 700; color: #6ee7b7; margin-bottom: 4px; }
    .banner-content p { font-size: 13.5px; color: #cbd5e1; line-height: 1.5; }

    .grid-2 {
      display: grid;
      grid-template-columns: 1.2fr 0.8fr;
      gap: 24px;
      margin-bottom: 24px;
    }
    @media (max-width: 900px) { .grid-2 { grid-template-columns: 1fr; } }

    .card {
      background: var(--card-bg);
      border: 1px solid var(--card-border);
      border-radius: 16px;
      padding: 22px;
      box-shadow: 0 4px 20px rgba(0, 0, 0, 0.2);
    }
    .card-title {
      font-size: 16px;
      font-weight: 700;
      display: flex;
      align-items: center;
      gap: 10px;
      margin-bottom: 16px;
      color: #f1f5f9;
    }
    .card-title span.badge {
      font-size: 11px;
      padding: 2px 8px;
      border-radius: 6px;
      background: rgba(6, 182, 212, 0.15);
      color: var(--cyan);
      font-weight: 600;
    }

    .form-group { margin-bottom: 14px; }
    label { display: block; font-size: 13px; font-weight: 600; color: var(--text-muted); margin-bottom: 6px; }
    input, textarea {
      width: 100%;
      background: #0b1120;
      border: 1px solid var(--card-border);
      border-radius: 10px;
      padding: 10px 14px;
      color: #fff;
      font-family: inherit;
      font-size: 14px;
      transition: border-color 0.15s ease;
    }
    input:focus, textarea:focus {
      outline: none;
      border-color: var(--primary);
      box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.15);
    }
    textarea { resize: vertical; min-height: 110px; }

    .template-chips {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
      margin-bottom: 12px;
    }
    .chip {
      background: rgba(255, 255, 255, 0.05);
      border: 1px solid var(--card-border);
      color: var(--text-muted);
      border-radius: 8px;
      padding: 5px 10px;
      font-size: 12px;
      cursor: pointer;
      transition: all 0.15s ease;
      font-weight: 500;
    }
    .chip:hover {
      background: rgba(16, 185, 129, 0.15);
      border-color: rgba(16, 185, 129, 0.4);
      color: #a7f3d0;
    }

    .btn-group { display: flex; flex-wrap: wrap; gap: 10px; margin-top: 14px; }
    .btn {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      gap: 8px;
      padding: 10px 18px;
      border-radius: 10px;
      font-size: 14px;
      font-weight: 600;
      cursor: pointer;
      text-decoration: none;
      transition: all 0.2s ease;
      border: none;
    }
    .btn-primary {
      background: linear-gradient(135deg, #10b981, #059669);
      color: #fff;
      box-shadow: 0 4px 14px rgba(16, 185, 129, 0.3);
    }
    .btn-primary:hover {
      transform: translateY(-1px);
      box-shadow: 0 6px 20px rgba(16, 185, 129, 0.45);
    }
    .btn-secondary {
      background: #1e293b;
      color: #e2e8f0;
      border: 1px solid var(--card-border);
    }
    .btn-secondary:hover { background: #334155; }
    .btn-outline {
      background: transparent;
      border: 1px solid rgba(6, 182, 212, 0.4);
      color: var(--cyan);
    }
    .btn-outline:hover { background: rgba(6, 182, 212, 0.1); }

    .faq-item {
      background: #0f172a;
      border: 1px solid var(--card-border);
      border-radius: 12px;
      padding: 14px;
      margin-bottom: 12px;
    }
    .faq-q { font-size: 13.5px; font-weight: 700; color: #f1f5f9; margin-bottom: 6px; }
    .faq-a { font-size: 12.5px; color: var(--text-muted); line-height: 1.55; }

    .table-container {
      overflow-x: auto;
      margin-top: 12px;
    }
    table {
      width: 100%;
      border-collapse: collapse;
      font-size: 13px;
      text-align: left;
    }
    th {
      background: #0f172a;
      color: var(--text-dim);
      font-weight: 600;
      padding: 10px 14px;
      border-bottom: 1px solid var(--card-border);
      text-transform: uppercase;
      font-size: 11px;
      letter-spacing: 0.5px;
    }
    td {
      padding: 12px 14px;
      border-bottom: 1px solid rgba(30, 41, 59, 0.7);
      color: #e2e8f0;
    }
    tr:hover td { background: rgba(255, 255, 255, 0.02); }
    .badge-sent {
      background: rgba(16, 185, 129, 0.15);
      color: #34d399;
      padding: 3px 8px;
      border-radius: 6px;
      font-size: 11px;
      font-weight: 600;
    }
    .badge-pending {
      background: rgba(245, 158, 11, 0.15);
      color: #fbbf24;
      padding: 3px 8px;
      border-radius: 6px;
      font-size: 11px;
      font-weight: 600;
    }
    .badge-failed {
      background: rgba(239, 68, 68, 0.15);
      color: #f87171;
      padding: 3px 8px;
      border-radius: 6px;
      font-size: 11px;
      font-weight: 600;
    }
    .mono { font-family: 'JetBrains Mono', monospace; font-size: 12px; }

    .result-box {
      margin-top: 14px;
      padding: 12px 14px;
      border-radius: 10px;
      font-size: 13px;
      display: none;
    }
    .result-success { background: rgba(16, 185, 129, 0.15); border: 1px solid rgba(16, 185, 129, 0.3); color: #6ee7b7; }
    .result-info { background: rgba(6, 182, 212, 0.15); border: 1px solid rgba(6, 182, 212, 0.3); color: #a5f3fc; }
  </style>
</head>
<body>
  <div class=""container"">
    <!-- Header -->
    <header>
      <div class=""brand"">
        <div class=""brand-icon"">🦷</div>
        <div>
          <h1>Dentia WhatsApp Clinical Hub</h1>
          <p>Practice Management Patient Communications & 1-Click WhatsApp Gateway</p>
        </div>
      </div>
      <div>
        <span class=""status-badge"" id=""statusBadge"">
          <span class=""pulse-dot""></span>
          <span id=""statusText"">Direct 1-Click WhatsApp Active</span>
        </span>
      </div>
    </header>

    <!-- Top Architecture Banner -->
    <div class=""banner"">
      <div class=""banner-icon"">💡</div>
      <div class=""banner-content"">
        <h3>Server Access Not Required — Zero-Server Direct Mode Active</h3>
        <p>
          Aapko Windows Server par login karne ya Node.js / Docker chalane ki <strong>bilkul zaroorat nahi hai</strong>. 
          Aapka IIS API server appointment confirmations, reminders, aur billing alerts ke liye <strong>Direct WhatsApp (wa.me)</strong> links aur automated DB queuing manage karta hai.
        </p>
      </div>
    </div>

    <!-- Main Content Grid -->
    <div class=""grid-2"">
      <!-- Left Column: 1-Click Dispatcher -->
      <div class=""card"">
        <div class=""card-title"">
          <span>📱</span> Quick WhatsApp Dispatcher
          <span class=""badge"">1-Click / API</span>
        </div>

        <div class=""form-group"">
          <label for=""phoneInput"">Patient Phone Number (Pakistani / International)</label>
          <input type=""text"" id=""phoneInput"" placeholder=""0300 1234567 or +923001234567"" oninput=""updatePreview()"">
          <span id=""phoneNormalized"" style=""font-size: 11px; color: var(--cyan); margin-top: 4px; display: block;""></span>
        </div>

        <div class=""form-group"">
          <label>Pre-filled Clinical Templates</label>
          <div class=""template-chips"">
            <span class=""chip"" onclick=""setTemplate('reminder')"">📅 Appointment Reminder</span>
            <span class=""chip"" onclick=""setTemplate('booking')"">✅ Booking Confirmed</span>
            <span class=""chip"" onclick=""setTemplate('invoice')"">🧾 Invoice / Bill</span>
            <span class=""chip"" onclick=""setTemplate('checkup')"">🦷 6-Month Checkup</span>
          </div>
        </div>

        <div class=""form-group"">
          <label for=""messageInput"">Message Text</label>
          <textarea id=""messageInput"" placeholder=""Type patient WhatsApp message..."" oninput=""updatePreview()""></textarea>
        </div>

        <div class=""btn-group"">
          <button class=""btn btn-primary"" onclick=""dispatchDirectWhatsApp()"">
            <span>🚀</span> Send via 1-Click WhatsApp (wa.me)
          </button>
          <button class=""btn btn-secondary"" onclick=""dispatchViaApi()"">
            <span>⚡</span> Dispatch & Log to DB
          </button>
        </div>

        <div id=""resultBox"" class=""result-box""></div>
      </div>

      <!-- Right Column: Architecture & IIS Explanation -->
      <div class=""card"">
        <div class=""card-title"">
          <span>⚙️</span> IIS & OpenWA Architecture
          <span class=""badge"">Explanation</span>
        </div>

        <div class=""faq-item"">
          <div class=""faq-q"">1. Mera API_dentist IIS Manager par deploy hai, OpenWA kaise chalay ga?</div>
          <div class=""faq-a"">
            IIS Manager sirf .NET C# API (DentistAPI.dll) chalata hai. IIS Node.js ko automatically nahi chalata.
            Is liye IIS par OpenWA ke bajaye <strong>Direct 1-Click WhatsApp Engine</strong> deploy kiya gaya hai jo bina kisi server setup ke 100% chalta hai.
          </div>
        </div>

        <div class=""faq-item"">
          <div class=""faq-q"">2. Main server par nahi ja sakta, toh kya WhatsApp chalay ga?</div>
          <div class=""faq-a"">
            <strong>Haan 100%!</strong> 1-Click WhatsApp mode receptionists aur doctors ke browser/mobile se direct official WhatsApp Web kholta hai. Is ke liye server par RDP login ki 0% zaroorat hai.
          </div>
        </div>

        <div class=""faq-item"">
          <div class=""faq-q"">3. WhatsApp Number Ban ka koi khatra?</div>
          <div class=""faq-a"">
            <strong>0% Khatra!</strong> Kyun ke message official WhatsApp client se jaata hai, WhatsApp ka algorithm aapke clinic ke number ko bot samajh kar block nahi karega.
          </div>
        </div>

        <div style=""margin-top: 16px;"">
          <a href=""https://dentistfrontend.vercel.app/admin/whatsapp"" target=""_blank"" class=""btn btn-outline"" style=""width: 100%; text-align: center;"">
            <span>🌐</span> Open Full Dentia Admin WhatsApp Portal
          </a>
        </div>
      </div>
    </div>

    <!-- Bottom Section: Live Audit Logs -->
    <div class=""card"">
      <div style=""display: flex; justify-content: space-between; align-items: center; margin-bottom: 14px;"">
        <div class=""card-title"" style=""margin-bottom: 0;"">
          <span>📋</span> Recent Clinical WhatsApp Message Logs
        </div>
        <button class=""btn btn-secondary"" style=""padding: 6px 12px; font-size: 12px;"" onclick=""fetchLogs()"">
          🔄 Refresh Logs
        </button>
      </div>

      <div class=""table-container"">
        <table>
          <thead>
            <tr>
              <th>ID</th>
              <th>Recipient Phone</th>
              <th>Message Type</th>
              <th>Status</th>
              <th>Date / Time (UTC)</th>
              <th>Action</th>
            </tr>
          </thead>
          <tbody id=""logsTableBody"">
            <tr>
              <td colspan=""6"" style=""text-align: center; color: var(--text-dim); padding: 24px;"">
                Loading WhatsApp message history from database...
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>

  <script>
    function normalizePhone(phone) {
      if (!phone) return '';
      let clean = phone.replace(/[^0-9]/g, '');
      if (clean.startsWith('03') && clean.length === 11) {
        return '92' + clean.substring(1);
      }
      if (clean.startsWith('923') && clean.length === 12) {
        return clean;
      }
      if (clean.startsWith('0092')) {
        return clean.substring(2);
      }
      return clean;
    }

    function updatePreview() {
      const phone = document.getElementById('phoneInput').value;
      const normalized = normalizePhone(phone);
      const span = document.getElementById('phoneNormalized');
      if (normalized) {
        span.textContent = 'Normalized International: +' + normalized + ' (wa.me/' + normalized + ')';
      } else {
        span.textContent = '';
      }
    }

    function setTemplate(type) {
      const msgBox = document.getElementById('messageInput');
      if (type === 'reminder') {
        msgBox.value = '🦷 *Dentia Dental Hospital Alert*\nDear Patient, this is a reminder for your upcoming dental appointment tomorrow. Please arrive 10 minutes early.\nLocation: Suite 402, Medical City.\nPhone: +92 51 2233445';
      } else if (type === 'booking') {
        msgBox.value = '✅ *Appointment Confirmed — Dentia Dental*\nDear Patient, your dental appointment has been confirmed with your doctor.\nWe look forward to serving you!';
      } else if (type === 'invoice') {
        msgBox.value = '🧾 *Dentia Dental Hospital — Billing Notice*\nDear Patient, your clinical treatment invoice is ready. Please view or pay online via the Dentia Patient Portal.\nThank you for choosing Dentia!';
      } else if (type === 'checkup') {
        msgBox.value = '🦷 *Routine Dental Care Notice*\nDear Patient, it has been 6 months since your last scaling/checkup. Maintaining healthy gums prevents cavities!\nCall us or reply to book your routine cleaning.';
      }
      updatePreview();
    }

    function dispatchDirectWhatsApp() {
      const phone = normalizePhone(document.getElementById('phoneInput').value);
      const text = document.getElementById('messageInput').value.trim();
      if (!phone) {
        alert('Please enter a valid phone number (e.g. 0300 1234567)');
        return;
      }
      const url = 'https://wa.me/' + phone + '?text=' + encodeURIComponent(text);
      window.open(url, '_blank');
      
      const res = document.getElementById('resultBox');
      res.className = 'result-box result-success';
      res.style.display = 'block';
      res.innerHTML = '✅ <strong>WhatsApp Web / App opened!</strong> If it did not open automatically, <a href=""' + url + '"" target=""_blank"" style=""color:#a7f3d0; text-decoration:underline;"">click here to launch</a>.';
    }

    async function dispatchViaApi() {
      const phone = document.getElementById('phoneInput').value.trim();
      const message = document.getElementById('messageInput').value.trim();
      const res = document.getElementById('resultBox');

      if (!phone) {
        alert('Please enter a valid phone number.');
        return;
      }

      res.className = 'result-box result-info';
      res.style.display = 'block';
      res.textContent = 'Submitting message to DentistAPI gateway...';

      try {
        const response = await fetch('/api/whatsapp/test-send', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ phone, message })
        });
        const data = await response.json();
        
        const directUrl = data.directWhatsAppUrl || ('https://wa.me/' + normalizePhone(phone) + '?text=' + encodeURIComponent(message));
        
        if (data.success) {
          res.className = 'result-box result-success';
          res.innerHTML = '✅ <strong>Sent via Gateway:</strong> ' + (data.message || 'Dispatched successfully.') + ' <a href=""' + directUrl + '"" target=""_blank"" style=""color:#a7f3d0; margin-left:8px; text-decoration:underline;"">Open in wa.me</a>';
        } else {
          res.className = 'result-box result-info';
          res.innerHTML = 'ℹ️ <strong>Queued in Clinical DB:</strong> ' + (data.message || 'Ready for 1-Click dispatch.') + '<br/><a href=""' + directUrl + '"" target=""_blank"" class=""btn btn-primary"" style=""margin-top:8px; display:inline-flex; font-size:12px; padding:6px 12px;"">🚀 Open Directly in WhatsApp (wa.me)</a>';
        }
        fetchLogs();
      } catch (err) {
        const directUrl = 'https://wa.me/' + normalizePhone(phone) + '?text=' + encodeURIComponent(message);
        res.className = 'result-box result-info';
        res.innerHTML = 'ℹ️ <strong>Direct Link Ready:</strong> <a href=""' + directUrl + '"" target=""_blank"" style=""color:#a5f3fc; text-decoration:underline;"">Click here to open in WhatsApp</a>';
      }
    }

    async function fetchLogs() {
      const tbody = document.getElementById('logsTableBody');
      try {
        const res = await fetch('/api/whatsapp/logs?limit=15');
        if (!res.ok) throw new Error('HTTP ' + res.status);
        const logs = await res.json();
        if (!logs || logs.length === 0) {
          tbody.innerHTML = '<tr><td colspan=""6"" style=""text-align:center; color:var(--text-dim); padding:20px;"">No messages recorded in database yet.</td></tr>';
          return;
        }

        tbody.innerHTML = logs.map(m => {
          let badgeClass = 'badge-pending';
          if (m.status === 'Sent') badgeClass = 'badge-sent';
          if (m.status === 'Failed') badgeClass = 'badge-failed';

          const dateStr = m.createdDate ? new Date(m.createdDate).toLocaleString() : 'N/A';
          const waUrl = 'https://wa.me/' + (m.recipientPhone || '') + '?text=' + encodeURIComponent(m.messageBody || '');

          return '<tr>' +
            '<td class=""mono"">#' + m.messageID + '</td>' +
            '<td class=""mono"">+' + (m.recipientPhone || 'N/A') + '</td>' +
            '<td>' + (m.messageType || 'General') + '</td>' +
            '<td><span class=""' + badgeClass + '"">' + (m.status || 'Queued') + '</span></td>' +
            '<td>' + dateStr + '</td>' +
            '<td><a href=""' + waUrl + '"" target=""_blank"" class=""btn btn-outline"" style=""padding:4px 8px; font-size:11px;"">Open wa.me</a></td>' +
          '</tr>';
        }).join('');
      } catch (err) {
        tbody.innerHTML = '<tr><td colspan=""6"" style=""text-align:center; color:var(--text-dim); padding:20px;"">Could not load logs (Table may be initializing).</td></tr>';
      }
    }

    // Auto-init
    document.addEventListener('DOMContentLoaded', () => {
      setTemplate('reminder');
      fetchLogs();
    });
  </script>
</body>
</html>";
        }
    }
}
