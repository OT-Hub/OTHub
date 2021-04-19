import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '@auth0/auth0-angular';
import { HubHttpService } from 'app/pages/hub-http-service';

@Component({
  selector: 'ngx-mynodesoverview',
  templateUrl: './mynodesoverview.component.html',
  styleUrls: ['./mynodesoverview.component.scss']
})
export class MynodesoverviewComponent implements OnInit {
  isLoggedIn: boolean;
  isLoading: boolean
  data: NodesPerYearMonthResponse;
  selectedData: NodeJobsPerYear;
  selectedNode: string;
  recentJobs: RecentJobsByDay[];

  constructor(private httpService: HubHttpService,
    private http: HttpClient, private auth: AuthService, private router: Router) {
    this.selectedNode = 'All Nodes';
    this.isLoggedIn = false;
    this.isLoading = true;
  }

  launchDataHolderPage() {
    this.router.navigateByUrl('nodes/dataholders/' + this.selectedData.NodeId);
  }

  changeNode(nodeName: string) {
    if (nodeName == 'All Nodes') {
      this.selectedData = this.data.AllNodes;
    } else {
      var node = this.data.Nodes.find(n => n.NodeId == nodeName);
      if (node != null) {
        this.selectedData = node;
      }
    }
  }

  ngOnInit(): void {



    this.auth.user$.subscribe(usr => {
      if (usr != null) {
        if (!this.isLoading) {
          return;
        }

        this.isLoading = false;
        this.isLoggedIn = true;
        const headers = new HttpHeaders()
        .set('Content-Type', 'application/json')
        .set('Accept', 'application/json');
      let url = this.httpService.ApiUrl + '/api/mynodes/JobsPerMonth';
      this.http.get<NodesPerYearMonthResponse>(url, { headers }).subscribe(data => {
        this.data = data;
        this.selectedData = this.data.AllNodes;
      });

      url = this.httpService.ApiUrl + '/api/mynodes/RecentJobs';
      this.http.get<RecentJobsByDay[]>(url, { headers }).subscribe(data => {
        this.recentJobs = data;
      });
      }

    });

  }

formatTime(value) {
  if (value > 1440) {
    const days = (value / 1440);
    if ((days / 365) % 1 == 0) {
      return (days / 365).toString() + ' years';
    }
    return +days.toFixed(1).replace(/[.,]00$/, '') + (days === 1 ? ' day' : ' days');
  }
  return value + ' minute' + (value == 1 ? '' : 's');
}

  formatAmount(value) {
    let tokenAmount = parseFloat(value);
    let formatted = +tokenAmount.toFixed(4);
    return formatted;
  }
}

export interface NodesPerYearMonthResponse {
  AllNodes: NodeJobsPerYear;
  Nodes: NodeJobsPerYear[];
}

export interface NodeJobsPerYear {
  DisplayName: string;
  NodeId: string;
  Years: JobsPerYear[];
}

export interface JobsPerYear {
  Title: string;
  Active: boolean;
  Months: JobsByMonth[];
}

export interface JobsByMonth {
  Month: string;
  TokenAmount: number;
  USDAmount: number;
  Down: boolean;
  JobCount: number;
}

export interface RecentJobsByDay {
  Day: string;
  Jobs: RecentJob[];
  Active: boolean;
}

export interface RecentJob {
  NodeId: string;
  DisplayName: string;
  OfferID: string;
  HoldingTimeInMinutes: number;
  TokenAmountPerHolder: number;
  FinalizedTimestamp: Date;
  USDAmount: number;
}