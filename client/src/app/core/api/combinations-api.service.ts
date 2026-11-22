import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  AllResponse,
  CurrentResponse,
  NextResponse,
  StartRequest,
  StartResponse
} from '../models/permutation.models';
import { SessionService } from '../session/session.service';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class CombinationsApiService {
  private readonly http = inject(HttpClient);
  private readonly sessions = inject(SessionService);
  private readonly endpoint = `${environment.apiBaseUrl}/combinations`;

  start(n: number): Observable<StartResponse> {
    const body: StartRequest = { n };
    const headers = new HttpHeaders({
      'X-Session-Id': this.sessions.sessionId ?? ''
    });
    return this.http.post<StartResponse>(`${this.endpoint}/start`, body, { headers });
  }

  next(): Observable<NextResponse> {
    return this.http.post<NextResponse>(`${this.endpoint}/next`, {}, {
      headers: this.sessionHeaders()
    });
  }

  all(pageSize: number, page: bigint, enter: boolean): Observable<AllResponse> {
    const params = new HttpParams()
      .set('pageSize', pageSize)
      .set('page', page.toString())
      .set('enter', enter);
    return this.http.get<AllResponse>(`${this.endpoint}/all`, {
      headers: this.sessionHeaders(),
      params
    });
  }

  current(): Observable<CurrentResponse> {
    return this.http.get<CurrentResponse>(`${this.endpoint}/current`, {
      headers: this.sessionHeaders()
    });
  }

  reset(): Observable<void> {
    return this.http.delete<void>(this.endpoint, {
      headers: this.sessionHeaders()
    });
  }

  private sessionHeaders(): HttpHeaders {
    const sessionId = this.sessions.sessionId;
    if (sessionId === null) {
      return new HttpHeaders({ 'X-Session-Id': '' });
    }

    return new HttpHeaders({ 'X-Session-Id': sessionId });
  }
}
