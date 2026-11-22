import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { SessionService } from '../session/session.service';
import { sessionExpiredInterceptor } from './session-expired.interceptor';

describe('sessionExpiredInterceptor', () => {
  let http: HttpClient;
  let testing: HttpTestingController;
  let sessions: SessionService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(withInterceptors([sessionExpiredInterceptor])),
        provideHttpClientTesting()
      ]
    });
    http = TestBed.inject(HttpClient);
    testing = TestBed.inject(HttpTestingController);
    sessions = TestBed.inject(SessionService);
  });

  afterEach(() => testing.verify());

  it('clears and announces the session on 404', () => {
    sessions.setSession('expired-session');
    let expired = false;
    sessions.expired$.subscribe(() => expired = true);

    http.get('/test').subscribe({
      error: () => undefined
    });
    const request = testing.expectOne('/test');
    request.flush('missing', { status: 404, statusText: 'Not Found' });

    expect(sessions.sessionId).toBeNull();
    expect(expired).toBeTrue();
  });
});
