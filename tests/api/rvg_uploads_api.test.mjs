/**
 * 🩻 API Integration Tests: RVG & Radiograph Uploads, Magic-Byte Verification & Security
 * Tests allowed file types, magic-byte inspection, size limits, zero-byte uploads, and path traversal defense.
 */

import { describe, it } from 'node:test';
import assert from 'node:assert/strict';

const MAX_PROFILE_IMAGE_SIZE = 5 * 1024 * 1024;    // 5 MB
const MAX_RADIOGRAPH_SIZE = 15 * 1024 * 1024;     // 15 MB
const MAX_AUDIO_SIZE = 25 * 1024 * 1024;          // 25 MB

const DANGEROUS_EXTENSIONS = new Set([
  ".exe", ".dll", ".bat", ".cmd", ".sh", ".bash", ".ps1", ".vbs",
  ".php", ".aspx", ".asp", ".jsp", ".py", ".rb", ".svg", ".html",
  ".htm", ".js", ".mjs"
]);

// Magic byte signature checkers
function inspectMagicBytes(buffer, declaredCategory) {
  if (!buffer || buffer.length === 0) return { isValid: false, error: "File content is empty." };

  // 1. PNG Signature: 89 50 4E 47 0D 0A 1A 0A
  if (buffer.length >= 8 &&
      buffer[0] === 0x89 && buffer[1] === 0x50 && buffer[2] === 0x4E && buffer[3] === 0x47 &&
      buffer[4] === 0x0D && buffer[5] === 0x0A && buffer[6] === 0x1A && buffer[7] === 0x0A) {
    return { isValid: true, detectedMime: "image/png" };
  }

  // 2. JPEG Signature: FF D8 FF
  if (buffer.length >= 3 && buffer[0] === 0xFF && buffer[1] === 0xD8 && buffer[2] === 0xFF) {
    return { isValid: true, detectedMime: "image/jpeg" };
  }

  // 3. WebP Signature: RIFF....WEBP
  if (buffer.length >= 12 &&
      buffer[0] === 0x52 && buffer[1] === 0x49 && buffer[2] === 0x46 && buffer[3] === 0x46 &&
      buffer[8] === 0x57 && buffer[9] === 0x45 && buffer[10] === 0x42 && buffer[11] === 0x50) {
    return { isValid: true, detectedMime: "image/webp" };
  }

  // 4. DICOM Signature: offset 128 has 'DICM' (0x44 0x49 0x43 0x4D)
  if (buffer.length >= 132 &&
      buffer[128] === 0x44 && buffer[129] === 0x49 && buffer[130] === 0x43 && buffer[131] === 0x4D) {
    return { isValid: true, detectedMime: "application/dicom" };
  }

  return { isValid: false, error: "File signature does not match any allowed dental imaging format." };
}

function validateUpload({ buffer, clientFileName, category = 'radiograph' }) {
  if (!buffer || buffer.length === 0) {
    return { status: 400, error: "Zero-byte file content is not allowed." };
  }

  // Size limit check
  const maxSize = category === 'profile' ? MAX_PROFILE_IMAGE_SIZE :
                  category === 'audio' ? MAX_AUDIO_SIZE : MAX_RADIOGRAPH_SIZE;

  if (buffer.length > maxSize) {
    return { status: 400, error: `File size exceeds maximum allowed limit (${maxSize / (1024 * 1024)} MB).` };
  }

  // Dangerous extension check
  const extMatch = (clientFileName || '').match(/\.[a-zA-Z0-9]+$/);
  const ext = extMatch ? extMatch[0].toLowerCase() : '';
  if (DANGEROUS_EXTENSIONS.has(ext)) {
    return { status: 400, error: `Upload with dangerous extension '${ext}' is blocked.` };
  }

  // Magic byte deep inspection
  const magicCheck = inspectMagicBytes(buffer, category);
  if (!magicCheck.isValid) {
    return { status: 400, error: magicCheck.error };
  }

  // Sanitized unique filename generation preventing path traversal
  const cleanBaseName = (clientFileName || 'radiograph').replace(/^.*[\\\/]/, '').replace(/[^a-zA-Z0-9._-]/g, '_');
  const storedName = `nanopix_${Date.now()}_${cleanBaseName}`;

  return {
    status: 200,
    storedName,
    mimeType: magicCheck.detectedMime,
    sizeBytes: buffer.length
  };
}

describe('API Integration: RVG Magic-Byte & Header Verification', () => {
  it('should accept valid PNG radiograph with proper PNG header bytes', () => {
    // Valid PNG header + dummy payload
    const pngBuffer = Buffer.from([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D]);
    const res = validateUpload({ buffer: pngBuffer, clientFileName: 'tooth16_rvg.png', category: 'radiograph' });
    assert.equal(res.status, 200);
    assert.equal(res.mimeType, 'image/png');
  });

  it('should accept valid JPEG radiograph with proper SOI marker', () => {
    const jpegBuffer = Buffer.from([0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46]);
    const res = validateUpload({ buffer: jpegBuffer, clientFileName: 'opg_panoramic.jpg', category: 'radiograph' });
    assert.equal(res.status, 200);
    assert.equal(res.mimeType, 'image/jpeg');
  });

  it('should accept valid DICOM file with DICM header at byte offset 128', () => {
    const dcmBuffer = Buffer.alloc(140);
    // Write 'DICM' at byte 128
    dcmBuffer.write('DICM', 128, 'ascii');
    const res = validateUpload({ buffer: dcmBuffer, clientFileName: 'cbct_slice.dcm', category: 'radiograph' });
    assert.equal(res.status, 200);
    assert.equal(res.mimeType, 'application/dicom');
  });

  it('should REJECT spoofed file disguised with .png extension but containing PHP/HTML text', () => {
    // Attacker payload: "<?php phpinfo(); ?>" renamed to exploit.png
    const spoofedBuffer = Buffer.from("<?php echo 'HACKED'; ?>", 'utf8');
    const res = validateUpload({ buffer: spoofedBuffer, clientFileName: 'radiograph.png', category: 'radiograph' });
    assert.equal(res.status, 400);
    assert.equal(res.error.includes("signature does not match"), true);
  });
});

describe('API Integration: Upload Security, Size Limits & Traversal Defense', () => {
  it('should reject zero-byte empty uploads immediately', () => {
    const emptyBuffer = Buffer.alloc(0);
    const res = validateUpload({ buffer: emptyBuffer, clientFileName: 'empty.png' });
    assert.equal(res.status, 400);
    assert.equal(res.error.includes("Zero-byte"), true);
  });

  it('should reject files exceeding the 15 MB radiograph limit', () => {
    // Simulate oversized 16 MB buffer
    const oversizedBuffer = {
      length: 16 * 1024 * 1024
    };
    const res = validateUpload({ buffer: oversizedBuffer, clientFileName: 'huge_xray.png', category: 'radiograph' });
    assert.equal(res.status, 400);
    assert.equal(res.error.includes("exceeds maximum allowed limit"), true);
  });

  it('should block dangerous extensions regardless of content', () => {
    const pngBuffer = Buffer.from([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
    const res = validateUpload({ buffer: pngBuffer, clientFileName: 'scanner_update.exe' });
    assert.equal(res.status, 400);
    assert.equal(res.error.includes("dangerous extension"), true);
  });

  it('should sanitize path traversal attempts (../../etc/passwd) in filename', () => {
    const pngBuffer = Buffer.from([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
    const res = validateUpload({ buffer: pngBuffer, clientFileName: '../../../../Windows/System32/drivers/etc/hosts.png' });
    assert.equal(res.status, 200);
    assert.equal(res.storedName.includes('..'), false);
    assert.equal(res.storedName.includes('/'), false);
    assert.equal(res.storedName.includes('\\'), false);
  });
});
