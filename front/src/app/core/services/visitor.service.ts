import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ResponseModel } from './response.model';
import { Visitor } from './visitor.model';

@Injectable({
  providedIn: 'root'
})
export class VisitorService {
  private apiUrl = 'https://localhost:5001/api/visitors';

  constructor(private http: HttpClient) { }

  createVisitor(visitor: Visitor): Observable<ResponseModel<Visitor>> {
    return this.http.post<ResponseModel<Visitor>>(this.apiUrl, visitor);
  }

  getVisitor(id: string): Observable<ResponseModel<Visitor>> {
    return this.http.get<ResponseModel<Visitor>>(`${this.apiUrl}/${id}`);
  }

  getAllVisitors(): Observable<ResponseModel<Visitor[]>> {
    return this.http.get<ResponseModel<Visitor[]>>(this.apiUrl);
  }

  updateVisitor(id: string, visitor: Visitor): Observable<ResponseModel<Visitor>> {
    return this.http.put<ResponseModel<Visitor>>(`${this.apiUrl}/${id}`, visitor);
  }

  deleteVisitor(id: string): Observable<ResponseModel<string>> {
    return this.http.delete<ResponseModel<string>>(`${this.apiUrl}/${id}`);
  }
}
