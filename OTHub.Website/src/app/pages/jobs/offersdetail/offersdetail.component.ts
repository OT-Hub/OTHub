import {Component, ElementRef, Inject, NgZone, OnInit, PLATFORM_ID, QueryList, ViewChildren} from '@angular/core';
import {
  HttpClient,
  HttpHeaders,
} from '@angular/common/http';
import { MomentModule } from 'ngx-moment';
import { OTOfferDetailModel, OTOfferDetailTimelineEventModel } from './offersdetail-models';
import { ActivatedRoute, Router } from '@angular/router';
import { MyNodeService } from '../../nodes/mynodeservice';
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

@Component({
  selector: 'app-offersdetail',
  templateUrl: './offersdetail.component.html',
  styleUrls: ['./offersdetail.component.scss'],
})
export class OffersDetailComponent implements OnInit {
  constructor(private http: HttpClient, private route: ActivatedRoute, private router: Router, private myNodeService: MyNodeService,
              private httpService: HubHttpService,
              @Inject(PLATFORM_ID) private platformId, private zone: NgZone,
              private datePipe: DatePipe) {
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
      if (lastSplit.length > 3) {
        lastSplit = lastSplit.substring(0, 3);
      }
      return split[0] + '.' + lastSplit;
    }
    return split[0];
  }

  getTimelineItemColour(timeline: OTOfferDetailTimelineEventModel) {

    return '#ffffff';

    if (timeline.Name === 'Offer Created') {
      return '#3949ab';
    } else if (timeline.Name === 'Offer Finalized') {
      return '#3949ab';
    } else if (timeline.Name === 'Offer Completed') {
      return '#3949ab';
    } else if (timeline.Name === 'Data Holder Replaced') {
      return '#c62828';
    } else if (timeline.Name === 'Data Holder Chosen (Replacement)') {
      return '#3949ab';
    } else if (timeline.Name === 'Litigation Initiated') {
      return '#f9a825';
    } else if (timeline.Name === 'Litigation Timed Out') {
      return '#c62828';
    } else if (timeline.Name === 'Litigation Answered') {
      return '#f9a825';
    } else if (timeline.Name === 'Litigation Failed') {
      return '#c62828';
    } else if (timeline.Name === 'Litigation Passed') {
      return '#689f38';
    } else if (timeline.Name.startsWith('Offer Paidout')) {
      return '#689f38';
    } else if (timeline.Name === 'Data Holder Chosen') {
      return '#3949ab';
    }
    return '#666';
  }

  toFixed(num, fixed) {
    const re = new RegExp('^-?\\d+(?:\.\\d{0,' + (fixed || -1) + '})?');
    return num.toString().match(re)[0];
  }

  getIdentityIcon(identity: string) {
    return this.httpService.ApiUrl + '/api/icon/node/' + identity + '/' + (this.isDarkTheme ? 'dark' : 'light') + '/16';
  }

  getTimelineItemIcon(timeline: OTOfferDetailTimelineEventModel) {
    if (timeline.Name === 'Offer Created') {
      return 'far fa-calendar';
    } else if (timeline.Name === 'Offer Finalized') {
      return 'far fa-calendar-plus';
    } else if (timeline.Name === 'Offer Completed') {
      return 'far fa-calendar-check';
    } else if (timeline.Name === 'Replacement Started') {
      return 'fas fa-bomb';
    } else if (timeline.Name === 'Data Holder Replaced') {
      return 'fas fa-bomb';
    } else if (timeline.Name === 'Litigation Initiated') {
      return 'fas fa-hourglass-start';
    } else if (timeline.Name === 'Litigation Timed Out') {
      return 'far fa-hourglass-end';
    } else if (timeline.Name === 'Litigation Answered') {
      return 'fas fa-hourglass-half';
    } else if (timeline.Name === 'Litigation Failed') {
      return 'fas fa-hourglass';
    } else if (timeline.Name === 'Litigation Passed') {
      return 'fas fa-hourglass';
    } else if (timeline.Name.startsWith('Offer Paidout')) {
      return 'fas fa-money-bill';
    } else if (timeline.Name === 'Data Holder Chosen') {
      return 'fas fa-plus';
    }
    return 'fas fa-terminal';
  }

  @ViewChildren("TimelineChart") TimelineChart: QueryList<ElementRef>;

  ngAfterViewInit() {
    const that = this;

    this.TimelineChart.changes.subscribe(({ first: elm }) => {

      // Chart code goes in here
      this.browserOnly(() => {
        am4core.useTheme(am4themes_animated);
        that.loadTimelineChart();
      });

    });


  }

  loadTimelineChart() {


      let chart = am4core.create("TimelineChart", am4plugins_timeline.SerpentineChart);


    chart.curveContainer.padding(50, 20, 50, 20);
    chart.levelCount = 3;
    chart.yAxisRadius = am4core.percent(25);
    chart.yAxisInnerRadius = am4core.percent(-25);
    chart.maskBullets = false;

    let colorSet = new am4core.ColorSet();
    colorSet.saturation = 0.5;

    let color = 0;

    let data = [

    ];

    this.OfferModel.Holders.forEach((v) => {
        data.push({
          "category": v.NodeId.substring(0, 20) + '...',
          "start": this.datePipe.transform(v.JobStarted,"yyyy-MM-dd"),
          "end": this.datePipe.transform(v.JobCompleted,"yyyy-MM-dd"),
          "color": colorSet.getIndex(color++),
          "task": v.NodeId
        });
    });

    data.push(      {
      "category": "Job",
      "start": this.datePipe.transform(this.OfferModel.FinalizedTimestamp,"yyyy-MM-dd"),
      "end": this.datePipe.transform(this.OfferModel.EndTimestamp,"yyyy-MM-dd"),
      "color": colorSet.getIndex(color++),
      "task": 'Job'
    });

    // this.OfferModel.Timeline.forEach((v) => {
    //   data.push({
    //     "category": "Module #1",
    //     "start": this.datePipe.transform(v.Timestamp,"yyyy-MM-dd"),
    //     "end": this.datePipe.transform(v.Timestamp,"yyyy-MM-dd"),
    //     "color": colorSet.getIndex(0),
    //     "task": v.Name
    //   });
    // });

    chart.data = data;

    // chart.data = [{
    //   "category": "Module #1",
    //   "start": "2019-01-10",
    //   "end": "2019-01-13",
    //   "color": colorSet.getIndex(0),
    //   "task": "Gathering requirements"
    // }, {
    //   "category": "Module #1",
    //   "start": "2019-02-05",
    //   "end": "2019-04-18",
    //   "color": colorSet.getIndex(0),
    //   "task": "Development"
    // }, {
    //   "category": "Module #2",
    //   "start": "2019-01-08",
    //   "end": "2019-01-10",
    //   "color": colorSet.getIndex(5),
    //   "task": "Gathering requirements"
    // }, {
    //   "category": "Module #2",
    //   "start": "2019-01-12",
    //   "end": "2019-01-15",
    //   "color": colorSet.getIndex(5),
    //   "task": "Producing specifications"
    // }, {
    //   "category": "Module #2",
    //   "start": "2019-01-16",
    //   "end": "2019-02-05",
    //   "color": colorSet.getIndex(5),
    //   "task": "Development"
    // }, {
    //   "category": "Module #2",
    //   "start": "2019-02-10",
    //   "end": "2019-02-18",
    //   "color": colorSet.getIndex(5),
    //   "task": "Testing and QA"
    // }, {
    //   "category": ""
    // }, {
    //   "category": "Module #3",
    //   "start": "2019-01-01",
    //   "end": "2019-01-19",
    //   "color": colorSet.getIndex(9),
    //   "task": "Gathering requirements"
    // }, {
    //   "category": "Module #3",
    //   "start": "2019-02-01",
    //   "end": "2019-02-10",
    //   "color": colorSet.getIndex(9),
    //   "task": "Producing specifications"
    // }, {
    //   "category": "Module #3",
    //   "start": "2019-03-10",
    //   "end": "2019-04-15",
    //   "color": colorSet.getIndex(9),
    //   "task": "Development"
    // }, {
    //   "category": "Module #3",
    //   "start": "2019-04-20",
    //   "end": "2019-04-30",
    //   "color": colorSet.getIndex(9),
    //   "task": "Testing and QA",
    //   "disabled2":false,
    //   "image2":"/wp-content/uploads/assets/timeline/rachel.jpg",
    //   "location":0
    // }, {
    //   "category": "Module #4",
    //   "start": "2019-01-15",
    //   "end": "2019-02-12",
    //   "color": colorSet.getIndex(15),
    //   "task": "Gathering requirements",
    //   "disabled1":false,
    //   "image1":"/wp-content/uploads/assets/timeline/monica.jpg"
    // }, {
    //   "category": "Module #4",
    //   "start": "2019-02-25",
    //   "end": "2019-03-10",
    //   "color": colorSet.getIndex(15),
    //   "task": "Development"
    // }, {
    //   "category": "Module #4",
    //   "start": "2019-03-23",
    //   "end": "2019-04-29",
    //   "color": colorSet.getIndex(15),
    //   "task": "Testing and QA"
    // }];

    chart.dateFormatter.dateFormat = "yyyy-MM-dd";
    chart.dateFormatter.inputDateFormat = "yyyy-MM-dd";
    chart.fontSize = 11;

    const categoryAxis = chart.yAxes.push(new am4charts.CategoryAxis<AxisRendererCurveY>());
    //let categoryAxis = chart.yAxes.push(new am4charts.CategoryAxis()) as any;
    categoryAxis.dataFields.category = "category";
    categoryAxis.renderer.grid.template.disabled = true;
    categoryAxis.renderer.labels.template.paddingRight = 5;
    categoryAxis.renderer.labels.template.truncate = true;
    categoryAxis.renderer.labels.template.fullWords = false;
    categoryAxis.renderer.labels.template.ellipsis = '.';
    //categoryAxis.renderer.labels.template.fontSize = 8;
    categoryAxis.renderer.minGridDistance = 10;
    categoryAxis.renderer.innerRadius = -60;
    categoryAxis.renderer.radius = -60;



    // categoryAxis.events.on("sizechanged", function(ev) {
    //   let axis = ev.target;
    //   let cellWidth = axis.pixelWidth / (axis.endIndex - axis.startIndex);
    //   axis.renderer.labels.template.maxWidth = cellWidth;
    // });

    let dateAxis = chart.xAxes.push(new am4charts.DateAxis()) as am4charts.DateAxis;
    dateAxis.renderer.minGridDistance = 150;
    dateAxis.baseInterval = { count: 1, timeUnit: "day" };
    dateAxis.renderer.tooltipLocation = 0;
    dateAxis.startLocation = -0.5;
    dateAxis.renderer.line.strokeDasharray = "1,4";
    dateAxis.renderer.line.strokeOpacity = 0.6;
    dateAxis.tooltip.background.fillOpacity = 0.2;
    dateAxis.tooltip.background.cornerRadius = 5;
    dateAxis.tooltip.label.fill = new am4core.InterfaceColorSet().getFor("alternativeBackground");
    dateAxis.tooltip.label.paddingTop = 7;

    let labelTemplate = dateAxis.renderer.labels.template;
    labelTemplate.verticalCenter = "middle";
    labelTemplate.fillOpacity = 0.7;
    labelTemplate.background.fill = new am4core.InterfaceColorSet().getFor("background");
    labelTemplate.background.fillOpacity = 1;
    labelTemplate.padding(7, 7, 7, 7);

    let series = chart.series.push(new am4plugins_timeline.CurveColumnSeries());
    series.columns.template.height = am4core.percent(10);
    series.columns.template.tooltipText = "{task}: [bold]{openDateX}[/] - [bold]{dateX}[/]";

    series.dataFields.openDateX = "start";
    series.dataFields.dateX = "end";
    series.dataFields.categoryY = "category";
    series.columns.template.propertyFields.fill = "color"; // get color from data
    series.columns.template.propertyFields.stroke = "color";
    series.columns.template.strokeOpacity = 0;

    let bullet = series.bullets.push(new am4charts.CircleBullet());
    bullet.circle.radius = 3;
    bullet.circle.strokeOpacity = 0;
    bullet.propertyFields.fill = "color";
    bullet.locationX = 0;


    let bullet2 = series.bullets.push(new am4charts.CircleBullet());
    bullet2.circle.radius = 3;
    bullet2.circle.strokeOpacity = 0;
    bullet2.propertyFields.fill = "color";
    bullet2.locationX = 1;


    // let imageBullet1 = series.bullets.push(new am4plugins_bullets.PinBullet());
    // imageBullet1.disabled = true;
    // imageBullet1.propertyFields.disabled = "disabled1";
    // imageBullet1.locationX = 1;
    // imageBullet1.circle.radius = 20;
    // imageBullet1.propertyFields.stroke = "color";
    // imageBullet1.background.propertyFields.fill = "color";
    // imageBullet1.image = new am4core.Image();
    // imageBullet1.image.propertyFields.href = "image1";
    //
    // let imageBullet2 = series.bullets.push(new am4plugins_bullets.PinBullet());
    // imageBullet2.disabled = true;
    // imageBullet2.propertyFields.disabled = "disabled2";
    // imageBullet2.locationX = 0;
    // imageBullet2.circle.radius = 20;
    // imageBullet2.propertyFields.stroke = "color";
    // imageBullet2.background.propertyFields.fill = "color";
    // imageBullet2.image = new am4core.Image();
    // imageBullet2.image.propertyFields.href = "image2";


    let eventSeries = chart.series.push(new am4plugins_timeline.CurveLineSeries());
    eventSeries.dataFields.dateX = "eventDate";
    eventSeries.dataFields.categoryY = "category";

    var eventSeriesData = [];

    this.OfferModel.TimelineEvents.forEach((v) => {
      eventSeriesData.push({
        "category": v.RelatedTo.substring(0, 20) + '...',
        "eventDate": this.datePipe.transform(v.Timestamp,"yyyy-MM-dd"),
         "color": v.Name.startsWith('Litigation Failed') ? am4core.color("red") : colorSet.getIndex(0),
        "description": v.Name,
        "letter": v.Name.substr(0, 1).toUpperCase()
      });
    });

    eventSeries.data = eventSeriesData;

    eventSeries.strokeOpacity = 0;

    let flagBullet = eventSeries.bullets.push(new am4plugins_bullets.FlagBullet())
    flagBullet.label.propertyFields.text = "letter";
    flagBullet.locationX = 0;
    flagBullet.tooltipText = "{description}";

    chart.scrollbarX = new am4core.Scrollbar();
    chart.scrollbarX.align = "center"
    chart.scrollbarX.width = am4core.percent(85);

    let cursor = new am4plugins_timeline.CurveCursor();
    chart.cursor = cursor;
    cursor.xAxis = dateAxis;
    cursor.yAxis = categoryAxis;
    cursor.lineY.disabled = true;
    cursor.lineX.strokeDasharray = "1,4";
    cursor.lineX.strokeOpacity = 1;

    dateAxis.renderer.tooltipLocation2 = 0;
    categoryAxis.cursorTooltipEnabled = false;

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
