import { Component, OnInit, ChangeDetectorRef, OnDestroy, NgZone, ViewChild, Inject, PLATFORM_ID } from '@angular/core';
import {Router} from '@angular/router';
import {
  HttpClient,
  HttpHeaders
} from '@angular/common/http';
import { OTOfferSummaryModel, OTOfferSummaryWithPaging } from './offers-models';
import { MomentModule } from 'ngx-moment';
import { HubHttpService } from '../../hub-http-service';
declare const $: any;
import * as moment from 'moment';
import { LocalDataSource, ServerDataSource } from 'ng2-smart-table';
import { DataCreatorColumnComponent } from './datacreatorcolumn.component';
import { OfferIdColumnComponent } from '../../miscellaneous/offeridcolumn.component';
import * as am4core from '@amcharts/amcharts4/core';
import * as am4charts from '@amcharts/amcharts4/charts';
import am4themes_animated from '@amcharts/amcharts4/themes/animated';
import {AxisDataItem, DateAxisDataItem} from "@amcharts/amcharts4/charts";
import { isPlatformBrowser } from '@angular/common';
import { HomeJobsChartDataModel } from 'app/pages/e-commerce/e-commerce.component';

@Component({
  selector: 'ngx-offers',
  templateUrl: './offers.component.html',
  styleUrls: ['./offers.component.scss']
})
export class OffersComponent implements OnInit, OnDestroy {

  constructor(private http: HttpClient, private chRef: ChangeDetectorRef, private httpService: HubHttpService, private router: Router,
              private zone: NgZone, 
              @Inject(PLATFORM_ID) private platformId) {
    this.isLoading = true;
    this.failedLoading = false;
    // const data = this.service.getData();
    // this.source.load(data);

    //const url = this.httpService.ApiUrl + '/api/jobs/paging?pageLength=' + pageLength + '&start=' + start + '&filter=' + searchFilter + '&' + (new Date()).getTime();
    const url = this.httpService.ApiUrl + '/api/jobs/paging';

    this.source = new ServerDataSource(http,
      { endPoint: url });
  }

  source: ServerDataSource;



  ExportToJson() {
    const url = this.httpService.ApiUrl + '/api/jobs/paging?export=true&exporttype=0';
    window.location.href = url;
  }

  ExportToCsv() {
    const url = this.httpService.ApiUrl + '/api/jobs/paging?export=true&exporttype=1';
    window.location.href = url;
  }

  pageSizeChanged(event) {
    this.source.setPaging(1, event, true);
  }

  browserOnly(f: () => void) {
    if (isPlatformBrowser(this.platformId)) {
      this.zone.runOutsideAngular(() => {
        f();
      });
    }
  }


  settings = {
    actions:  {
add: false,
edit: false,
delete: false
    },
    columns: {
      // DCIdentity: {
      //   title: 'DC',
      //   width: '1%',
      //   type: 'custom',
      //   filter: false,
      //   sort: false,
      //   editable: false,
      //   addable: false,
      //   renderComponent: DataCreatorColumnComponent,
      //   // valuePrepareFunction: (value) => {
      //   //   if (!value) {
      //   //     return 'Unknown';
      //   //   }
      //
      //   //   return '<a target=_self href="/nodes/datacreators/' + value +
      //   //    '""><img class="lazy" style="height:16px;width:16px;" title="' +
      //   //     value + '" src="' + this.getIdentityIcon(value) + '"></a>';
      //   // }
      // },
      BlockchainDisplayName: {
        type: 'string',
        sort: false,
        filter: false,
        title: 'Blockchain'
      },
      OfferId: {
        sort: false,
        title: 'Offer ID',
        type: 'custom',
        renderComponent: OfferIdColumnComponent,
        // valuePrepareFunction: (value) => {
        //   return '<a class="navigateJqueryToAngular" href="/offers/' + value + '" onclick="return false;" title="' + value + '" >' + value.substring(0, 40) + '...</a>';
        // }
      },
      // CreatedTimestamp: {
      //   sort: true,
      //   sortDirection: 'desc',
      //   //width: '10%',
      //   title: 'Created',
      //   type: 'string',
      //   filter: false,
      //   valuePrepareFunction: (value) => {
      //     const stillUtc = moment.utc(value).toDate();
      //     const local = moment(stillUtc).local().format('DD/MM/YYYY HH:mm');
      //     return local;
      //   }
      // },
      FinalizedTimestamp: {
         sort: true,
         sortDirection: 'desc',
        title: 'Started',
        type: 'string',
        filter: false,
        valuePrepareFunction: (value) => {
          if (value == null)
          return '';
          const stillUtc = moment.utc(value).toDate();
          const local = moment(stillUtc).local().format('DD/MM/YYYY HH:mm');
          return local;
        }
      },
      DataSetSizeInBytes: {
        sort: true,
        //width: '5%',
        title: 'Data Set Size',
        type: 'string',
        filter: false,
        valuePrepareFunction: (value) => { return (value / 1000).toFixed(2).replace(/[.,]00$/, '') + ' KB';}
      },
      HoldingTimeInMinutes: {
        sort: true,
        //width: '5%',
        title: 'Holding Time',
        type: 'string',
        filter: false,
        valuePrepareFunction: (value) => {
          if (value > 1440) {
            const days = (value / 1440);
            if ((days / 365) % 1 == 0) {
              return (days / 365).toString() + ' years';
            }
            return +days.toFixed(1).replace(/[.,]00$/, '') + (days === 1 ? ' day' : ' days');
          }
          return value + ' minute(s)';
        }
      },
      TokenAmountPerHolder: {
        sort: true,
        title: 'Token Amount',
        //width: '5%',
        type: 'number',
        filter: false,
        valuePrepareFunction: (value) => {
          let tokenAmount = parseFloat(value);
          let formatted = +tokenAmount.toFixed(4);
          return formatted;
        }
      },
      Status: {
        sort: false,
        title: 'Status',
        type: 'string',
        //width: '5%',
        filter: false
      }
    },
    pager: {
      display: true,
      perPage: 25
    }
  };

  Summary: OTOfferSummaryWithPaging;
  OffersModel: OTOfferSummaryModel[];
  dataTable: any;
  dataTableOptions: any;
  exportOptionsObj: any;
  GetOffersObserver: any;
  isTableInit = false;
  failedLoading: boolean;
  isLoading: boolean;
  isDarkTheme: boolean;

  ngAfterViewInit() {
    var that = this;
    // Chart code goes in here
    this.browserOnly(() => {
      am4core.useTheme(am4themes_animated);
      that.loadJobsChart();
    });
  }

  getJobsChartData() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    const url = this.httpService.ApiUrl + '/api/home/JobsChartDataV3';
    return this.http.get<HomeJobsChartDataModel[]>(url, { headers });
  }

  loadJobsChart() {
    this.getJobsChartData().subscribe(chartData => {
      const endTime = new Date();
      this.failedLoading = false;
      this.isLoading = false;

      let chart = am4core.create("JobsHistoryChart", am4charts.XYChart);

      chart.paddingRight = 20;

      let data = [];

      chartData.forEach((v) => {
        data.push({ date: v.Date, name: "name", newJobs: v.NewJobs, completedJobs: v.CompletedJobs });
      });

      // let visits = 10;
      // for (let i = 1; i < 366; i++) {
      //   visits += Math.round((Math.random() < 0.5 ? 1 : -1) * Math.random() * 10);
      //   data.push({ date: new Date(2018, 0, i), name: "name" + i, value: visits });
      // }

      chart.data = data;
      chart.legend = new am4charts.Legend();
      chart.legend.maxHeight = 150;
      chart.legend.scrollable = true;
      chart.legend.useDefaultMarker = true;

      let dateAxis = chart.xAxes.push(new am4charts.DateAxis());
      dateAxis.renderer.grid.template.location = 0;



      let valueAxis = chart.yAxes.push(new am4charts.ValueAxis());
      valueAxis.tooltip.disabled = true;
      valueAxis.renderer.minWidth = 35;
      valueAxis.title.text = 'Jobs';

      let series = chart.series.push(new am4charts.LineSeries());
      series.dataFields.dateX = "date";
      series.dataFields.valueY = "newJobs";
      series.tooltipText = "{valueY.value}";
      series.name = 'Started';
      series.stroke = am4core.color('#00d68f');
      series.fill = am4core.color('#00d68f');
      series.strokeWidth = 3;

      let series2 = chart.series.push(new am4charts.LineSeries());
      series2.dataFields.dateX = "date";
      series2.dataFields.valueY = "completedJobs";
      series2.tooltipText = "{valueY.value}";
      series2.name = 'Completed';
      series2.strokeWidth = 3;

      // let bullet = series.bullets.push(new am4charts.CircleBullet());
      // bullet.circle.strokeWidth = 2;
      // bullet.circle.radius = 4;
      // bullet.circle.fill = am4core.color("#fff");
      //
      // let bullethover = bullet.states.create("hover");
      // bullethover.properties.scale = 1.3;

      // bullet = series2.bullets.push(new am4charts.CircleBullet());
      // bullet.circle.strokeWidth = 2;
      // bullet.circle.radius = 4;
      // bullet.circle.fill = am4core.color("#fff");
      //
      // bullethover = bullet.states.create("hover");
      // bullethover.properties.scale = 1.3;

      chart.cursor = new am4charts.XYCursor();
      chart.cursor.behavior = "panXY";
      chart.cursor.xAxis = dateAxis;
      chart.cursor.selection 
      //chart.cursor.snapToSeries = series;

      let scrollbarX = new am4charts.XYChartScrollbar();
      scrollbarX.series.push(series);
      scrollbarX.series.push(series2);
      chart.scrollbarX = scrollbarX;
      scrollbarX.parent = chart.chartAndLegendContainer;


      // let scrollAxisX = chart.xAxes.getIndex(0);
      // let range: DateAxisDataItem;
      // range = scrollAxisX.axisRanges.create() as DateAxisDataItem;
      //
      // range.date = new Date(2020, 2, 4);
      // range.endDate = new Date(2020, 2, 7);
      // range.axisFill.fill = am4core.color("#396478");
      // range.axisFill.fillOpacity = 0.2;
      // range.grid.strokeOpacity = 0;

      // scrollbarX.series.push(series);
      // scrollbarX.series.push(series2);
      // chart.scrollbarX = scrollbarX;

      chart.events.on('ready', () => {
        let start = new Date();
        start.setDate(start.getDate() - 31);
        let end = new Date();
        dateAxis.zoomToDates(start, end);
      });

      let title = chart.titles.create();





      title.text = "Jobs";
      title.fontSize = 18;
      title.marginBottom = 15;
    }, err => {
      this.failedLoading = true;
      this.isLoading = false;
    });
  }



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

  // getOffers(pageLength: number, start: number, searchFilter: string) {
  //   const headers = new HttpHeaders()
  //     .set('Content-Type', 'application/json')
  //     .set('Accept', 'application/json');
  //   // tslint:disable-next-line:max-line-length
  //   const url = this.httpService.ApiUrl + '/api/jobs/paging?pageLength=' + pageLength + '&start=' + start + '&filter=' + searchFilter + '&' + (new Date()).getTime();
  //   return this.http.get<OTOfferSummaryWithPaging>(url, { headers });
  // }

  // copyToClipboard(options, dataTable) {
  //   const that = { processing(isProcessing) { } };
  //   const e = null;
  //   const button = $(dataTable).dataTableExt.buttons.copyHtml5;
  //   button.exportOptions = options;
  //   $(dataTable).dataTableExt.buttons.copyHtml5.action.call(that, e, dataTable, options, button);
  // }

  // exportToCSV(options, dataTable) {
  //   const that = { processing(isProcessing) { } };
  //   const e = null;
  //   const button = $(dataTable).dataTableExt.buttons.csvHtml5;
  //   button.exportOptions = options;
  //   $(dataTable).dataTableExt.buttons.csvHtml5.action.call(that, e, dataTable, options, button);
  // }


  // exportToExcel(options, dataTable) {
  //   const that = { processing(isProcessing) { } };
  //   const e = null;
  //   const button = $(dataTable).dataTableExt.buttons.excelHtml5;
  //   button.exportOptions = options;
  //   $(dataTable).dataTableExt.buttons.excelHtml5.action.call(that, e, dataTable, options, button);
  // }

  // print(options, dataTable) {
  //   const that = { processing(isProcessing) { } };
  //   const e = null;
  //   const button = $(dataTable).dataTableExt.buttons.print;
  //   button.exportOptions = options;
  //   $(dataTable).dataTableExt.buttons.print.action.call(that, e, dataTable, options, button);
  // }

  ngOnDestroy() {
    // this.chRef.detach();
    // if (this.GetOffersObserver != null) {
    //   this.GetOffersObserver.unsubscribe();
    // }
    //$(document).off('click', '.navigateJqueryToAngular');
  }

  ngOnInit() {

    const that = this;

    // $(() => {
    //   $(document).on('click', '.navigateJqueryToAngular', (sender) => {
    //     that.ngZone.run(() => {
    //       that.router.navigateByUrl(sender.currentTarget.getAttribute('href'));
    //     });
    //   });
    // });

    // this.isDarkTheme = $('body').hasClass('dark');
    // const startTime = new Date();

    // this.loadTable();

    // this.GetOffersObserver = this.getOffers().subscribe(data => {
    //   const endTime = new Date();
    //   //this.OffersModel = data;

    //   this.chRef.detectChanges();
    //   this.loadTable();

    //   const diff = endTime.getTime() - startTime.getTime();
    //   let minWait = 0;
    //   if (diff < 100) {
    //     minWait = 100 - diff;
    //   }
    //   setTimeout(() => {
    //     this.isLoading = false;
    //     if (this.OffersModel == null) {
    //       this.failedLoading = true;
    //     }
    //   }, minWait);
    // }, err => {
    //   this.failedLoading = true;
    //   this.isLoading = false;
    // });
  }

  // loadTable() {
  //   const that = this;

  //   if (this.isTableInit === false) {
  //     this.isTableInit = true;
  //     const exportColumns = [1, 3, 5, 6, 7, 8, 9];
  //     this.exportOptionsObj = {
  //       columns: exportColumns,
  //       format: {
  //         body(text, row, column, node) {
  //           if ((column === 3 || column === 4 || column === 5) && typeof (text) === typeof (String)) {
  //             text = text.replace(/[^0-9.]+/g, '');
  //           }
  //           return text;
  //         },
  //         header(text, column) {
  //           if (column === 3) {
  //             text = 'Created ' + text;
  //           } else if (column === 5) {
  //             text += '';
  //           } else if (column === 6) {
  //             text += '';
  //           } else if (column === 7) {
  //             //text += ' (TRAC)';
  //           }
  //           return text;
  //         }
  //       }
  //     };

  //     const table: any = $('.js-jobs');

  //     this.dataTableOptions = {
  //       responsive: true,
  //       serverSide: true,
  //       pageLength: 50,
  //       start: 0,
  //       processing: true,
  //       ajax: (dataTablesParameters: any, callback) => {

  //         const searchFilter = dataTablesParameters.search.value;

  //         that.GetOffersObserver = that.getOffers(dataTablesParameters.length, dataTablesParameters.start, searchFilter).subscribe(resp => {
  //           this.chRef.detectChanges();
  //           that.Summary = resp;
  //           that.OffersModel = that.Summary.data;
  //           callback({
  //             recordsTotal: resp.recordsTotal,
  //             recordsFiltered: resp.recordsFiltered,
  //             data: resp.data
  //           });
  //           this.isLoading = false;
  //         });


  //       },
  //       columnDefs: [
  //         {
  //           orderable: false, targets: 0, visible: true, data: 'DCIdentity',
  //           render(data, type, row) {
  //             // tslint:disable-next-line:max-line-length
  //             if (!row.DCIdentity) {
  //               return 'Unknown';
  //             }
  //             return '<a class="navigateJqueryToAngular" href="/nodes/datacreators/' + row.DCIdentity + '" onclick="return false;"><img class="lazy" style="height:16px;width:16px;" title="' + row.DCIdentity + '" src="' + that.getIdentityIcon(row.DCIdentity) + '"></a>';
  //           }
  //         },
  //         { orderable: false, targets: 1, visible: false, data: 'OfferId' },
  //         {
  //           orderable: true, targets: 2, orderData: [1], searchable: false, data: 'OfferId',
  //           render(data, type, row) {
  //             // tslint:disable-next-line:max-line-length
  //             return '<a class="navigateJqueryToAngular" href="/offers/' + row.OfferId + '" onclick="return false;">' + row.OfferId.substring(0, 50) + '...</a>';
  //           },
  //         },
  //         { orderable: false, targets: 3, visible: false, data: 'Timestamp' },
  //         {
  //           orderable: true, targets: 4, orderData: [3], searchable: false, data: 'Timestamp',
  //           render(data, type, row) {
  //             const stillUtc = moment.utc(row.Timestamp).toDate();
  //             const local = moment(stillUtc).local().format('DD/MM/YYYY HH:mm');
  //             return local;
  //           },
  //         },
  //         {
  //           orderable: false, targets: 5, data: 'DataSetSizeInBytes',
  //           render(data, type, row) {
  //             return (row.DataSetSizeInBytes / 1000).toFixed(2).replace(/[.,]00$/, '') + ' KB';
  //           },
  //         },
  //         {
  //           orderable: false, targets: 6, data: 'HoldingTimeInMinutes',
  //           render(data, type, row) {
  //             if (row.HoldingTimeInMinutes > 1440) {
  //               const days = (row.HoldingTimeInMinutes / 1440);
  //               return days.toFixed(1).replace(/[.,]00$/, '') + (days === 1 ? ' day' : ' days');
  //             }
  //             return row.HoldingTimeInMinutes + ' minute(s)';
  //           },
  //         },
  //         {
  //           orderable: false, targets: 7, data: 'TokenAmountPerHolder',
  //           render(data, type, row) {
  //             const tokenAmount = parseFloat(row.TokenAmountPerHolder);
  //             return tokenAmount.toFixed(2).replace(/[.,]00$/, '');
  //           },
  //         },
  //         {
  //           orderable: false, targets: 8, data: 'Status',
  //           render(data, type, row) {
  //             return row.Status;
  //           },
  //         },
  //         { orderable: false, targets: 9, visible: false, data: 'EndTimestamp' },
  //       ],
  //       // drawCallback() {
  //       //   $('img.lazy').lazyload();
  //       // }
  //     };

  //     this.dataTable = table.DataTable(this.dataTableOptions);
  //   }
  // }
}
