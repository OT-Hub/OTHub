import { Component, Input, OnInit } from '@angular/core';
import * as moment from 'moment';
import { ViewCell } from 'ng2-smart-table';
import { NbIconLibraries, NbIconComponent, NbComponentStatus, NbToastrConfig, NbToastrService } from '@nebular/theme';
import { DataHolderTestOnlineResult } from '../dataholder/dataholder-models';
import { HttpHeaders, HttpClient } from '@angular/common/http';
import { HubHttpService } from '../../hub-http-service';
declare const $: any;
@Component({
    template: `
    <nb-icon [icon]="icon" [status]="status" [nbPopover]="templateRef" nbPopoverTrigger="hover"></nb-icon>
    <ng-template #templateRef>
    <nb-card class="margin-bottom-0">
    <nb-card-header>{{rowData.Identity}}</nb-card-header>
    <br>
    <p *ngIf="LastSeenOnline != null" style="padding:0px 20px;">
    Last Seen Online: {{LastSeenOnline}}
    </p>
    <p *ngIf="status == 'danger' && LastSeenOffline != null && LastSeenOnline != LastSeenOffline" style="padding:0px 20px;">
    Last Seen Offline: {{LastSeenOffline}}
    </p>
    <div class="alert alert-danger" *ngIf="status == 'danger' && WarnAboutOTHubIssues == true" style="margin:0px 10px;">
    OT Hub has not checked your node in the last hour.
    <br>
    Check the System Status page for any outages/issues.
  </div>
  <button nbButton status="success" outline style="margin:10px;"
  (click)="CheckNodeOnline()">Check Online</button>
  </nb-card>

</ng-template>
  `,
})
export class OnlineIndicatorRenderComponent implements ViewCell, OnInit {

    renderValue: string;

    @Input() value: string | number;
    @Input() rowData: any;

    status: NbComponentStatus;
    icon: string;
    LastSeenOnline: string;
    LastSeenOffline: string;
    WarnAboutOTHubIssues: boolean;

    constructor(private httpService: HubHttpService, private http: HttpClient, private toastrService: NbToastrService) {
        this.WarnAboutOTHubIssues = false;
    }

    recalculate() {
        if (this.rowData.LastSeenOnline) {

            const now = moment();
            const inst = moment.utc(this.rowData.LastSeenOnline).local();

            const difference = now.diff(inst, 'minutes');

            this.LastSeenOnline = inst.format('DD/MM/YYYY HH:mm');

            if (difference >= 65) {
                this.status = 'danger';
                this.icon = 'alert-triangle';
            } else {
                this.status = 'success';
                this.icon = 'checkmark-circle';
            }
        } else {
            this.status = 'warning';
            this.icon = 'alert-triangle';
        }

        if (this.rowData.LastSeenOffline) {
            const inst = moment.utc(this.rowData.LastSeenOffline).local();

            const now = moment();
            const difference = now.diff(inst, 'minutes');

            if (difference >= 65) {
                this.WarnAboutOTHubIssues = true;
            }

            this.LastSeenOffline = inst.format('DD/MM/YYYY HH:mm');
        } else {
            if (this.status == 'danger') {
                this.WarnAboutOTHubIssues = true;
            }
        }
    }


    ngOnInit() {
        this.renderValue = null;
        this.recalculate();
    }

    config: NbToastrConfig;
    toastStatus: NbComponentStatus;

    CheckNodeOnline() {

        const headers = new HttpHeaders()
            .set('Content-Type', 'application/json')
            .set('Accept', 'application/json');
        const url = this.httpService.ApiUrl + '/api/nodes/dataholder/checkonline?identity=' + this.rowData.Identity;
        const test = this.http.get<DataHolderTestOnlineResult>(url, { headers });

        const startTime = new Date();
        test.subscribe(data => {

            this.toastStatus = 'success';

            if (data.Success) {
                this.toastStatus = 'success';
                this.rowData.LastSeenOnline = new Date();
            } else {
                this.toastStatus = 'danger';
            }

            this.config = new NbToastrConfig({ duration: 8000 });
            this.config.status = this.toastStatus;
            this.config.icon = data.Success ? 'checkmark-circle' : 'alert-triangle';
            this.toastrService.show(data.Message, data.Header, this.config);

            this.recalculate();
        }, err => {

        });
    }
}