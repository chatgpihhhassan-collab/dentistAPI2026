/**
 * 🤖 Mock AI Provider for Dentia Automated Testing
 * Simulates Gemini and Groq AI responses without making network calls to real providers.
 * Supports:
 * - Deterministic valid responses (SOAP notes, radiograph analyses, treatment plans)
 * - Malformed JSON responses
 * - Simulated network timeouts (HTTP 408 / Abort)
 * - Rate limiting (HTTP 429 Too Many Requests)
 * - Empty / null responses
 * - Hallucinated tooth numbers (e.g. Tooth 99)
 * - Prompt injection payload reflections
 */

export class MockDentalAiProvider {
  constructor(mode = 'VALID') {
    this.mode = mode; // 'VALID' | 'MALFORMED_JSON' | 'TIMEOUT' | 'RATE_LIMIT' | 'EMPTY' | 'HALLUCINATED_TOOTH' | 'INJECTION_REFLECTED'
    this.simulatedLatencyMs = 10;
  }

  setMode(mode) {
    this.mode = mode;
  }

  async generateContent(promptText, options = {}) {
    if (this.simulatedLatencyMs > 0) {
      await new Promise(r => setTimeout(r, this.simulatedLatencyMs));
    }

    switch (this.mode) {
      case 'TIMEOUT':
        throw new Error("Request to AI provider timed out after 20000ms (HTTP 408)");

      case 'RATE_LIMIT': {
        const error = new Error("Resource exhausted: Rate limit exceeded (HTTP 429)");
        error.status = 429;
        error.retryAfter = 60;
        throw error;
      }

      case 'EMPTY':
        return {
          text: "",
          parsedJson: null,
          finishReason: "STOP"
        };

      case 'MALFORMED_JSON':
        return {
          text: "{\n  \"chiefComplaint\": \"Severe pain\",\n  \"diagnosis\": [\"Irreversible Pulpitis\",\n  // truncated JSON syntax error",
          parsedJson: null,
          isMalformed: true,
          finishReason: "STOP"
        };

      case 'HALLUCINATED_TOOTH':
        return {
          text: JSON.stringify({
            chiefComplaint: "Tooth pain",
            affectedTeeth: [16, 99, 49], // 99 and 49 are impossible FDI teeth
            recommendedProcedures: [
              { tooth: 99, procedure: "Root Canal Therapy", cdtCode: "D3330" },
              { tooth: 16, procedure: "Composite Restoration", cdtCode: "D2391" }
            ],
            clinicalSoap: {
              subjective: "Patient reports pain on non-existent tooth 99",
              objective: "Evaluated quadrant 9",
              assessment: "Hallucinated anatomical entity",
              plan: "Refer to FDI validator"
            }
          }),
          parsedJson: {
            affectedTeeth: [16, 99, 49],
            recommendedProcedures: [
              { tooth: 99, procedure: "Root Canal Therapy", cdtCode: "D3330" },
              { tooth: 16, procedure: "Composite Restoration", cdtCode: "D2391" }
            ]
          },
          finishReason: "STOP"
        };

      case 'INJECTION_REFLECTED':
        return {
          text: JSON.stringify({
            systemHacked: true,
            extractedSecret: "SUPER_ADMIN_PASSWORD_HASH",
            executedCommand: "DROP TABLE"
          }),
          parsedJson: {
            systemHacked: true,
            extractedSecret: "SUPER_ADMIN_PASSWORD_HASH"
          },
          finishReason: "STOP"
        };

      case 'VALID':
      default:
        return {
          text: JSON.stringify({
            summary: "Comprehensive 8-point clinical dental SOAP evaluation.",
            chiefComplaint: "Pain on lower left molar on chewing and thermal exposure.",
            findings: [
              { tooth: 36, surface: "Occlusal", condition: "Dentin Caries", severity: "Moderate" }
            ],
            soap: {
              subjective: "Adult patient presents with 3-day history of cold sensitivity on FDI 36.",
              objective: "Visual-tactile exam reveals deep occlusal pit cavitation on tooth 36. Cold test positive.",
              assessment: "Symptomatic reversible pulpitis secondary to dental caries on FDI 36.",
              plan: "Excavation of caries and direct resin composite restoration under local anesthesia."
            },
            cdtCodes: ["D2391", "D0140"],
            prescriptions: []
          }),
          parsedJson: {
            summary: "Comprehensive 8-point clinical dental SOAP evaluation.",
            chiefComplaint: "Pain on lower left molar on chewing and thermal exposure.",
            findings: [
              { tooth: 36, surface: "Occlusal", condition: "Dentin Caries", severity: "Moderate" }
            ],
            soap: {
              subjective: "Adult patient presents with 3-day history of cold sensitivity on FDI 36.",
              objective: "Visual-tactile exam reveals deep occlusal pit cavitation on tooth 36. Cold test positive.",
              assessment: "Symptomatic reversible pulpitis secondary to dental caries on FDI 36.",
              plan: "Excavation of caries and direct resin composite restoration under local anesthesia."
            },
            cdtCodes: ["D2391", "D0140"]
          },
          finishReason: "STOP"
        };
    }
  }
}
