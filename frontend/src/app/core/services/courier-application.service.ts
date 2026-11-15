import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApplyCourierDto, CourierProfileDto } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class CourierApplicationService {
  private readonly http = inject(HttpClient);

  applyCourier(dto: ApplyCourierDto): Observable<CourierProfileDto> {
    return this.http.post<CourierProfileDto>('/api/v1/courier/apply', dto);
  }

  getMyCourierProfile(): Observable<CourierProfileDto> {
    return this.http.get<CourierProfileDto>('/api/v1/courier/profile');
  }
}
