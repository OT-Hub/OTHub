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
          return ((row.DailySuccessTotal / row.DailyRequestsTotal) * 100).toString() + '%';
        }
      },
      MonthlyRequestsTotal: {
        type: 'number',
        sort: false,
        filter: false,
        title: 'Monthly Requets'
      },
      MonthlySuccessTotal: {
        type: 'string',
        sort: false,
        filter: false,
        title: 'Monthly Score (Reliability)',
        valuePrepareFunction: (value, row) => {
          return ((row.MonthlySuccessTotal / row.MonthlyRequestsTotal) * 100).toString() + '%';
        }
      },
    }
  };


  ngOnInit(): void {

  }

  ngOnDestroy() {
}


}