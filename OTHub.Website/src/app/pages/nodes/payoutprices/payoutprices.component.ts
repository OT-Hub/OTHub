import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { HubHttpService } from '../../hub-http-service';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpHeaders, HttpClient } from '@angular/common/http';
import { PayoutInUsdModel } from './payoutprices-model';
//import Web3 from 'web3';
import { MyNodeService } from '../mynodeservice';
import * as moment from 'moment';
import { ServerDataSource } from 'ng2-smart-table';
import { OfferIdColumnComponent } from '../../miscellaneous/offeridcolumn.component';

declare const $: any;
@Component({
    selector: 'app-payoutprices',
    templateUrl: './payoutprices.component.html',
    styleUrls: ['./payoutprices.component.scss']
})
export class PayoutPricesComponent implements OnInit, OnDestroy {

    constructor(private httpService: HubHttpService, private route: ActivatedRoute, private router: Router,
                private http: HttpClient, public myNodeService: MyNodeService) {
        this.isLoading = true;
        this.failedLoading = false;
        //this.web3 = new Web3();
        this.totalTracPaid = 0;
        this.totalEquivUSDPaid = 0;
        this.averageTracTickerPrice = 0;
    }

    //Items: PayoutInUsdModel[];
    RouteObservable: any;
    identity: string;
    failedLoading: boolean;
    isLoading: boolean;
    //GetDataObservable: any;
    web3: any;
    //payoutsDataTable: any;
    //payoutsExportOptionsObj: any;
    totalTracPaid: number;
    totalEquivUSDPaid: number;
    averageTracTickerPrice: number;
    totalPayoutsLength : number;

    source: ServerDataSource;

    ExportToJson() {
      const url = this.httpService.ApiUrl +'/api/nodes/dataholder/PayoutsInUSDForDataHolder?identity=' + this.identity + '&export=true&exporttype=0';
      window.location.href = url;
    }

    ExportToCsv() {
      const url = this.httpService.ApiUrl + '/api/nodes/dataholder/PayoutsInUSDForDataHolder?identity=' + this.identity + '&export=true&exporttype=1';
      window.location.href = url;
    }

    pageSizeChanged(event) {
      this.source.setPaging(1, event, true);
    }

    getIdentityIcon(identity: string) {
      return this.httpService.ApiUrl + '/api/icon/node/' + identity + '/' + (false ? 'dark' : 'light') + '/16';
    }

    ngOnInit() {



        this.RouteObservable = this.route.params.subscribe(params => {
            this.identity = params.identity;

            const url = this.httpService.ApiUrl + '/api/nodes/dataholder/PayoutsInUSDForDataHolder?identity=' + this.identity;

            this.source = new ServerDataSource(this.http,
                { endPoint: url });

            this.source.onChanged().subscribe(e => {
                if (e.action == 'refresh') {
                    const eles = e.elements;

                    let totalTracPaidCounter = 0;
                    let totalEquivUSDPaidCounter = 0;

                    let sum = 0;

                    eles.forEach(element => {
                        totalTracPaidCounter += element.TRACAmount;
                        totalEquivUSDPaidCounter += element.USDAmount;
                        sum += element.TickerUSDPrice;
                    });

                    this.totalTracPaid = totalTracPaidCounter;
                    this.totalEquivUSDPaid = totalEquivUSDPaidCounter;

                    const avg = sum / eles.length;

                    this.totalPayoutsLength = eles.length;

                    this.averageTracTickerPrice = avg;

                    this.isLoading = false;
                }
              });

            // this.GetDataObservable = this.getData().subscribe(data => {
            //     this.Items = data;

            //     let totalTracPaidCounter = 0;
            //     let totalEquivUSDPaidCounter = 0;

            //     let sum = 0;

            //     data.forEach(element => {
            //         totalTracPaidCounter += element.TRACAmount;
            //         totalEquivUSDPaidCounter += element.USDAmount;
            //         sum += element.TickerUSDPrice;
            //     });

            //     this.totalTracPaid = totalTracPaidCounter;
            //     this.totalEquivUSDPaid = totalEquivUSDPaidCounter;

            //     const avg = sum / data.length;

            //     this.averageTracTickerPrice = avg;

            //     this.chRef.detectChanges();
            //     //this.loadPayoutsTable();
            //     this.isLoading = false;


            // }, err => {
            //     this.failedLoading = true;
            //     this.isLoading = false;
            // });
        });
    }

    // copyToClipboard(options, dataTable) {
    //     const that = { processing(isProcessing) { } };
    //     const e = null;
    //     const button = $(dataTable).dataTableExt.buttons.copyHtml5;
    //     button.exportOptions = options;
    //     $(dataTable).dataTableExt.buttons.copyHtml5.action.call(that, e, dataTable, options, button);
    // }

    // exportToCSV(options, dataTable) {
    //     const that = { processing(isProcessing) { } };
    //     const e = null;
    //     const button = $(dataTable).dataTableExt.buttons.csvHtml5;
    //     button.exportOptions = options;
    //     $(dataTable).dataTableExt.buttons.csvHtml5.action.call(that, e, dataTable, options, button);
    // }


    // exportToExcel(options, dataTable) {
    //     const that = { processing(isProcessing) { } };
    //     const e = null;
    //     const button = $(dataTable).dataTableExt.buttons.excelHtml5;
    //     button.exportOptions = options;
    //     $(dataTable).dataTableExt.buttons.excelHtml5.action.call(that, e, dataTable, options, button);
    // }

    // print(options, dataTable) {
    //     const that = { processing(isProcessing) { } };
    //     const e = null;
    //     const button = $(dataTable).dataTableExt.buttons.print;
    //     button.exportOptions = options;
    //     $(dataTable).dataTableExt.buttons.print.action.call(that, e, dataTable, options, button);
    // }

    // loadPayoutsTable() {
    //     const payoutsExportColumns = [0, 2, 3, 5, 7, 8];
    //     this.payoutsExportOptionsObj = {
    //         columns: payoutsExportColumns,
    //         format: {
    //             body(text, row, column, node) {
    //                 // if (column === 4) {
    //                 //     text = text.replace(/[^0-9.]+/g, '');
    //                 // }
    //                 return text;
    //             },
    //             header(text, column) {
    //                 return text;
    //             }
    //         }
    //     };

    //     const table: any = $('.js-payoutsusd-table');

    //     if (table.dataTable.fnIsDataTable()) {
    //         table.dataTable().fnClearTable();
    //         table.dataTable().fnDestroy();
    //     }

    //     this.payoutsDataTable = table.DataTable({
    //         responsive: true,
    //         searching: false,
    //         columnDefs: [
    //             { targets: 0, visible: false },
    //             { targets: 1, visible: true, orderData: [0], searchable: false },
    //             { targets: 2, visible: true },
    //             { targets: 3, visible: true },
    //             { targets: 4, visible: false },
    //             { targets: 5, visible: true, orderData: [4], searchable: false },
    //             { targets: 6, visible: false },
    //             { targets: 7, visible: true, orderData: [6], searchable: false },
    //             { targets: 8, visible: true },
    //         ]
    //     });
    // }

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

    ngOnDestroy() {
        //this.chRef.detach();
        this.RouteObservable.unsubscribe();
        //this.GetDataObservable.unsubscribe();
    }

    getData() {
        const headers = new HttpHeaders()
            .set('Content-Type', 'application/json')
            .set('Accept', 'application/json');
        const url = this.httpService.ApiUrl + '/api/nodes/dataholder/PayoutsInUSDForDataHolder?identity=' + this.identity;

        return this.http.get<PayoutInUsdModel[]>(url, { headers });
    }

    settings = {
        actions:  {
    add: false,
    edit: false,
    delete: false
        },
        columns: {
            OfferID: {
            sort: false,
            title: 'Offer ID',
            type: 'custom',
            renderComponent: OfferIdColumnComponent,
            // valuePrepareFunction: (value) => {
            //   return '<a class="navigateJqueryToAngular" href="/offers/' + value + '" onclick="return false;" title="' + value + '" >' + value.substring(0, 40) + '...</a>';
            // }
          },
          TRACAmount: {
            sort: true,
            title: 'TRAC Amount Paid',
            //width: '1%',
            type: 'string',
            filter: false
          },
          USDAmount: {
            sort: true,
            title: 'USD Equivalent Paid',
            //width: '1%',
            type: 'string',
            filter: false,
            valuePrepareFunction: (value) => {
                return value.toFixed(2);
              }
          },
          PayoutTimestamp: {
            sort: true,
            sortDirection: 'desc',
            title: 'Payout Timestamp',
            width: '10%',
            type: 'string',
            filter: false,
            valuePrepareFunction: (value) => {
              const stillUtc = moment.utc(value).toDate();
              const local = moment(stillUtc).local().format('DD/MM/YYYY HH:mm');
              return local;
            }
          },
          TickerTimestamp: {
            sort: true,
            title: 'Ticker Timestamp',
            width: '10%',
            type: 'string',
            filter: false,
            valuePrepareFunction: (value) => {
              const stillUtc = moment.utc(value).toDate();
              const local = moment(stillUtc).local().format('DD/MM/YYYY HH:mm');
              return local;
            }
          },
        },
        pager: {
          display: false,
          perPage: 10
        }
      };
}
