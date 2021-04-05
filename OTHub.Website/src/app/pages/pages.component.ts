import { Component, OnInit } from '@angular/core';

import { MENU_ITEMS } from './pages-menu';
import { NbSidebarService } from '@nebular/theme';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { HubHttpService } from './hub-http-service';

@Component({
  selector: 'ngx-pages',
  styleUrls: ['pages.component.scss'],
  template: `
    <ngx-one-column-layout>
      <nb-menu [items]="menu" autoCollapse="true"></nb-menu>
      <router-outlet></router-outlet>
    </ngx-one-column-layout>
  `,
})
export class PagesComponent implements OnInit {

  constructor(private http: HttpClient, private httpService: HubHttpService) {

  }

  ngOnInit(): void {

    this.getBadge().subscribe(data => {
      var jobItem = this.menu.filter(x => x.title === 'Jobs')[0];

      var nodesItem = this.menu.filter(x => x.title === 'Nodes')[0];
      var dataHoldersItem = nodesItem.children.filter(x => x.title == 'Data Holders')[0];
      var dataCreatorsItem = nodesItem.children.filter(x => x.title == 'Data Creators')[0];

      jobItem.badge = {
        text: data.TotalJobs.toString(),
        status: 'primary',
      };

      dataHoldersItem.badge = {
        text: data.DataHolders.toString(),
        status: 'info',
      };

      dataCreatorsItem.badge = {
        text: data.DataCreators.toString(),
        status: 'info',
      };
    });
  }

  menu = MENU_ITEMS;

  getBadge() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    const url = this.httpService.ApiUrl + '/api/badge' + '?' + (new Date()).getTime();
    return this.http.get<BadgeModel>(url, { headers });
  }


}

export class BadgeModel {
  TotalJobs: number;
  DataHolders: number;
  DataCreators: number;
}
