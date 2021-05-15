import { PriceFactorCalculatorComponent } from './pricefactorcalculator/pricefactorcalculator.component';
import { RouterModule, Routes } from '@angular/router';
import { NgModule } from '@angular/core';
import { DonationsComponent } from './donations/donations.component';
import { StarfleetboardingComponent } from '../starfleetboarding/starfleetboarding.component';
import { RecentUpdatesComponent } from './recent-updates/recent-updates.component';


const routes: Routes = [
  {
    path: 'pricefactor/calculator',
    component: PriceFactorCalculatorComponent
  },
  {
    path: 'donations',
    component: DonationsComponent
  },
  {
    path: 'xdaibounty',
    component: StarfleetboardingComponent,
  },
  {
    path: 'recentupdates',
    component: RecentUpdatesComponent,
  },
];


@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class MiscRoutingModule { }
