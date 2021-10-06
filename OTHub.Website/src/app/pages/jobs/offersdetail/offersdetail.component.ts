import {Component, ElementRef, Inject, NgZone, OnInit, PLATFORM_ID, QueryList, ViewChildren} from '@angular/core';
import {
  HttpClient,
  HttpHeaders,
} from '@angular/common/http';
import { MomentModule } from 'ngx-moment';
import { OTOfferDetailModel, OTOfferDetailTimelineEventModel } from './offersdetail-models';
import { ActivatedRoute, Router } from '@angular/router';
import { HubHttpService } from '../../hub-http-service';
import * as am4core from '@amcharts/amcharts4/core';
import am4themes_animated from '@amcharts/amcharts4/themes/animated';
import {DatePipe, isPlatformBrowser} from '@angular/common';
import * as am4charts from "@amcharts/amcharts4/charts";
declare const $: any;
import * as am4plugins_timeline from "@amcharts/amcharts4/plugins/timeline";
import * as am4plugins_bullets from "@amcharts/amcharts4/plugins/bullets";
import {Axis, CategoryAxis} from "@amcharts/amcharts4/charts";
import {AxisRendererCurveY} from "@amcharts/amcharts4/plugins/timeline";
import {AxisRenderer} from "@amcharts/amcharts4/.internal/charts/axes/AxisRenderer";
import {Color} from "@amcharts/amcharts4/core";
import { NbThemeService } from '@nebular/theme';

@Component({
  selector: 'app-offersdetail',
  templateUrl: './offersdetail.component.html',
  styleUrls: ['./offersdetail.component.scss'],
})
export class OffersDetailComponent implements OnInit {
  constructor(private http: HttpClient, private route: ActivatedRoute, private router: Router, 
              private httpService: HubHttpService,
              @Inject(PLATFORM_ID) private platformId, private zone: NgZone,
              private datePipe: DatePipe, private themeService: NbThemeService) {
    this.isLoading = true;
    this.failedLoading = false;
  }

  OfferModel: OTOfferDetailModel;
  offerId: string;
  isLoading: boolean;
  failedLoading: boolean;
  isDarkTheme: boolean;

  formatAmount(amount) {
    if (amount === null) {
      return null;
    }
    const split = amount.toString().split('.');
    let lastSplit = '';
    if (split.length === 2) {
      lastSplit = split[1];
      while(lastSplit[lastSplit.length - 1] == '0') {
        lastSplit = lastSplit.substr(0, lastSplit.length - 1);
      }
      if (lastSplit == '') {
        return split[0];
      }
      return split[0] + '.' + lastSplit;
    }
    return split[0];
  }



  toFixed(num, fixed) {
    const re = new RegExp('^-?\\d+(?:\.\\d{0,' + (fixed || -1) + '})?');
    return num.toString().match(re)[0];
  }

  getIdentityIcon(identity: string) {
    return this.httpService.ApiUrl + '/api/icon/node/' + identity + '/' + (this.isDarkTheme ? 'dark' : 'light') + '/16';
  }


  ngAfterViewInit() {
    const that = this;



  }

  loadStakeChart() {


    let chart = am4core.create("stakeChart", am4charts.PieChart);

    chart.data = [
      {
        Name: 'Data Creator',
        Staked: this.OfferModel.TokenAmountPerHolder * 3
      },
      {
        Name: 'Data Holder 1',
        Staked: this.OfferModel.TokenAmountPerHolder
      },
      {
        Name: 'Data Holder 2',
        Staked: this.OfferModel.TokenAmountPerHolder
      },
      {
        Name: 'Data Holder 3',
        Staked: this.OfferModel.TokenAmountPerHolder
      }
    ];

    let pieSeries = chart.series.push(new am4charts.PieSeries());
    pieSeries.dataFields.value = "Staked";
    pieSeries.dataFields.category = "Name";
    pieSeries.slices.template.stroke = am4core.color("#fff");
    pieSeries.slices.template.strokeOpacity = 1;

    // This creates initial animation
    pieSeries.hiddenState.properties.opacity = 1;
    pieSeries.hiddenState.properties.endAngle = -90;
    pieSeries.hiddenState.properties.startAngle = -90;

    pieSeries.ticks.template.disabled = true;
    pieSeries.labels.template.disabled = true;

    chart.legend = new am4charts.Legend();
    chart.legend.position = "left";
    chart.legend.width = undefined;
    chart.legend.itemContainers.template.width = undefined;
    
    chart.legend.labels.template.truncate = false;
    chart.legend.valueLabels.template.horizontalCenter = "right";
    chart.legend.valueLabels.template.text = "{value.value} TRAC";
    chart.hiddenState.properties.radius = am4core.percent(0);
    chart.seriesContainer.align = "left";
    chart.seriesContainer.paddingLeft = 30;
    if (this.themeService.currentTheme != 'light' && this.themeService.currentTheme != 'corporate' && this.themeService.currentTheme != 'default') {
      chart.legend.labels.template.fill = am4core.color('white');
      chart.legend.valueLabels.template.fill = am4core.color('white');
    }
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



  browserOnly(f: () => void) {
    if (isPlatformBrowser(this.platformId)) {
      this.zone.runOutsideAngular(() => {
        f();
      });
    }
  }

  getOffer() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    const url = this.httpService.ApiUrl + '/api/job/detail/' + this.offerId + '?' + (new Date()).getTime();
    const promise = this.http.get<OTOfferDetailModel>(url, { headers: headers });
    return promise;
  }

  ngOnInit() {
    this.isDarkTheme = $('body').hasClass('dark');
    this.route.params.subscribe(params => {
      this.offerId = params.offerId;

      const startTime = new Date();
      this.getOffer().subscribe(data => {
        const endTime = new Date();
        this.OfferModel = data;
        this.loadStakeChart();
        const diff = endTime.getTime() - startTime.getTime();
        let minWait = 0;
        if (diff < 150) {
          minWait = 150 - diff;
        }
        setTimeout(() => {
          this.isLoading = false;
        }, minWait);
      }, err => {
        this.failedLoading = true;
        this.isLoading = false;
      });
    });
  }
}
