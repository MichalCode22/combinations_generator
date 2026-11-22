import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { CombinationsStore } from '../../core/store/combinations.store';
import { SingleViewComponent } from './single-view.component';

describe('SingleViewComponent', () => {
  let http: HttpTestingController;
  let store: CombinationsStore;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SingleViewComponent],
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

  it('enables All when combinations remain and disables it after exhaustion', () => {
    store.start(3).subscribe();
    http.expectOne((req) => req.url.endsWith('/combinations/start')).flush({
      sessionId: 'session-1',
      total: '6'
    });

    const fixture = TestBed.createComponent(SingleViewComponent);
    fixture.detectChanges();
    const allButton = () => fixture.nativeElement.querySelectorAll('button')[2] as HTMLButtonElement;
    expect(allButton().disabled).toBeFalse();

    store.next().subscribe();
    http.expectOne((req) => req.url.endsWith('/combinations/next')).flush({
      hasMore: false,
      index: null,
      permutation: null,
      message: 'אין יותר קומבינציות'
    });
    fixture.detectChanges();
    expect(allButton().disabled).toBeTrue();
  });

  it('reset returns the app store to start', () => {
    store.start(3).subscribe();
    http.expectOne((req) => req.url.endsWith('/combinations/start')).flush({
      sessionId: 'session-1',
      total: '6'
    });

    const fixture = TestBed.createComponent(SingleViewComponent);
    fixture.detectChanges();
    const resetButton = fixture.nativeElement.querySelectorAll('button')[0] as HTMLButtonElement;
    resetButton.click();
    http.expectOne((req) => req.url.endsWith('/combinations') && req.method === 'DELETE').flush(null);

    expect(store.state().mode).toBe('start');
  });
});
