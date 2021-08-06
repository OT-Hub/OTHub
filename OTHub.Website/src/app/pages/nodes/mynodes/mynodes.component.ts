import { DatacreatorsComponent } from './../datacreators/datacreators.component';
import { DataHoldersComponent } from './../dataholders/dataholders.component';
import { Component, OnInit, ViewChildren, OnDestroy, AfterViewInit, ViewChild, ElementRef, ChangeDetectorRef, AfterViewChecked, Inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { DataHolderDetailedModel } from '../dataholder/dataholder-models';
import { FormsModule } from '@angular/forms';
import { HubHttpService } from '../../hub-http-service';
import * as moment from 'moment';
import { RecentActivityJobModel } from './mynodes-model';
import { AuthService } from '@auth0/auth0-angular';
import { DOCUMENT } from '@angular/common';
import { WidgetConfiguration } from 'angular-telegram-login-widget/lib/types';
declare const $: any;
declare const swal: any;
@Component({
  selector: 'app-mynodes',
  templateUrl: './mynodes.component.html',
  styleUrls: ['./mynodes.component.scss']
})
export class MynodesComponent implements OnInit, OnDestroy, AfterViewInit, AfterViewChecked {
  usdAmountCalculationMode: string;


  constructor(private http: HttpClient, private httpService: HubHttpService, private auth: AuthService,  @Inject(DOCUMENT) private _document: Document) {
    this.isLoggedIn = false;
    this.isLoading = true;
  }

  isLoggedIn: boolean;
  isLoading: boolean
  showDataHolders: boolean;
  showDataCreators: boolean;
  showRecentActivity: boolean;
  isDarkTheme: boolean;
  @ViewChildren(DataHoldersComponent) dataHolders;
  @ViewChildren(DatacreatorsComponent) dataCreators;
  @ViewChild('recentActivityView') public recentActivityView: ElementRef;
  @ViewChild('dataHoldersView') public dataHoldersView: ElementRef;
  getNode(identity: string) {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    const url = this.httpService.ApiUrl + '/api/nodes/dataholder/' + identity + '?' + (new Date()).getTime();
    return this.http.get<DataHolderDetailedModel>(url, { headers });
  }

  ngAfterViewInit() {
  }

  ngAfterViewChecked() {

  }

  onTelegramLoad() {
    
  }

  onTelegramLoadError() {
   
  }

  onTelegramLogin(user: any) {
    this.sendTelegramLoginHash(JSON.stringify(user, null, 4));
  }

  sendTelegramLoginHash(body: string) {
    const headers = new HttpHeaders()
    .set('Content-Type', 'application/json')
    .set('Accept', 'application/json');
  const url = this.httpService.ApiUrl + '/api/telegram/linkaccount';
  this.http.post(url, body, { headers }).subscribe(data => {
  });
  }


  onUsdAmountCalculationModeChange(value: string) {
    const headers = new HttpHeaders()
    .set('Content-Type', 'application/json')
    .set('Accept', 'application/json');
  const url = this.httpService.ApiUrl + '/api/mynodes/UpdateMyNodesPriceCalculationMode?mode=' + value;
  this.http.post(url, { headers }).subscribe(data => {
  });
  }


  importNodes() {
    let data = prompt("Please paste the text you copied from the old OT Hub website.", "");
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    const url = this.httpService.ApiUrl + '/api/mynodes/importnodes?identities=' + data;
    this.http.post(url, { headers }).subscribe(data => {
      this._document.defaultView.location.reload();
    });
  }

  option;


  ngOnDestroy() {
    // this.recentActivityObserver.unsubscribe();
    // this.cdr.detach();
  }


  loadMyNodes() {

  }


  afterDataHoldersLoad(count: number) {
    //this.showDataHolders = count > 0;
  }

  afterDataCreatorsLoad(count: number) {
    //this.showDataCreators = count > 0;
  }

  showDataHoldersChanged(value: boolean) {
    this.showDataHolders = value;
    localStorage.setItem('MyNodes_ShowDataHolders', value.toString().toLowerCase());
  }

  showDataCreatorsChanged(value: boolean) {
    this.showDataCreators = value;
    localStorage.setItem('MyNodes_ShowDataCreators', value.toString().toLowerCase());
  }

  showRecentActivityChanged(value: boolean) {
    this.showRecentActivity = value;
    localStorage.setItem('MyNodes_ShowRecentActivity', value.toString().toLowerCase());
  }



  getIdentityIcon(identity: string, size: number) {
    return this.httpService.ApiUrl + '/api/icon/node/' + identity + '/' + (this.isDarkTheme ? 'dark' : 'light') + '/' + size;
  }


  // getRecentActivity() {
  //   const headers = new HttpHeaders()
  //     .set('Content-Type', 'application/json')
  //     .set('Accept', 'application/json');
  //   let url = this.httpService.ApiUrl + '/api/recentactivity?1=1';

  //   const myNodes = this.myNodeService.GetAll();
  //   // tslint:disable-next-line:prefer-for-of
  //   for (let index = 0; index < Object.keys(myNodes).length; index++) {
  //     const element = Object.values(myNodes)[index];
  //     url += '&identity=' + element.Identity;
  //   }

  //   url += '&' + (new Date()).getTime();
  //   return this.http.get<RecentActivityJobModel[]>(url, { headers });
  // }

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

  ngOnInit() {
    const self = this;

    self.loadMyNodes();

    this.auth.user$.subscribe(usr => {
      if (usr != null && this.isLoading == false) {
        this.isLoggedIn = true;
      
        const headers = new HttpHeaders()
        .set('Content-Type', 'application/json')
        .set('Accept', 'application/json');
      const url = this.httpService.ApiUrl + '/api/mynodes/MyNodesPriceCalculationMode';
      this.http.get<Number>(url, { headers }).subscribe(data => {
       this.usdAmountCalculationMode = data.toString();
      });
      }
      this.isLoading = false;
    });

    // self.recentActivityObserver = this.getRecentActivity().subscribe(data => {
    //   self.recentActivity = data;
    // });

    let rawValue = localStorage.getItem('MyNodes_ShowDataHolders');
    this.showDataHolders = rawValue ? rawValue == 'true' : true;

    rawValue = localStorage.getItem('MyNodes_ShowDataCreators');
    this.showDataCreators = rawValue ? rawValue == 'true' : false;

    rawValue = localStorage.getItem('MyNodes_ShowRecentActivity');
    this.showRecentActivity = rawValue ? rawValue == 'true' : true;


    this.isDarkTheme = $('body').hasClass('dark');

  }

}
