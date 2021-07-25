import { Component, OnInit } from '@angular/core';

import { MENU_ITEMS } from './pages-menu';
import { NbSidebarService } from '@nebular/theme';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { HubHttpService } from './hub-http-service';
import { Router } from '@angular/router';

@Component({
  selector: 'ngx-pages',
  styleUrls: ['pages.component.scss'],
  template: `
    <ngx-one-column-layout>
      <nb-menu [items]="menu" autoCollapse="true"></nb-menu>
      <router-outlet>
      <div class="alert alert-danger" *ngIf="liveOutages != null && liveOutages.length > 0">
        <b style="margin-bottom:6px;   display: flex;   align-items:center;">
         <nb-icon icon="alert-triangle-outline" status="danger" style="font-size:2rem;"></nb-icon> 
         <span style="margin-left:5px;">OT Hub has detected potential outages affecting the website</span>
        </b>
<div style="margin-top:10px;">
<div *ngFor="let error of liveOutages">
         {{error}}
         </div>
       <div style="margin-top:10px;" *ngIf="route != '/system/status'">
       For more details see the <a href="/system/status"> website status page</a>.
</div>
</div>
        </div>
      </router-outlet>
    </ngx-one-column-layout>
  `,
})
export class PagesComponent implements OnInit {
  route: string;

  constructor(private http: HttpClient, private httpService: HubHttpService, private router: Router) {

  }

  ngOnInit(): void {

    this.route = this.router.url;

    this.getBadge().subscribe(data => {

      this.liveOutages = data.LiveOutages;

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
  liveOutages: string[];

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
  LiveOutages: string[];
}
