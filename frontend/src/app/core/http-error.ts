import { HttpErrorResponse } from '@angular/common/http';

/**
 * Pull a human message from API errors.
 * Supports: plain string, { message }, ProblemDetails (title/detail), validation errors.
 */
export function readApiError(err: unknown, fallback = 'Something went wrong'): string {
  if (!(err instanceof HttpErrorResponse)) {
    if (typeof err === 'string' && err.trim()) return err;
    return fallback;
  }

  const body = err.error;

  if (typeof body === 'string' && body.trim()) {
    try {
      const parsed = JSON.parse(body);
      const nested = messageFromObject(parsed);
      if (nested) return nested;
    } catch {
      /* plain text */
    }
    return body.trim();
  }

  if (body && typeof body === 'object') {
    const fromObj = messageFromObject(body as Record<string, unknown>);
    if (fromObj) return fromObj;
  }

  if (err.status === 0) return 'Cannot reach API. Is the backend running on :8080?';
  if (err.status === 404) return 'Not found.';
  if (err.status === 400) return 'Invalid request.';
  if (err.status >= 500) return 'Server error. Try again.';

  return fallback;
}

function messageFromObject(obj: Record<string, unknown>): string | null {
  if (typeof obj['message'] === 'string' && (obj['message'] as string).trim()) {
    return (obj['message'] as string).trim();
  }
  if (typeof obj['detail'] === 'string' && (obj['detail'] as string).trim()) {
    return (obj['detail'] as string).trim();
  }
  if (typeof obj['title'] === 'string' && (obj['title'] as string).trim()) {
    const title = (obj['title'] as string).trim();
    if (typeof obj['detail'] === 'string' && (obj['detail'] as string).trim()) {
      return `${title}: ${(obj['detail'] as string).trim()}`;
    }
    return title;
  }

  const errors = obj['errors'];
  if (errors && typeof errors === 'object') {
    const parts: string[] = [];
    for (const [, val] of Object.entries(errors as Record<string, unknown>)) {
      if (Array.isArray(val)) {
        for (const item of val) {
          if (typeof item === 'string' && item.trim()) parts.push(item.trim());
        }
      } else if (typeof val === 'string' && val.trim()) {
        parts.push(val.trim());
      }
    }
    if (parts.length) return parts.join(' ');
  }

  return null;
}
