import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { CombinationsStore } from '../../core/store/combinations.store';

@Component({
  selector: 'app-start',
  imports: [FormsModule],
  templateUrl: './start.component.html',
  styleUrl: './start.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StartComponent {
  protected readonly store = inject(CombinationsStore);
  protected readonly nInput = signal('');
  private readonly pastedInvalidText = signal(false);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly validationMessage = computed(() => {
    const value = this.nInput();
    if (value.length === 0) {
      return 'יש להזין מספר.';
    }
    if (/\s/.test(value)) {
      return 'אין להזין רווחים.';
    }
    if (this.pastedInvalidText()) {
      return 'הטקסט שהודבק אינו מספר תקין.';
    }
    if (!/^\d+$/.test(value)) {
      return 'יש להזין מספר שלם בלבד.';
    }
    const number = Number(value);
    if (number < 1) {
      return 'יש להזין מספר גדול או שווה ל־1.';
    }
    if (number > 20) {
      return 'ניתן להזין מספר עד 20 בלבד.';
    }
    return null;
  });

  protected onInput(event: Event): void {
    this.nInput.set((event.target as HTMLInputElement).value);
  }

  protected onPaste(event: ClipboardEvent): void {
    const pasted = event.clipboardData?.getData('text') ?? '';
    this.pastedInvalidText.set(!/^\d+$/.test(pasted));
  }

  protected onKeydown(event: KeyboardEvent): void {
    if (event.key.length === 1 || event.key === 'Backspace' || event.key === 'Delete') {
      this.pastedInvalidText.set(false);
    }
  }

  protected submit(): void {
    const validationMessage = this.validationMessage();
    if (validationMessage !== null || this.store.state().busy) {
      return;
    }

    this.store.start(Number(this.nInput()))
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe();
  }
}
