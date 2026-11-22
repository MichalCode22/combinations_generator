import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { SessionService } from '../session/session.service';

export const sessionExpiredInterceptor: HttpInterceptorFn = (request, next) => {
  const sessions = inject(SessionService);
  return next(request).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 404) {
        sessions.expireSession();
      }
      return throwError(() => error);
    })
  );
};
