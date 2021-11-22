import { AUTO_STYLE } from '@angular/animations';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Component, Input, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { NbComponentStatus } from '@nebular/theme';
import { HubHttpService } from 'app/pages/hub-http-service';
import { OfferIdColumnComponent } from 'app/pages/miscellaneous/offeridcolumn.component';
import { ServerDataSource, ViewCell } from 'ng2-smart-table';

@Component({
  selector: 'ngx-rpc',
  templateUrl: './rpc.component.html',
  styleUrls: ['./rpc.component.scss']
})
export class RpcComponent implements OnInit, OnDestroy {
  source: ServerDataSource;
  rows: any[];

  constructor(private http: HttpClient, private route: ActivatedRoute, private router: Router,
    private httpService: HubHttpService) {
    this.isLoading = true;
    this.failedLoading = false;
    const url = this.httpService.ApiUrl + '/api/rpc';

    const that = this;

    this.source = new ServerDataSource(http,
      { endPoint: url });

    this.source.onChanged().subscribe(e => {

      this.source.getAll().then(a => {
        that.rows = a;
      })
    });
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
      // Custom: {
      //   type: 'html',
      //   //renderComponent: RPCHealthColumnComponent,
      //   sort: false,
      //   filter: false,
      //   title: '',
      //   valuePrepareFunction: (value, row) =>  {

      //     const myBlockNumber = row.BlockNumber;

      //     var blockchainRows = this.rows.filter(r => r.BlockchainName == row.BlockchainName && row.Name != r.Name);

      //     let maxBlockNumber = myBlockNumber;

      //     for (let index = 0; index < blockchainRows.length; index++) {
      //       const blockchainRow = blockchainRows[index];
      //       const rowBlockNumber = blockchainRow.BlockNumber;

      //       if (rowBlockNumber > maxBlockNumber) {
      //         maxBlockNumber = rowBlockNumber;
      //       }
      //     }

      //     if (maxBlockNumber > myBlockNumber) {
      //       const difference = maxBlockNumber - myBlockNumber;
      //       if (difference == 1) {
      //         return '<span class="pass"></span>';
      //       } else if (difference <= 50) {
      //       return '<span class="amber"></span>';
      //       }
      //       return '<span class="fail"></span>';
      //     }

      //     return '<span class="pass"></span>';
      //   }
      // },
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
        type: 'html',
        sort: false,
        filter: false,
        title: 'Height',
        valuePrepareFunction: (value, row) =>  {

          const myBlockNumber = row.BlockNumber;

          var blockchainRows = this.rows.filter(r => r.BlockchainName == row.BlockchainName && row.Name != r.Name);

          let maxBlockNumber = myBlockNumber;

          for (let index = 0; index < blockchainRows.length; index++) {
            const blockchainRow = blockchainRows[index];
            const rowBlockNumber = blockchainRow.BlockNumber;

            if (rowBlockNumber > maxBlockNumber) {
              maxBlockNumber = rowBlockNumber;
            }
          }

          if (maxBlockNumber > myBlockNumber) {
            const difference = maxBlockNumber - myBlockNumber;
            if (difference == 1) {
              return '<div class="par"><span class="pass"></span>' + '<span>' + value + '</span></div>';
            } else if (difference <= 50) {
              return '<div class="par"><span class="amber"></span>' + '<span>' + value + '</span></div>';
            }
            return '<div class="par"><span class="fail"></span>' + '<span>' + value + '</span></div>';
          }

          return '<div class="par"><span class="pass"></span>' + '<span>' + value + '</span></div>';
        }
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
        type: 'html',
        sort: false,
        filter: false,
        title: 'Daily Score (Reliability)',
        valuePrepareFunction: (value, row) => {

          if (row.DailyRequestsTotal == 0) {
            return '';
          }

          const score = (Math.round(((row.DailySuccessTotal / row.DailyRequestsTotal) * 100) * 100) / 100);

          if (score > 99.0) {
            return '<div class="par"><span class="pass"></span>' + '<span>' + score.toString() + '%</span></div>';
          }
          if (score < 95) {
            return '<div class="par"><span class="fail"></span>' + '<span>' + score.toString() + '%</span></div>';
          }

          return '<div class="par"><span class="amber"></span>' + '<span>' + score.toString() + '%</span></div>';

        }
      },
      MonthlyRequestsTotal: {
        type: 'number',
        sort: false,
        filter: false,
        title: 'Monthly Requests'
      },
      MonthlySuccessTotal: {
        type: 'html',
        sort: false,
        filter: false,
        title: 'Monthly Score (Reliability)',
        valuePrepareFunction: (value, row) => {

          if (row.MonthlyRequestsTotal == 0) {
            return '';
          }

          const score = (Math.round(((row.MonthlySuccessTotal / row.MonthlyRequestsTotal) * 100) * 100) / 100);

          if (score > 99.0) {
            return '<div class="par"><span class="pass"></span>' + '<span>' + score.toString() + '%</span></div>';
          }
          if (score < 95) {
            return '<div class="par"><span class="fail"></span>' + '<span>' + score.toString() + '%</span></div>';
          }

          return '<div class="par"><span class="amber"></span>' + '<span>' + score.toString() + '%</span></div>';

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

@Component({
  selector: 'rpchealthcolumn',
  template: `
<nb-icon [icon]="icon"></nb-icon>
`,
})
export class RPCHealthColumnComponent implements ViewCell, OnInit {

  renderValue: string;

  @Input() value: string;
  @Input() rowData: any;

  status: NbComponentStatus;
  icon: string;

  constructor() {

  }

  ngOnInit() {
    debugger;
    this.status = 'danger';
    this.icon = 'alert-triangle';
      this.renderValue = this.value;
  }
}