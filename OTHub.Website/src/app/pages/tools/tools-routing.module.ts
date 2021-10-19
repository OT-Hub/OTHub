import { RouterModule, Routes } from '@angular/router';
import { NgModule } from '@angular/core';
import { FindNodesByWalletComponent } from './find-nodes-by-wallet/find-nodes-by-wallet.component';
import { UnstakeboardingComponent } from './unstakeboarding/unstakeboarding.component';


const routes: Routes = [
  {
    path: 'findnodesbywallet',
    component: FindNodesByWalletComponent
  },
  {
    path: 'unstakeboarding',
    component: UnstakeboardingComponent
  }
];


@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ToolsRoutingModule { }
