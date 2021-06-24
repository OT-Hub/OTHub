import { Component, OnInit, Input } from '@angular/core';
import { Router } from '@angular/router';
import { HubHttpService } from '../../../hub-http-service';
import { HttpClient } from '@angular/common/http';
import { ServerDataSource } from 'ng2-smart-table';
import * as moment from 'moment';
import { OfferIdColumnComponent } from '../../../miscellaneous/offeridcolumn.component';
@Component({
  selector: 'ngx-payouts',
  templateUrl: './payouts.component.html',
  styleUrls: ['./payouts.component.scss']
})
export class PayoutsComponent implements OnInit {

  constructor(private httpService: HubHttpService, private http: HttpClient, private router: Router) { 
 
  }

  @Input('identity') identity: string; 
  isDarkTheme: boolean;

  ngOnInit(): void {
    const url = this.httpService.ApiUrl + '/api/nodes/dataholder/' + this.identity + '/payouts';

    this.source = new ServerDataSource(this.http, 
      { endPoint: url });
  }

  source: ServerDataSource;

  ExportToJson() {
    const url = this.httpService.ApiUrl +'/api/nodes/dataholder/' + this.identity + '/payouts?export=true&exporttype=0';
    window.location.href = url;
  }

  ExportToCsv() {
    const url = this.httpService.ApiUrl + '/api/nodes/dataholder/' + this.identity + '/payouts?export=true&exporttype=1';
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
        //   return '<a class="navigateJqueryToAngular" href="/offers/' + value + '" onclick="return false;" title="' + value + '" >' + value + '</a>';
        // }
      },
      TransactionHash: {
        sort: false,
        title: 'Transaction Hash',
        type: 'string',
        filter: true,
      },
      Timestamp: {
        sort: true,
        sortDirection: 'desc',
        title: 'Timestamp',
        type: 'string',
        filter: false,
        valuePrepareFunction: (value) => {
          const stillUtc = moment.utc(value).toDate();
          const local = moment(stillUtc).local().format('DD/MM/YYYY HH:mm');
          return local;
        }
      },
      Amount: {
        sort: true,
        title: 'Token Amount',
        type: 'number',
        filter: false,
        valuePrepareFunction: (value) => {
          const tokenAmount = parseFloat(value);
          return +tokenAmount.toFixed(4).replace(/[.,]00$/, '');
        }
      },
      GasUsed: {
        sort: true,
        title: 'Transaction Fee',
        type: 'string',
        filter: false,
        valuePrepareFunction: (value, row) => {
          return +(row.GasUsed * (row.GasPrice / 1000000000000000000)).toFixed(8) + ' ' + row.GasTicker;
        }
      },
    },
    pager: {
      display: true,
      perPage: 10
    }
  };
  ViewPayoutsInUSD() {
    this.router.navigateByUrl('/nodes/dataholders/' + this.identity + '/report/usd');
  }

}
