import { Injectable, OnDestroy } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import * as signalRMsgPack from '@microsoft/signalr-protocol-msgpack';
import { BehaviorSubject, Observable, Subject } from 'rxjs';
import { mapObjectImageResponseToObjectImage, ObjectImage, ObjectImageResponse, SendSceneImageRequest } from '../..';
import { environment } from '../../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class SceneSplitHubClientService implements OnDestroy {
  private hubConnection!: signalR.HubConnection;

  private readonly imagesSubject = new BehaviorSubject<ObjectImage[]>([]);
  private readonly errorSubject = new Subject<Error>();

  images$: Observable<ObjectImage[]> = this.imagesSubject.asObservable();
  errors$: Observable<Error> = this.errorSubject.asObservable();

  startConnection(userId: string) {
    if (this.hubConnection) {
      console.warn('Hub connection already exists.');
      return;
    }

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(environment.hubUrl)
      .withHubProtocol(new signalRMsgPack.MessagePackHubProtocol())
      .withAutomaticReconnect()
      .build();

    this.hubConnection
      .start()
      .then(() => {
        console.log('Connected to SceneSplitHub');
        return this.hubConnection.invoke('StartListenToUserObjectImages', userId);
      })
      .catch(err => {
        this.errorSubject.next(new Error(`Hub connection failed: ${err}`));
      });

    this.hubConnection.on('ReceiveImageLinks', (images: ObjectImageResponse[]) => {
      this.imagesSubject.next(images.map(mapObjectImageResponseToObjectImage));
    });
  }

  stopConnection() {
    if (this.hubConnection) {
      this.hubConnection.stop();
      this.hubConnection = undefined!;
    }
  }

  ngOnDestroy() {
    this.stopConnection();
    this.imagesSubject.complete();
    this.errorSubject.complete();
  }

  async uploadSceneImage(userId: string, file: File): Promise<void> {
    if (!this.hubConnection) {
      throw new Error('Hub connection is not established.');
    }

    const arrayBuffer = await file.arrayBuffer();
    const bytes = new Uint8Array(arrayBuffer);

    const req: SendSceneImageRequest = {
      FileName: file.name,
      FileContent: bytes
    };

    await this.hubConnection.invoke('UploadSceneImageForUser', userId, req);
  }
}