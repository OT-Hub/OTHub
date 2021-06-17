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
  isJobsPerMonthLoading: boolean;
  isRecentJobsLoading: boolean;
  data: NodesPerYearMonthResponse;
  selectedData: NodeJobsPerYear;
  selectedNode: string;
  recentJobs: RecentJobsByDay[];
  nodeStats: NodeStats;

  constructor(private httpService: HubHttpService,
    private http: HttpClient, private auth: AuthService, private router: Router) {
    this.selectedNode = 'All Nodes';
    this.isLoggedIn = false;
    this.isLoading = true;
    this.isJobsPerMonthLoading = true;
    this.isRecentJobsLoading = true;
  }

  launchDataHolderPage() {
    this.router.navigateByUrl('nodes/dataholders/' + this.selectedData.NodeId);
  }

  changeNode(nodeName: string) {
    this.nodeStats = null;
    this.selectedNode = nodeName;
    if (nodeName == 'All Nodes') {
      this.selectedData = this.data.AllNodes;
      this.loadNodeStats(null);
    } else {
      var node = this.data.Nodes.find(n => n.NodeId == nodeName);
      if (node != null) {
        this.selectedData = node;
        this.loadNodeStats(node.NodeId);
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
          this.isJobsPerMonthLoading = false;
        });

        url = this.httpService.ApiUrl + '/api/mynodes/RecentJobs';
        this.http.get<RecentJobsByDay[]>(url, { headers }).subscribe(data => {
          this.recentJobs = data;
          this.isRecentJobsLoading = false;
        });
        this.loadNodeStats(null);
      }

    });

  }

  loadNodeStats(nodeIDFilter: string) {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    let params = '';
    if (nodeIDFilter != null) {
      params = '?nodeID=' + nodeIDFilter;
    }
    let url = this.httpService.ApiUrl + '/api/mynodes/GetNodeStats' + params;
    this.http.get<NodeStats>(url, { headers }).subscribe(data => {
      this.nodeStats = data;
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

  formatAmountConstrained(value) {
    let tokenAmount = parseFloat(value);
    let formatted = +tokenAmount.toFixed(2);
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

export interface NodeStats {
  TotalJobs: NodeStatsNumeric;
  TotalRewards: NodeStatsToken;
  TotalStaked: NodeStatsToken;
  TotalLocked: NodeStatsToken;
}

export interface NodeStatsNumeric {
  Value: number;
  BetterThanActiveNodesPercentage: number;
}

export interface NodeStatsToken {
  USDAmount: number;
  TokenAmount: number;
  BetterThanActiveNodesPercentage: number;
}