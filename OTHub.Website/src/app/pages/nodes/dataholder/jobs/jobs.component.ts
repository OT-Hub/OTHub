import { Component, OnInit, Input } from '@angular/core';
import { HubHttpService } from '../../../hub-http-service';
import { ServerDataSource } from 'ng2-smart-table';
import { HttpClient } from '@angular/common/http';
import * as moment from 'moment';
import { OfferIdColumnComponent } from '../../../miscellaneous/offeridcolumn.component';
import { PaidoutColumnComponent } from './paidoutcolumns.component';

@Component({
  selector: 'ngx-jobs',
  templateUrl: './jobs.component.html',
  styleUrls: ['./jobs.component.scss']
})
export class JobsComponent implements OnInit {

  constructor(private httpService: HubHttpService, private http: HttpClient) { 
 
  }

  @Input('identity') identity: string; 
  isDarkTheme: boolean;

  ngOnInit(): void {
    const url = this.httpService.ApiUrl + '/api/nodes/dataholder/' + this.identity + '/jobs';

    this.source = new ServerDataSource(this.http, 
      { endPoint: url });
  }

  source: ServerDataSource;

  ExportToJson() {
    const url = this.httpService.ApiUrl +'/api/nodes/dataholder/' + this.identity + '/jobs?export=true&exporttype=0';
    window.location.href = url;
  }

  ExportToCsv() {
    const url = this.httpService.ApiUrl + '/api/nodes/dataholder/' + this.identity + '/jobs?export=true&exporttype=1';
    window.location.href = url;
  }

  pageSizeChanged(event) {
    this.source.setPaging(1, event, true);
  }

  getIdentityIcon(identity: string) {
    return this.httpService.ApiUrl + '/api/icon/node/' + identity + '/' + (this.isDarkTheme ? 'dark' : 'light') + '/16';
  }

  settings = {
    actions:  {
add: false,
edit: false,
delete: false
    },
    columns: {
      OfferId: {
        sort: false,
        title: 'Offer ID',
        type: 'custom',
        renderComponent: OfferIdColumnComponent,
        // valuePrepareFunction: (value) => {
        //   return '<a class="navigateJqueryToAngular" href="/offers/' + value + '" onclick="return false;" title="' + value + '" >' + value.substring(0, 40) + '...</a>';
        // }
      },
      FinalizedTimestamp: {
        sort: true,
        sortDirection: 'desc',
        title: 'Start',
        type: 'string',
        filter: false,
        valuePrepareFunction: (value) => {
          const stillUtc = moment.utc(value).toDate();
          const local = moment(stillUtc).local().format('DD/MM/YYYY HH:mm');
          return local;
        }
      },
      EndTimestamp: {
        sort: true,
        title: 'End',
        type: 'string',
        filter: false,
        valuePrepareFunction: (value) => {
          const stillUtc = moment.utc(value).toDate();
          const local = moment(stillUtc).local().format('DD/MM/YYYY HH:mm');
          return local;
        }
      },
      HoldingTimeInMinutes: {
        sort: true,
        title: 'Total Time',
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
      RemainingTime: {
        sort: false,
        title: 'Remaining Time',
        type: 'string',
        filter: false,
        valuePrepareFunction: (value, row) => {

          let endDate = row.EndTimestamp;

          const stillUtc = moment.utc(endDate);
          const nowUtc = moment.utc();

          let daysRemaining = stillUtc.diff(nowUtc, 'days');

          if (daysRemaining > 365) {
            if (daysRemaining > 730) {
              let yearsRemaining = stillUtc.diff(nowUtc, 'years', true);
              return +yearsRemaining.toFixed(1) + ' years';
            } else {
            let monthsRemaining = stillUtc.diff(nowUtc, 'months');
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
        type: 'number',
        filter: false,
        valuePrepareFunction: (value) => {
          let tokenAmount = parseFloat(value);
          let formatted = +tokenAmount.toFixed(4);
          return formatted;
        }
      },
      Status: {
        sort: true,
        title: 'Status',
        type: 'string',
        filter: false
      },
      Paidout: {
        sort: true,
        title: 'Paidout',
        type: 'custom',
        renderComponent: PaidoutColumnComponent,
        filter: false,
        // valuePrepareFunction: (value, row) => {
        //   if (row.CanPayout === true) {
        //     if (this.myNodeService.Get(this.identity)) {
        //       return 'No (<a href="/nodes/dataholders/' + this.identity + '/payout/' + row.OfferId + '">Payout</a>)';
        //     }
        //   }
          
        //   return row.Paidout === true ? 'Yes' : 'No';
        // }
      }
    },
    pager: {
      display: true,
      perPage: 10
    }
  };

}
