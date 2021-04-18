import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
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

  constructor(private httpService: HubHttpService,
    private http: HttpClient, private auth: AuthService) {
    this.selectedNode = 'All Nodes';
    this.isLoggedIn = false;
    this.isLoading = true;
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
        this.isLoggedIn = true;
        const headers = new HttpHeaders()
        .set('Content-Type', 'application/json')
        .set('Accept', 'application/json');
      const url = this.httpService.ApiUrl + '/api/mynodes/JobsPerMonth';
      this.http.get<NodesPerYearMonthResponse>(url, { headers }).subscribe(data => {
        this.data = data;
        this.selectedData = this.data.AllNodes;
      });
      }
      this.isLoading = false;
    });

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