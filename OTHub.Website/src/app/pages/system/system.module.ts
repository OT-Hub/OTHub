import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MomentModule } from 'ngx-moment';
import { SharedModule } from '../shared.module';
import { SystemRoutingModule } from './system-routing.module';
import { SystemStatusComponent } from './status/systemstatus.component';
import { SmartcontractsComponent } from './smartcontracts/smartcontracts.component';
import {MatCardModule} from '@angular/material/card';
import {MatIconModule} from '@angular/material/icon';
import {MatListModule} from '@angular/material/list';
@NgModule({
  declarations: [SystemStatusComponent, SmartcontractsComponent ],
  imports: [
    CommonModule,
    SystemRoutingModule,
    MomentModule,
    SharedModule,
    MatCardModule,
    MatIconModule,
    MatListModule
  ]
})
export class SystemModule { }
