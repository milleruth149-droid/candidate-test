import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { RequestListComponent } from './components/request-list.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  template: `<router-outlet></router-outlet>`
})
export class App {
  protected readonly title = signal('client');
}
