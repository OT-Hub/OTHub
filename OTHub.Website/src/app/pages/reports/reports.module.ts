import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { StakedTokensByDayComponent } from './staked-tokens-by-day/staked-tokens-by-day.component';
import { FormsModule } from '@angular/forms';
import { NbCardModule, NbRadioModule } from '@nebular/theme';
import { MomentModule } from 'ngx-moment';
import { ReportsRoutingModule } from './reports-routing.module';
import { TGSComponent } from './tgs/tgs.component';



@NgModule({
  declarations: [StakedTokensByDayComponent, TGSComponent],
  imports: [
    CommonModule,
    NbCardModule,
    ReportsRoutingModule,
    FormsModule,
    MomentModule,
    NbRadioModule
  ]
})
export class ReportsModule { }
