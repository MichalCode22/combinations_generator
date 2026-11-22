import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { CombinationsStore } from './combinations.store';

describe('CombinationsStore', () => {
  let http: HttpTestingController;
  let store: CombinationsStore;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    http = TestBed.inject(HttpTestingController);
    store = TestBed.inject(CombinationsStore);
  });

  afterEach(() => http.verify());

  it('defaults to 10 items per page', () => {
    expect(store.state().pageSize).toBe(10);
  });

  function start() {
    store.start(3).subscribe();
    http.expectOne((req) => req.url.endsWith('/combinations/start')).flush({
      sessionId: 'session-1',
      total: '6'
    });
  }

  it('increments serial indices on Next', () => {
    start();

    store.next().subscribe();
    http.expectOne((req) => req.url.endsWith('/combinations/next')).flush({
      hasMore: true,
      index: '1',
      permutation: [1, 2, 3],
      message: null
    });
    expect(store.state().currentIndex).toBe(1n);
    expect(store.state().current).toEqual([1, 2, 3]);

    store.next().subscribe();
    http.expectOne((req) => req.url.endsWith('/combinations/next')).flush({
      hasMore: true,
      index: '2',
      permutation: [1, 3, 2],
      message: null
    });
    expect(store.state().currentIndex).toBe(2n);
  });

  it('keeps the state unchanged when Next reaches exhaustion', () => {
    start();
    store.next().subscribe();
    http.expectOne((req) => req.url.endsWith('/combinations/next')).flush({
      hasMore: false,
      index: null,
      permutation: null,
      message: 'אין יותר קומבינציות'
    });

    expect(store.state().hasMore).toBeFalse();
    expect(store.state().message).toBe('אין יותר קומבינציות');
    const stateBeforeRepeat = store.state();
    store.next().subscribe();
    http.expectOne((req) => req.url.endsWith('/combinations/next')).flush({
      hasMore: false,
      index: null,
      permutation: null,
      message: 'אין יותר קומבינציות'
    });
    expect(store.state()).toEqual(stateBeforeRepeat);
  });

  it('reset returns to the start state', () => {
    start();
    store.reset().subscribe();
    http.expectOne((req) => req.url.endsWith('/combinations') && req.method === 'DELETE').flush(null);

    expect(store.state().mode).toBe('start');
    expect(store.state().n).toBeNull();
    expect(store.state().total).toBeNull();
  });

  it('Back restores the last all-view permutation from current', () => {
    start();
    store.enterAll().subscribe();
    http.expectOne((req) => req.url.includes('/combinations/all')).flush({
      items: [
        { index: '1', permutation: [1, 2, 3] },
        { index: '2', permutation: [1, 3, 2] }
      ],
      page: '1',
      totalPages: '1',
      startIndex: '1',
      total: '6',
      message: null
    });

    store.backToSingle().subscribe();
    http.expectOne((req) => req.url.endsWith('/combinations/current')).flush({
      index: '2',
      permutation: [1, 3, 2]
    });

    expect(store.state().mode).toBe('single');
    expect(store.state().currentIndex).toBe(2n);
    expect(store.state().current).toEqual([1, 3, 2]);
  });

  it('All with nothing left returns an empty state and exhaustion message', () => {
    start();
    store.loadAllPage(1n).subscribe();
    http.expectOne((req) => req.url.includes('/combinations/all')).flush({
      items: [],
      page: '1',
      totalPages: '1',
      startIndex: null,
      total: '6',
      message: 'אין יותר קומבינציות'
    });
    expect(store.state().items).toEqual([]);
    expect(store.state().message).toBe('אין יותר קומבינציות');
  });
});
