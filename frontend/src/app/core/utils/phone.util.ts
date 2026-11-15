const EGYPT_MOBILE_PATTERN = /^01[0125]\d{8}$/;

/**
 * Normalizes any Egyptian mobile input into canonical E.164 format (`+201XXXXXXXXX`)
 * or returns `null` if invalid, matching backend `PhoneNumber.TryParseEgyptianMobile`.
 */
export function normalizeEgyptianMobile(raw: string | null | undefined): string | null {
  if (!raw) return null;

  const cleaned = convertArabicDigitsToAscii(raw).replace(/[\s\-()]+/g, '').trim();
  if (!cleaned) return null;

  let local11 = cleaned;

  if (local11.startsWith('+20')) {
    local11 = `0${local11.slice(3)}`;
  } else if (local11.startsWith('0020')) {
    local11 = `0${local11.slice(4)}`;
  } else if (local11.startsWith('20') && local11.length === 12) {
    local11 = `0${local11.slice(2)}`;
  } else if (/^1[0125]\d{8}$/.test(local11)) {
    local11 = `0${local11}`;
  }

  if (!EGYPT_MOBILE_PATTERN.test(local11)) {
    return null;
  }

  return `+20${local11.slice(1)}`;
}

/**
 * Formats `+201012345678` or `01012345678` into spaced display digits `010 1234 5678`.
 */
export function formatLocalEgyptianPhone(raw: string | null | undefined): string {
  const normalized = normalizeEgyptianMobile(raw);
  if (!normalized) {
    return raw ?? '';
  }
  const local = `0${normalized.slice(3)}`;
  return `${local.slice(0, 3)} ${local.slice(3, 7)} ${local.slice(7)}`;
}

/**
 * Extracts local 10 digits after `+20` (e.g. `1012345678`) or raw digits for the `+20` badge input.
 */
export function stripCountryPrefixForInput(raw: string | null | undefined): string {
  if (!raw) return '';
  const cleaned = convertArabicDigitsToAscii(raw).replace(/[\s\-()]+/g, '').trim();
  if (cleaned.startsWith('+20')) {
    return cleaned.slice(3);
  }
  if (cleaned.startsWith('0020')) {
    return cleaned.slice(4);
  }
  if (cleaned.startsWith('20') && cleaned.length >= 12) {
    return cleaned.slice(2);
  }
  return cleaned;
}

export function convertArabicDigitsToAscii(value: string): string {
  return value
    .replace(/[٠-٩]/g, (d) => String(d.charCodeAt(0) - 0x0660))
    .replace(/[۰-۹]/g, (d) => String(d.charCodeAt(0) - 0x06f0));
}
