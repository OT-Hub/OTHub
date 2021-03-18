import { Component, OnInit } from '@angular/core';
import {OfferIdColumnComponent} from "../miscellaneous/offeridcolumn.component";
import * as moment from "moment";
import {ServerDataSource} from "ng2-smart-table";
import {HttpClient, HttpHeaders} from "@angular/common/http";
import {HubHttpService} from "../hub-http-service";
import {DataHolderDetailedModel} from "../nodes/dataholder/dataholder-models";

@Component({
  selector: 'ngx-starfleetboarding',
  templateUrl: './starfleetboarding.component.html',
  styleUrls: ['./starfleetboarding.component.scss']
})
export class StarfleetboardingComponent implements OnInit {

  constructor(private http: HttpClient, private httpService: HubHttpService) {
    const url = this.httpService.ApiUrl + '/api/starfleetboarding';

    this.source = new ServerDataSource(http,
      { endPoint: url });
  }

  stakedTotal: number;
  source: ServerDataSource;


  getStaked() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    let url = this.httpService.ApiUrl + '/api/starfleetboarding/total';
    return this.http.get<number>(url, {headers});
  }

  ngOnInit(): void {
    this.getStaked().subscribe(data => {
      this.stakedTotal = data;
    });
  }

  pageSizeChanged(event) {
    this.source.setPaging(1, event, true);
  }

  settings = {
    actions:  {
      add: false,
      edit: false,
      delete: false
    },
    columns: {
      Address: {
        type: 'string',
        sort: true,
        filter: true,
        title: 'Address'
      },
      Amount: {
        title: 'Amount',
        type: 'number',
        filter: false,
        sort: true,
        sortDirection: 'desc',
        valuePrepareFunction: (value) => {
          const tokenAmount = parseFloat(value);
          return tokenAmount.toFixed(2).replace(/[.,]00$/, '');
        }
      },
    },
    pager: {
      display: true,
      perPage: 25
    }
  };

}
