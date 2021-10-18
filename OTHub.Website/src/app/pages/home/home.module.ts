import { NgModule } from '@angular/core';
import {
  NbButtonModule,
  NbCardModule,
  NbIconModule,
} from '@nebular/theme';

import { ThemeModule } from '../../@theme/theme.module';
import { HomeComponent } from './home.component';
import { BlockchainBreakdownComponent } from './breakdown/blockchain-breakdown/blockchain-breakdown.component';
import { RouterModule } from '@angular/router';


@NgModule({
  imports: [
    ThemeModule,
    NbCardModule,
    RouterModule,
    NbButtonModule,
    NbIconModule
  ],
  declarations: [
    HomeComponent,
    BlockchainBreakdownComponent
  ],
  providers: [
    
  ],
})
export class HomeModule { }
