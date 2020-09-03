import { Component, OnInit, ChangeDetectorRef, OnDestroy } from '@angular/core';
import {
  HttpClient,
  HttpHeaders
} from '@angular/common/http';
import { DataHolderDetailedModel, DataHolderTestOnlineResult } from './dataholder-models';
import { MomentModule } from 'ngx-moment';
import { ActivatedRoute, Router } from '@angular/router';
import { MyNodeService } from '../mynodeservice';
import { HubHttpService } from '../../hub-http-service';
import { MyNodeModel } from '../mynodemodel';
declare const $: any;
declare const d3: any;
declare const visavailChart: any;
import Web3 from 'web3';
@Component({
  selector: 'app-nodeprofile',
  templateUrl: './dataholder.component.html',
  styleUrls: ['./dataholder.component.scss']
})
export class DataHolderComponent implements OnInit, OnDestroy {
  web3: any;


  constructor(private http: HttpClient, private route: ActivatedRoute, 
              private chRef: ChangeDetectorRef, public myNodeService: MyNodeService, private httpService: HubHttpService) {
    this.isLoading = true;
    this.failedLoading = false;
    this.IsTestNet = httpService.IsTestNet;
    this.web3 = new Web3();
  }

  IsTestNet: boolean;
  DisplayName: string;
  NodeModel: DataHolderDetailedModel;
  // jobsDataTable: any;
  // litigationsExportOptionsObj: any;
  // litigationsDataTable: any;
  // payoutsDataTable: any;
  // profileDataTable: any;
  identity: string;
  // jobsExportOptionsObj: any;
  // payoutsExportOptionsObj: any;
  // profileExportOptionsObj: any;
  failedLoading: boolean;
  isLoading: boolean;
  GetNodeObservable: any;
  RouteObservable: any;
  isCheckingNodeUptime = false;
  MyNode: MyNodeModel;
  OnlineCheckResult: DataHolderTestOnlineResult;
  uptimeChart: any;
  chartData: any;
  identityIconUrl: string;
  isDarkTheme: boolean;

  getNode() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    let url = this.httpService.ApiUrl + '/api/nodes/dataholder/' + this.identity;
    if (this.MyNode || this.IsTestNet === true) {
      url += '?includeNodeUptime=true&' + (new Date()).getTime();
    } else {
    url += '?' + (new Date()).getTime();
    }
    return this.http.get<DataHolderDetailedModel>(url, { headers });
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

  CheckNodeOnline() {
    this.OnlineCheckResult = null;
    this.isCheckingNodeUptime = true;

    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    const url = this.httpService.ApiUrl + '/api/nodes/dataholder/checkonline?identity=' + this.identity;
    const test = this.http.get<DataHolderTestOnlineResult>(url, { headers });

    const startTime = new Date();
    test.subscribe(data => {
      const endTime = new Date();
      this.OnlineCheckResult = data;

      const diff = endTime.getTime() - startTime.getTime();
      let minWait = 0;
      if (diff < 150) {
        minWait = 150 - diff;
      }
      setTimeout(() => {
        this.isCheckingNodeUptime = false;
      }, minWait);
     }, err => {
      this.isCheckingNodeUptime = false;

      $.notify({
        message: 'There was a problem connecting to the OT Hub API.'
    },
        {
            type: 'bg-orange',
            allow_dismiss: true,
            newest_on_top: true,
            timer: 1000,
            placement: {
                from: 'top',
                align: 'left'
            },
            animate: {
                enter: 'animated fadeInLeft',
                exit: 'animated fadeOutLeft'
            },
            template: '<div data-notify="container" class="bootstrap-notify-container alert alert-dismissible {0} ' + (true ? 'p-r-35' : '') + '" role="alert">' +
                '<button type="button" aria-hidden="true" class="close" data-notify="dismiss">×</button>' +
                '<span data-notify="icon"></span> ' +
                '<span data-notify="title">{1}</span> ' +
                '<span data-notify="message">{2}</span>' +
                '<div class="progress" data-notify="progressbar">' +
                '<div class="progress-bar progress-bar-{0}" role="progressbar" aria-valuenow="0" aria-valuemin="0" aria-valuemax="100" style="width: 0%;"></div>' +
                '</div>' +
                '<a href="{3}" target="{4}" data-notify="url"></a>' +
                '</div>'
        });
    });
  }

  ngOnDestroy() {
    this.chRef.detach();
    this.GetNodeObservable.unsubscribe();
    this.RouteObservable.unsubscribe();
  }



  ngOnInit() {
    this.RouteObservable = this.route.params.subscribe(params => {
      // this.isDarkTheme = $('body').hasClass('dark');
      this.identity = params.identity;

      // tslint:disable-next-line:max-line-length
      this.identityIconUrl = this.httpService.ApiUrl + '/api/icon/node/' + this.identity + '/' + (this.isDarkTheme ? 'dark' : 'light') + '/48';

      if (this.identity) {
        this.MyNode = this.myNodeService.Get(this.identity);
        if (this.MyNode) {
          this.DisplayName = this.MyNode.DisplayName;
        }
      }

      const startTime = new Date();
      this.GetNodeObservable = this.getNode().subscribe(data => {
        const endTime = new Date();

        // this.destroyTables();
        this.NodeModel = data;
        // debugger;
        this.chRef.detectChanges();
        // this.loadJobsTable();
        // this.loadPayoutsTable();
        // this.loadLitigationsTable();
        // this.loadProfileTransfersTable();

        const diff = endTime.getTime() - startTime.getTime();
        let minWait = 0;
        if (diff < 150) {
          minWait = 150 - diff;
        }
        setTimeout(() => {
          this.isLoading = false;
          if (this.NodeModel == null) {
            this.failedLoading = true;
            return;
          }

          const that = this;

          if (this.NodeModel.NodeUptime && this.NodeModel.NodeUptime.ChartData) {
          this.chartData = JSON.parse(this.NodeModel.NodeUptime.ChartData);

          $(function() {
            that.uptimeChart = visavailChart().drawTitle(0);
            that.draw_visavail();

          $(window).resize(function() { return that.draw_visavail(); });
          });
        }

        }, minWait);

      }, err => {
        this.failedLoading = true;
        this.isLoading = false;
      });

    });

    const self = this;
  }

  draw_visavail() {
    this.uptimeChart.width($('#visavail_container').width());
    $('#todayUptimeChart').text('');
    d3.select('#todayUptimeChart')
    .datum([{
      categories: {
        Online: { color: '#5cb85c' },
        Offline: { color: '#d9534d' }
        },
      data: this.chartData
    }])
    .call(this.uptimeChart);
  }

  // destroyTables() {
  //   this.destroyTable($('.js-jobs-table'));
  //   this.destroyTable($('.js-payouts-table'));
  //   this.destroyTable($('.js-litigations-table'));
  //   this.destroyTable($('.js-profile-table'));
  // }

  // destroyTable(table) {
  //   table.dataTable().fnClearTable();
  //   table.dataTable().fnDestroy();
  // }

  // loadJobsTable() {
  //   const table: any = $('.js-jobs-table');
  //   const jobsExportColumns = [0, 2, 4, 6, 7, 8, 9];
  //   this.jobsExportOptionsObj = {
  //     columns: jobsExportColumns,
  //     format: {
  //       body(text, row, column, node) {
  //         if (column === 3) {
  //           text = text.replace(/[^0-9.]+/g, '');
  //         }
  //         return text;
  //       },
  //       header(text, column) {
  //         if (column === 6) {
  //           text += ' (days)';
  //         }
  //         return text;
  //       }
  //     }
  //   };

  //   this.jobsDataTable = table.DataTable({
  //     responsive: true,
  //     columnDefs: [
  //       { targets: 0, visible: false },
  //       { targets: 1, visible: true },
  //       { targets: 2, visible: false },
  //       { targets: 3, visible: true, orderData: [2], searchable: false },
  //       { targets: 4, visible: false },
  //       { targets: 5, visible: true, orderData: [4], searchable: false },
  //       { targets: 6, visible: true },
  //       { targets: 7, visible: true },
  //       { targets: 8, visible: true },
  //       { targets: 9, visible: false },
  //       { targets: 10, visible: true, orderData: [9], searchable: false }
  //     ]
  //   });
  // }

  // loadPayoutsTable() {
  //   const payoutsExportColumns = [0, 2, 4, 6, 7];
  //   this.payoutsExportOptionsObj = {
  //     columns: payoutsExportColumns,
  //     format: {
  //       body(text, row, column, node) {
  //         if (column === 4) {
  //           text = text.replace(/[^0-9.]+/g, '');
  //         }
  //         return text;
  //       },
  //       header(text, column) {
  //         return text;
  //       }
  //     }
  //   };

  //   const table: any = $('.js-payouts-table');

  //   if (table.dataTable.fnIsDataTable()) {
  //     table.dataTable().fnClearTable();
  //     table.dataTable().fnDestroy();
  //   }

  //   this.payoutsDataTable = table.DataTable({
  //     responsive: true,
  //     columnDefs: [
  //       { targets: 0, visible: false },
  //       { targets: 1, visible: true, orderData: [0], searchable: false },
  //       { targets: 2, visible: false },
  //       { targets: 3, visible: true, orderData: [2], searchable: false },
  //       { targets: 4, visible: false },
  //       { targets: 5, visible: true, orderData: [4], searchable: false },
  //       { targets: 6, visible: true },
  //       { targets: 7, visible: true }
  //     ]
  //   });
  // }

  // loadLitigationsTable() {
  //   const litigationsExportColumns = [0, 2, 4, 5];
  //   this.litigationsExportOptionsObj = {
  //     columns: litigationsExportColumns,
  //     format: {
  //       body(text, row, column, node) {
  //         if (column === 4) {
  //           text = text.replace(/[^0-9.]+/g, '');
  //         }
  //         return text;
  //       },
  //       header(text, column) {
  //         return text;
  //       }
  //     }
  //   };

  //   const table: any = $('.js-litigations-table');

  //   if (table.dataTable.fnIsDataTable()) {
  //     table.dataTable().fnClearTable();
  //     table.dataTable().fnDestroy();
  //   }

  //   this.litigationsDataTable = table.DataTable({
  //     responsive: true,
  //     columnDefs: [
  //       { targets: 0, visible: false },
  //       { targets: 1, visible: true, orderData: [0], searchable: false },
  //       { targets: 2, visible: false },
  //       { targets: 3, visible: true, orderData: [2], searchable: false },
  //       { targets: 4, visible: true },
  //       { targets: 5, visible: true }
  //     ]
  //   });
  // }

  // loadProfileTransfersTable() {
  //   const profileExportColumns = [0, 2, 4, 5, 6];
  //   this.profileExportOptionsObj = {
  //     columns: profileExportColumns,
  //     format: {
  //       body(text, row, column, node) {
  //         if (column === 4) {
  //           text = text.replace(/[^0-9.]+/g, '');
  //         }
  //         return text;
  //       },
  //       header(text, column) {
  //         return text;
  //       }
  //     }
  //   };

  //   const table: any = $('.js-profile-table');

  //   if (table.dataTable.fnIsDataTable()) {
  //     table.dataTable().fnClearTable();
  //     table.dataTable().fnDestroy();
  //   }

  //   this.profileDataTable = table.DataTable({
  //     responsive: true,
  //     columnDefs: [
  //       { targets: 0, visible: false },
  //       { targets: 1, visible: true, orderData: [0], searchable: false },
  //       { targets: 2, visible: false },
  //       { targets: 3, visible: true, orderData: [2], searchable: false },
  //       { targets: 4, visible: true },
  //       { targets: 5, visible: true },
  //       { targets: 6, visible: true }
  //     ]
  //   });
  // }
}
