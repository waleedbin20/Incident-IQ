import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  constructor(private http: HttpClient) { }

  investigate(description: string, repository: string): Observable<any> {
    return this.http.post(`${environment.apiUrl}/incident/investigate`, {
      incident_id: 'P1-9942',
      description: description || 'Users are reporting checkout timeouts.',
      repo_name: repository || 'microsoft/TypeScript'
    });
  }
}
