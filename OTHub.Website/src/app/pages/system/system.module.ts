import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MomentModule } from 'ngx-moment';
import { SharedModule } from '../shared.module';
import { SystemRoutingModule } from './system-routing.module';
import { SystemStatusComponent } from './status/systemstatus.component';
import { SmartcontractsComponent } from './smartcontracts/smartcontracts.component';
@NgModule({
  declarations: [SystemStatusComponent, SmartcontractsComponent ],
  imports: [
    CommonModule,
    SystemRoutingModule,
    MomentModule,
    SharedModule
  ]
})
export class SystemModule { }
