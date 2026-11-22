import { DestroyRef, Injectable, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { EMPTY, Observable, catchError, map, tap } from 'rxjs';
import { CombinationsApiService } from '../api/combinations-api.service';
import { CombinationViewState } from '../models/permutation.models';
import { SessionService } from '../session/session.service';

const initialState = (): CombinationViewState => ({
  mode: 'start',
  n: null,
  total: null,
  currentIndex: null,
  current: [],
  hasMore: false,
  items: [],
  page: 1n,
  totalPages: 0n,
  pageSize: 10,
  startIndex: null,
  busy: false,
  message: null,
  error: null
});

@Injectable({ providedIn: 'root' })
export class CombinationsStore {
  private readonly api = inject(CombinationsApiService);
  private readonly sessions = inject(SessionService);
  private readonly destroyRef = inject(DestroyRef);

  readonly state = signal<CombinationViewState>(initialState());

  constructor() {
    this.sessions.expired$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.state.set({
          ...initialState(),
          error: 'פג תוקף המפגש. התחילו מחדש.'
        });
      });
  }

  start(n: number): Observable<void> {
    return this.execute(this.api.start(n), (response) => {
      this.sessions.setSession(response.sessionId);
      this.state.set({
        ...initialState(),
        mode: 'single',
        n,
        total: BigInt(response.total),
        hasMore: true
      });
    });
  }

  next(): Observable<void> {
    return this.execute(this.api.next(), (response) => {
      if (!response.hasMore) {
        this.patch({
          busy: false,
          hasMore: false,
          message: response.message ?? 'אין יותר קומבינציות'
        });
        return;
      }

      if (response.index === null || response.permutation === null) {
        throw new Error('The API returned an incomplete permutation.');
      }

      const currentIndex = BigInt(response.index);
      const total = this.state().total;
      this.patch({
        busy: false,
        currentIndex,
        current: response.permutation,
        hasMore: total !== null && currentIndex < total,
        message: null
      });
    });
  }

  enterAll(): Observable<void> {
    return this.loadAllPage(1n, true);
  }

  loadAllPage(page: bigint, enter = false, pageSize = this.state().pageSize): Observable<void> {
    return this.execute(this.api.all(pageSize, page, enter), (response) => {
      const items = response.items.map((item) => ({
        index: BigInt(item.index),
        permutation: item.permutation
      }));
      const last = items.at(-1);
      this.patch({
        mode: 'all',
        busy: false,
        items,
        page: BigInt(response.page),
        totalPages: BigInt(response.totalPages),
        pageSize,
        startIndex: response.startIndex === null ? null : BigInt(response.startIndex),
        total: BigInt(response.total),
        currentIndex: last?.index ?? this.state().currentIndex,
        current: last?.permutation ?? this.state().current,
        hasMore: last !== undefined && last.index < BigInt(response.total),
        message: response.message
      });
    });
  }

  changePageSize(pageSize: number): Observable<void> {
    const state = this.state();
    const firstItemOffset = (state.page - 1n) * BigInt(state.pageSize);
    const page = firstItemOffset / BigInt(pageSize) + 1n;
    return this.loadAllPage(page, false, pageSize);
  }

  backToSingle(): Observable<void> {
    return this.execute(this.api.current(), (response) => {
      const currentIndex = response.index === null ? null : BigInt(response.index);
      const total = this.state().total;
      this.patch({
        mode: 'single',
        busy: false,
        currentIndex,
        current: response.permutation,
        hasMore: currentIndex === null || total === null || currentIndex < total,
        message: null
      });
    });
  }

  reset(): Observable<void> {
    return this.execute(this.api.reset(), () => {
      this.sessions.clearSession();
      this.state.set(initialState());
    });
  }

  private execute<T>(request: Observable<T>, onSuccess: (value: T) => void): Observable<void> {
    this.patch({ busy: true, error: null });
    return request.pipe(
      tap(onSuccess),
      map(() => undefined),
      catchError((error: unknown) => {
        if (!(error instanceof HttpErrorResponse && error.status === 404)) {
          this.patch({ busy: false, error: this.errorMessage(error) });
        }
        return EMPTY;
      })
    );
  }

  private patch(update: Partial<CombinationViewState>): void {
    this.state.update((current) => ({ ...current, ...update }));
  }

  private errorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse && error.status === 0) {
      return 'לא ניתן להתחבר לשרת. בדקו את החיבור ונסו שוב.';
    }
    if (error instanceof HttpErrorResponse && error.status === 400) {
      return 'הבקשה אינה תקינה. בדקו את הנתונים ונסו שוב.';
    }
    return 'אירעה שגיאה. נסו שוב בעוד רגע.';
  }
}
