import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../environments/environment';

export interface TraceLog {
  timestamp: string;
  level: string;
  action: string;
  message: string;
}

@Injectable({
  providedIn: 'root'
})
export class SignalrService {
  private hubConnection: signalR.HubConnection | undefined;
  public trace$ = new Subject<TraceLog>();

  public startConnection() {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(environment.hubUrl)
      .withAutomaticReconnect()
      .build();

    this.hubConnection
      .start()
      .then(() => console.log('SignalR Connection started'))
      .catch(err => console.error('Error while starting SignalR connection: ' + err));
      
    this.hubConnection.on('ReceiveTrace', (data: TraceLog) => {
      this.trace$.next(data);
    });
  }
}
