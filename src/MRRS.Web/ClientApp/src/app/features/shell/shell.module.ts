import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { ShellRoutingModule } from './shell-routing.module';
import { ShellComponent } from './shell.component';

@NgModule({
  declarations: [ShellComponent],
  imports: [SharedModule, ShellRoutingModule]
})
export class ShellModule {}
