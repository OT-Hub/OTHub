import { Component, OnInit, ChangeDetectorRef, OnDestroy, NgZone, ViewChild, Inject, PLATFORM_ID } from '@angular/core';
import {Router} from '@angular/router';
import {
  HttpClient,
  HttpHeaders
} from '@angular/common/http';
import { OTOfferSummaryModel, OTOfferSummaryWithPaging } from './offers-models';
import { MomentModule } from 'ngx-moment';
import { HubHttpService } from '../../hub-http-service';
declare const $: any;
import * as moment from 'moment';
import { LocalDataSource, ServerDataSource } from 'ng2-smart-table';
import { DataCreatorColumnComponent } from './datacreatorcolumn.component';
import { OfferIdColumnComponent } from '../../miscellaneous/offeridcolumn.component';
import * as am4core from '@amcharts/amcharts4/core';
import * as am4charts from '@amcharts/amcharts4/charts';
import am4themes_animated from '@amcharts/amcharts4/themes/animated';
import {AxisDataItem, DateAxisDataItem} from "@amcharts/amcharts4/charts";
import { isPlatformBrowser } from '@angular/common';
import { HomeJobsChartDataModel } from 'app/pages/home/home.component';
import { NbThemeService } from '@nebular/theme';

@Component({
  selector: 'ngx-offers',
  templateUrl: './offers.component.html',
  styleUrls: ['./offers.component.scss']
})
export class OffersComponent implements OnInit, OnDestroy {

  constructor(private http: HttpClient, private chRef: ChangeDetectorRef, private httpService: HubHttpService, private router: Router,
              private zone: NgZone, 
              private themeService: NbThemeService,
              @Inject(PLATFORM_ID) private platformId) {
    this.isLoading = true;
    this.failedLoading = false;
    // const data = this.service.getData();
    // this.source.load(data);

    //const url = this.httpService.ApiUrl + '/api/jobs/paging?pageLength=' + pageLength + '&start=' + start + '&filter=' + searchFilter + '&' + (new Date()).getTime();
    const url = this.httpService.ApiUrl + '/api/jobs/paging';

    this.source = new ServerDataSource(http,
      { endPoint: url });
  }

  source: ServerDataSource;



  ExportToJson() {
    const url = this.httpService.ApiUrl + '/api/jobs/paging?export=true&exporttype=0';
    window.location.href = url;
  }

  ExportToCsv() {
    const url = this.httpService.ApiUrl + '/api/jobs/paging?export=true&exporttype=1';
    window.location.href = url;
  }

  pageSizeChanged(event) {
    this.source.setPaging(1, event, true);
  }

  browserOnly(f: () => void) {
    if (isPlatformBrowser(this.platformId)) {
      this.zone.runOutsideAngular(() => {
        f();
      });
    }
  }


  settings = {
    actions:  {
add: false,
edit: false,
delete: false
    },
    columns: {
      // DCIdentity: {
      //   title: 'DC',
      //   width: '1%',
      //   type: 'custom',
      //   filter: false,
      //   sort: false,
      //   editable: false,
      //   addable: false,
      //   renderComponent: DataCreatorColumnComponent,
      //   // valuePrepareFunction: (value) => {
      //   //   if (!value) {
      //   //     return 'Unknown';
      //   //   }
      //
      //   //   return '<a target=_self href="/nodes/datacreators/' + value +
      //   //    '""><img class="lazy" style="height:16px;width:16px;" title="' +
      //   //     value + '" src="' + this.getIdentityIcon(value) + '"></a>';
      //   // }
      // },
      BlockchainDisplayName: {
        type: 'string',
        sort: false,
        filter: false,
        title: 'Blockchain'
      },
      OfferId: {
        sort: false,
        title: 'Offer ID',
        type: 'custom',
        renderComponent: OfferIdColumnComponent,
        // valuePrepareFunction: (value) => {
        //   return '<a class="navigateJqueryToAngular" href="/offers/' + value + '" onclick="return false;" title="' + value + '" >' + value.substring(0, 40) + '...</a>';
        // }
      },
      // CreatedTimestamp: {
      //   sort: true,
      //   sortDirection: 'desc',
      //   //width: '10%',
      //   title: 'Created',
      //   type: 'string',
      //   filter: false,
      //   valuePrepareFunction: (value) => {
      //     const stillUtc = moment.utc(value).toDate();
      //     const local = moment(stillUtc).local().format('DD/MM/YYYY HH:mm');
      //     return local;
      //   }
      // },
      FinalizedTimestamp: {
         sort: true,
         sortDirection: 'desc',
        title: 'Started',
        type: 'string',
        filter: false,
        valuePrepareFunction: (value) => {
          if (value == null)
          return '';
          const stillUtc = moment.utc(value).toDate();
          const local = moment(stillUtc).local().format('DD/MM/YYYY HH:mm');
          return local;
        }
      },
      DataSetSizeInBytes: {
        sort: true,
        //width: '5%',
        title: 'Data Set Size',
        type: 'string',
        filter: false,
        valuePrepareFunction: (value) => { return (value / 1000).toFixed(2).replace(/[.,]00$/, '') + ' KB';}
      },
      HoldingTimeInMinutes: {
        sort: true,
        //width: '5%',
        title: 'Holding Time',
        type: 'string',
        filter: false,
        valuePrepareFunction: (value) => {
          const nowUtc = moment.utc();
          const endDate = moment.utc().add(value, 'minutes');
       
          let daysRemaining = endDate.diff(nowUtc, 'days');

          if (daysRemaining > 365) {
            if (daysRemaining > 730) {
              let yearsRemaining = endDate.diff(nowUtc, 'years', true);
              return +yearsRemaining.toFixed(1) + ' years';
            } else {
            let monthsRemaining = endDate.diff(nowUtc, 'months');
            return monthsRemaining + ' months';
            }
          }
          else if (daysRemaining >= 1) {
            return daysRemaining + ' days';
          } else if (daysRemaining < 0) {
            return 'None';
          } else {
            let hoursRemaining = endDate.diff(nowUtc, 'hours');
            if (hoursRemaining < 2) {
              let minutesRemaining = endDate.diff(nowUtc, 'minutes');
              return minutesRemaining + ' minutes';
            }
            return hoursRemaining + ' hours';
          }
        }
      },
      TokenAmountPerHolder: {
        sort: true,
        title: 'Token Amount',
        //width: '5%',
        type: 'number',
        filter: false,
        valuePrepareFunction: (value) => {
          let tokenAmount = parseFloat(value);
          let formatted = +tokenAmount.toFixed(20);
          return formatted;
        }
      },
      EstimatedLambda: {
        title: 'Price Factor',
        sort: false,
        type: 'number',
        filter: false,
        valuePrepareFunction: (value, row) => {
          if (value == null) {
            return 'N/A';
          }
          return value + ' (' + row.EstimatedLambdaConfidence + '% match)';
        }
      },
      Status: {
        sort: false,
        title: 'Status',
        type: 'string',
        //width: '5%',
        filter: false
      }
    },
    pager: {
      display: true,
      perPage: 25
    }
  };

  Summary: OTOfferSummaryWithPaging;
  OffersModel: OTOfferSummaryModel[];
  dataTable: any;
  dataTableOptions: any;
  exportOptionsObj: any;
  GetOffersObserver: any;
  isTableInit = false;
  failedLoading: boolean;
  isLoading: boolean;
  isDarkTheme: boolean;

  


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



  ngOnDestroy() {

  }

  ngOnInit() {

    const that = this;

  
  }

 
}
