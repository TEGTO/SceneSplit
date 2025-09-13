import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { BookTemplateApiService } from './book-template-api.service';

describe('BookTemplateApiService', () => {
  let service: BookTemplateApiService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [],
      providers: [
        BookTemplateApiService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(BookTemplateApiService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
