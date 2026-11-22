import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection, WritableSignal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { StartComponent } from './start.component';

describe('StartComponent', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StartComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('validates the n-input matrix', () => {
    const fixture = TestBed.createComponent(StartComponent);
    const component = fixture.componentInstance as unknown as {
      nInput: WritableSignal<string>;
      validationMessage: () => string | null;
    };

    const cases: Array<[string, string | null]> = [
      ['', 'יש להזין מספר.'],
      [' ', 'אין להזין רווחים.'],
      ['1.5', 'יש להזין מספר שלם בלבד.'],
      ['abc', 'יש להזין מספר שלם בלבד.'],
      ['0', 'יש להזין מספר גדול או שווה ל־1.'],
      ['21', 'ניתן להזין מספר עד 20 בלבד.'],
      ['1', null],
      ['20', null]
    ];

    for (const [value, expected] of cases) {
      component.nInput.set(value);
      expect(component.validationMessage()).toBe(expected);
    }
  });

  it('starts a valid session', () => {
    const fixture = TestBed.createComponent(StartComponent);
    fixture.detectChanges();
    const input = fixture.nativeElement.querySelector('#permutation-size') as HTMLInputElement;
    input.value = '3';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;
    expect(button.disabled).toBeFalse();
    button.click();

    const request = http.expectOne((req) => req.url.endsWith('/combinations/start') && req.method === 'POST');
    expect(request.request.body).toEqual({ n: 3 });
    request.flush({ sessionId: 'session-1', total: '6' });
  });
});
