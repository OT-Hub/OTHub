import { Component, Input, OnInit, EventEmitter, Output } from '@angular/core';
import * as moment from 'moment';
import { ViewCell } from 'ng2-smart-table';
import { NbIconLibraries, NbIconComponent, NbComponentStatus } from '@nebular/theme';
import { RouterModule } from '@angular/router';
import { HubHttpService } from '../../../hub-http-service';

@Component({
    selector: 'paidoutcolumn',
    template: `
    <span> 
    <span style="margin-right:3px;">
    {{formatAmount(rowData.PaidoutAmount)}}
    </span>
  <a *ngIf="canPayout === true" routerLink="/nodes/dataholders/{{nodeID}}/payout/{{rowData.BlockchainID}}/{{rowData.Identity}}/{{offerID}}">
  <button nbButton size="tiny" tooltip="Start Payout" title="Start Payout">
            <nb-icon icon="flash-outline" pack="eva"></nb-icon>
        </button>
</a>
         <!-- <a routerLink="/nodes/dataholders/{{nodeID}}/payout/{{offerID}}">Payout</a> -->
   

</span>
  `,
})
export class PaidoutColumnComponent implements ViewCell, OnInit {

    renderValue: string;

    @Input() value: string;
    @Input() rowData: any;

    canPayout: boolean;
    offerID: string;
    nodeID: string;

    constructor() {

    }

    formatAmount(input: string) {
        if (input == null) {
            return '0';
        }
        let tokenAmount = parseFloat(input);
        let formatted = +tokenAmount.toFixed(4);
        return formatted;
    }


    ngOnInit() {
        this.renderValue = null;
        this.canPayout = this.rowData.CanPayout && this.rowData.IsMyNode;
        this.offerID = this.rowData.OfferId;
        this.nodeID = this.rowData.NodeId;
        debugger;
    }
}