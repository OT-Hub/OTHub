import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MomentModule } from 'ngx-moment';
import { SharedModule } from '../shared.module';
import { PriceFactorCalculatorComponent } from './pricefactorcalculator/pricefactorcalculator.component';
import { MiscRoutingModule } from './misc-routing.module';
import { NbListModule, NbCardModule } from '@nebular/theme';
@NgModule({
  declarations: [PriceFactorCalculatorComponent ],
  imports: [
    CommonModule,
    MiscRoutingModule,
    MomentModule,
    SharedModule,
    NbListModule,
    NbCardModule
  ]
})
export class MiscModule { }
