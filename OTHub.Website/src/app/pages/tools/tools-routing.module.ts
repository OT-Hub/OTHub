import { RouterModule, Routes } from '@angular/router';
import { NgModule } from '@angular/core';
import { FindNodesByWalletComponent } from './find-nodes-by-wallet/find-nodes-by-wallet.component';


const routes: Routes = [
  {
    path: 'findnodesbywallet',
    component: FindNodesByWalletComponent
  }
];


@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ToolsRoutingModule { }
