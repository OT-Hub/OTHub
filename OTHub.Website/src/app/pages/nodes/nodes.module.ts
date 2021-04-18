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
import { NbCardModule, NbButtonModule, NbSelectModule, NbStepperModule, NbPopoverModule, NbIconModule, NbListModule, NbBadgeModule, NbAlertModule, NbToggleModule, NbActionsModule, NbUserModule, NbTabsetComponent, NbTabComponent, NbTabsetModule } from '@nebular/theme';
import { Ng2SmartTableModule } from 'ng2-smart-table';
import { JobsComponent } from './dataholder/jobs/jobs.component';
import { LitigationsComponent } from './dataholder/litigations/litigations.component';
import { PayoutsComponent } from './dataholder/payouts/payouts.component';
import { TransfersComponent } from './dataholder/transfers/transfers.component';
import { TransfersComponent as DCTransfersComponent } from './datacreator/transfers/transfers.component';
import { LitigationsComponent as DCLitigationsComponent } from './datacreator/litigations/litigations.component';
import { JobsComponent as DCJobsComponent } from './datacreator/jobs/jobs.component';
import { OnlineIndicatorRenderComponent } from './dataholders/onlineindicator.component';
import { PaidoutColumnComponent } from './dataholder/jobs/paidoutcolumns.component';
import { NbAccordionModule } from '@nebular/theme';
import { MynodesoverviewComponent } from './mynodesoverview/mynodesoverview.component';
@NgModule({
  declarations: [DataHoldersComponent, DataHolderComponent, DatacreatorsComponent,
     DatacreatorComponent, MynodesComponent, FindbymanagementwalletComponent, ManualPayoutPageComponent, SafePipe,
     PayoutPricesComponent,
     JobsComponent,
     LitigationsComponent,
     PayoutsComponent,
     TransfersComponent, OnlineIndicatorRenderComponent, DCTransfersComponent, DCLitigationsComponent, DCJobsComponent, PaidoutColumnComponent, MynodesoverviewComponent ],
  imports: [
    CommonModule,
    NodesRoutingModule,
    MomentModule,
    SharedModule,
    NbIconModule,
    NbCardModule,
    Ng2SmartTableModule,
    NbSelectModule,
    NbButtonModule,
    NbStepperModule,
    NbPopoverModule,
    NbListModule ,
    NbBadgeModule,
    NbAlertModule,
    NbToggleModule,
    NbUserModule,
    NbAccordionModule,
    NbTabsetModule
  ]
})
export class NodesModule { }
