# Anam AI: Complete Technical Evaluation, Pricing & Integration Guide for Dentia

> **Project:** Dentia — Dental Clinic & Patient-Provider Workspace  
> **Topic:** Anam AI (`anam.ai`) Digital Human Evaluation, Free Tier Verification, Operatory Workflow & Implementation  
> **Date:** September 2026  

---

## 1. Executive Summary & Pricing: Is Anam AI Free?

### The Verdict: **Freemium (Free Tier Available for Prototyping)**

| Feature | Status | Details |
| :--- | :--- | :--- |
| **Developer Free Tier** | **Yes (Free)** | Provides **~30 to 60 free minutes per month** of real-time conversational avatar streaming upon signup. No credit card required for initial developer sandbox. |
| **Commercial / Production** | **Paid (Usage-Based)** | Billed per minute (typically ~$0.15 to ~$0.25/minute depending on volume and subscription plan like Starter, Explorer, or Professional). |
| **Concurrency on Free Tier** | 1 Active Session | Suitable for 1 doctor or patient testing at a time. |
| **Overage Protection** | Hard Cap Option | You can set spend limits to $0 in the dashboard so you never get charged unexpectedly. |

### Can Doctors Use It Completely Free?
**Yes, through two practical methods:**

1. **Strategic Free Quota Usage:**  
   Because 30-60 minutes equates to roughly **30 to 60 brief consultations** (1 minute per patient intake or operatory query), a small dental clinic or individual doctor can utilize the monthly free tier for pilot demonstrations, interactive patient intake, or daily morning briefings without paying anything.
2. **Hybrid Free Architecture (Recommended):**  
   - **For Doctor's Continuous Operatory Hands-Free Scribing:** Use Dentia’s built-in **Web Speech API (`aiVoiceAssistant.js`) + Three.js 3D Odontogram**, which is **100% FREE forever with zero limits**.  
   - **For Patient Welcome & Consultation:** Trigger the **Anam AI Digital Human Avatar** only when needed (e.g., when a new patient clicks "Talk to AI Dental Advisor"), staying well within the free monthly quota.

---

## 2. What is Anam AI and How Does It Work?

**Anam AI** (`anam.ai`) is an ultra-low latency (<600ms) real-time interactive **Digital Human Video Streaming Engine**. Unlike traditional video generators that take minutes to render a static MP4 file (like HeyGen or D-ID), Anam works live via **WebRTC** (similar to a Zoom/Google Meet video call).

### End-to-End Pipeline
```
[User / Doctor / Patient]
         │ (Speaks into microphone)
         ▼
[Browser Microphone: WebRTC Audio Stream]
         │
         ▼
[Anam AI Real-Time Cloud Engine]
   ├─ 1. Whisper / Fast STT (Speech to Text)
   ├─ 2. Clinical LLM (Understands query & formulates response)
   ├─ 3. Expressive Neural TTS (Synthesizes natural doctor voice)
   └─ 4. Real-Time Face NeRF / Lip-Sync Synthesis
         │
         ▼ (WebRTC Video + Audio Stream, <600ms latency)
[React `<video>` Element in Dentia Frontend]
         │
         ▼
[Real-Time Visual Talking Doctor Avatar on Screen]
```

---

## 3. How It Helps the Doctor in Dentia

### 1. Hands-Free Operatory Voice Partner (Zero Sterility Breach)
While wearing sterile surgical gloves during a procedure, the dentist cannot touch the keyboard or mouse. The doctor can speak directly to the Anam AI avatar:
* *"Anam, check patient's last logged blood pressure and penicillin allergies."*
* Anam's avatar instantly speaks back with realistic facial expressions: *"Doctor, patient Ali has stage 1 hypertension (138/88) and confirmed Penicillin anaphylaxis."*

### 2. Autonomous Patient Intake & Triage (Kiosk / Reception)
When a new patient arrives at `/new-patient`:
* Instead of filling out complex medical forms, the patient talks directly with the Anam AI virtual dental nurse.
* Anam asks: *"Where are you feeling pain? Does it hurt when drinking cold water?"*
* Anam automatically populates the form fields and alerts the dentist before the patient enters the operatory.

### 3. Post-Operative Treatment Explainer
* After a surgical extraction or bone graft, patients often forget verbal instructions.
* Anam AI can present personalized aftercare instructions to the patient on a clinic tablet or their phone:
  *"Hello Ali, Dr. Sarah completed your bone graft on tooth 16. Please avoid using straws and do not rinse vigorously for 24 hours."*

---

## 4. Architectural Integration in Dentia

### Backend Session Token Proxy (`DentistAPIClone / ASP.NET Core .NET 10`)
To keep your `ANAM_API_KEY` secure and never expose it to the public browser, your .NET backend exchanges the API key for an ephemeral, short-lived session token.

```csharp
// File: Controllers/AnamSessionController.cs
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace DentistAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnamSessionController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public AnamSessionController(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpPost("token")]
        public async Task<IActionResult> GetSessionToken([FromBody] SessionRequest request)
        {
            var apiKey = _config["AnamAI:ApiKey"];
            var personaId = _config["AnamAI:PersonaId"] ?? "persona_doctor_sarah";

            var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anam.ai/v1/sessions/token");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            req.Content = JsonContent.Create(new
            {
                personaId = personaId,
                personaConfig = new
                {
                    systemPrompt = "You are Dr. Sarah, an expert dental AI assistant for Dentia Clinic. You assist dentists with tooth diagnoses and explain dental procedures to patients kindly and concisely."
                }
            });

            var res = await _httpClient.SendAsync(req);
            if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, "Failed to get Anam session");

            var json = await res.Content.ReadAsStringAsync();
            return Content(json, "application/json");
        }
    }

    public class SessionRequest { public string? PatientId { get; set; } }
}
```

---

## 5. Frontend React Component Implementation (`Dentistfrontend`)

Install the official SDK in `Dentistfrontend`:
```bash
npm install @anam-ai/js-sdk
```

Create a reusable interactive assistant component:
```jsx
// File: src/components/ai/AnamDoctorAssistant.jsx
import React, { useEffect, useRef, useState } from 'react';
import { createClient } from '@anam-ai/js-sdk';
import { Mic, MicOff, Video, VideoOff, X, Sparkles, Volume2 } from 'lucide-react';

export default function AnamDoctorAssistant({ isOpen, onClose, doctorName = "Dr. Hassan" }) {
  const videoRef = useRef(null);
  const [anamClient, setAnamClient] = useState(null);
  const [isSessionActive, setIsSessionActive] = useState(false);
  const [isMuted, setIsMuted] = useState(false);
  const [statusText, setStatusText] = useState("Initializing Virtual Doctor...");

  useEffect(() => {
    if (!isOpen) {
      if (anamClient) {
        anamClient.stopSession();
        setIsSessionActive(false);
      }
      return;
    }

    let client = null;

    async function initAnam() {
      try {
        setStatusText("Requesting secure session token...");
        
        // Fetch session token from your ASP.NET backend
        const res = await fetch("https://your-dentist-api.com/api/anamsession/token", {
          method: "POST",
          headers: { "Content-Type": "application/json" }
        });
        const data = await res.json();
        const sessionToken = data.sessionToken;

        // Initialize Anam WebRTC Client
        client = createClient(sessionToken, {
          disableDefaultAudioOutput: false
        });

        // Event Listeners
        client.addListener('CONNECTION_ESTABLISHED', () => {
          setStatusText("Dr. Sarah is listening live (WebRTC Active)");
          setIsSessionActive(true);
        });

        client.addListener('TALKING_STARTED', () => {
          setStatusText("Dr. Sarah is speaking...");
        });

        client.addListener('TALKING_ENDED', () => {
          setStatusText("Dr. Sarah is listening...");
        });

        // Attach video stream to DOM
        if (videoRef.current) {
          await client.streamToVideoElement(videoRef.current);
        }

        setAnamClient(client);
      } catch (err) {
        console.error("Anam AI error:", err);
        setStatusText("Could not connect to Anam AI. Using offline fallback.");
      }
    }

    initAnam();

    return () => {
      if (client) {
        client.stopSession();
      }
    };
  }, [isOpen]);

  const toggleMute = () => {
    if (anamClient) {
      if (isMuted) {
        anamClient.unmuteMicrophone();
        setIsMuted(false);
      } else {
        anamClient.muteMicrophone();
        setIsMuted(true);
      }
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed bottom-6 right-6 z-50 w-96 rounded-2xl bg-slate-900/95 border border-teal-500/30 shadow-2xl backdrop-blur-xl overflow-hidden text-white flex flex-col">
      {/* Header */}
      <div className="flex items-center justify-between px-4 py-3 bg-slate-800/80 border-b border-slate-700/50">
        <div className="flex items-center gap-2">
          <Sparkles className="w-5 h-5 text-teal-400 animate-pulse" />
          <span className="font-semibold text-sm">Dr. Sarah — AI Operatory Copilot</span>
        </div>
        <button onClick={onClose} className="p-1 text-slate-400 hover:text-white rounded-lg hover:bg-slate-700">
          <X className="w-4 h-4" />
        </button>
      </div>

      {/* Video Streaming Window */}
      <div className="relative w-full h-64 bg-slate-950 flex items-center justify-center">
        <video 
          ref={videoRef} 
          autoPlay 
          playsInline 
          className="w-full h-full object-cover"
        />
        {!isSessionActive && (
          <div className="absolute inset-0 flex flex-col items-center justify-center bg-slate-950/80 p-4 text-center">
            <div className="w-8 h-8 border-2 border-teal-400 border-t-transparent rounded-full animate-spin mb-3"></div>
            <p className="text-xs text-slate-300">{statusText}</p>
          </div>
        )}
      </div>

      {/* Control Bar */}
      <div className="p-3 bg-slate-800/90 flex items-center justify-between border-t border-slate-700/50 text-xs">
        <span className="text-slate-300 truncate max-w-[180px]">{statusText}</span>
        <div className="flex items-center gap-2">
          <button 
            onClick={toggleMute}
            className={`p-2 rounded-xl transition ${isMuted ? 'bg-red-500/20 text-red-400' : 'bg-teal-500/20 text-teal-400'}`}
            title={isMuted ? "Unmute Mic" : "Mute Mic"}
          >
            {isMuted ? <MicOff className="w-4 h-4" /> : <Mic className="w-4 h-4" />}
          </button>
          <button 
            onClick={() => { anamClient?.stopSession(); onClose(); }}
            className="px-3 py-1.5 rounded-xl bg-red-600/80 hover:bg-red-600 text-white font-medium transition"
          >
            End Call
          </button>
        </div>
      </div>
    </div>
  );
}
```

---

## 6. Comparison: Anam AI vs. Built-In Dentia Voice AI

| Capability | Anam AI (`anam.ai`) | Built-In Dentia Solution (`aiVoiceAssistant.js`) |
| :--- | :--- | :--- |
| **Visual Avatar** | Photorealistic interactive human face (NeRF / Video) | 3D Interactive Odontogram (Three.js Tooth Mesh) |
| **Cost** | Free for 30-60 mins/month, then ~$0.15-$0.25/min | **100% Free Forever ($0, Unlimited usage)** |
| **Network Requirement**| Requires broadband connection for WebRTC video | Works completely inside browser, minimal data |
| **Latency** | ~500ms - 800ms WebRTC roundtrip | **< 150ms Instant Local Audio Processing** |
| **Best Purpose** | Patient Welcome, Video Consultations, Virtual Nurse | Hands-free Operatory Scribing & Tooth Charting |

---

## 7. Recommended Roadmap for Dentia

1. **Keep Built-In Voice Free Scribe as Default:**  
   The operatory chart updates, 3D odontogram shaders, and SOAP notes should continue running on the native Web Speech API + Three.js to guarantee **unlimited, zero-cost operation for every doctor**.
2. **Use Anam AI as a Premium Virtual Avatar Add-On:**  
   Sign up on [anam.ai](https://anam.ai) to claim the monthly free developer minutes. Embed the `AnamDoctorAssistant` component in the Patient Landing Page or Appointment Booking modal as a "Virtual Consultation" feature.
3. **Set Hard Cost Limits:**  
   In the Anam dashboard, set spending caps to $0 so the system automatically falls back to standard voice mode when free minutes expire.
