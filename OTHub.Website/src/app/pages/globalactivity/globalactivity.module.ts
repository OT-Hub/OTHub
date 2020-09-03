import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MomentModule } from 'ngx-moment';
import { SharedModule } from '../shared.module';
import { GlobalActivityRoutingModule } from './globalactivity-routing.module';
import { GlobalActivityComponent } from './globalactivity.component';
import { NbCardModule, NbSelectModule, NbButtonModule, NbStepperModule, NbPopoverModule } from '@nebular/theme';
import { Ng2SmartTableModule } from 'ng2-smart-table';
@NgModule({
  declarations: [GlobalActivityComponent ],
  imports: [
    CommonModule,
    GlobalActivityRoutingModule,
    MomentModule,
    SharedModule,
    NbCardModule,
    Ng2SmartTableModule,
    NbSelectModule,
    NbButtonModule,
    NbStepperModule,
    NbPopoverModule
  ]
})
export class GlobalActivityModule {
  
 }
