import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { DataHolderDetailedModel, DataHolderTestOnlineResult } from './dataholder-models';
import { ActivatedRoute } from '@angular/router';
import { HubHttpService } from '../../hub-http-service';

declare const $: any;

@Component({
  selector: 'app-nodeprofile',
  templateUrl: './dataholder.component.html',
  styleUrls: ['./dataholder.component.scss']
})
export class DataHolderComponent implements OnInit, OnDestroy {


  constructor(private http: HttpClient, private route: ActivatedRoute,
    private chRef: ChangeDetectorRef, private httpService: HubHttpService) {
    this.isLoading = true;
    this.failedLoading = false;
    this.IsTestNet = httpService.IsTestNet;
    this.IsConvertingIdentityToNodeID = false;
  }

  IsConvertingIdentityToNodeID: boolean;
  IsTestNet: boolean;
  DisplayName: string;
  NodeModel: DataHolderDetailedModel;
  identity: string;
  failedLoading: boolean;
  isLoading: boolean;
  GetNodeObservable: any;
  RouteObservable: any;
  isCheckingNodeUptime = false;
  chartData: any;
  identityIconUrl: string;
  isDarkTheme: boolean;

  getNode() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    let url = this.httpService.ApiUrl + '/api/nodes/dataholder/' + this.identity;

    url += '?' + (new Date()).getTime();

    return this.http.get<DataHolderDetailedModel>(url, { headers });
  }

  AddToMyNodes() {
    // if (this.myNodeService.Get(this.NodeModel.Identity) == null) {
    //   const model = new MyNodeModel();
    //   model.Identity = this.NodeModel.Identity;
    //   model.DisplayName = null;
    //   this.myNodeService.Add(model);
    //   this.MyNode = model;
    // }
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





  ngOnDestroy() {
    this.chRef.detach();
    this.GetNodeObservable.unsubscribe();
    this.RouteObservable.unsubscribe();
  }

  loadData() {

    this.GetNodeObservable = this.getNode().subscribe(data => {

      this.NodeModel = data;
      // debugger;
      this.chRef.detectChanges();

    }, err => {
      this.failedLoading = true;
      this.isLoading = false;
    });
  }

  ngOnInit() {
    this.RouteObservable = this.route.params.subscribe(params => {
      // this.isDarkTheme = $('body').hasClass('dark');
      this.identity = params.identity;

      // tslint:disable-next-line:max-line-length
      this.identityIconUrl = this.httpService.ApiUrl + '/api/icon/node/' + this.identity + '/' + (this.isDarkTheme ? 'dark' : 'light') + '/48';

      if (this.identity.startsWith('0x')) {
        this.IsConvertingIdentityToNodeID = true;

        const headers = new HttpHeaders()
          .set('Content-Type', 'application/json')
          .set('Accept', 'application/json');
        let url = this.httpService.ApiUrl + '/api/nodes/dataholders/GetNodeIDForIdentity?identity=' + this.identity;
        this.http.get<string>(url, { headers }).subscribe(data => {
          this.identity = data;
          if (this.identity != null) {
            this.loadData();
            this.IsConvertingIdentityToNodeID = false;
          }
        });

      } else {
        this.loadData();
      }
    });
  }


}
