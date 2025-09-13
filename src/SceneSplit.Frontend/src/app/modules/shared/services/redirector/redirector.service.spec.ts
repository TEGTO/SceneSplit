import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { RedirectorService } from './redirector.service';

describe('RedirectorContollerService', () => {
  let mockRouter: jasmine.SpyObj<Router>;
  let service: RedirectorService;

  beforeEach(() => {
    mockRouter = jasmine.createSpyObj('Router', ['navigate']);
    const activatedRouteMock = jasmine.createSpyObj('ActivatedRoute', [], {
      snapshot: {
        paramMap: jasmine.createSpyObj('ParamMap', ['get'])
      }
    });

    TestBed.configureTestingModule({
      providers: [
        RedirectorService,
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: activatedRouteMock }
      ]
    });
    service = TestBed.inject(RedirectorService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should navigate to home', () => {
    service.redirectToHome();

    expect(mockRouter.navigate).toHaveBeenCalledWith(['']);
  });

  it('should navigate to some path', () => {
    const somePath = "some path";

    service.redirectTo(somePath);

    expect(mockRouter.navigate).toHaveBeenCalledWith([somePath]);
  });
});
