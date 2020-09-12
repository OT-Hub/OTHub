import { Component, Input, OnInit, EventEmitter, Output } from '@angular/core';
import * as moment from 'moment';
import { ViewCell } from 'ng2-smart-table';
import { NbIconLibraries, NbIconComponent, NbComponentStatus } from '@nebular/theme';
import { RouterModule } from '@angular/router';
import { HubHttpService } from '../../../hub-http-service';
import { MyNodeService } from '../../mynodeservice';
import { MyNodeModel } from '../../mynodemodel';

@Component({
    selector: 'paidoutcolumn',
    template: `
    <span>
    <span *ngIf="canPayout === true && MyNode">
        No (<a routerLink="/nodes/dataholders/{{identity}}/payout/{{offerID}}">Payout</a>)
    </span>
    <span *ngIf="!MyNode || canPayout === false">
        {{rowData.Paidout === true ? 'Yes' : 'No'}}
    </span>

</span>
  `,
})
export class PaidoutColumnComponent implements ViewCell, OnInit {

    renderValue: string;

    @Input() value: string;
    @Input() rowData: any;

    MyNode: MyNodeModel;
    canPayout: boolean;
    offerID: string;
    identity: string;

    constructor(private httpService: HubHttpService, public myNodeService: MyNodeService) {

    }


    ngOnInit() {
        this.renderValue = null;

        this.MyNode = this.myNodeService.Get(this.rowData.Identity);
        this.canPayout = this.rowData.CanPayout;
        this.offerID = this.rowData.OfferId;
        this.identity = this.rowData.Identity;
    }

    getIdentityIcon(identity: string) {
        return this.httpService.ApiUrl + '/api/icon/node/' + identity + '/' + (false ? 'dark' : 'light') + '/16';
      }

}