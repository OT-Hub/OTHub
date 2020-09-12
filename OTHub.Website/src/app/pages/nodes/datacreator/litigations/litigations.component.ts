import { Component, OnInit, Input } from '@angular/core';
import * as moment from 'moment';
import { HubHttpService } from '../../../hub-http-service';
import { HttpClient } from '@angular/common/http';
import { MyNodeService } from '../../mynodeservice';
import { ServerDataSource } from 'ng2-smart-table';
@Component({
  selector: 'datacreator-litigations',
  templateUrl: './litigations.component.html',
  styleUrls: ['./litigations.component.scss']
})
export class LitigationsComponent implements OnInit {

  constructor(private httpService: HubHttpService, private http: HttpClient, public myNodeService: MyNodeService) { }

  @Input('identity') identity: string; 
  isDarkTheme: boolean;

  ngOnInit(): void {
    const url = this.httpService.ApiUrl + '/api/nodes/datacreator/' + this.identity + '/litigations';

    this.source = new ServerDataSource(this.http, 
      { endPoint: url });
  }

  source: ServerDataSource;

  ExportToJson() {
    const url = this.httpService.ApiUrl +'/api/nodes/datacreator/' + this.identity + '/litigations?export=true&exporttype=0';
    window.location.href = url;
  }

  ExportToCsv() {
    const url = this.httpService.ApiUrl + '/api/nodes/datacreator/' + this.identity + '/litigations?export=true&exporttype=1';
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
        type: 'html',
        valuePrepareFunction: (value) => {
          return '<a class="navigateJqueryToAngular" href="/offers/' + value + '" onclick="return false;" title="' + value + '" >' + value + '</a>';
        }
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
      HolderIdentity: {
        sort: false,
        title: 'Data Holder',
        type: 'html',
        filter: true,
        valuePrepareFunction: (value) => {
          const name = this.myNodeService.GetName(value, false);
          return '<a class="navigateJqueryToAngular" href="/nodes/dataholders/' + value + '" onclick="return false;"><img class="lazy" style="height:16px;width:16px;" title="' + value + '" src="' + this.getIdentityIcon(value) + '"> ' + name + '</a>';
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
