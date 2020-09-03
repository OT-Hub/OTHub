import { PriceFactorCalculatorComponent } from './pricefactorcalculator/pricefactorcalculator.component';
import { RouterModule, Routes } from '@angular/router';
import { NgModule } from '@angular/core';


const routes: Routes = [
  {
    path: 'pricefactor/calculator',
    component: PriceFactorCalculatorComponent
  }
];


@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class MiscRoutingModule { }
