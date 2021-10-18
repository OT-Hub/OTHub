import { RouterModule, Routes } from '@angular/router';
import { NgModule } from '@angular/core';
import { StakedTokensByDayComponent } from './staked-tokens-by-day/staked-tokens-by-day.component';
import { TGSComponent } from './tgs/tgs.component';
import { HoldingTimePerMonthComponent } from './holding-time-per-month/holding-time-per-month.component';
import { JobHeatmapComponent } from './job-heatmap/job-heatmap.component';


const routes: Routes = [
  {
    path: 'stakedtokens',
    component: StakedTokensByDayComponent
  },
  {
    path: 'tgs',
    component: TGSComponent
  },
  {
    path: 'holdingtime',
    component: HoldingTimePerMonthComponent
  },
  {
    path: 'jobheatmap',
    component: JobHeatmapComponent
  }
];


@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ReportsRoutingModule { }
