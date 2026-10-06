import { Injectable } from '@angular/core';
import { HttpClient, HttpParams, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResult, RequestDto, RequestSearchParameters } from '../models/request.model';

@Injectable({
  providedIn: 'root' // הופך את ה-Service לזמין בכל האפליקציה באופן גלובלי
})
export class RequestService {
  // כתובת ה-API של השרת (ניתן לשנות בהתאם לסביבת הפיתוח או הפרודקשן)
 private apiUrl = 'https://bug-free-rotary-phone-97r9x649p9ggfppr6-60701.app.github.dev/api/requests';

  constructor(private http: HttpClient) {}

// פונקציה שמחזירה את רשימת הסטטוסים מהשרת
  getRequestStatuses(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/statuses`);
  }
    // פונקציה שמחזירה את רשימת סוגי הבקשות מהשרת
  getRequestTypes(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/types`);
  }
  // פונקציה לחיפוש וסינון בקשות
  searchRequests(
    params: RequestSearchParameters, 
    userId: number, 
    isAdmin: boolean
  ): Observable<PagedResult<RequestDto>> {
    
    //  הוספת Headers עם מזהה המשתמש והרשאותיו
    let headers = new HttpHeaders()
      .set('X-User-Id', userId.toString())
      .set('X-Is-Admin', isAdmin.toString());

    // בניית ה-Query String מתוך האובייקט שהתקבל
    let httpParams = new HttpParams()
      .set('Page', params.page.toString())
      .set('PageSize', params.pageSize.toString());

    if (params.requestNumber) {
      httpParams = httpParams.set('RequestNumber', params.requestNumber);
    }
    if (params.type !== undefined && params.type !== null) {
      httpParams = httpParams.set('Type', params.type.toString());
    }
    if (params.createdFrom) {
      httpParams = httpParams.set('CreatedFrom', params.createdFrom);
    }
    if (params.createdTo) {
      httpParams = httpParams.set('CreatedTo', params.createdTo);
    }
    if (params.sortBy !== undefined) {
      httpParams = httpParams.set('SortBy', params.sortBy.toString());
    }
    if (params.sortDir !== undefined) {
      httpParams = httpParams.set('SortDir', params.sortDir.toString());
    }
    
    // טיפול במערך סטטוסים (אם קיים) והוספתם ל-Query String
    if (params.status && params.status.length > 0) {
      params.status.forEach(s => {
        httpParams = httpParams.append('Status', s.toString());
      });
    }

    // קריאה ל-API עם ה-Headers וה-Query String שהוגדרו
    return this.http.get<PagedResult<RequestDto>>(this.apiUrl, {
      headers: headers,
      params: httpParams
    });
  }
}