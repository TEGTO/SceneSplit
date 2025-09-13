/* eslint-disable @typescript-eslint/no-explicit-any */
import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { SnackbarManager, URLDefiner } from '../..';

@Injectable({
  providedIn: 'root'
})
export abstract class BaseApiService {
  HttpStatusCodes: Record<number, string> = {
    400: 'Bad Request',
    401: 'Unauthorized',
    403: 'Forbidden',
    404: 'Not Found',
    500: 'Internal Server Error',
  };

  protected get httpClient(): HttpClient { return this._httpClient }
  protected get urlDefiner(): URLDefiner { return this._urlDefiner }

  constructor(
    private readonly _httpClient: HttpClient,
    private readonly _urlDefiner: URLDefiner,
    private readonly snackbarManager: SnackbarManager
  ) { }

  protected handleError(error: any): Observable<never> {
    const message = this.handleApiError(error);
    return throwError(() => new Error(message));
  }

  private handleApiError(error: any): string {
    let errorMessage;
    if (error.error) {
      if (error.error.messages) {
        errorMessage = error.error.messages.join('\n');
      }
    } else if (error.message) {
      errorMessage = error.message;
    }
    if (!errorMessage) {
      const statusCode = this.getStatusCodeDescription(error.status);
      errorMessage = `An unknown error occurred! (${statusCode})`
    }
    console.error(errorMessage);
    this.snackbarManager.openErrorSnackbar([errorMessage]);
    return errorMessage;
  }

  private getStatusCodeDescription(statusCode: number): string {
    return this.HttpStatusCodes[statusCode] || 'Unknown Status Code';
  }
}