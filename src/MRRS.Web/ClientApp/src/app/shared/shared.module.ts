import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AlertBannerComponent } from './alert-banner.component';

@NgModule({
  declarations: [AlertBannerComponent],
  imports: [CommonModule],
  exports: [CommonModule, ReactiveFormsModule, RouterModule, AlertBannerComponent]
})
export class SharedModule {}
