import { Component, OnInit, Input } from '@angular/core';
import * as moment from 'moment';
import { HubHttpService } from '../../../hub-http-service';
import { HttpClient } from '@angular/common/http';
import { ServerDataSource } from 'ng2-smart-table';

@Component({
  selector: 'datacreator-transfers',
  templateUrl: './transfers.component.html',
  styleUrls: ['./transfers.component.scss']
})
export class TransfersComponent implements OnInit {

  constructor(private httpService: HubHttpService, private http: HttpClient) { 
 
  }

  @Input('identity') identity: string; 
  isDarkTheme: boolean;

  ngOnInit(): void {
    const url = this.httpService.ApiUrl + '/api/nodes/datacreator/' + this.identity + '/profiletransfers';

    this.source = new ServerDataSource(this.http, 
      { endPoint: url });
  }

  source: ServerDataSource;

  ExportToJson() {
    const url = this.httpService.ApiUrl +'/api/nodes/datacreator/' + this.identity + '/profiletransfers?export=true&exporttype=0';
    window.location.href = url;
  }

  ExportToCsv() {
    const url = this.httpService.ApiUrl + '/api/nodes/datacreator/' + this.identity + '/profiletransfers?export=true&exporttype=1';
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
      Type: {
        sort: false,
        filter: false,
        title: 'Type',
        type: 'string',
        valuePrepareFunction: (value, row) => {
          return row.Amount > 0 ? 'Deposit' : 'Withdrawal';
        }
      },
      Amount: {
        sort: true,
        title: 'Token Amount',
        type: 'number',
        filter: false,
        valuePrepareFunction: (value) => {
          const tokenAmount = parseFloat(value);
          return tokenAmount.toFixed(2).replace(/[.,]00$/, '');
        }
      },
      GasUsed: {
        sort: true,
        title: 'Transaction Fee',
        type: 'string',
        filter: false,
        valuePrepareFunction: (value, row) => {
          return (row.GasUsed * (row.GasPrice / 1000000000000000000)).toFixed(6) + ' ETH';
        }
      },
    },
    pager: {
      display: true,
      perPage: 10
    }
  };

}
