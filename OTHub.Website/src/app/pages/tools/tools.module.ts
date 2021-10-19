import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FindNodesByWalletComponent } from './find-nodes-by-wallet/find-nodes-by-wallet.component';
import { NbCardModule, NbRadioModule } from '@nebular/theme';
import { ToolsRoutingModule } from './tools-routing.module';
import { FormsModule } from '@angular/forms';
import { MomentModule } from 'ngx-moment';
import { UnstakeboardingComponent } from './unstakeboarding/unstakeboarding.component';



@NgModule({
  declarations: [FindNodesByWalletComponent, UnstakeboardingComponent],
  imports: [
    CommonModule,
    NbCardModule,
    ToolsRoutingModule,
    FormsModule,
    MomentModule,
    NbRadioModule
  ]
})
export class ToolsModule { }
