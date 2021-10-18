import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '@auth0/auth0-angular';
import { HubHttpService } from 'app/pages/hub-http-service';
import * as am4core from '@amcharts/amcharts4/core';
import * as am4charts from '@amcharts/amcharts4/charts';
import am4themes_animated from '@amcharts/amcharts4/themes/animated';
import { isPlatformBrowser } from '@angular/common';
import { NbThemeService } from '@nebular/theme';

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
  holdingTimeByMonth: GetHoldingTimeByMonthModel[];

  constructor(private httpService: HubHttpService,
    private http: HttpClient, private auth: AuthService, private router: Router, private themeService: NbThemeService) {
    this.selectedNode = 'All Nodes';
    this.isLoggedIn = false;
    this.isLoading = true;
    this.isJobsPerMonthLoading = true;
    this.isRecentJobsLoading = true;
  }

  launchDataHolderPage() {
    this.router.navigateByUrl('nodes/dataholders/' + this.selectedData.NodeId);
  }

  launchNodeProfileWebsite() {
    window.open('https://node-profile.origintrail.io/ ', "_blank");
  }

  launchOTDocHubWebsite() {
    window.open('https://docs.origintrail.io/ ', "_blank");
  }

  launchOTNodeWebsite() {
    window.open('https://www.otnode.com/ ', "_blank");
  }

  changeNode(nodeName: string) {
    this.nodeStats = null;
    this.selectedNode = nodeName;
    this.loadJobs();
    this.loadHoldingTimePerMonth();
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

  loadHoldingTimePerMonth() {
    const headers = new HttpHeaders()
    .set('Content-Type', 'application/json')
    .set('Accept', 'application/json');
    let url = this.httpService.ApiUrl + '/api/mynodes/GetHoldingTimeByMonth?nodeID=' + (this.selectedNode == 'All Nodes' ? '' : this.selectedNode);
    this.http.get<GetHoldingTimeByMonthModel[]>(url, { headers }).subscribe(data => {
      this.holdingTimeByMonth = data;

      let chart = am4core.create("holdingTimeChart", am4charts.PieChart);


      let adjustedData = data.map(x => {
        return {
          HoldingTimeInMonths: x.HoldingTimeInMonths.toString() + ' Months',
          Count: x.Count
        };
      });

      chart.data = adjustedData;

      let pieSeries = chart.series.push(new am4charts.PieSeries());
      pieSeries.dataFields.value = "Count";
      pieSeries.dataFields.category = "HoldingTimeInMonths";
      pieSeries.slices.template.stroke = am4core.color("#fff");
      pieSeries.slices.template.strokeOpacity = 1;

      // This creates initial animation
      pieSeries.hiddenState.properties.opacity = 1;
      pieSeries.hiddenState.properties.endAngle = -90;
      pieSeries.hiddenState.properties.startAngle = -90;

      pieSeries.ticks.template.disabled = true;
      pieSeries.labels.template.disabled = true;

      chart.legend = new am4charts.Legend();

      chart.hiddenState.properties.radius = am4core.percent(0);

      if (this.themeService.currentTheme != 'light' && this.themeService.currentTheme != 'corporate' && this.themeService.currentTheme != 'default') {
        //title.fill = am4core.color('white');
        //dateAxis.renderer.labels.template.fill = am4core.color('white');
        //valueAxis.renderer.labels.template.fill = am4core.color('white');
        chart.legend.labels.template.fill = am4core.color('white');
        chart.legend.valueLabels.template.fill = am4core.color('white');
      }
    });
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
          if (this.data.Nodes.length == 1) {
            this.selectedData = this.data.Nodes[0];
            this.selectedNode = this.selectedData.NodeId;
          } else {
          this.selectedData = this.data.AllNodes;
          }
          this.loadNodeStats(this.selectedNode == 'All Nodes' ? null : this.selectedNode);
          this.isJobsPerMonthLoading = false;
        });

        this.loadJobs();
        this.loadHoldingTimePerMonth();
      }
    });
  }

  loadJobs() {
    const headers = new HttpHeaders()
    .set('Content-Type', 'application/json')
    .set('Accept', 'application/json');
    let url = this.httpService.ApiUrl + '/api/mynodes/RecentJobs?nodeID=' + (this.selectedNode == 'All Nodes' ? '' : this.selectedNode);
    this.http.get<RecentJobsByDay[]>(url, { headers }).subscribe(data => {
      this.recentJobs = data;
      this.isRecentJobsLoading = false;
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

  formatTimeAsDate(timestamp: string, value: number) {
    let d = new Date(timestamp); 
    d.setMinutes(d.getMinutes() + value);

    return d;
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
  Blockchain: string;
  BlockchainLogo: string;
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

export interface GetHoldingTimeByMonthModel {
  HoldingTimeInMonths: number;
  Count: number;
}