import {ChangeDetectorRef, Component, OnDestroy, OnInit} from '@angular/core';
import {HttpClient, HttpHeaders} from '@angular/common/http';
import {DataHolderDetailedModel, DataHolderTestOnlineResult} from './dataholder-models';
import {ActivatedRoute} from '@angular/router';
import {MyNodeService} from '../mynodeservice';
import {HubHttpService} from '../../hub-http-service';
import {MyNodeModel} from '../mynodemodel';

declare const $: any;

@Component({
  selector: 'app-nodeprofile',
  templateUrl: './dataholder.component.html',
  styleUrls: ['./dataholder.component.scss']
})
export class DataHolderComponent implements OnInit, OnDestroy {


  constructor(private http: HttpClient, private route: ActivatedRoute,
              private chRef: ChangeDetectorRef, public myNodeService: MyNodeService, private httpService: HubHttpService) {
    this.isLoading = true;
    this.failedLoading = false;
    this.IsTestNet = httpService.IsTestNet;
  }

  IsTestNet: boolean;
  DisplayName: string;
  NodeModel: DataHolderDetailedModel;
  identity: string;
  failedLoading: boolean;
  isLoading: boolean;
  GetNodeObservable: any;
  RouteObservable: any;
  isCheckingNodeUptime = false;
  MyNode: MyNodeModel;
  OnlineCheckResult: DataHolderTestOnlineResult;
  uptimeChart: any;
  chartData: any;
  identityIconUrl: string;
  isDarkTheme: boolean;

  getNode() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    let url = this.httpService.ApiUrl + '/api/nodes/dataholder/' + this.identity;
    if (this.MyNode || this.IsTestNet === true) {
      url += '?includeNodeUptime=true&' + (new Date()).getTime();
    } else {
      url += '?' + (new Date()).getTime();
    }
    return this.http.get<DataHolderDetailedModel>(url, {headers});
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



  CheckNodeOnline() {
    this.OnlineCheckResult = null;
    this.isCheckingNodeUptime = true;

    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    const url = this.httpService.ApiUrl + '/api/nodes/dataholder/checkonline?identity=' + this.identity;
    const test = this.http.get<DataHolderTestOnlineResult>(url, {headers});

    const startTime = new Date();
    test.subscribe(data => {
      const endTime = new Date();
      this.OnlineCheckResult = data;

      const diff = endTime.getTime() - startTime.getTime();
      let minWait = 0;
      if (diff < 150) {
        minWait = 150 - diff;
      }
      setTimeout(() => {
        this.isCheckingNodeUptime = false;
      }, minWait);
    }, err => {
      this.isCheckingNodeUptime = false;

      //   $.notify({
      //     message: 'There was a problem connecting to the OT Hub API.'
      // },
      //     {
      //         type: 'bg-orange',
      //         allow_dismiss: true,
      //         newest_on_top: true,
      //         timer: 1000,
      //         placement: {
      //             from: 'top',
      //             align: 'left'
      //         },
      //         animate: {
      //             enter: 'animated fadeInLeft',
      //             exit: 'animated fadeOutLeft'
      //         },
      //         template: '<div data-notify="container" class="bootstrap-notify-container alert alert-dismissible {0} ' + (true ? 'p-r-35' : '') + '" role="alert">' +
      //             '<button type="button" aria-hidden="true" class="close" data-notify="dismiss">×</button>' +
      //             '<span data-notify="icon"></span> ' +
      //             '<span data-notify="title">{1}</span> ' +
      //             '<span data-notify="message">{2}</span>' +
      //             '<div class="progress" data-notify="progressbar">' +
      //             '<div class="progress-bar progress-bar-{0}" role="progressbar" aria-valuenow="0" aria-valuemin="0" aria-valuemax="100" style="width: 0%;"></div>' +
      //             '</div>' +
      //             '<a href="{3}" target="{4}" data-notify="url"></a>' +
      //             '</div>'
      //     });
      // });
    });
  }

    ngOnDestroy()
    {
      this.chRef.detach();
      this.GetNodeObservable.unsubscribe();
      this.RouteObservable.unsubscribe();
    }


    ngOnInit()
    {
      this.RouteObservable = this.route.params.subscribe(params => {
        // this.isDarkTheme = $('body').hasClass('dark');
        this.identity = params.identity;

        // tslint:disable-next-line:max-line-length
        this.identityIconUrl = this.httpService.ApiUrl + '/api/icon/node/' + this.identity + '/' + (this.isDarkTheme ? 'dark' : 'light') + '/48';

        if (this.identity) {
          this.MyNode = this.myNodeService.Get(this.identity);
          if (this.MyNode) {
            this.DisplayName = this.MyNode.DisplayName;
          }
        }

        this.GetNodeObservable = this.getNode().subscribe(data => {

          this.NodeModel = data;
          // debugger;
          this.chRef.detectChanges();

        }, err => {
          this.failedLoading = true;
          this.isLoading = false;
        });

      });
    }


}
