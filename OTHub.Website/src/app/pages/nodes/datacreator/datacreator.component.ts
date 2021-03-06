import { DataCreatorSummaryModel } from './../datacreators/datacreators-models';
import { Component, OnInit, ChangeDetectorRef, OnDestroy } from '@angular/core';

import { ActivatedRoute, Router } from '@angular/router';
import { MyNodeService } from '../mynodeservice';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { DataCreatedDetailedModel } from './datacreator-model';
import { HubHttpService } from '../../hub-http-service';
declare const $: any;
@Component({
  selector: 'app-datacreator',
  templateUrl: './datacreator.component.html',
  styleUrls: ['./datacreator.component.scss']
})
export class DatacreatorComponent implements OnInit, OnDestroy {

  constructor(private http: HttpClient, private route: ActivatedRoute, private router: Router,
              private chRef: ChangeDetectorRef, public myNodeService: MyNodeService, private httpService: HubHttpService) {
    this.isLoading = true;
    this.failedLoading = false;
    this.IsTestNet = httpService.IsTestNet;
  }

  IsTestNet: boolean;
  failedLoading: boolean;
  isLoading: boolean;
  NodeModel: DataCreatedDetailedModel;
  //litigationsExportOptionsObj: any;
  //litigationsDataTable: any;
  identity: string;
  //jobsDataTable: any;
  //profileDataTable: any;
  //jobsExportOptionsObj: any;
  //profileExportOptionsObj: any;
  GetNodeObservable: any;
  RouteObservable: any;
  identityIconUrl: string;
  isDarkTheme: boolean;

  formatAmount(amount) {
    if (amount === null) {
      return null;
    }
    const split = amount.toString().split('.');
    let lastSplit = '';
    if (split.length === 2) {
      lastSplit = split[1];
      if (lastSplit.length > 3) {
        lastSplit = lastSplit.substring(0, 3);
      }
      return split[0] + '.' + lastSplit;
    }
    return split[0];
  }

  getNode() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    const url = this.httpService.ApiUrl + '/api/nodes/datacreator/' + this.identity + '?' + (new Date()).getTime();
    return this.http.get<DataCreatedDetailedModel>(url, { headers });
  }


  ngOnDestroy() {
    this.chRef.detach();
    this.GetNodeObservable.unsubscribe();
    this.RouteObservable.unsubscribe();
  }

  ngOnInit() {
    this.isDarkTheme = $('body').hasClass('dark');
    this.RouteObservable = this.route.params.subscribe(params => {
      this.identity = params.identity;

      const startTime = new Date();

      this.identityIconUrl = this.httpService.ApiUrl + '/api/icon/node/' + this.identity + '/' + (this.isDarkTheme ? 'dark' : 'light') + '/48';

      // this.getIcon().subscribe(data => {
      //   debugger;
      // });

      this.GetNodeObservable = this.getNode().subscribe(data => {
        const endTime = new Date();
        // this.destroyTables();
        this.NodeModel = data;



        const diff = endTime.getTime() - startTime.getTime();
        let minWait = 0;
        if (diff < 150) {
          minWait = 150 - diff;
        }
        setTimeout(() => {
          this.isLoading = false;
          if (this.NodeModel == null) {
            this.failedLoading = true;
          }
        }, minWait);

      }, err => {
        this.failedLoading = true;
        this.isLoading = false;
      });

    });
  }

}
