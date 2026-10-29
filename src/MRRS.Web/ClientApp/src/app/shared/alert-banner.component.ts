import { Component, Input } from '@angular/core';

@Component({
  selector: 'mrrs-alert',
  templateUrl: './alert-banner.component.html',
  styleUrl: './alert-banner.component.css',
  standalone: false
})
export class AlertBannerComponent {
  @Input() message: string | null = null;
  @Input() kind: 'error' | 'ok' = 'error';
}
