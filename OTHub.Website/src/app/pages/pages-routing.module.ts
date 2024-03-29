import { RouterModule, Routes } from '@angular/router';
import { NgModule } from '@angular/core';

import { PagesComponent } from './pages.component';
import { HomeComponent } from './home/home.component';
import { NotFoundComponent } from './miscellaneous/not-found/not-found.component';
import { StarfleetboardingComponent } from './starfleetboarding/starfleetboarding.component';
import { FindNodesByWalletComponent } from './tools/find-nodes-by-wallet/find-nodes-by-wallet.component';
import { BlockchainBreakdownComponent } from './home/breakdown/blockchain-breakdown/blockchain-breakdown.component';

const routes: Routes = [{
  path: '',
  component: PagesComponent,
  children: [
    {
      path: 'dashboard',
      component: HomeComponent,
    },
    {
      path: 'dashboard/blockchain/breakdown',
      component: BlockchainBreakdownComponent,
    },
    // {
    //   path: 'iot-dashboard',
    //   component: DashboardComponent,
    // },
    {
      path: 'jobs',
      loadChildren: () => import('./jobs/jobs.module')
        .then(m => m.JobsModule),
    },
    {
      path: 'offers',
      loadChildren: () => import('./jobs/jobs.module')
        .then(m => m.JobsModule),
    },
    {
      path: 'tools',
      loadChildren: () => import('./tools/tools.module')
        .then(m => m.ToolsModule),
    },
    {
      path: 'reports',
      loadChildren: () => import('./reports/reports.module')
        .then(m => m.ReportsModule),
    },
    {
      path: 'globalactivity',
      loadChildren: './globalactivity/globalactivity.module#GlobalActivityModule'
    },
    {
      path: 'system',
      loadChildren: './system/system.module#SystemModule'
    },
    {
      path: 'misc',
      loadChildren: './misc/misc.module#MiscModule'
    },
    {
      path: 'offer/:offerId',
      redirectTo: 'jobs/:offerId',
      pathMatch: 'full'
    },
    {
      path: 'identity/:identity',
      redirectTo: 'nodes/dataholders/:identity',
      pathMatch: 'full'
    },
    {
      path: 'nodes',
      loadChildren: './nodes/nodes.module#NodesModule'
    },
    {
      path: 'miscellaneous',
      loadChildren: () => import('./miscellaneous/miscellaneous.module')
        .then(m => m.MiscellaneousModule),
    },
    {
      path: '',
      redirectTo: 'dashboard',
      pathMatch: 'full',
    },
    {
      path: '**',
      component: NotFoundComponent,
    },
  ],
}];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class PagesRoutingModule {
}
