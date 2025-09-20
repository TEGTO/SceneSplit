import { TestBed } from '@angular/core/testing';

import { UserIdManagerService } from './user-id-manager.service';

describe('UserIdManagerService', () => {
  let service: UserIdManagerService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(UserIdManagerService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
