import { Component, OnInit, ChangeDetectorRef, OnDestroy, NgZone } from '@angular/core';

import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
declare const $: any;
import * as moment from 'moment';
import { MyNodeService } from '../../nodes/mynodeservice';
import { HubHttpService } from '../../hub-http-service';
import { SystemStatusModel } from './system-models';
@Component({
    selector: 'app-systemstatus',
    templateUrl: './systemstatus.component.html',
    styleUrls: ['./systemstatus.component.scss']
})
export class SystemStatusComponent implements OnInit, OnDestroy {
    constructor(private http: HttpClient, private route: ActivatedRoute, private router: Router,
                public myNodeService: MyNodeService, private httpService: HubHttpService) {
        this.isLoading = true;
        this.failedLoading = false;
        this.IsTestNet = httpService.IsTestNet;
    }
    failedLoading: boolean;
    IsTestNet: boolean;
    isLoading: boolean;
    isDarkTheme: boolean;
    getDataObservable: any;
    Data: SystemStatusModel;

    ngOnInit() {
        this.getDataObservable = this.getData().subscribe(data => {
            const endTime = new Date();
            this.Data = data;
            this.failedLoading = false;
            this.isLoading = false;
          }, err => {
            this.failedLoading = true;
            this.isLoading = false;
          });
    }

    ngOnDestroy() {
        this.getDataObservable.unsubscribe();
    }

    getData() {
        const headers = new HttpHeaders()
          .set('Content-Type', 'application/json')
          .set('Accept', 'application/json');
        const url = this.httpService.ApiUrl + '/api/system';
        return this.http.get<SystemStatusModel>(url, { headers });
      }
}
