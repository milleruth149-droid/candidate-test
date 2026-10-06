import { Routes } from '@angular/router';
import { RequestListComponent } from './components/request-list.component';

export const routes: Routes = [

    { path: '', component: RequestListComponent }, // 👈 מציג את רשימת הבקשות מיד בעליית האתר
];

