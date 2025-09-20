import { TestBed } from '@angular/core/testing';

import { SceneSplitHubClientService } from '../scene-split-hub-client.service';

describe('SceneSplitHubClientService', () => {
  let service: SceneSplitHubClientService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(SceneSplitHubClientService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
