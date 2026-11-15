import { HttpErrorResponse } from '@angular/common/http';
import { UiLocale, ProblemDetails } from '../models/api.models';
import { TRANSLATIONS } from '../i18n/translations';

export interface ParsedApiError {
  status: number;
  message: string;
  fieldErrors: Record<string, string[]>;
  traceId: string | null;
  retryAfterSeconds: number | null;
  isSuspended: boolean;
  isInvalidCredentials: boolean;
  isDuplicateConflict: boolean;
  isMfaInvalid: boolean;
  remainingAttempts: number | null;
}

export function parseHttpError(error: unknown, locale: UiLocale): ParsedApiError {
  const t = TRANSLATIONS[locale];

  if (!(error instanceof HttpErrorResponse)) {
    return {
      status: 0,
      message: t.errGeneric,
      fieldErrors: {},
      traceId: null,
      retryAfterSeconds: null,
      isSuspended: false,
      isInvalidCredentials: false,
      isDuplicateConflict: false,
      isMfaInvalid: false,
      remainingAttempts: null,
    };
  }

  if (error.status === 0) {
    return {
      status: 0,
      message: t.errNetwork,
      fieldErrors: {},
      traceId: null,
      retryAfterSeconds: null,
      isSuspended: false,
      isInvalidCredentials: false,
      isDuplicateConflict: false,
      isMfaInvalid: false,
      remainingAttempts: null,
    };
  }

  const problem = normalizeProblemPayload(error.error);
  const rawFieldErrors = normalizeFieldErrors(problem?.errors);
  const rawDetail = (problem?.detail || problem?.title || '').trim();
  const traceId = typeof problem?.traceId === 'string' && problem.traceId.trim()
    ? problem.traceId.trim()
    : error.headers?.get('x-correlation-id') ?? null;

  const retryAfterHeader = error.headers?.get('Retry-After');
  const retryFromHeader = retryAfterHeader ? Number.parseInt(retryAfterHeader, 10) : Number.NaN;
  const retryFromBody = typeof problem?.retryAfterSeconds === 'number'
    ? problem.retryAfterSeconds
    : extractSecondsFromText(rawDetail);

  const retryAfterSeconds = Number.isFinite(retryFromBody) && retryFromBody !== null
    ? retryFromBody
    : (Number.isFinite(retryFromHeader) ? retryFromHeader : null);

  const lowerDetail = `${problem?.title ?? ''} ${rawDetail}`.toLowerCase();
  const isSuspended = lowerDetail.includes('suspend') || lowerDetail.includes('banned') || lowerDetail.includes('disabled');
  const isDuplicateConflict = error.status === 409;
  const isMfaInvalid = lowerDetail.includes('mfa') || lowerDetail.includes('recovery');
  const isInvalidCredentials =
    error.status === 401 ||
    lowerDetail.includes('invalid credential') ||
    lowerDetail.includes('incorrect') ||
    lowerDetail.includes('invalid otp');

  const remainingAttempts = extractRemainingAttempts(rawDetail);

  let message = rawDetail;
  if (!message) {
    const firstFieldError = Object.values(rawFieldErrors).flat()[0];
    if (firstFieldError) {
      message = firstFieldError;
    }
  }

  if (!message) {
    switch (error.status) {
      case 401:
        message = t.errUnauthorized;
        break;
      case 403:
        message = t.errForbidden;
        break;
      case 404:
        message = t.errNotFound;
        break;
      case 409:
        message = t.errConflict;
        break;
      case 429:
        message = t.errRateLimited;
        break;
      default:
        message = error.status >= 500 ? t.errServer : t.errGeneric;
    }
  }

  return {
    status: error.status,
    message,
    fieldErrors: rawFieldErrors,
    traceId,
    retryAfterSeconds,
    isSuspended,
    isInvalidCredentials,
    isDuplicateConflict,
    isMfaInvalid,
    remainingAttempts,
  };
}

export function getFieldError(parsed: ParsedApiError | null, ...candidateKeys: string[]): string | null {
  if (!parsed) return null;
  for (const key of candidateKeys) {
    const exact = parsed.fieldErrors[key];
    if (exact && exact.length > 0) return exact[0];

    const lowered = key.toLowerCase();
    for (const [errKey, messages] of Object.entries(parsed.fieldErrors)) {
      if (errKey.toLowerCase() === lowered || errKey.toLowerCase().endsWith(`.${lowered}`)) {
        if (messages.length > 0) return messages[0];
      }
    }
  }
  return null;
}

function normalizeProblemPayload(payload: unknown): ProblemDetails | null {
  if (!payload) return null;
  if (typeof payload === 'string') {
    try {
      return JSON.parse(payload) as ProblemDetails;
    } catch {
      return { detail: payload };
    }
  }
  if (typeof payload === 'object') {
    return payload as ProblemDetails;
  }
  return null;
}

function normalizeFieldErrors(errors: Record<string, string[]> | undefined): Record<string, string[]> {
  if (!errors || typeof errors !== 'object') return {};
  const result: Record<string, string[]> = {};
  for (const [key, value] of Object.entries(errors)) {
    if (Array.isArray(value)) {
      const normalizedKey = key.replace(/^\$\./, '');
      result[normalizedKey] = value.map(String);
    }
  }
  return result;
}

function extractSecondsFromText(text: string): number | null {
  const match = text.match(/(\d+)\s*(?:second|sec|s\b|ثانية)/i);
  if (!match) return null;
  const parsed = Number.parseInt(match[1], 10);
  return Number.isFinite(parsed) ? parsed : null;
}

function extractRemainingAttempts(text: string): number | null {
  const match = text.match(/(\d+)\s*(?:attempt|attempts|محاولة|محاولات)/i);
  if (!match) return null;
  const parsed = Number.parseInt(match[1], 10);
  return Number.isFinite(parsed) ? parsed : null;
}
