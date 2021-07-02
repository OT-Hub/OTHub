import { RouterModule, Routes } from '@angular/router';
import { NgModule } from '@angular/core';
import { StakedTokensByDayComponent } from './staked-tokens-by-day/staked-tokens-by-day.component';


const routes: Routes = [
  {
    path: 'stakedtokens',
    component: StakedTokensByDayComponent
  }
];


@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ReportsRoutingModule { }
