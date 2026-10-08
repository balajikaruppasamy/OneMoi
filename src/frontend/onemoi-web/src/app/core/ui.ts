import { Injectable, Pipe, PipeTransform, inject, signal } from '@angular/core';
import { ApiService } from './api.service';
import { Transliteration } from './models';

/** Small toast messages (bottom of the screen). */
@Injectable({ providedIn: 'root' })
export class ToastService {
  readonly items = signal<{ id: number; text: string; type: 'ok' | 'err' | 'info' }[]>([]);
  private n = 0;
  show(text: string, type: 'ok' | 'err' | 'info' = 'info') {
    const id = ++this.n;
    this.items.update(l => [...l, { id, text, type }]);
    setTimeout(() => this.items.update(l => l.filter(t => t.id !== id)), 3200);
  }
  ok(text: string) { this.show(text, 'ok'); }
  err(text: string) { this.show(text, 'err'); }
}

/** Display language for names / reports. Data is always stored in both. */
@Injectable({ providedIn: 'root' })
export class LangService {
  readonly lang = signal<'en' | 'ta'>(readLang());
  set(l: 'en' | 'ta') {
    this.lang.set(l);
    try { localStorage.setItem('onemoi.lang', l); } catch { /* ignore */ }
  }
  pick(en?: string | null, ta?: string | null) {
    return this.lang() === 'ta' && ta ? ta : (en ?? ta ?? '');
  }
}
function readLang(): 'en' | 'ta' {
  try { return localStorage.getItem('onemoi.lang') === 'ta' ? 'ta' : 'en'; } catch { return 'en'; }
}

/** English → Tamil via the API (Google Input Tools + offline fallback). */
@Injectable({ providedIn: 'root' })
export class TamilService {
  private api = inject(ApiService);
  private cache = new Map<string, Transliteration>();
  async convert(text: string): Promise<Transliteration | null> {
    const t = text.trim();
    if (!t) return null;
    if (this.cache.has(t)) return this.cache.get(t)!;
    try {
      const r = await this.api.get<Transliteration>('/api/tools/transliterate', { text: t, max: 4 });
      this.cache.set(t, r);
      return r;
    } catch { return null; }
  }
}

/** 125000 → ₹1,25,000 (Indian grouping) */
@Pipe({ name: 'inr' })
export class InrPipe implements PipeTransform {
  transform(v: number | null | undefined, decimals = 0): string {
    if (v === null || v === undefined) return '—';
    return '₹' + Number(v).toLocaleString('en-IN', { minimumFractionDigits: decimals, maximumFractionDigits: decimals });
  }
}

/** {{ pick(en, ta) }} as a pipe: {{ item.name | pick: item.nameTa }} — reacts to the EN/TA toggle. */
@Pipe({ name: 'pick', pure: false })
export class PickPipe implements PipeTransform {
  private lang = inject(LangService);
  transform(en?: string | null, ta?: string | null): string { return this.lang.pick(en, ta); }
}

/** "06:00:00" → "6:00 AM" */
@Pipe({ name: 'hm' })
export class TimePipe implements PipeTransform {
  transform(v?: string | null): string {
    if (!v) return '';
    const [h, m] = v.split(':').map(Number);
    return `${((h + 11) % 12) + 1}:${String(m).padStart(2, '0')} ${h < 12 ? 'AM' : 'PM'}`;
  }
}

export function maskMobile(m?: string | null) {
  return !m || m.length < 10 ? (m ?? '') : `${m.slice(0, 2)}xxx xx${m.slice(-3)}`;
}

/** "2026-10-06T00:00:00" → Date at local midnight (date-only columns must not shift by timezone). */
export function dateOnly(s: string) { return new Date(s.slice(0, 10) + 'T00:00:00'); }
