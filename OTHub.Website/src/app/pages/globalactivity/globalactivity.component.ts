import { GlobalActivityModel, GlobalActivityModelWithPaging } from './globalactivity-models';
import { Component, OnInit, ChangeDetectorRef, OnDestroy, NgZone } from '@angular/core';
import { LocalDataSource, ServerDataSource } from 'ng2-smart-table';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
declare const $: any;
import { HubHttpService } from '../hub-http-service';
import { MyNodeService } from '../nodes/mynodeservice';
import * as moment from 'moment';
@Component({
    selector: 'app-globalactivity',
    templateUrl: './globalactivity.component.html',
    styleUrls: ['./globalactivity.component.scss']
})
export class GlobalActivityComponent {

    constructor(private http: HttpClient,
        public myNodeService: MyNodeService, private httpService: HubHttpService,
        private router: Router,
        private ngZone: NgZone) {
        this.isLoading = true;
        this.isTableInit = false;
        this.failedLoading = false;
        this.IsTestNet = httpService.IsTestNet;
        this.pageNumber = 0;
        this.resultsPerPage = 25;
        this.filterOptions = ['New Offer', 'Finalized Offer', 'Data Holder Chosen', 'Offer Payout', 'Tokens Deposited', 'Tokens Withdrawn', 'Identity Created', 'Node Approved',
            'Litigation Initiated', 'Litigation Answered', 'Litigation Failed', 'Litigation Passed', 'Replacement Started', 'Data Holder Chosen as Replacement'];
        this.selectedFilterOptions = ['New Offer', 'Finalized Offer', 'Data Holder Chosen', 'Offer Payout', 'Tokens Deposited', 'Tokens Withdrawn', 'Identity Created', 'Node Approved',
            'Litigation Initiated', 'Litigation Answered', 'Litigation Failed', 'Litigation Passed', 'Replacement Started', 'Data Holder Chosen as Replacement'];

        const url = this.httpService.ApiUrl + '/api/globalactivity';

        this.source = new ServerDataSource(http,
            { endPoint: url });
    }
    Summary: GlobalActivityModelWithPaging;
    ActivityModels: GlobalActivityModel[];
    getDataObserver: any;
    failedLoading: boolean;
    IsTestNet: boolean;
    isLoading: boolean;
    pageNumber: number;
    searchFilter: string;
    resultsPerPage: number;
    dataTableOptions: any;
    dataTable: any;
    exportOptionsObj: any;
    isTableInit: boolean;
    filterOptions: string[];
    selectedFilterOptions: string[];
    isDarkTheme: boolean;

    source: ServerDataSource;

    ExportToJson() {
        const url = this.httpService.ApiUrl + '/api/globalactivity?export=true&exporttype=0';
        window.location.href = url;
      }

      ExportToCsv() {
        const url = this.httpService.ApiUrl + '/api/globalactivity?export=true&exporttype=1';
        window.location.href = url;
      }

    getIdentityIcon(identity: string) {
        return this.httpService.ApiUrl + '/api/icon/node/' + identity + '/' + (this.isDarkTheme ? 'dark' : 'light') + '/16';
    }

    settings = {
        actions: {
            add: false,
            edit: false,
            delete: false
        },
        columns: {
            Timestamp: {
                sort: true,
                sortDirection: 'desc',
                title: 'Time',
                type: 'string',
                filter: false,
                valuePrepareFunction: (value) => {
                    const stillUtc = moment.utc(value).toDate();
                    const local = moment(stillUtc).local().format('DD/MM/YYYY HH:mm');
                    return local;
                }
            },
          BlockchainDisplayName: {
                type: 'string',
                sort: false,
                filter: false,
                title: 'Blockchain'
              },
            EventName: {
                sort: true,
                title: 'Event',
                type: 'string',
                // valuePrepareFunction: (value) => {
                //   return '<a class="navigateJqueryToAngular" href="/offers/' + value + '" onclick="return false;" title="' + value + '" >' + value.substring(0, 40) + '...</a>';
                // }
            },
            RelatedEntity: {
                sort: true,
                title: 'Related to',
                type: 'html',
                filter: true,
                valuePrepareFunction: (value, row) => {
                    if (row.EventName === 'Offer Payout' || row.EventName === 'Tokens Withdrawn'
                        || row.EventName === 'Tokens Deposited' || row.EventName === 'Identity Created' || row.EventName === 'Litigation Failed' ||
                        row.EventName === 'Litigation Passed' || row.EventName === 'Litigation Answered' || row.EventName === 'Litigation Initiated' ||
                        row.EventName === 'Replacement Started' || row.EventName === 'Data Holder Chosen as Replacement' || row.EventName === 'Data Holder Chosen') {
                        const name = this.myNodeService.GetName(value, false);
                        return '<a class="navigateJqueryToAngular" href="/nodes/dataholders/' + value + '" onclick="return false;">' + name + '</a>';
                    }
                    if (row.EventName === 'New Offer' || row.EventName === 'Finalized Offer') {
                        return '<a title="' + value + '" class="navigateJqueryToAngular" href="/offers/' + value + '" onclick="return false;">' + value.substring(0, 40) + '...</a>';
                    }
                    return 'Unknown';
                }
            },
            RelatedEntity2: {
                sort: true,
                title: 'Related to (secondary)',
                type: 'html',
                filter: true,
                valuePrepareFunction: (value, row) => {
                    if (row.EventName === 'Litigation Failed' ||
                        row.EventName === 'Litigation Passed' || row.EventName === 'Litigation Answered' || row.EventName === 'Litigation Initiated' ||
                        row.EventName === 'Replacement Started' || row.EventName === 'Data Holder Chosen as Replacement' || row.EventName === 'Data Holder Chosen') {
                        const name = this.myNodeService.GetName(value, false);
                        return '<a title="' + value + '" class="navigateJqueryToAngular" href="/offers/' + value + '" onclick="return false;">' + value.substring(0, 40) + '...</a>';
                    }

                    return '';
                }
            },
            TransactionHash: {
                sort: true,
                title: 'Transaction Hash',
                type: 'html',
                filter: true,
                valuePrepareFunction: (data, row) => {
                    if (this.IsTestNet) {
                        return '<a title="' + data + '" href="https://rinkeby.etherscan.io/tx/' + data + '" target="_blank">' + data.substring(0, 25) + '...</a>';
                    }
                    return '<a title="' + data + '" href="https://etherscan.io/tx/' + data + '" target="_blank">' + data.substring(0, 25) + '...</a>';
                }
            },
        },
        pager: {
            display: true,
            perPage: 25
        }
    };

    pageSizeChanged(event) {
        this.source.setPaging(1, event, true);
    }

    ngOnInit() {
        const that = this;
        //this.isDarkTheme = $('body').hasClass('dark');
        $(() => {
            $(document).on('click', '.navigateJqueryToAngular', (sender) => {
                that.ngZone.run(() => {
                    that.router.navigateByUrl(sender.currentTarget.getAttribute('href'));
                });
            });

            // $('#filtersGlobalActivity a').on('click', (event) => {
            //     debugger;
            //     const $target = $(event.currentTarget);
            //     const val = $target.attr('data-value');
            //     const $inp = $target.find('input');
            //     let idx;

            //     idx = that.filterOptions.indexOf(val);

            //     if (idx > -1) {
            //         that.filterOptions.splice(idx, 1);
            //         setTimeout(() => { $inp.prop('checked', false); }, 0);
            //     } else {
            //         that.filterOptions.push(val);
            //         setTimeout(() => { $inp.prop('checked', true); }, 0);
            //     }

            //     $(event.target).blur();

            //     return false;
            // });

        });


        // this.loadTable();

    }

    // onFilterClick(event) {

    //     const $target = $(event.currentTarget);
    //     const val = $target.attr('data-value');
    //     const $inp = $target.find('input');
    //     let idx;

    //     idx = this.selectedFilterOptions.indexOf(val);

    //     if (idx > -1) {
    //         this.selectedFilterOptions.splice(idx, 1);
    //         setTimeout(() => { $inp.prop('checked', false); }, 0);
    //     } else {
    //         this.selectedFilterOptions.push(val);
    //         setTimeout(() => { $inp.prop('checked', true); }, 0);
    //     }

    //     $(event.target).blur();


    //     const table: any = $('#js-activityTable');

    //     table.DataTable().ajax.reload();

    //     return false;
    // }

    ngOnDestroy() {
        // this.chRef.detach();
        // this.getDataObserver.unsubscribe();
    }

    // getData() {
    //     const headers = new HttpHeaders()
    //         .set('Content-Type', 'application/json')
    //         .set('Accept', 'application/json');
    //     let url = this.httpService.ApiUrl + '/api/globalactivity?pageLength=' +
    //         this.resultsPerPage + '&start=' + (this.pageNumber * this.resultsPerPage).toString() + '&searchText=' + this.searchFilter + '&filters=' + this.selectedFilterOptions.join('&filters=');
    //     url += '&' + (new Date()).getTime();
    //     return this.http.get<GlobalActivityModelWithPaging>(url, { headers });
    // }

    // loadTable() {
    //     const that = this;

    //     if (this.isTableInit === false) {
    //         this.isTableInit = true;
    //         const exportColumns = [0, 1, 2, 3, 4];
    //         this.exportOptionsObj = {
    //             columns: exportColumns,
    //             format: {
    //                 body(text, row, column, node) {
    //                     return text;
    //                 },
    //                 header(text, column) {
    //                     return text;
    //                 }
    //             }
    //         };

    //         const table: any = $('#js-activityTable');

    //         this.dataTableOptions = {
    //             responsive: true,
    //             serverSide: true,
    //             pageLength: 25,
    //             start: 0,
    //             processing: true,
    //             ajax: (dataTablesParameters: any, callback) => {
    //                 that.resultsPerPage = dataTablesParameters.length;
    //                 that.pageNumber = dataTablesParameters.start / that.resultsPerPage;
    //                 that.searchFilter = dataTablesParameters.search.value;
    //                 that.getDataObserver = that.getData().subscribe(resp => {
    //                     this.chRef.detectChanges();
    //                     that.Summary = resp;
    //                     that.ActivityModels = that.Summary.data;
    //                     callback({
    //                         recordsTotal: resp.recordsTotal,
    //                         recordsFiltered: resp.recordsFiltered,
    //                         data: resp.data
    //                     });
    //                     this.isLoading = false;
    //                 });


    //             },
    //             columnDefs: [
    //                 {
    //                     orderable: true, targets: 0, visible: true, data: 'Timestamp',
    //                     render(data, type, row) {
    //                         // tslint:disable-next-line:max-line-length
    //                         const stillUtc = moment.utc(row.Timestamp).toDate();
    //                         const local = moment(stillUtc).local().format('DD/MM/YYYY HH:mm');
    //                         return local;
    //                         //return '<a class="navigateJqueryToAngular" href="/nodes/datacreators/' + row.DCIdentity + '" onclick="return false;"><img class="lazy" style="height:16px;width:16px;" title="' + row.DCIdentity + '" src="' + that.getIdentityIcon(row.DCIdentity) + '"></a>';
    //                     }
    //                 },
    //                 {
    //                     orderable: true, targets: 1, visible: true, data: 'EventName',
    //                     render(data, type, row) {
    //                         return data;
    //                     }
    //                 },
    //                 {
    //                     orderable: true, targets: 2, searchable: true, data: 'RelatedEntity',
    //                     render(data, type, row) {
    //                         if (row.EventName === 'Offer Payout' || row.EventName === 'Tokens Withdrawn'
    //                             || row.EventName === 'Tokens Deposited' || row.EventName === 'Identity Created' || row.EventName === 'Litigation Failed' ||
    //                             row.EventName === 'Litigation Passed' || row.EventName === 'Litigation Answered' || row.EventName === 'Litigation Initiated' ||
    //                             row.EventName === 'Replacement Started' || row.EventName === 'Data Holder Chosen as Replacement' || row.EventName === 'Data Holder Chosen') {
    //                             const name = that.myNodeService.GetName(data, false);
    //                             return '<a class="navigateJqueryToAngular" href="/nodes/dataholders/' + data + '" onclick="return false;"><img class="lazy" style="height:16px;width:16px;" title="' + data + '" src="' + that.getIdentityIcon(data) + '"> ' + name + '</a>';
    //                         }
    //                         if (row.EventName === 'New Offer' || row.EventName === 'Finalized Offer') {
    //                             return '<a title="' + data + '" class="navigateJqueryToAngular" href="/offers/' + data + '" onclick="return false;">' + data.substring(0, 40) + '...</a>';
    //                         }
    //                         return 'Unknown';
    //                     }
    //                 },
    //                 {
    //                     orderable: true, targets: 3, searchable: true, data: 'RelatedEntity2',
    //                     render(data, type, row) {
    //                         if (row.EventName === 'Litigation Failed' ||
    //                             row.EventName === 'Litigation Passed' || row.EventName === 'Litigation Answered' || row.EventName === 'Litigation Initiated' ||
    //                             row.EventName === 'Replacement Started' || row.EventName === 'Data Holder Chosen as Replacement' || row.EventName === 'Data Holder Chosen') {
    //                             const name = that.myNodeService.GetName(data, false);
    //                             return '<a title="' + data + '" class="navigateJqueryToAngular" href="/offers/' + data + '" onclick="return false;">' + data.substring(0, 40) + '...</a>';
    //                         }

    //                         return '';
    //                     }
    //                 },
    //                 {
    //                     orderable: true, targets: 4, searchable: true, data: 'TransactionHash',
    //                     render(data, type, row) {
    //                         if (that.IsTestNet) {
    //                             return '<a title="' + data + '" href="https://rinkeby.etherscan.io/tx/' + data + '" target="_blank">' + data.substring(0, 25) + '...</a>';
    //                         }
    //                         return '<a title="' + data + '" href="https://etherscan.io/tx/' + data + '" target="_blank">' + data.substring(0, 25) + '...</a>';
    //                     }
    //                 },
    //             ],
    //             // drawCallback() {
    //             //   $('img.lazy').lazyload();
    //             // }
    //         };

    //         this.dataTable = table.DataTable(this.dataTableOptions);
    //     }
    // }
}
