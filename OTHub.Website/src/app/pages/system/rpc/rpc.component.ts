import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { HubHttpService } from 'app/pages/hub-http-service';
import { ServerDataSource } from 'ng2-smart-table';

@Component({
  selector: 'ngx-rpc',
  templateUrl: './rpc.component.html',
  styleUrls: ['./rpc.component.scss']
})
export class RpcComponent implements OnInit, OnDestroy {
  source: ServerDataSource;

  constructor(private http: HttpClient, private route: ActivatedRoute, private router: Router,
    private httpService: HubHttpService) {
    this.isLoading = true;
    this.failedLoading = false;
    const url = this.httpService.ApiUrl + '/api/rpc';

    this.source = new ServerDataSource(http,
      { endPoint: url });
   }
  isLoading: boolean;
  failedLoading: boolean;

  settings = {
    actions:  {
add: false,
edit: false,
delete: false
    },
    columns: {
      BlockchainName: {
        type: 'string',
        sort: false,
        filter: false,
        title: 'Blockchain'
      },
      Name: {
        type: 'string',
        sort: false,
        filter: false,
        title: 'RPC Name'
      },
      BlockNumber: {
        type: 'number',
        sort: false,
        filter: false,
        title: 'Height'
      },
      Weight: {
        type: 'string',
        sort: false,
        filter: false,
        title: 'Weight',
        valuePrepareFunction: (value) => {
          return value + '%';
        }
      },
      DailyRequestsTotal: {
        type: 'number',
        sort: false,
        filter: false,
        title: 'Daily Requests'
      },
      DailySuccessTotal: {
        type: 'string',
        sort: false,
        filter: false,
        title: 'Daily Score (Reliability)',
        valuePrepareFunction: (value, row) => {

          if (row.DailyRequestsTotal == 0) {
            return '';
          }

          return (Math.round(((row.DailySuccessTotal / row.DailyRequestsTotal) * 100) * 100) / 100).toString() + '%';
        }
      },
      MonthlyRequestsTotal: {
        type: 'number',
        sort: false,
        filter: false,
        title: 'Monthly Requests'
      },
      MonthlySuccessTotal: {
        type: 'string',
        sort: false,
        filter: false,
        title: 'Monthly Score (Reliability)',
        valuePrepareFunction: (value, row) => {

          if (row.MonthlyRequestsTotal == 0) {
            return '';
          }

          return (Math.round(((row.MonthlySuccessTotal / row.MonthlyRequestsTotal) * 100) * 100) / 100).toString() + '%';
        }
      },
      Performance: {
        type: 'string',
        sort: false,
        filter: false,
        title: 'Average Response',
        valuePrepareFunction: (value, row) => {
          if (value != null) {
            if (value < 1000) {
              return value + 'ms';
            } else if (value <= 60000) {
              return Math.round((value / 1000) * 100) / 100 + ' seconds';
            }
            else {
              return Math.round(((value / 1000) / 60) * 100) / 100 + ' minutes';
            }
          }

          return '';
        }
      },
    }
  };


  ngOnInit(): void {

  }

  ngOnDestroy() {
}


}