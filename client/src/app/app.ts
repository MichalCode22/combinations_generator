import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AllViewComponent } from './features/all-view/all-view.component';
import { SingleViewComponent } from './features/single-view/single-view.component';
import { StartComponent } from './features/start/start.component';
import { CombinationsStore } from './core/store/combinations.store';

@Component({
  selector: 'app-root',
  imports: [AllViewComponent, SingleViewComponent, StartComponent],
  templateUrl: './app.html',
  styleUrl: './app.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class App {
  protected readonly store = inject(CombinationsStore);
}
