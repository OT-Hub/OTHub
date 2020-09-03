import { RouterModule, Routes } from '@angular/router';
import { NgModule } from '@angular/core';
import { SystemStatusComponent } from './status/systemstatus.component';


const routes: Routes = [
  {
    path: 'status',
    component: SystemStatusComponent
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
