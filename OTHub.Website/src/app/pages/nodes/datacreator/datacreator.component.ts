import { DataCreatorSummaryModel } from './../datacreators/datacreators-models';
import { Component, OnInit, ChangeDetectorRef, OnDestroy } from '@angular/core';

import { ActivatedRoute, Router } from '@angular/router';
import { MyNodeService } from '../mynodeservice';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { DataCreatedDetailedModel } from './datacreator-model';
import { HubHttpService } from '../../hub-http-service';
declare const $: any;
import Web3 from 'web3';
@Component({
  selector: 'app-datacreator',
  templateUrl: './datacreator.component.html',
  styleUrls: ['./datacreator.component.scss']
})
export class DatacreatorComponent implements OnInit, OnDestroy {
  web3: any;
  constructor(private http: HttpClient, private route: ActivatedRoute, private router: Router,
              private chRef: ChangeDetectorRef, public myNodeService: MyNodeService, private httpService: HubHttpService) {
    this.isLoading = true;
    this.failedLoading = false;
    this.IsTestNet = httpService.IsTestNet;
    this.web3 = new Web3();
  }

  IsTestNet: boolean;
  failedLoading: boolean;
  isLoading: boolean;
  NodeModel: DataCreatedDetailedModel;
  //litigationsExportOptionsObj: any;
  //litigationsDataTable: any;
  identity: string;
  //jobsDataTable: any;
  //profileDataTable: any;
  //jobsExportOptionsObj: any;
  //profileExportOptionsObj: any;
  GetNodeObservable: any;
  RouteObservable: any;
  identityIconUrl: string;
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

  getNode() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    const url = this.httpService.ApiUrl + '/api/nodes/datacreator/' + this.identity + '?' + (new Date()).getTime();
    return this.http.get<DataCreatedDetailedModel>(url, { headers });
  }

  // getIcon() {
  //   const headers = new HttpHeaders()
  //     .set('Content-Type', 'application/json')
  //     .set('Accept', 'application/json');
  //   const url = this.httpService.ApiUrl + '/api/icon?size=100&identity=' + this.identity;
  //   return this.http.get<string>(url, { headers });
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
    this.chRef.detach();
    this.GetNodeObservable.unsubscribe();
    this.RouteObservable.unsubscribe();
  }

  ngOnInit() {
    this.isDarkTheme = $('body').hasClass('dark');
    this.RouteObservable = this.route.params.subscribe(params => {
      this.identity = params.identity;

      const startTime = new Date();

      this.identityIconUrl = this.httpService.ApiUrl + '/api/icon/node/' + this.identity + '/' + (this.isDarkTheme ? 'dark' : 'light') + '/48';

      // this.getIcon().subscribe(data => {
      //   debugger;
      // });

      this.GetNodeObservable = this.getNode().subscribe(data => {
        const endTime = new Date();
        // this.destroyTables();
        this.NodeModel = data;

        // this.chRef.detectChanges();
        // this.loadJobsTable();
        // this.loadProfileTransfersTable();
        // this.loadLitigationsTable();

        const diff = endTime.getTime() - startTime.getTime();
        let minWait = 0;
        if (diff < 150) {
          minWait = 150 - diff;
        }
        setTimeout(() => {
          this.isLoading = false;
          if (this.NodeModel == null) {
            this.failedLoading = true;
          }
        }, minWait);

      }, err => {
        this.failedLoading = true;
        this.isLoading = false;
      });

    });
  }

  // destroyTables() {
  //   this.destroyTable($('.js-jobs'));
  //   this.destroyTable($('.js-profile-table'));
  //   this.destroyTable($('.js-litigations-table'));
  // }

  // destroyTable(table) {
  //   table.dataTable().fnClearTable();
  //   table.dataTable().fnDestroy();
  // }

  // loadJobsTable() {
  //   const exportColumns = [0, 2, 4, 5, 6, 7, 8];
  //   this.jobsExportOptionsObj = {
  //     columns: exportColumns,
  //     format: {
  //       body(text, row, column, node) {
  //         if (column === 2 || column === 3 || column === 4) {
  //           text = text.replace(/[^0-9.]+/g, '');
  //         }
  //         return text;
  //       },
  //       header(text, column) {
  //         if (column === 2) {
  //           text = 'Created ' + text;
  //         } else if (column === 4) {
  //           text += ' (KB)';
  //         } else if (column === 5) {
  //           text += ' (days)';
  //         } else if (column === 6) {
  //           //text += ' (TRAC)';
  //         }
  //         return text;
  //       }
  //     }
  //   };

  //   const table: any = $('.js-jobs');
  //   this.jobsDataTable = table.DataTable({
  //     responsive: true,
  //     columnDefs: [
  //       { orderable: false, targets: 0, visible: false },
  //       { orderable: true, targets: 1, orderData: [0], searchable: false },
  //       { orderable: false, targets: 2, visible: false },
  //       { orderable: true, targets: 3, orderData: [2], searchable: false },
  //       { orderable: false, targets: 4 },
  //       { orderable: false, targets: 5 },
  //       { orderable: false, targets: 6 },
  //       { orderable: false, targets: 7 },
  //       { orderable: false, targets: 8, visible: false },
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

  // loadLitigationsTable() {
  //   const litigationsExportColumns = [0, 2, 4, 6, 7];
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
  //       { targets: 4, visible: false },
  //       { targets: 5, visible: true, orderData: [4], searchable: false },
  //       { targets: 6, visible: true },
  //       { targets: 7, visible: true }
  //     ]
  //   });
  // }
}
