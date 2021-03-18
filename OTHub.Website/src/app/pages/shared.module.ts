import { FailedLoadingPageComponent } from './ui/failed-loading-page/failed-loading-page.component';
import { NgModule } from '@angular/core';
import { CommonModule, SlicePipe } from '@angular/common';
import { CopyclipboardiconComponent } from './ui/copyclipboardicon/copyclipboardicon.component';
import { NbIconModule, NbPopoverModule } from '@nebular/theme';
import { OfferIdColumnComponent } from './miscellaneous/offeridcolumn.component';
import { RouterModule } from '@angular/router';
import { DataHolderIdentityColumnComponent, DataCreatorIdentityColumnComponent } from './miscellaneous/identitycolumn.component';



@NgModule({
  imports: [
    NbIconModule,
    NbPopoverModule,
    RouterModule,
    CommonModule
  ],
  declarations: [
    CopyclipboardiconComponent,
    FailedLoadingPageComponent,
    OfferIdColumnComponent,
    DataHolderIdentityColumnComponent,
    DataCreatorIdentityColumnComponent
  ],
  exports: [
    CopyclipboardiconComponent, FailedLoadingPageComponent
  ]
})

export class SharedModule {

}
