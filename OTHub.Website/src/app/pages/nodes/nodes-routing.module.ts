import { PayoutPricesComponent } from './payoutprices/payoutprices.component';
import { FindbymanagementwalletComponent } from './findbymanagementwallet/findbymanagementwallet.component';
import { MynodesComponent } from './mynodes/mynodes.component';
import { DatacreatorComponent } from './datacreator/datacreator.component';
import { DataHoldersComponent } from './dataholders/dataholders.component';
import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { DataHolderComponent } from './dataholder/dataholder.component';
import { DatacreatorsComponent } from './datacreators/datacreators.component';
import { ManualPayoutPageComponent } from './manual-payout-page/manual-payout-page.component';
import { MynodesoverviewComponent } from './mynodesoverview/mynodesoverview.component';
import { MynodestaxexportComponent } from './mynodestaxexport/mynodestaxexport.component';

const routes: Routes = [
  {
    path: '',
    redirectTo: 'mynodes',
    pathMatch: 'full'
  },
  {
    path: 'dataholders',
    component: DataHoldersComponent
  },
  {
    path: 'dataholders/managementwallet/:address',
    component: FindbymanagementwalletComponent
  },
  { path: 'dataholders/:identity',
  component: DataHolderComponent
  },
  { path: 'dataholders/:identity/payout/:offerId',
  component: ManualPayoutPageComponent
  },
  {
    path: 'datacreators',
    component: DatacreatorsComponent
  },
  {
    path: 'datacreators/:identity',
    component: DatacreatorComponent
  },
  {
    path: 'mynodes/settings',
    component: MynodesComponent
  },
  {
    path: 'mynodes/taxreport',
    component: MynodestaxexportComponent
  },
  {
    path: 'mynodes',
    component: MynodesoverviewComponent
  },
  { path: 'dataholders/:identity/report/usd',
  component: PayoutPricesComponent
  },
];


@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class NodesRoutingModule { }
