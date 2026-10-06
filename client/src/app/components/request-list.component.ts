import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RequestService } from '../services/request.service';
import { RequestDto, PagedResult } from '../models/request.model';

@Component({
  selector: 'app-request-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './request-list.component.html',
  styleUrls: ['./request-list.component.css']
})
export class RequestListComponent implements OnInit {

  // הזרקת שירותים בצורה אחידה 
  private requestService = inject(RequestService);
  private cdr = inject(ChangeDetectorRef);

  // הרשאות משתמש (נשלחות כקונטקסט לשרת שאוכף אותן בפועל)
  currentUserId: number = 1;
  isAdministrator: boolean = true;
//משתנה שמכיל את כל התוצאות
  result: PagedResult<RequestDto> | null = null;

  //עבור עמודים 
  currentPage: number = 1;
  pageSize: number = 10;

  //  עבור סטטוס וסוג הבקשה מערכים דינמיים שיתמלאו מהשרת
  statusOptions: any[] = [];
  typeOptions: any[] = [];

  // אובייקט הסינונים והחיפוש
  searchFilters: any = {
    requestNumber: '',
    type: null,
    createdFrom: '',
    createdTo: '',
    status: [],
    sortBy: 0,
    sortDirection: 0
  };
  
//טיפול בסוגי שגיאות שונות מהשרת והצגתן למשתמש
  errorMessage: string = '';


  ngOnInit() {
    this.loadStatuses();
    this.loadRequestTypes();
    this.loadRequests();
  }

  // טעינת סטטוסים מהשרת
  loadStatuses() {
    this.requestService.getRequestStatuses().subscribe({
      next: (statuses) => {
        this.statusOptions = statuses;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('שגיאה בטעינת הסטטוסים:', err);
      }
    });
  }

  // טעינת סוגי בקשות מהשרת
  loadRequestTypes() {
    this.requestService.getRequestTypes().subscribe({
      next: (types) => {
        this.typeOptions = types;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('שגיאה בטעינת סוגי הבקשות:', err);
      }
    });
  }

  // טעינת הנתונים מהשרת בהתאם לסינונים ולעמוד הנוכחי
  loadRequests() {
    const params = {
      page: this.currentPage,
      pageSize: this.pageSize,
      requestNumber: this.searchFilters.requestNumber ? this.searchFilters.requestNumber.trim() : undefined,
      type: this.searchFilters.type !== null && this.searchFilters.type !== '' ? Number(this.searchFilters.type) : undefined,
      //createdFrom: this.searchFilters.createdFrom ? new Date(this.searchFilters.createdFrom).toISOString() : undefined,
      //createdTo: this.searchFilters.createdTo ? new Date(this.searchFilters.createdTo).toISOString() : undefined,
      createdFrom: this.searchFilters.createdFrom ? this.searchFilters.createdFrom : undefined,
createdTo: this.searchFilters.createdTo ? this.searchFilters.createdTo : undefined,
      status: this.searchFilters.status && this.searchFilters.status.length > 0 
        ? this.searchFilters.status.map((s: any) => Number(s)) 
        : undefined,
      sortBy: this.searchFilters.sortBy,
      sortDirection: this.searchFilters.sortDirection
    };


    // קריאה לשירות שמבצע את החיפוש בפועל
    this.requestService.searchRequests(
      params,
      this.currentUserId,
      this.isAdministrator
    ).subscribe({
      next: (data) => {
        this.result = data;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('שגיאה בתקשורת מול השרת:', err);
        
        // בדיקה האם זו שגיאת 400 עם פרטי ולידציה מהשרת
        if (err.status === 400 && err.error?.errors) {
          const validationErrors = err.error.errors['request'];
          if (validationErrors && validationErrors.length > 0) {
            this.errorMessage = validationErrors.join(', ');
          } else {
            this.errorMessage = 'נתוני החיפוש אינם תקינים.';
          }
        } else if (err.status === 401) {
          this.errorMessage = 'שגיאת הזדהות: משתמש אינו מורשה.';
        } else {
          this.errorMessage = 'אירעה שגיאה בטעינת הנתונים מול השרת.';
        }
        
        this.cdr.detectChanges(); // לעדכן את המסך להצגת השגיאה
      }
    });
  }

      // חישוב מספר העמודים הכולל
  get totalPages(): number {
    if (!this.result) return 1;
    return Math.ceil(this.result.totalCount / this.pageSize) || 1;
  }

  
  nextPage() {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.loadRequests();
    }
  }

  prevPage() {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.loadRequests();
    }
  }

  // עדכון מערך הסטטוסים בזמן אמת כשמסמנים/מבטלים צ'ק-באקס
  onStatusCheckboxChange(statusId: number, event: any) {
    if (event.target.checked) {
      if (!this.searchFilters.status.includes(statusId)) {
        this.searchFilters.status.push(statusId);
      }
    } else {
      this.searchFilters.status = this.searchFilters.status.filter((id: number) => id !== statusId);
    }
  }

  // פעלת חיפוש חדש (מתאפס לעמוד 1)
  onSearch() {
    this.errorMessage = '';
    this.currentPage = 1;
    
    this.loadRequests();
  }

  // איפוס סינונים
  resetFilters() {
    this.searchFilters = {
      requestNumber: '',
      type: null,
      createdFrom: '',
      createdTo: '',
      status: []
    };
    this.currentPage = 1;
    this.loadRequests();
  }

  //  לא בשימוש עדכון הנתונים בעת שינוי הרשאות או משתמש
  onPermissionChange() {
    this.currentPage = 1;
    this.loadRequests();
  }
}