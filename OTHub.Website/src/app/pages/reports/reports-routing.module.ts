import { RouterModule, Routes } from '@angular/router';
import { NgModule } from '@angular/core';
import { StakedTokensByDayComponent } from './staked-tokens-by-day/staked-tokens-by-day.component';
import { TGSComponent } from './tgs/tgs.component';
import { HoldingTimePerMonthComponent } from './holding-time-per-month/holding-time-per-month.component';


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
  }
];


@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ReportsRoutingModule { }
