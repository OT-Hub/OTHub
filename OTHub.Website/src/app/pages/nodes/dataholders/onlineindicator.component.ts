import { Component, Input, OnInit } from '@angular/core';
import * as moment from 'moment';
import { ViewCell } from 'ng2-smart-table';
import { NbIconLibraries, NbIconComponent, NbComponentStatus } from '@nebular/theme';

@Component({
    template: `
    <nb-icon [icon]="icon" [status]="status" [nbPopover]="templateRef" nbPopoverTrigger="hover"></nb-icon>
    <ng-template #templateRef>
    <nb-card class="margin-bottom-0">
    <nb-card-header>{{rowData.Identity}}</nb-card-header>
    <br>
    <p *ngIf="LastSeenOnline != null">
    Last Seen Online: {{LastSeenOnline}}
    </p>
    <p *ngIf="status == 'danger' && LastSeenOffline != null && LastSeenOnline != LastSeenOffline">
    Last Seen Offline: {{LastSeenOffline}}
    </p>
    <div class="alert alert-danger" *ngIf="status == 'danger' && WarnAboutOTHubIssues == true">
    OT Hub has not checked your node in the last hour.
    <br>
    Check the System Status page for any outages/issues.
  </div>
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

    constructor() {
        this.WarnAboutOTHubIssues= false;
    }


    ngOnInit() {
        this.renderValue = null;

        if (this.rowData.LastSeenOnline) {

            const now = moment();
            const inst = moment.utc(this.rowData.LastSeenOnline).local();

            const difference = now.diff(inst, 'minutes');

            this.LastSeenOnline = inst.format('DD/MM/YYYY HH:mm');

            if (difference >= 65) {
                this.status = 'danger';
                this.icon = 'close-circle';
            } else {
                this.status = 'success';
                this.icon = 'checkmark-circle';
            }
        } else {
            this.status = 'danger';
            this.icon = 'close-circle';
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
}