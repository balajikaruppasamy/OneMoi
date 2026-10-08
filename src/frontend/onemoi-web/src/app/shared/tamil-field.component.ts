import { Component, inject, input, model, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TamilService } from '../core/ui';

/**
 * English input + Tamil input pair. Typing "Ram illa villa" fills "ராம் இல்லா வில்லா"
 * automatically. Each word shows alternative spellings; the Tamil box can also be edited by hand.
 *
 *   <mk-tamil-field label="Name" [(en)]="form.name" [(ta)]="form.nameTa" />
 */
@Component({
  selector: 'mk-tamil-field',
  imports: [FormsModule],
  template: `
    <div class="field">
      <label class="label">{{ label() }} @if (required()) {<span class="req">*</span>}</label>
      <input class="input" [ngModel]="en()" (ngModelChange)="onEn($event)" [ngModelOptions]="{ standalone: true }"
             [placeholder]="placeholder()" [attr.autocomplete]="'off'" [class.is-invalid]="invalid()" />
      <div class="input-group mt-1">
        <span class="addon ta" title="Tamil">த</span>
        <input class="input ta-input" [ngModel]="ta()" (ngModelChange)="onTa($event)" [ngModelOptions]="{ standalone: true }"
               placeholder="தமிழில் (தானாக நிரப்பப்படும்)" />
        @if (busy()) { <span class="addon" style="border:0;background:none"><span class="spinner" style="width:14px;height:14px;border:2px solid var(--muted);border-right-color:transparent;border-radius:50%;animation:spin .7s linear infinite"></span></span> }
      </div>
      @if (words().length && !manual()) {
        <div class="suggest">
          @for (w of words(); track $index; let wi = $index) {
            @if (w.options.length > 1) {
              @for (o of w.options; track o; let oi = $index) {
                <button type="button" [class.on]="chosen()[wi] === oi" (click)="choose(wi, oi)">{{ o }}</button>
              }
            }
          }
        </div>
      }
      @if (invalid()) { <span class="field-error">{{ errorText() }}</span> }
    </div>`
})
export class TamilFieldComponent {
  private tamil = inject(TamilService);
  label = input('');
  placeholder = input('');
  required = input(false);
  invalid = input(false);
  errorText = input('This field is required');
  en = model<string | null | undefined>('');
  ta = model<string | null | undefined>('');

  protected words = signal<{ word: string; options: string[] }[]>([]);
  protected chosen = signal<number[]>([]);
  protected busy = signal(false);
  protected manual = signal(false);   // user typed Tamil by hand → stop auto-filling
  private timer?: ReturnType<typeof setTimeout>;
  private lastAuto = '';

  onEn(v: string) {
    this.en.set(v);
    if (!v?.trim()) { this.words.set([]); if (!this.manual()) this.ta.set(''); this.manual.set(false); return; }
    if (this.manual()) return;
    clearTimeout(this.timer);
    this.timer = setTimeout(() => this.convert(v), 350);
  }

  onTa(v: string) {
    this.ta.set(v);
    this.manual.set(!!v && v !== this.lastAuto);
  }

  choose(wi: number, oi: number) {
    this.chosen.update(c => c.map((x, i) => (i === wi ? oi : x)));
    this.compose();
  }

  private async convert(text: string) {
    this.busy.set(true);
    const r = await this.tamil.convert(text);
    this.busy.set(false);
    if (!r || this.en() !== text || this.manual()) return;
    this.words.set(r.words);
    this.chosen.set(r.words.map(() => 0));
    this.compose();
  }

  /** Rebuild the Tamil text keeping spaces / dots / numbers from the English input. */
  private compose() {
    const tokens = (this.en() ?? '').match(/[A-Za-z]+|[^A-Za-z]+/g) ?? [];
    let wi = 0;
    const out = tokens.map(t => (/^[A-Za-z]+$/.test(t) ? this.words()[wi]?.options[this.chosen()[wi++]] ?? t : t)).join('');
    this.lastAuto = out;
    this.ta.set(out);
  }
}
