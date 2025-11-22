import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Person, CreatePerson, UpdatePerson } from '../models/person.model';
import { ResponseModel } from '../types/response.model';

@Injectable({
  providedIn: 'root',
})
export class PersonService {
  private apiUrl = `${environment.apiUrl}/persons`;

  constructor(private http: HttpClient) {}

  searchPersons(documentNumber: string): Observable<Person[]> {
    const params = new HttpParams().set('documentNumber', documentNumber);
    return this.http
      .get<ResponseModel<Person[]>>(this.apiUrl, { params })
      .pipe(map((response) => response.data!));
  }

  createPerson(person: CreatePerson): Observable<Person> {
    return this.http
      .post<ResponseModel<Person>>(this.apiUrl, person)
      .pipe(map((response) => response.data!));
  }

  updatePerson(id: string, person: UpdatePerson): Observable<Person> {
    return this.http
      .put<ResponseModel<Person>>(`${this.apiUrl}/${id}`, person)
      .pipe(map((response) => response.data!));
  }
}
