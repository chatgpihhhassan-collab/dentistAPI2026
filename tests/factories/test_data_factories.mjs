/**
 * 🏭 Dentia Synthetic Test Data Factories & Fixtures
 * Generates HIPAA/GDPR-compliant synthetic dental records. ZERO real patient data.
 */

export const EdgeCasePayloads = {
  EMPTY_STRING: "",
  WHITESPACE_ONLY: "   \t\r\n   ",
  NULL_VALUE: null,
  UNDEFINED_VALUE: undefined,
  VERY_LONG_STRING_10K: "A".repeat(10000),
  VERY_LONG_STRING_50K: "X".repeat(50000),
  NEGATIVE_ONE: -1,
  NEGATIVE_LARGE: -99999999,
  ZERO: 0,
  TINY_DECIMAL: 0.0000001,
  LARGE_NUMBER: 999999999999,
  NOT_A_NUMBER: NaN,
  INFINITY_VALUE: Infinity,
  
  // Internationalization, Urdu & Multi-lingual vectors
  URDU_NASTALIQ: "مریض کے دائیں داڑھ میں شدید درد ہے اور سوجن بھی ہے",
  ROMAN_URDU: "Dant number 16 me shadeed keera laga hua hai aur cold se dard hota hai",
  EMOJI_CLINICAL: "Tooth 14 broken 🦷💥 severe sensitivity ❄️⚡",
  ARABIC_RTL: "علاج قناة الجذر للسن رقم 26",
  
  // Security XSS & SQLi attack vectors
  XSS_SCRIPT_TAG: "<script>alert('XSS_ATTACK')</script>",
  XSS_IMG_ONERROR: "<img src=x onerror=alert(document.cookie)>",
  XSS_JAVASCRIPT_URI: "javascript:/*--></title></style></textarea></script></xmp><svg/onload='+/'/+/onmouseover=1/+/[*/[]/+alert(1)//'>",
  SQLI_DROP_TABLE: "'; DROP TABLE [dentist].[Patients];--",
  SQLI_AUTH_BYPASS: "' OR '1'='1' --",
  SQLI_UNION_SELECT: "' UNION SELECT DoctorID, PasswordHash, Username, NULL, NULL FROM [dentist].[Doctors]--",
  PATH_TRAVERSAL_DOT_DOT: "../../../../../Windows/System32/drivers/etc/hosts",
  PATH_TRAVERSAL_NULL_BYTE: "radiograph.png\0.exe",
  
  // Date & calendar boundary vectors
  LEAP_YEAR_FEB_29: "2028-02-29T10:00:00Z",
  YEAR_END_BOUNDARY: "2026-12-31T23:59:59Z",
  YEAR_START_BOUNDARY: "2027-01-01T00:00:01Z",
  PAST_DATE_24H_AGO: new Date(Date.now() - 24 * 3600 * 1000).toISOString(),
  FUTURE_DATE_NEXT_WEEK: new Date(Date.now() + 7 * 24 * 3600 * 1000).toISOString(),
  OUTSIDE_HOURS_MIDNIGHT: "2026-10-20T02:00:00Z",
  OUTSIDE_HOURS_EARLY: "2026-10-20T06:30:00Z",
  OUTSIDE_HOURS_LATE: "2026-10-20T21:45:00Z"
};

let patientCounter = 1000;
let appointmentCounter = 2000;
let invoiceCounter = 3000;

export function buildSyntheticPatient(overrides = {}) {
  const id = ++patientCounter;
  return {
    patientID: id,
    doctorId: 1,
    firstName: "Ahmed",
    lastName: "Razzaq",
    dob: "1992-06-18",
    gender: "Male",
    phone: `+92300${String(id).padStart(7, '0')}`,
    email: `patient.${id}@synthetic-dentia.test`,
    cnic: `35201-${String(id).padStart(7, '0')}-1`,
    address: "Street 14, Sector F-8/2, Islamabad",
    city: "Islamabad",
    country: "PK",
    medicalHistory: "No systemic conditions, penicillin allergy confirmed",
    allergiesConfirmed: true,
    dentitionType: "Adult",
    referenceNumber: `DEN-2026-${String(id).padStart(5, '0')}`,
    createdAt: new Date().toISOString(),
    ...overrides
  };
}

export function buildSyntheticAppointment(overrides = {}) {
  const id = ++appointmentCounter;
  // Default to tomorrow 10:00 AM (strictly within 08:00 to 20:00 working hours)
  const tomorrow = new Date();
  tomorrow.setDate(tomorrow.getDate() + 1);
  tomorrow.setHours(10, 0, 0, 0);

  return {
    appointmentId: id,
    doctorId: 1,
    fullName: "Amina Tariq",
    phone: "+923215551234",
    email: "amina.tariq@synthetic-dentia.test",
    preferredDate: tomorrow.toISOString(),
    status: "Pending",
    reason: "Routine 6-month preventive checkup and scaling",
    notes: "Patient requests topical anesthetic before examination",
    ...overrides
  };
}

export function buildSyntheticInvoice(overrides = {}) {
  const id = ++invoiceCounter;
  const subtotal = 15000.00;
  const discount = 1500.00;
  const tax = 0.00;
  const total = subtotal - discount + tax;

  return {
    invoiceId: id,
    invoiceNumber: `INV-2026-${String(id).padStart(5, '0')}`,
    patientId: 1001,
    doctorId: 1,
    currency: "PKR",
    issueDate: new Date().toISOString(),
    status: "Issued",
    subtotalAmount: subtotal,
    discountAmount: discount,
    taxAmount: tax,
    totalAmount: total,
    paidAmount: 0.00,
    balanceAmount: total,
    items: [
      {
        procedureCode: "D0150",
        procedureName: "Comprehensive Oral Evaluation",
        toothNumber: null,
        surface: null,
        unitPrice: 5000.00,
        discount: 500.00,
        total: 4500.00
      },
      {
        procedureCode: "D1110",
        procedureName: "Adult Prophylaxis & Scaling",
        toothNumber: null,
        surface: null,
        unitPrice: 10000.00,
        discount: 1000.00,
        total: 9000.00
      }
    ],
    ...overrides
  };
}

export function buildSyntheticDoctor(overrides = {}) {
  return {
    doctorId: 1,
    username: "dr.sarah.dentist",
    firstName: "Sarah",
    lastName: "Khan",
    email: "dr.sarah@dentia.clinic",
    specialization: "Endodontics & Restorative Dentistry",
    region: "PK",
    isSuperAdmin: false,
    isActive: true,
    organizations: [1],
    ...overrides
  };
}

export function buildSyntheticSuperAdmin(overrides = {}) {
  return buildSyntheticDoctor({
    doctorId: 99,
    username: "superadmin.dentia",
    firstName: "Executive",
    lastName: "Administrator",
    email: "admin@dentia.global",
    isSuperAdmin: true,
    ...overrides
  });
}

export function buildSyntheticRadiograph(overrides = {}) {
  return {
    id: 501,
    patientId: 1001,
    doctorId: 1,
    toothKey: "16",
    modality: "Periapical RVG",
    imagePath: "/uploads/xrays/nanopix_1001_sample.png",
    sensorSN: "PF640002T0315211340",
    resolution: "1300x1800",
    mimeType: "image/png",
    isEncrypted: true,
    uploadedAt: new Date().toISOString(),
    ...overrides
  };
}
