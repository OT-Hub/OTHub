import { Component, OnInit, ViewChild } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { HubHttpService } from 'app/pages/hub-http-service';
import { NbCalendarRange } from '@nebular/theme';
import { ServerDataSource } from 'ng2-smart-table';
import { OfferIdColumnComponent } from 'app/pages/miscellaneous/offeridcolumn.component';

@Component({
  selector: 'ngx-mynodestaxexport',
  templateUrl: './mynodestaxexport.component.html',
  styleUrls: ['./mynodestaxexport.component.scss']
})
export class MynodestaxexportComponent implements OnInit {

  constructor(private http: HttpClient, private httpService: HubHttpService) { 
    this.usdAmountCalculationMode = '1';
    this.includeActiveJobs = false;
    this.includeCompletedJobs = true;
    // this.includeActiveJobsDisabled = true;
    // this.includeCompletedJobsDisabled = true;

    var start = new Date();
var end = new Date();
start.setDate(end.getDate()-365);

    this.range = {
      start: start,
      end: end
    };
  }

  settings = {
    actions:  {
add: false,
edit: false,
delete: false
    },
    columns: {
      OfferID: {
        sort: false,
        title: 'Offer ID',
        type: 'custom',
        renderComponent: OfferIdColumnComponent,
      },
      Date: {
        sort: false,
        title: 'Start',
        type: 'string',
        filter: false,
        valuePrepareFunction: (value) => {
          return value;
        }
      },
      Amount: {
        sort: false,
        title: 'Token Amount',
        type: 'number',
        filter: false,
        valuePrepareFunction: (value) => {
          let tokenAmount = parseFloat(value);
          let formatted = +tokenAmount.toFixed(4);
          return value;
        }
      },
      USDAmount: {
        sort: false,
        title: 'USD Amount',
        type: 'number',
        filter: false,
        valuePrepareFunction: (value) => {
          let tokenAmount = parseFloat(value);
          let formatted = +tokenAmount.toFixed(4);
          return value;
        }
      },
      TickerTimestamp: {
        sort: false,
        title: 'USD Ticker Timestamp',
        type: 'number',
        filter: false,
        valuePrepareFunction: (value) => {
          return value;
        }
      },
      TickerUSDPrice: {
        sort: false,
        title: 'USD Ticker Amount',
        type: 'number',
        filter: false,
        valuePrepareFunction: (value) => {
          return value;
        }
      },
    }
  };
  source: ServerDataSource;

  range: NbCalendarRange<Date>;
  usdAmountCalculationMode: string;
  includeActiveJobs: boolean;
  includeCompletedJobs: boolean;
  includeActiveJobsDisabled: boolean;
  includeCompletedJobsDisabled: boolean;
  selectedNode: string;
  @ViewChild('formpicker') input; 

  onUsdAmountCalculationModeChange(value: string) {
    if (value == '0' || value == '1') {
      this.includeActiveJobs = false;
      //this.includeActiveJobsDisabled = true;
      //this.includeCompletedJobsDisabled = true;
    } else {
      this.includeActiveJobsDisabled = false;
      this.includeCompletedJobsDisabled = false;
    }
  }

  changeNode(nodeName: string) {
  }

  toggleActiveJobs(checked: boolean) {
    this.includeActiveJobs = checked;
  }

  toggleCompletedJobs(checked: boolean) {
    this.includeCompletedJobs = checked;
  }

  generateReport() {

    let start = this.range.start.toISOString();
    let end = this.range.end.toISOString();

    const headers = new HttpHeaders()
    .set('Content-Type', 'application/json')
    .set('Accept', 'application/json');
  const url = this.httpService.ApiUrl + '/api/mynodes/TaxReport?usdMode=' + this.usdAmountCalculationMode + '&nodeID=&startDate=' + start + '&endDate=' + end + '&includeActiveJobs=' + this.includeActiveJobs + '&includeCompletedJobs=' + this.includeCompletedJobs;

  this.source = new ServerDataSource(this.http, 
    { endPoint: url });
  // this.http.post(url, { headers }).subscribe(data => {
  //   debugger;
  // });
  }

  ngOnInit(): void {
  

  }
}
