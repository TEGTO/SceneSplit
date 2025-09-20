/* eslint-disable @typescript-eslint/no-explicit-any */
import { TestBed } from '@angular/core/testing';
import { ConfigService } from './config.service';

describe('ConfigService', () => {
  let service: ConfigService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ConfigService);

    (window as any).fetch = jasmine.createSpy().and.callFake(() =>
      Promise.resolve({
        json: () =>
          Promise.resolve({
            hubUrl: 'http://localhost:5162/hubs/scene-split',
            maxFileSize: 5242880,
            allowedImageTypes: 'image/png,image/jpeg'
          }),
      } as Response)
    );
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should load config and return hubUrl', async () => {
    await service.load();
    expect(service.hubUrl).toBe('http://localhost:5162/hubs/scene-split');
  });

  it('should load config and return maxFileSize', async () => {
    await service.load();
    expect(service.maxSizeFile).toBe(5242880);
  });

  it('should load config and return allowedImageTypes as array', async () => {
    await service.load();
    expect(service.allowedImageTypes).toEqual(['image/png', 'image/jpeg']);
  });
});