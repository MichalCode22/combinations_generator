import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { CombinationsStore } from '../../core/store/combinations.store';
import { AllViewComponent } from './all-view.component';

describe('AllViewComponent', () => {
  let http: HttpTestingController;
  let store: CombinationsStore;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AllViewComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
    store = TestBed.inject(CombinationsStore);
  });

  afterEach(() => http.verify());

  it('Back restores the last permutation shown in all view', () => {
    store.start(3).subscribe();
    http.expectOne((req) => req.url.endsWith('/combinations/start')).flush({
      sessionId: 'session-1',
      total: '6'
    });
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

    const fixture = TestBed.createComponent(AllViewComponent);
    fixture.detectChanges();
    const backButton = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    backButton.click();
    http.expectOne((req) => req.url.endsWith('/combinations/current')).flush({
      index: '2',
      permutation: [1, 3, 2]
    });
    fixture.detectChanges();

    expect(store.state().mode).toBe('single');
  });
});
