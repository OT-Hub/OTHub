import { OffersComponent } from './offers/offers.component';
import { NgModule } from '@angular/core';
import {CommonModule, DatePipe} from '@angular/common';
import { JobsRoutingModule } from './jobs-routing.module';
import { MomentModule } from 'ngx-moment';
import { OffersDetailComponent } from './offersdetail/offersdetail.component';
import { SharedModule } from '../shared.module';
import { Ng2SmartTableModule } from 'ng2-smart-table';
import { RouterModule } from '@angular/router';
import {DataCreatorColumnComponent} from './offers/datacreatorcolumn.component';
import {MatCardModule} from '@angular/material/card';
import {MatIconModule} from '@angular/material/icon';
import {MatListModule} from '@angular/material/list';
@NgModule({
  declarations: [ OffersComponent, OffersDetailComponent, DataCreatorColumnComponent],
  imports: [
    CommonModule,
    JobsRoutingModule,
    MomentModule,
    SharedModule,
    Ng2SmartTableModule,
    RouterModule,
    MatCardModule,
    MatIconModule,
    MatListModule
  ],
  providers: [
    DatePipe
  ]
})
export class JobsModule { }
