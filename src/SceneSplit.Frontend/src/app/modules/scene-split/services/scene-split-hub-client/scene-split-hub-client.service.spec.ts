/* eslint-disable @typescript-eslint/no-explicit-any */
import { TestBed } from '@angular/core/testing';
import * as signalR from '@microsoft/signalr';
import { SceneSplitHubClientService } from './scene-split-hub-client.service';

describe('SceneSplitHubClientService', () => {
  let service: SceneSplitHubClientService;
  let mockHubConnection: jasmine.SpyObj<signalR.HubConnection>;

  beforeEach(() => {
    mockHubConnection = jasmine.createSpyObj('HubConnection', ['start', 'stop', 'invoke', 'on']);

    spyOn(signalR.HubConnectionBuilder.prototype, 'withUrl').and.returnValue({
      withHubProtocol: () => ({
        withAutomaticReconnect: () => ({
          build: () => mockHubConnection
        })
      })
    } as any);

    TestBed.configureTestingModule({
      providers: [
        SceneSplitHubClientService,
      ]
    });

    service = TestBed.inject(SceneSplitHubClientService);
  });

  afterEach(() => {
    service.ngOnDestroy();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('startConnection', () => {
    it('should build and start hub connection', async () => {
      mockHubConnection.start.and.returnValue(Promise.resolve());
      mockHubConnection.invoke.and.returnValue(Promise.resolve());

      service.startConnection('user1');
      await mockHubConnection.start.calls.mostRecent().returnValue;

      expect(mockHubConnection.start).toHaveBeenCalled();
      expect(mockHubConnection.invoke).toHaveBeenCalledWith('StartListenToUserObjectImages', 'user1');
    });

    it('should emit error if connection fails', (done) => {
      const error = new Error('Connection failed');
      mockHubConnection.start.and.returnValue(Promise.reject(error));

      service.errors$.subscribe(err => {
        expect(err.message).toContain('Hub connection failed');
        done();
      });

      service.startConnection('user1');
    });

    it('should set up ReceiveImageLinks handler', async () => {
      mockHubConnection.start.and.returnValue(Promise.resolve());
      mockHubConnection.invoke.and.returnValue(Promise.resolve());

      service.startConnection('user1');
      await mockHubConnection.start.calls.mostRecent().returnValue;

      expect(mockHubConnection.on).toHaveBeenCalledWith('ReceiveImageLinks', jasmine.any(Function));

      const onCallback = mockHubConnection.on.calls.mostRecent().args[1];
      const mockResponse = [{ ImageUrl: 'url1', Description: 'desc1' }];
      let result: any;
      service.images$.subscribe(images => result = images);

      onCallback(mockResponse);

      expect(result.length).toBeGreaterThan(0);
      expect(result[0].imageUrl).toBe('url1');
    });
  });

  describe('stopConnection', () => {
    it('should stop and clear hub connection', async () => {
      service['hubConnection'] = mockHubConnection;
      mockHubConnection.stop.and.returnValue(Promise.resolve());

      service.stopConnection();

      expect(mockHubConnection.stop).toHaveBeenCalled();
      expect(service['hubConnection']).toBeUndefined();
    });
  });

  describe('uploadSceneImage', () => {
    it('should throw error if hub connection is not established', async () => {
      service['hubConnection'] = undefined!;

      await expectAsync(service.uploadSceneImage('user1', new File([], 'test.png')))
        .toBeRejectedWithError('Hub connection is not established.');
    });

    it('should call invoke with proper request', async () => {
      const mockFile = new File([new ArrayBuffer(4)], 'test.png');
      service['hubConnection'] = mockHubConnection;
      mockHubConnection.invoke.and.returnValue(Promise.resolve());

      await service.uploadSceneImage('user1', mockFile);

      const reqArg = mockHubConnection.invoke.calls.mostRecent().args[2];
      expect(mockHubConnection.invoke).toHaveBeenCalledWith(
        'UploadSceneImageForUser',
        'user1',
        jasmine.objectContaining({
          FileName: 'test.png',
          FileContent: jasmine.any(Uint8Array)
        })
      );
      expect(reqArg.FileContent).toBeInstanceOf(Uint8Array);
    });
  });

  describe('ngOnDestroy', () => {
    it('should stop connection and complete subjects', () => {
      const completeImagesSpy = spyOn(service['imagesSubject'], 'complete').and.callThrough();
      const completeErrorsSpy = spyOn(service['errorSubject'], 'complete').and.callThrough();

      service['hubConnection'] = mockHubConnection;
      mockHubConnection.stop.and.returnValue(Promise.resolve());

      service.ngOnDestroy();

      expect(mockHubConnection.stop).toHaveBeenCalled();
      expect(completeImagesSpy).toHaveBeenCalled();
      expect(completeErrorsSpy).toHaveBeenCalled();
    });
  });
});