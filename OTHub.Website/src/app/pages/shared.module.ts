import { FailedLoadingPageComponent } from './ui/failed-loading-page/failed-loading-page.component';
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CopyclipboardiconComponent } from './ui/copyclipboardicon/copyclipboardicon.component';
import { NbIconModule, NbPopoverModule } from '@nebular/theme';



@NgModule({
  imports: [
    NbIconModule,
    NbPopoverModule
  ],
  declarations: [
  CopyclipboardiconComponent, FailedLoadingPageComponent
  ],
  exports: [
    CopyclipboardiconComponent, FailedLoadingPageComponent
  ]
})

export class SharedModule {

}
