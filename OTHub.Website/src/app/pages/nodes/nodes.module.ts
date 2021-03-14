import { PayoutPricesComponent } from './payoutprices/payoutprices.component';
import { DataHoldersComponent } from './dataholders/dataholders.component';
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { NodesRoutingModule } from './nodes-routing.module';
import { MomentModule } from 'ngx-moment';
import { DataHolderComponent } from './dataholder/dataholder.component';
import { DatacreatorsComponent } from './datacreators/datacreators.component';
import { DatacreatorComponent } from './datacreator/datacreator.component';
import { MynodesComponent } from './mynodes/mynodes.component';
import { SharedModule } from '../shared.module';
import { FindbymanagementwalletComponent } from './findbymanagementwallet/findbymanagementwallet.component';
import { ManualPayoutPageComponent } from './manual-payout-page/manual-payout-page.component';
import { SafePipe } from './safe.pipe';
import { Ng2SmartTableModule } from 'ng2-smart-table';
import { JobsComponent } from './dataholder/jobs/jobs.component';
import { LitigationsComponent } from './dataholder/litigations/litigations.component';
import { PayoutsComponent } from './dataholder/payouts/payouts.component';
import { TransfersComponent } from './dataholder/transfers/transfers.component';
import { TransfersComponent as DCTransfersComponent } from './datacreator/transfers/transfers.component';
import { LitigationsComponent as DCLitigationsComponent } from './datacreator/litigations/litigations.component';
import { JobsComponent as DCJobsComponent } from './datacreator/jobs/jobs.component';
import { PaidoutColumnComponent } from './dataholder/jobs/paidoutcolumns.component';
import {MatCardModule} from '@angular/material/card';
import {MatIconModule} from '@angular/material/icon';
import {MatListModule} from '@angular/material/list';
@NgModule({
  declarations: [DataHoldersComponent, DataHolderComponent, DatacreatorsComponent,
     DatacreatorComponent, MynodesComponent, FindbymanagementwalletComponent, ManualPayoutPageComponent, SafePipe,
     PayoutPricesComponent,
     JobsComponent,
     LitigationsComponent,
     PayoutsComponent,
     TransfersComponent, DCTransfersComponent, DCLitigationsComponent, DCJobsComponent, PaidoutColumnComponent ],
  imports: [
    CommonModule,
    NodesRoutingModule,
    MomentModule,
    SharedModule,
    Ng2SmartTableModule,
    MatCardModule,
    MatIconModule,
    MatListModule
  ]
})
export class NodesModule { }
