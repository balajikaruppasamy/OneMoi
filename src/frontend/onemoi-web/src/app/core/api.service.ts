import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiError } from './models';

/** Thin promise-based wrapper around HttpClient. All feature services use this. */
@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);
  readonly base = environment.apiUrl;

  get<T>(path: string, params?: Record<string, unknown>) {
    return firstValueFrom(this.http.get<T>(this.base + path, { params: toParams(params) }));
  }
  post<T>(path: string, body?: unknown) {
    return firstValueFrom(this.http.post<T>(this.base + path, body ?? {}));
  }
  put<T>(path: string, body?: unknown) {
    return firstValueFrom(this.http.put<T>(this.base + path, body ?? {}));
  }
  delete<T>(path: string) {
    return firstValueFrom(this.http.delete<T>(this.base + path));
  }
  upload<T>(path: string, file: File, field = 'file') {
    const fd = new FormData();
    fd.append(field, file);
    return firstValueFrom(this.http.post<T>(this.base + path, fd));
  }
  async download(path: string, params?: Record<string, unknown>) {
    const res = await firstValueFrom(this.http.get(this.base + path, { params: toParams(params), responseType: 'blob', observe: 'response' }));
    const name = /filename\*?=(?:UTF-8'')?"?([^";]+)/i.exec(res.headers.get('content-disposition') ?? '')?.[1] ?? 'download';
    const url = URL.createObjectURL(res.body!);
    const a = Object.assign(document.createElement('a'), { href: url, download: decodeURIComponent(name) });
    a.click();
    URL.revokeObjectURL(url);
  }
  /** Turns a relative upload path (/uploads/…) into a full URL. */
  file(path?: string | null) {
    return path ? (path.startsWith('http') ? path : this.base + path) : null;
  }
}

/** Reads the API's { message, errors } shape from any HTTP error. */
export function apiError(e: unknown): ApiError {
  if (e instanceof HttpErrorResponse) {
    if (e.status === 0) return { message: 'Cannot reach the OneMoi server. Is the API running?' };
    const body = e.error as ApiError | null;
    if (body?.message) return body;
    if (e.status === 403) return { message: 'You do not have access to this.' };
    return { message: e.message };
  }
  return { message: (e as Error)?.message ?? 'Something went wrong' };
}

function toParams(params?: Record<string, unknown>) {
  let p = new HttpParams();
  for (const [k, v] of Object.entries(params ?? {})) if (v !== undefined && v !== null && v !== '') p = p.set(k, String(v));
  return p;
}
