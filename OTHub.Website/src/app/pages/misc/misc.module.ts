import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MomentModule } from 'ngx-moment';
import { SharedModule } from '../shared.module';
import { PriceFactorCalculatorComponent } from './pricefactorcalculator/pricefactorcalculator.component';
import { MiscRoutingModule } from './misc-routing.module';
import { NbListModule, NbCardModule, NbSelectModule } from '@nebular/theme';
import { DonationsComponent } from './donations/donations.component';
import { StarfleetboardingComponent } from '../starfleetboarding/starfleetboarding.component';
import { Ng2SmartTableModule } from 'ng2-smart-table';
@NgModule({
  declarations: [PriceFactorCalculatorComponent, DonationsComponent, StarfleetboardingComponent ],
  imports: [
    CommonModule,
    MiscRoutingModule,
    MomentModule,
    SharedModule,
    NbListModule,
    NbCardModule,
    Ng2SmartTableModule,
    NbCardModule,
    NbSelectModule,
  ]
})
export class MiscModule { }
