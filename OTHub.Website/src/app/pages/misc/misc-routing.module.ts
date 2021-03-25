import { PriceFactorCalculatorComponent } from './pricefactorcalculator/pricefactorcalculator.component';
import { RouterModule, Routes } from '@angular/router';
import { NgModule } from '@angular/core';
import { DonationsComponent } from './donations/donations.component';


const routes: Routes = [
  {
    path: 'pricefactor/calculator',
    component: PriceFactorCalculatorComponent
  },
  {
    path: 'donations',
    component: DonationsComponent
  }
];


@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class MiscRoutingModule { }
