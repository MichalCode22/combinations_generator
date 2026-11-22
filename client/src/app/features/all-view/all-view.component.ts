import { ChangeDetectionStrategy, Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CombinationsStore } from '../../core/store/combinations.store';
import { BigintThousandsPipe } from '../../shared/pipes/bigint-thousands.pipe';
import { PagerComponent } from '../../shared/pager/pager.component';

@Component({
  selector: 'app-all-view',
  imports: [BigintThousandsPipe, PagerComponent],
  templateUrl: './all-view.component.html',
  styleUrl: './all-view.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AllViewComponent {
  protected readonly store = inject(CombinationsStore);
  private readonly destroyRef = inject(DestroyRef);

  protected loadPage(page: bigint): void {
    this.store.loadAllPage(page)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe();
  }

  protected changePageSize(pageSize: number): void {
    this.store.changePageSize(pageSize)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe();
  }

  protected back(): void {
    this.store.backToSingle()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe();
  }
}
