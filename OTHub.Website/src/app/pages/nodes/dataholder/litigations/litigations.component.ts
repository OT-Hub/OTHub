import { Component, OnInit, Input } from '@angular/core';
import { HubHttpService } from '../../../hub-http-service';
import { HttpClient } from '@angular/common/http';
import { MyNodeService } from '../../mynodeservice';
import { ServerDataSource } from 'ng2-smart-table';
import * as moment from 'moment';
import { OfferIdColumnComponent } from '../../../miscellaneous/offeridcolumn.component';

@Component({
  selector: 'ngx-litigations',
  templateUrl: './litigations.component.html',
  styleUrls: ['./litigations.component.scss']
})
export class LitigationsComponent implements OnInit {

  constructor(private httpService: HubHttpService, private http: HttpClient, public myNodeService: MyNodeService) { }

  @Input('identity') identity: string; 
  isDarkTheme: boolean;

  ngOnInit(): void {
    const url = this.httpService.ApiUrl + '/api/nodes/dataholder/' + this.identity + '/litigations';

    this.source = new ServerDataSource(this.http, 
      { endPoint: url });
  }

  source: ServerDataSource;

  ExportToJson() {
    const url = this.httpService.ApiUrl +'/api/nodes/dataholder/' + this.identity + '/litigations?export=true&exporttype=0';
    window.location.href = url;
  }

  ExportToCsv() {
    const url = this.httpService.ApiUrl + '/api/nodes/dataholder/' + this.identity + '/litigations?export=true&exporttype=1';
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
        sort: true,
        title: 'Offer ID',
        renderComponent: OfferIdColumnComponent,
        type: 'custom',
        // valuePrepareFunction: (value) => {
        //   return '<a class="navigateJqueryToAngular" href="/offers/' + value + '" onclick="return false;" title="' + value + '" >' + value + '</a>';
        // }
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
      RequestedBlockIndex: {
        sort: true,
        title: 'Requested Block Index',
        type: 'string',
        filter: false,
        // valuePrepareFunction: (value) => {
        //   const stillUtc = moment.utc(value).toDate();
        //   const local = moment(stillUtc).local().format('DD/MM/YYYY HH:mm');
        //   return local;
        // }
      },
      RequestedObjectIndex: {
        sort: true,
        title: 'Requested Object Index',
        type: 'string',
        filter: false,
        // valuePrepareFunction: (value) => {
        //   if (value > 1440) {
        //     const days = (value / 1440);
        //     return days.toFixed(1).replace(/[.,]00$/, '') + (days === 1 ? ' day' : ' days');
        //   }
        //   return value + ' minute(s)';
        // }
      },
    },
    pager: {
      display: true,
      perPage: 10
    }
  };
}