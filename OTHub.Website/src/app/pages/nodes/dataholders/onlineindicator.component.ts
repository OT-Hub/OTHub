import { Component, Input, OnInit, ChangeDetectorRef } from '@angular/core';
import * as moment from 'moment';
import { ViewCell } from 'ng2-smart-table';
import { NbIconLibraries, NbIconComponent, NbComponentStatus, NbToastrConfig, NbToastrService } from '@nebular/theme';
import { DataHolderTestOnlineResult } from '../dataholder/dataholder-models';
import { HttpHeaders, HttpClient } from '@angular/common/http';
import { HubHttpService } from '../../hub-http-service';
declare const $: any;
@Component({
    template: `
   {{rowData.NodeId}}
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

    constructor(private httpService: HubHttpService, private http: HttpClient, private toastrService: NbToastrService,
         private cdr: ChangeDetectorRef) {
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

    // config: NbToastrConfig;
    // toastStatus: NbComponentStatus;
    //
    // CheckNodeOnline() {
    //
    //     const headers = new HttpHeaders()
    //         .set('Content-Type', 'application/json')
    //         .set('Accept', 'application/json');
    //     const url = this.httpService.ApiUrl + '/api/nodes/dataholder/checkonline?identity=' + this.rowData.Identity;
    //     const test = this.http.get<DataHolderTestOnlineResult>(url, { headers });
    //
    //     const startTime = new Date();
    //     test.subscribe(data => {
    //
    //         this.toastStatus = 'success';
    //
    //         if (data.Success) {
    //             this.toastStatus = 'success';
    //             this.rowData.LastSeenOnline = new Date();
    //         } else {
    //             this.toastStatus = 'danger';
    //         }
    //
    //         this.config = new NbToastrConfig({ duration: 8000 });
    //         this.config.status = this.toastStatus;
    //         this.config.icon = data.Success ? 'checkmark-circle' : 'alert-triangle';
    //         this.toastrService.show(data.Message, data.Header, this.config);
    //
    //         this.recalculate();
    //         this.cdr.detectChanges();
    //     }, err => {
    //
    //     });
    // }
}
