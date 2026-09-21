import { Component, inject } from '@angular/core';
import { SettingsService } from './settings.service';

@Component({
  selector: 'app-settings',
  standalone: true,
  templateUrl: './settings.page.html',
  styleUrl: './settings.page.scss',
})
export class SettingsPage {
  readonly svc = inject(SettingsService);
}
