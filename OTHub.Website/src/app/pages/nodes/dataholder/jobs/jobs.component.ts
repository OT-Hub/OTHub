import { Component, OnInit, Input } from '@angular/core';
import { HubHttpService } from '../../../hub-http-service';
import { ServerDataSource } from 'ng2-smart-table';
import { HttpClient } from '@angular/common/http';
import * as moment from 'moment';
import { MyNodeService } from '../../mynodeservice';

@Component({
  selector: 'ngx-jobs',
  templateUrl: './jobs.component.html',
  styleUrls: ['./jobs.component.scss']
})
export class JobsComponent implements OnInit {

  constructor(private httpService: HubHttpService, private http: HttpClient, public myNodeService: MyNodeService) { 
 
  }

  @Input('identity') identity: string; 
  isDarkTheme: boolean;

  ngOnInit(): void {
    const url = this.httpService.ApiUrl + '/api/nodes/dataholder/' + this.identity + '/GetJobs';

    this.source = new ServerDataSource(this.http, 
      { endPoint: url });
  }

  source: ServerDataSource;

  ExportToJson() {
    const url = this.httpService.ApiUrl +'/api/nodes/dataholder/' + this.identity + '/GetJobs?export=true&exporttype=0';
    window.location.href = url;
  }

  ExportToCsv() {
    const url = this.httpService.ApiUrl + '/api/nodes/dataholder/' + this.identity + '/GetJobs?export=true&exporttype=1';
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
        type: 'html',
        valuePrepareFunction: (value) => {
          return '<a class="navigateJqueryToAngular" href="/offers/' + value + '" onclick="return false;" title="' + value + '" >' + value.substring(0, 40) + '...</a>';
        }
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
        title: 'Holding Time',
        type: 'string',
        filter: false,
        valuePrepareFunction: (value) => {
          if (value > 1440) {
            const days = (value / 1440);
            return days.toFixed(1).replace(/[.,]00$/, '') + (days === 1 ? ' day' : ' days');
          }
          return value + ' minute(s)';
        }
      },
      TokenAmountPerHolder: {
        sort: true,
        title: 'Token Amount',
        type: 'number',
        filter: false,
        valuePrepareFunction: (value) => {
          const tokenAmount = parseFloat(value);
          return tokenAmount.toFixed(2).replace(/[.,]00$/, '');
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
        type: 'html',
        filter: false,
        valuePrepareFunction: (value, row) => {
          if (row.CanPayout === true) {
            if (this.myNodeService.Get(this.identity)) {
              return 'No (<a routerLink="/nodes/dataholders/' + this.identity + '/payout/' + row.OfferId + '">Payout</a>)';
            }
          }
          
          return row.Paidout === true ? 'Yes' : 'No';
        }
      }
    },
    pager: {
      display: true,
      perPage: 10
    }
  };

}