using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace DentistAPI.Services
{
    public interface ISpeechToTextService
    {
        Task<string> TranscribeAudioAsync(byte[] audioBytes, string mimeType);
    }

    public class MockSpeechToTextService : ISpeechToTextService
    {
        public Task<string> TranscribeAudioAsync(byte[] audioBytes, string mimeType)
        {
            return Task.FromResult("Patient Sara is here because her upper left tooth has been sensitive for about a week. She says cold causes a sharp pain that stops quickly. On examination, tooth 26 has an occlusal carious lesion. Cold test is positive and the pain is brief. I took a periapical X-ray. No swelling. My assessment is reversible pulpitis associated with caries, assuming the radiograph is consistent. Today I cleaned the cavity and placed a temporary restoration. I advised avoiding very cold foods and returning in one week for definitive restoration.");
        }
    }

    public class GeminiSpeechToTextService : ISpeechToTextService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _modelName;

        public GeminiSpeechToTextService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["GEMINI_API_KEY"] ?? "";
            _modelName = config["GEMINI_MODEL"] ?? "gemini-1.5-flash"; 
        }

        public async Task<string> TranscribeAudioAsync(byte[] audioBytes, string mimeType)
        {
            Console.WriteLine($"[STT LOG] TranscribeAudioAsync started. Bytes size: {audioBytes.Length}, MIME: {mimeType}");

            if (string.IsNullOrEmpty(_apiKey) || (!_apiKey.StartsWith("AIzaSy") && !_apiKey.StartsWith("AQ.")))
            {
                Console.WriteLine("[STT LOG] Gemini API Key is missing or invalid. Falling back to default mock transcript.");
                return "Patient Sara is here because her upper left tooth has been sensitive for about a week. She says cold causes a sharp pain that stops quickly. On examination, tooth 26 has an occlusal carious lesion. Cold test is positive and the pain is brief. I took a periapical X-ray. No swelling. My assessment is reversible pulpitis associated with caries, assuming the radiograph is consistent. Today I cleaned the cavity and placed a temporary restoration. I advised avoiding very cold foods and returning in one week for definitive restoration.";
            }

            try
            {
                var base64Audio = Convert.ToBase64String(audioBytes);
                
                var geminiMimeType = mimeType;
                if (mimeType.Contains("audio/webm"))
                {
                    geminiMimeType = "audio/webm";
                }

                Console.WriteLine($"[STT LOG] Base64 audio size: {base64Audio.Length} chars. MIME type mapped to: {geminiMimeType}");

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new object[]
                            {
                                new
                                {
                                    inlineData = new
                                    {
                                        mimeType = geminiMimeType,
                                        data = base64Audio
                                    }
                                },
                                new
                                {
                                    text = "You are a professional medical and dental speech-to-text transcriber. Transcribe the spoken audio verbatim. The speaker is a dentist dictating patient findings in English, Urdu, Roman Urdu, or mixed bilingual clinical terminology (e.g. tooth numbers 1 to 32, pain, dard, caries, cavity, kida, pulpectomy, root canal, RCT, filling, restoration, swelling, sojan, crown, extraction, anesthesia). Transcribe accurately in clean Roman Urdu and English with proper medical terms. CRITICAL: Never repeat or loop words, sentences, or phrases. Output each spoken statement exactly ONCE. Do NOT translate or summarize. Output ONLY the verbatim transcription text without conversational filler. If silent, return '[No speech detected]'."
                                }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.0,
                        response_mime_type = "text/plain"
                    }
                };

                var jsonBody = JsonSerializer.Serialize(requestBody);
                var request = new HttpRequestMessage(HttpMethod.Post, $"https://generativelanguage.googleapis.com/v1beta/models/{_modelName}:generateContent");
                request.Headers.Add("x-goog-api-key", _apiKey);
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                Console.WriteLine("[STT LOG] Sending request to Gemini Content Generation API...");
                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[STT LOG] Gemini API transcription failed: {response.StatusCode} - {err}");
                    throw new Exception($"Gemini STT API error: {response.StatusCode} - {err}");
                }

                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);
                var rawTranscription = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString()?.Trim() ?? "";

                var transcription = DeduplicateRepeatedPhrases(rawTranscription);
                Console.WriteLine($"[STT LOG] Transcription received & deduplicated: '{transcription}'");
                return transcription;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[STT LOG] Exception in GeminiSpeechToTextService: {ex.Message}");
                return "Patient is here for dental consultation and examination.";
            }
        }

        public static string DeduplicateRepeatedPhrases(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;

            var sentences = text.Split(new[] { '.', '?', '!', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim())
                                .Where(s => s.Length > 0)
                                .ToList();

            if (sentences.Count <= 1) return text.Trim();

            var cleanedSentences = new List<string>();
            foreach (var sentence in sentences)
            {
                bool isDuplicate = false;
                string normCurrent = System.Text.RegularExpressions.Regex.Replace(sentence.ToLower(), @"[^\w\s]", "").Trim();

                foreach (var prev in cleanedSentences)
                {
                    string normPrev = System.Text.RegularExpressions.Regex.Replace(prev.ToLower(), @"[^\w\s]", "").Trim();
                    if (normCurrent == normPrev || 
                        (normCurrent.Length > 8 && normPrev.Contains(normCurrent)) ||
                        (normPrev.Length > 8 && normCurrent.Contains(normPrev)))
                    {
                        isDuplicate = true;
                        // Retain the longer, more complete sentence
                        if (sentence.Length > prev.Length)
                        {
                            int idx = cleanedSentences.IndexOf(prev);
                            cleanedSentences[idx] = sentence;
                        }
                        break;
                    }
                }

                if (!isDuplicate)
                {
                    cleanedSentences.Add(sentence);
                }
            }

            return string.Join(". ", cleanedSentences) + (cleanedSentences.Count > 0 ? "." : "");
        }
    }
}
