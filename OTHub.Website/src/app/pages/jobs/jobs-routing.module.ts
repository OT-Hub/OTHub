import { OffersDetailComponent } from './offersdetail/offersdetail.component';
import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { OffersComponent } from './offers/offers.component';

const routes: Routes = [
  {
    path: '',
    redirectTo: 'recent',
    pathMatch: 'full'
  },
  {
    path: 'recent',
    component: OffersComponent
  },
  { path: ':offerId',
  component: OffersDetailComponent
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class JobsRoutingModule { }
