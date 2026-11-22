import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CombinationsStore } from '../../core/store/combinations.store';
import { BigintThousandsPipe } from '../../shared/pipes/bigint-thousands.pipe';

@Component({
  selector: 'app-single-view',
  imports: [BigintThousandsPipe],
  templateUrl: './single-view.component.html',
  styleUrl: './single-view.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SingleViewComponent {
  protected readonly store = inject(CombinationsStore);
  protected readonly notice = computed(() => {
    const state = this.store.state();
    if (state.message !== null) {
      return state.message;
    }
    return state.currentIndex !== null && !state.hasMore ? 'אין יותר קומבינציות' : null;
  });
  private readonly destroyRef = inject(DestroyRef);

  protected next(): void {
    this.store.next().pipe(takeUntilDestroyed(this.destroyRef)).subscribe();
  }

  protected enterAll(): void {
    this.store.enterAll().pipe(takeUntilDestroyed(this.destroyRef)).subscribe();
  }

  protected reset(): void {
    this.store.reset().pipe(takeUntilDestroyed(this.destroyRef)).subscribe();
  }
}
