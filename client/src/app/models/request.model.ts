// מייצג בקשה בודדת כפי שהיא מגיעה מהשרת (RequestDto)
export interface RequestDto {
  id: number;
  requestNumber: string;
  customerId: number;
  ownerId: number;
  assignedToUserId: number;
  status: number;
  requestType: number;
  createdAt: string;
}

// מבנה התוצאות המחולקות לעמודים שהשרת מחזיר (PagedResult)
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

// הפרמטרים שהשרת מקבל ב-Query String עבור חיפוש וסינון (RequestSearchRequest)
export interface RequestSearchParameters {
  requestNumber?: string;
  status?: number[];
  type?: number;
  createdFrom?: string; // פורמט תאריך 'YYYY-MM-DD'
  createdTo?: string;   // פורמט תאריך 'YYYY-MM-DD'
  sortBy?: number;
  sortDir?: number;     // 0 ל-Asc, 1 ל-Desc
  page: number;
  pageSize: number;
}