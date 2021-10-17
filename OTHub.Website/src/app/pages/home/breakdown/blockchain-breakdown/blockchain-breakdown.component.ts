import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Component, Inject, NgZone, OnInit, PLATFORM_ID } from '@angular/core';

import { NbThemeService, NbMediaBreakpointsService, NbMediaBreakpoint } from '@nebular/theme';
import { HubHttpService } from 'app/pages/hub-http-service';
import { HomeV3Model } from '../../home.component';
import * as moment from 'moment';
import { Router } from '@angular/router';

@Component({
  selector: 'ngx-blockchain-breakdown',
  templateUrl: './blockchain-breakdown.component.html',
  styleUrls: ['./blockchain-breakdown.component.scss']
})
export class BlockchainBreakdownComponent implements OnInit {
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
    private http: HttpClient,
    private router: Router,
    @Inject(PLATFORM_ID) private platformId, private zone: NgZone) {
    this.breakpoints = this.breakpointService.getBreakpointsMap();
    this.themeSubscription = this.themeService.onMediaQueryChange()
      .subscribe(([oldValue, newValue]) => {
        this.breakpoint = newValue;
      });
  }

  formatTime(time: number) {
    if (time == null) {
      return '?';
    }
    return moment.duration(time, 'minutes').humanize();
  }

  ngOnInit() {
    this.getDataObservable = this.getHomeData().subscribe(data => {
      const endTime = new Date();
      this.Data = data;
      this.failedLoading = false;
      this.isLoading = false;
    }, err => {
      this.failedLoading = true;
      this.isLoading = false;
    });
  }

  getHomeData() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    const url = this.httpService.ApiUrl + '/api/home/HomeV3';
    return this.http.get<HomeV3Model>(url, { headers });
  }

  goBackClick() {
    this.router.navigateByUrl('/');
  }

  formatAmount(amount) {
    if (amount === null) {
      return null;
    }
    const split = amount.toString().split('.');
    let lastSplit = '';
    if (split.length === 2) {
      lastSplit = split[1];
      while(lastSplit[lastSplit.length - 1] == '0') {
        lastSplit = lastSplit.substr(0, lastSplit.length - 1);
      }
      if (lastSplit == '') {
        return split[0];
      }
      return split[0] + '.' + lastSplit;
    }
    return split[0];
  }

}
