import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class SessionService {
  private currentSessionId: string | null = null;
  private readonly expiredSubject = new Subject<void>();

  readonly expired$ = this.expiredSubject.asObservable();

  get sessionId(): string | null {
    return this.currentSessionId;
  }

  setSession(sessionId: string): void {
    this.currentSessionId = sessionId;
  }

  clearSession(): void {
    this.currentSessionId = null;
  }

  expireSession(): void {
    this.clearSession();
    this.expiredSubject.next();
  }
}
