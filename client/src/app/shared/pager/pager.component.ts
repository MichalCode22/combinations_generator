import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, signal } from '@angular/core';
import {
  firstPage,
  lastPage,
  nextPage,
  PagerItem,
  parseGoToPage,
  previousPage,
  slidingPageWindow
} from './pager.logic';

@Component({
  selector: 'app-pager',
  templateUrl: './pager.component.html',
  styleUrl: './pager.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PagerComponent {
  @Input({ required: true }) page!: bigint;
  @Input({ required: true }) totalPages!: bigint;
  @Input({ required: true }) pageSize!: number;
  @Input() busy = false;
  @Output() readonly pageChange = new EventEmitter<bigint>();
  @Output() readonly pageSizeChange = new EventEmitter<number>();

  protected readonly pageSizes = [10, 20, 50, 100];
  protected readonly pageInput = signal('');
  protected readonly invalidPage = signal(false);

  protected pageItems(): PagerItem[] {
    return slidingPageWindow(this.page, this.totalPages);
  }

  protected hasPages(): boolean {
    return this.totalPages > 0n;
  }

  protected isFirstPage(): boolean {
    return this.totalPages <= 0n || this.page <= firstPage(this.totalPages);
  }

  protected isLastPage(): boolean {
    return this.totalPages <= 0n || this.page >= lastPage(this.totalPages);
  }

  protected goFirst(): void {
    this.pageChange.emit(firstPage(this.totalPages));
  }

  protected goPrevious(): void {
    this.pageChange.emit(previousPage(this.page, this.totalPages));
  }

  protected goNext(): void {
    this.pageChange.emit(nextPage(this.page, this.totalPages));
  }

  protected goLast(): void {
    this.pageChange.emit(lastPage(this.totalPages));
  }

  protected onPageInput(event: Event): void {
    this.pageInput.set((event.target as HTMLInputElement).value);
    this.invalidPage.set(false);
  }

  protected submitPage(event: Event): void {
    event.preventDefault();
    const page = parseGoToPage(this.pageInput(), this.totalPages);
    if (page === null) {
      this.invalidPage.set(true);
      return;
    }

    this.invalidPage.set(false);
    this.pageInput.set('');
    this.pageChange.emit(page);
  }

  protected onPageSizeChange(event: Event): void {
    const size = Number((event.target as HTMLSelectElement).value);
    if (this.pageSizes.includes(size)) {
      this.pageSizeChange.emit(size);
    }
  }
}
