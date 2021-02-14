import { RouterModule, Routes } from '@angular/router';
import { NgModule } from '@angular/core';
import { SystemStatusComponent } from './status/systemstatus.component';
import { SmartcontractsComponent } from './smartcontracts/smartcontracts.component';


const routes: Routes = [
  {
    path: 'status',
    component: SystemStatusComponent
  },
  {
    path: 'smartcontracts',
    component: SmartcontractsComponent
  },
  {
    path: '',
    component: SystemStatusComponent
  }
];


@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class SystemRoutingModule { }
