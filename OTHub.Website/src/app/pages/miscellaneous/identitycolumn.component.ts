import { Component, Input, OnInit, EventEmitter, Output } from '@angular/core';
import * as moment from 'moment';
import { ViewCell } from 'ng2-smart-table';
import { RouterModule } from '@angular/router';
import { HubHttpService } from '../hub-http-service';

@Component({
    selector: 'identitycolumn',
    template: `
    <a [routerLink]="[link]">
    {{value}}
    </a>
  `,
})
export class DataHolderIdentityColumnComponent implements ViewCell, OnInit {

    renderValue: string;

    @Input() value: string;
    @Input() rowData: any;

    link: string;
    iconUrl: string;

    constructor(private httpService: HubHttpService) {

    }


    ngOnInit() {
        this.renderValue = null;
        this.link = '/nodes/dataholders/' + this.value;
        this.iconUrl = this.getIdentityIcon(this.value);
    }

    getIdentityIcon(identity: string) {
        return this.httpService.ApiUrl + '/api/icon/node/' + identity + '/' + (false ? 'dark' : 'light') + '/16';
      }

}

@Component({
    selector: 'identitycolumn',
    template: `
    <a [routerLink]="[link]">
    {{value}}
    </a>
  `,
})
export class DataCreatorIdentityColumnComponent implements ViewCell, OnInit {

    renderValue: string;

    @Input() value: string;
    @Input() rowData: any;

    link: string;
    iconUrl: string;

    constructor(private httpService: HubHttpService) {

    }


    ngOnInit() {
        this.renderValue = null;
        this.link = '/nodes/datacreators/' + this.value;
        this.iconUrl = this.getIdentityIcon(this.value);
    }

    getIdentityIcon(identity: string) {
        return this.httpService.ApiUrl + '/api/icon/node/' + identity + '/' + (false ? 'dark' : 'light') + '/16';
      }
}
