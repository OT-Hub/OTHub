import { DataCreatorSummaryModel } from './datacreators-models';
import { Component, OnInit, ChangeDetectorRef, Input, OnDestroy, Output, EventEmitter } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { MomentModule } from 'ngx-moment';
import { MyNodeService } from '../mynodeservice';
import { HubHttpService } from '../../hub-http-service';
declare const $: any;
import * as moment from 'moment';
import Web3 from 'web3';
import { ServerDataSource } from 'ng2-smart-table';
import { MyNodeModel } from '../mynodemodel';
import { NbToastrConfig, NbToastrService, NbComponentStatus } from '@nebular/theme';
import { DataHolderDetailedModel } from '../dataholder/dataholder-models';
import { ServerSourceConf } from 'ng2-smart-table/lib/lib/data-source/server/server-source.conf';
import {DataCreatorIdentityColumnComponent} from '../../miscellaneous/identitycolumn.component'
@Component({
  selector: 'app-datacreators',
  templateUrl: './datacreators.component.html',
  styleUrls: ['./datacreators.component.scss']
})
export class DatacreatorsComponent implements OnInit  {
  getNodesObserver: any;
  web3: any;
  constructor(private http: HttpClient, private myNodeService: MyNodeService,
              private httpService: HubHttpService, private toastrService: NbToastrService) {
    this.isLoading = true;
    this.failedLoading = false;
    this.web3 = new Web3();
  }

  settings: any;
  NodeModel: DataCreatorSummaryModel[];
  dataTable: any;
  exportOptionsObj: any;
  isTableInit: boolean;
  failedLoading: boolean;
  isLoading: boolean;
  isDarkTheme: boolean;
  @Input() hideBreadcrumb: boolean;
  @Input() showOnlyMyNodes: string;
  @Output() afterLoadWithCount = new EventEmitter<number>();

  source: OTHubServerDataSource;

  pageSizeChanged(event) {
    this.source.setPaging(1, event, true);
  }
  
  ExportToJson() {
    const url = this.httpService.ApiUrl + '/api/nodes/datacreators?ercVersion=1&export=true&exporttype=0';
    window.location.href = url;
  }

  ExportToCsv() {
    const url = this.httpService.ApiUrl + '/api/nodes/datacreators?ercVersion=1&export=true&exporttype=1';
    window.location.href = url;
  }


  // getNodes() {
  //   const headers = new HttpHeaders()
  //     .set('Content-Type', 'application/json')
  //     .set('Accept', 'application/json');
  //   let url = this.httpService.ApiUrl + '/api/nodes/datacreators?ercVersion=1';
  //   if (this.showOnlyMyNodes) {
  //     const myNodes = this.myNodeService.GetAll();
  //     // tslint:disable-next-line:prefer-for-of
  //     for (let index = 0; index < Object.keys(myNodes).length; index++) {
  //       const element = Object.values(myNodes)[index];
  //       url += '&identity=' + element.Identity;
  //     }
  //   }
  //   url += '&' + (new Date()).getTime();
  //   return this.http.get<DataCreatorSummaryModel[]>(url, { headers });
  // }

  getIdentityIcon(identity: string) {
    return this.httpService.ApiUrl + '/api/icon/node/' + identity + '/' + (this.isDarkTheme ? 'dark' : 'light') + '/16';
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



  // ngOnDestroy() {

  // }

  // copyToClipboard() {
  //   const that = { processing(isProcessing) { } };
  //   const e = null;
  //   const button = $.fn.dataTable.ext.buttons.copyHtml5;
  //   const config = this.exportOptionsObj;
  //   button.exportOptions = config;
  //   $.fn.dataTable.ext.buttons.copyHtml5.action.call(that, e, this.dataTable, config, button);
  // }

  // exportToCSV() {
  //   const that = { processing(isProcessing) { } };
  //   const e = null;
  //   const button = $.fn.dataTable.ext.buttons.csvHtml5;
  //   const config = this.exportOptionsObj;
  //   button.exportOptions = config;
  //   $.fn.dataTable.ext.buttons.csvHtml5.action.call(that, e, this.dataTable, config, button);
  // }


  // exportToExcel() {
  //   const that = { processing(isProcessing) { } };
  //   const e = null;
  //   const button = $.fn.dataTable.ext.buttons.excelHtml5;
  //   const config = this.exportOptionsObj;
  //   button.exportOptions = config;
  //   $.fn.dataTable.ext.buttons.excelHtml5.action.call(that, e, this.dataTable, config, button);
  // }

  // print() {
  //   const that = { processing(isProcessing) { } };
  //   const e = null;
  //   const button = $.fn.dataTable.ext.buttons.print;
  //   const config = this.exportOptionsObj;
  //   button.exportOptions = config;
  //   $.fn.dataTable.ext.buttons.print.action.call(that, e, this.dataTable, config, button);
  // }

  // Reload() {
  //   const startTime = new Date();
  //   this.getNodesObserver = this.getNodes().subscribe(data => {
  //     const endTime = new Date();
  //     this.NodeModel = data;

  //     this.chRef.detectChanges();

  //     this.afterLoadWithCount.emit(this.NodeModel.length);

  //     if (!this.isTableInit) {
  //       const exportColumns = [0, 2, 3, 5, 6, 7, 8, 9, 10, 11];
  //       this.exportOptionsObj = {
  //         columns: exportColumns,
  //         format: {
  //           body(text, row, column, node) {
  //             if (column === 7 || column === 8 || column === 9) {
  //               text = text.replace(/[^0-9.]+/g, '');
  //             }
  //             return text;
  //           },
  //           header(text, column) {
  //             if (column === 9) {
  //               text += ' (KB)';
  //             } else if (column === 10) {
  //               text += ' (days)';
  //             } else if (column === 11) {
  //               //text += ' (TRAC)';
  //             }
  //             return text;
  //           }
  //         }
  //       };

  //       const table: any = $('.js-datacreators-table');
  //       this.dataTable = table.DataTable({
  //         responsive: true,
  //         pageLength: 50,
  //         columnDefs: [
  //           { targets: 0, visible: false },
  //           { targets: 1, visible: true, orderData: [0], searchable: false },
  //           { targets: 2, visible: false },
  //           { targets: 3, visible: false },
  //           { targets: 4, visible: true, orderData: [3], searchable: false },
  //           { targets: 5, visible: true },
  //           { targets: 6, visible: true },
  //           { targets: 7, visible: true },
  //           { targets: 8, visible: true },
  //           { targets: 9, visible: true },
  //           { targets: 10, visible: true },
  //           { targets: 11, visible: true }
  //         ],
  //         drawCallback() {
  //           $('img.lazy').lazyload();
  //         }
  //       });
  //       this.isTableInit = true;
  //     }

  //     const diff = endTime.getTime() - startTime.getTime();
  //     let minWait = 0;
  //     if (diff < 100) {
  //       minWait = 100 - diff;
  //     }
  //     setTimeout(() => {
  //       this.isLoading = false;
  //       if (this.NodeModel == null) {
  //         this.failedLoading = true;
  //       }
  //     }, minWait);
  //   }, err => {
  //     this.failedLoading = true;
  //     this.isLoading = false;
  //   });
  // }

  getNode(identity: string) {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    const url = this.httpService.ApiUrl + '/api/nodes/datacreators/' + identity + '?' + (new Date()).getTime();
    return this.http.get<DataHolderDetailedModel>(url, { headers });
  }


  config: NbToastrConfig;
  toastStatus: NbComponentStatus;

  onEdit(event) {
    const oldData = event.data;
    const newData = event.newData;

    this.getNode(newData.Identity).subscribe(data => {
      if (data) {

        const oldModel = new MyNodeModel();
        oldModel.Identity = oldData.Identity;
        oldModel.DisplayName = '';
        this.myNodeService.Remove(oldModel);

        const model = new MyNodeModel();
        model.Identity = newData.Identity;
        model.DisplayName = newData.DisplayName;
        this.myNodeService.Add(model);
        this.resetSource();
        event.confirm.resolve();
        this.source.refresh();
      } else {
        this.config = new NbToastrConfig({duration: 8000});
        this.config.status = "warning";
        this.toastrService.show(
          'A node was not found by searching for the identity ' + newData.Identity + '. Please check you have entered the right identity.',  'Add Node', this.config);
      }
    });
  }

  onDelete(event) {
    var deleteData = event.data;

    const model = new MyNodeModel();
    model.Identity = deleteData.Identity;
    model.DisplayName = '';
    this.myNodeService.Remove(model);
    this.resetSource();
    event.confirm.resolve();
  }

  onCreate(event) {
  var newData = event.newData;

  this.getNode(newData.Identity).subscribe(data => {
    if (data) {
      const model = new MyNodeModel();
      model.Identity = newData.Identity;
      model.DisplayName = newData.DisplayName;
      this.myNodeService.Add(model);
      this.resetSource();
      event.confirm.resolve();
    } else {
      this.config = new NbToastrConfig({duration: 8000});
      this.config.status = "warning";
      this.config.icon = 'alert-triangle';
      this.toastrService.show(
        'A data creator node was not found by searching for the identity ' + newData.Identity + '. Identities must have created at least one job to qualify as a data creator.',  'Add Node', this.config);
    }
  });
  }

  ngOnInit() {
    // this.isDarkTheme = $('body').hasClass('dark');
    // this.Reload();

    this.settings = {
      mode: 'inline',
      actions: {
        add: this.showOnlyMyNodes === 'true',
        edit: this.showOnlyMyNodes  === 'true',
        delete: this.showOnlyMyNodes  === 'true'
      },
      add: {
        addButtonContent: '<i class="nb-plus"></i>',
        createButtonContent: '<i class="nb-checkmark"></i>',
        cancelButtonContent: '<i class="nb-close"></i>',
        confirmCreate: true
      },
      edit: {
        editButtonContent: '<i class="nb-edit"></i>',
        saveButtonContent: '<i class="nb-checkmark"></i>',
        cancelButtonContent: '<i class="nb-close"></i>',
        confirmSave: true
      },
      delete: {
        deleteButtonContent: '<i class="nb-trash"></i>',
        confirmDelete: true,
      },
      columns: {
        Identity: {
          sort: false,
          title: 'Identity',
          type: 'custom',
          filter: true,
          renderComponent: DataCreatorIdentityColumnComponent
          // valuePrepareFunction: (value) => {
          //   if (!value) {
          //     return 'Unknown';
          //   }
  
          //   return '<a target=_self href="/nodes/datacreators/' + value +
          //    '""><img class="lazy" style="height:16px;width:16px;" title="' +
          //     value + '" src="' + this.getIdentityIcon(value) + '">' + value + '</a>';
          // }
        },
        DisplayName: {
          title: 'Name',
          type: 'text',
          show: false,
          filter: false,
          sort: false,
          editable: true,
          addable: true,
        },
        OffersTotal: {
          sort: true,
          title: 'Jobs',
          type: 'number',
          filter: false,
          editable: false,
          addable: false,
          width: '1%'
          // valuePrepareFunction: (value) => {
          //   const stillUtc = moment.utc(value).toDate();
          //   const local = moment(stillUtc).local().format('DD/MM/YYYY HH:mm');
          //   return local;
          // }
        },
        OffersLast7Days: {
          sort: true,
          sortDirection: 'desc',
          title: 'Jobs (7 Days)',
          type: 'number',
          filter: false,
          editable: false,
          addable: false,
          width: '1%'
          // valuePrepareFunction: (value) => { return (value / 1000).toFixed(2).replace(/[.,]00$/, '') + ' KB';}
        },
        LastJob: {
          sort: true,
          title: 'Last Job',
          type: 'string',
          filter: false,
          editable: false,
          addable: false,
          valuePrepareFunction: (value) => {
            const stillUtc = moment.utc(value).toDate();
            const local = moment(stillUtc).local().format('DD/MM/YYYY');
            return local;
          },
          width: '1%'
        },
        StakeTokens: {
          sort: true,
          title: 'Staked Tokens',
          type: 'number',
          filter: false,
          editable: false,
          addable: false,
          valuePrepareFunction: (value) => {
            return this.formatAmount(value);
          },
          width: '1%'
        },
        StakeReservedTokens: {
          sort: true,
          title: 'Locked Tokens',
          type: 'number',
          filter: false,
          editable: false,
          addable: false,
          valuePrepareFunction: (value) => {
            return this.formatAmount(value);
          },
          width: '1%'
        },
        AvgDataSetSizeKB: {
          sort: true,
          title: 'Offer Dataset Size (Avg)',
          type: 'number',
          filter: false,
          editable: false,
          addable: false,
          valuePrepareFunction: (value) => {
            return this.formatAmount(value) + 'KB';
          },
          width: '1%'
        },
        AvgHoldingTimeInMinutes: {
          title: 'Offer Holding Time (Avg)',
          filter: false,
          sort: true,
          editable: false,
          addable: false,
          valuePrepareFunction: (value, node) => {
            var val = node.AvgHoldingTimeInMinutes > 1440 ? node.AvgHoldingTimeInMinutes / 1440 : node.AvgHoldingTimeInMinutes;
            var rounded = Math.round(val);
            var text =(node.AvgHoldingTimeInMinutes > 1440 ? ' day' : ' minute');
            if (rounded != 1) {
              text += "s";
            }
  
            return rounded + text;
          },
          width: '1%'
        },
        AvgTokenAmountPerHolder: {
          title: 'Offer Token Amount (Avg)',
          filter: false,
          sort: true,
          editable: false,
          addable: false,
          width: '1%'
        }
      },
      pager: {
        display: true,
        perPage: 25
      }
    };

    if (this.showOnlyMyNodes !== 'true') {
      delete this.settings.columns.DisplayName;
    }

    this.resetSource();
  }

  resetSource() {
    let url = this.httpService.ApiUrl + '/api/nodes/datacreators?ercVersion=1';
    if (this.showOnlyMyNodes === 'true') {
      const myNodes = this.myNodeService.GetAll();
      // tslint:disable-next-line:prefer-for-of
      const l = Object.keys(myNodes).length;
      for (let index = 0; index < l; index++) {
        const element = Object.values(myNodes)[index];
        url += '&identity=' + element.Identity;
      }

      if (l == 0) {
        url += "&identity=N/A";
      }
    }

    if (this.source == null) {
    this.source = new OTHubServerDataSource(this.http, this.myNodeService,
      { endPoint: url,} ) ;
    }
    else {
      this.source.ResetEndpoint(url);
    }
  }
}

class OTHubServerDataSource extends ServerDataSource { 

  ResetEndpoint(endpoint: string) {
    this.conf.endPoint = endpoint;
  }

  constructor(http: HttpClient, private myNodeService: MyNodeService, conf?: ServerSourceConf | {}) {
    super(http, conf);
  }

  protected extractDataFromResponse(res: any): Array<any> {
    var data = super.extractDataFromResponse(res);

    data.forEach(element => {
      element.DisplayName = this.myNodeService.GetName(element.Identity, true);
    });
    return data;
  }

  public update(element, values): Promise<any> {
    return new Promise((resolve, reject) => {
        this.find(element).then(found => {
            //Copy the new values into element so we use the same instance
            //in the update call.
            // element.name = values.name;
            // element.enabled = values.enabled;
            // element.condition = values.condition;
            element.Identity = values.Identity;
            element.DisplayName = values.DisplayName;

            //Don't call super because that will cause problems - instead copy what DataSource.ts does.
            ///super.update(found, values).then(resolve).catch(reject);
            this.emitOnUpdated(element);
            this.emitOnChanged('update');
            resolve();
        }).catch(reject);
    });
}

  find(element) {
    const found = this.data.find(el => el.Identity == element.Identity);
    if (found) {
      return Promise.resolve(found);
    }
    return Promise.reject(new Error('Element was not found in the dataset'));
  }
}