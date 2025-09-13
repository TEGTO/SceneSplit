import { Injectable } from '@angular/core';
import { catchError, map, Observable } from 'rxjs';
import { mapObjectImageResponseToObjectImage, ObjectImage, ObjectImageResponse, SendSceneImageRequest } from '../..';
import { BaseApiService } from '../../../shared';

@Injectable({
  providedIn: 'root'
})
export class BookTemplateApiService extends BaseApiService {
  createBook(req: SendSceneImageRequest): Observable<ObjectImage> {
    return this.httpClient.post<ObjectImageResponse>(this.combinePathWithBookApiUrl(``), req).pipe(
      map((response) => mapObjectImageResponseToObjectImage(response)),
      catchError((resp) => this.handleError(resp))
    );
  }

  getBookById(id: string): Observable<ObjectImage> {
    return this.httpClient.get<ObjectImageResponse>(this.combinePathWithBookApiUrl(`/${id}`)).pipe(
      map((response) => mapObjectImageResponseToObjectImage(response)),
      catchError((resp) => this.handleError(resp))
    );
  }

  getBooks(): Observable<ObjectImage[]> {
    return this.httpClient.get<ObjectImageResponse[]>(this.combinePathWithBookApiUrl(``)).pipe(
      map((response) => response.map(mapObjectImageResponseToObjectImage)),
      catchError((resp) => this.handleError(resp))
    );
  }

  private combinePathWithBookApiUrl(subpath: string): string {
    return this.urlDefiner.combineWithApiUrl("/book" + subpath);
  }
}
