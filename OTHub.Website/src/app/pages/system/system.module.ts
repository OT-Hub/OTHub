import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MomentModule } from 'ngx-moment';
import { SharedModule } from '../shared.module';
import { SystemRoutingModule } from './system-routing.module';
import { SystemStatusComponent } from './status/systemstatus.component';
import { NbCardModule, NbListModule, NbBadgeModule, NbIconModule, NbSpinnerModule } from '@nebular/theme';
import { SmartcontractsComponent } from './smartcontracts/smartcontracts.component';
import { RpcComponent, RPCHealthColumnComponent } from './rpc/rpc.component';
import { Ng2SmartTableModule } from 'ng2-smart-table';
@NgModule({
  declarations: [SystemStatusComponent, SmartcontractsComponent, RpcComponent, RPCHealthColumnComponent ],
  imports: [
    CommonModule,
    SystemRoutingModule,
    MomentModule,
    SharedModule,
    NbCardModule,
    NbListModule,
    NbBadgeModule,
    NbIconModule,
    NbSpinnerModule,
    Ng2SmartTableModule,
    NbIconModule
  ]
})
export class SystemModule { }
