import {Component, OnDestroy, OnInit} from '@angular/core';
import { NbThemeService, NbMediaBreakpoint, NbMediaBreakpointsService } from '@nebular/theme';
import {SystemStatusModel} from "../system/status/system-models";
import {HttpClient, HttpHeaders} from "@angular/common/http";
import {MyNodeService} from "../nodes/mynodeservice";
import {HubHttpService} from "../hub-http-service";

@Component({
  selector: 'ngx-ecommerce',
  templateUrl: './e-commerce.component.html',
  styleUrls: ['./e-commerce.component.scss'],
})
export class ECommerceComponent implements OnDestroy, OnInit {
  breakpoint: NbMediaBreakpoint;
  breakpoints: any;
  themeSubscription: any;
  getDataObservable: any;
  Data: HomeV3Model;
  failedLoading: boolean;
  isLoading: boolean;

  constructor(private themeService: NbThemeService,
              private breakpointService: NbMediaBreakpointsService,
              private httpService: HubHttpService,
              private http: HttpClient) {

    this.breakpoints = this.breakpointService.getBreakpointsMap();
    this.themeSubscription = this.themeService.onMediaQueryChange()
      .subscribe(([oldValue, newValue]) => {
        this.breakpoint = newValue;
      });
  }

  ngOnDestroy() {
    this.themeSubscription.unsubscribe();
    this.getDataObservable.unsubscribe();
  }


  getData() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    const url = this.httpService.ApiUrl + '/api/home/HomeV3';
    return this.http.get<HomeV3Model>(url, { headers });
  }

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
}

export class HomeV3Model {
TotalJobs: number;
ActiveNodes: number;
ActiveJobs: number;
Jobs24H: number;
StakedTokens: string;
FeesByBlockchain: HomeFeesByBlockchainModel[];
StakedByBlockchain: HomeStakedTokensByBlockchainModel[];
}
export class HomeFeesByBlockchainModel {
  BlockchainName: string;
  NetworkName: string;
  ShowCostInUSD: boolean;
  JobCreationCost: number;
  JobFinalisedCost: number;
}
export class HomeStakedTokensByBlockchainModel {
  BlockchainName: string;
  NetworkName: string;
  StakedTokens: string;
}
