import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { StakedTokensByDayComponent } from './staked-tokens-by-day/staked-tokens-by-day.component';
import { FormsModule } from '@angular/forms';
import { NbCardModule, NbOptionModule, NbRadioModule, NbSelectModule } from '@nebular/theme';
import { MomentModule } from 'ngx-moment';
import { ReportsRoutingModule } from './reports-routing.module';
import { TGSComponent } from './tgs/tgs.component';
import { HoldingTimePerMonthComponent } from './holding-time-per-month/holding-time-per-month.component';
import { JobHeatmapComponent } from './job-heatmap/job-heatmap.component';



@NgModule({
  declarations: [StakedTokensByDayComponent, TGSComponent, HoldingTimePerMonthComponent, JobHeatmapComponent],
  imports: [
    CommonModule,
    NbCardModule,
    ReportsRoutingModule,
    FormsModule,
    MomentModule,
    NbRadioModule,
    NbSelectModule,
    NbOptionModule
  ]
})
export class ReportsModule { }
