import { PriceFactorCalculatorComponent } from './pricefactorcalculator/pricefactorcalculator.component';
import { RouterModule, Routes } from '@angular/router';
import { NgModule } from '@angular/core';
import { DonationsComponent } from './donations/donations.component';
import { StarfleetboardingComponent } from '../starfleetboarding/starfleetboarding.component';


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
];


@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class MiscRoutingModule { }
