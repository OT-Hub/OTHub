import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MomentModule } from 'ngx-moment';
import { SharedModule } from '../shared.module';
import { GlobalActivityRoutingModule } from './globalactivity-routing.module';
import { GlobalActivityComponent } from './globalactivity.component';
import { Ng2SmartTableModule } from 'ng2-smart-table';
import {MatCardModule} from '@angular/material/card';
import {MatIconModule} from '@angular/material/icon';
@NgModule({
  declarations: [GlobalActivityComponent ],
  imports: [
    CommonModule,
    GlobalActivityRoutingModule,
    MomentModule,
    SharedModule,
    Ng2SmartTableModule,
    MatCardModule,
    MatIconModule
  ]
})
export class GlobalActivityModule {
  
 }
