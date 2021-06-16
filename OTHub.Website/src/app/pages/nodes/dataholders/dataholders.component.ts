import { Component, OnInit, ChangeDetectorRef, Input, OnDestroy, EventEmitter, Output } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { OTNodeSummaryModel } from './dataholders-models';
import { MomentModule } from 'ngx-moment';
import { HubHttpService } from '../../hub-http-service';
declare const $: any;
import { ServerDataSource } from 'ng2-smart-table';
import { DecimalPipe } from '@angular/common';
import { DataHolderDetailedModel } from '../dataholder/dataholder-models';
import { NbToastrService, NbGlobalLogicalPosition, NbToastrConfig, NbComponentStatus } from '@nebular/theme';
import { ServerSourceConf } from 'ng2-smart-table/lib/lib/data-source/server/server-source.conf';
import { DataHolderIdentityColumnComponent } from '../../miscellaneous/identitycolumn.component';
@Component({
  selector: 'app-dataholders',
  templateUrl: './dataholders.component.html',
  styleUrls: ['./dataholders.component.scss']
})
export class DataHoldersComponent implements OnInit, OnDestroy {
  getNodesObserver: any;
  settings: any;

  constructor(private http: HttpClient, private chRef: ChangeDetectorRef,
    private httpService: HubHttpService, private toastrService: NbToastrService) {
    this.isTableInit = false;
    this.isLoading = true;
    this.failedLoading = false;
  }

  NodeModel: OTNodeSummaryModel[];
  dataTable: any;
  exportOptionsObj: any;
  isTableInit: boolean;
  failedLoading: boolean;
  isLoading: boolean;
  isDarkTheme: boolean;
  @Input() hideBreadcrumb: boolean;
  @Input() showOnlyMyNodes: string;
  @Input() managementWallet: string;
  @Output() afterLoadWithCount = new EventEmitter<number>();

  source: OTHubServerDataSource;

  ExportToJson() {
    const url = this.getUrl() + '&export=true&exporttype=0';
    window.location.href = url;
  }

  ExportToCsv() {
    const url = this.getUrl() + '&export=true&exporttype=1';
    window.location.href = url;
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

      if (lastSplit == '000') {
        return split[0];
      }

      return split[0] + '.' + lastSplit;
    }
    return split[0];
  }

  getIdentityIcon(identity: string) {
    return this.httpService.ApiUrl + '/api/icon/node/' + identity + '/' + (this.isDarkTheme ? 'dark' : 'light') + '/16';
  }

  pageSizeChanged(event) {
    this.source.setPaging(1, event, true);
  }



  ngOnDestroy() {
    // this.chRef.detach();

    // this.getNodesObserver.unsubscribe();
  }

  // Reload() {
  //   const self = this;

  //   const startTime = new Date();
  //   self.getNodesObserver = this.getNodes().subscribe(data => {
  //     const endTime = new Date();
  //     this.NodeModel = data;

  //     this.chRef.detectChanges();

  //     this.afterLoadWithCount.emit(this.NodeModel.length);

  //     if (!this.isTableInit) {
  //       this.isTableInit = true;
  //       const exportColumns = [0, 2, 3, 4, 5, 6, 7, 8];
  //       this.exportOptionsObj = {
  //         columns: exportColumns,
  //         format: {
  //           body(text, row, column, node) {
  //             return text;
  //           },
  //           header(text, column) {
  //             return text;
  //           }
  //         }
  //       };

  //       const table: any = $('.js-dataholders-table');
  //       this.dataTable = table.DataTable({
  //         responsive: true,
  //         pageLength: 25,
  //         columnDefs: [
  //           { targets: 0, visible: false },
  //           { targets: 1, visible: true },
  //           { targets: 2, visible: false },
  //           { targets: 3, visible: true },
  //           { targets: 4, visible: true },
  //           { targets: 5, visible: true },
  //           { targets: 6, visible: true },
  //           { targets: 7, visible: true }
  //         ],
  //         drawCallback() {
  //           $('img.lazy').lazyload();
  //         }
  //       });
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
    const url = this.httpService.ApiUrl + '/api/nodes/dataholder/' + identity + '?' + (new Date()).getTime();
    return this.http.get<DataHolderDetailedModel>(url, { headers });
  }


  config: NbToastrConfig;
  toastStatus: NbComponentStatus;

  onEdit(event) {
    const oldData = event.data;
    const newData = event.newData;

    this.getNode(newData.NodeId).subscribe(data => {
      if (data) {

        // const oldModel = new MyNodeModel();
        // oldModel.Identity = oldData.Identity;
        // oldModel.DisplayName = '';
        //this.myNodeService.Remove(oldModel);

        // const model = new MyNodeModel();
        // model.Identity = newData.Identity;
        // model.DisplayName = newData.DisplayName;
        //this.myNodeService.Add(model);
        const headers = new HttpHeaders()
          .set('Content-Type', 'application/json')
          .set('Accept', 'application/json');
        let url = this.httpService.ApiUrl + '/api/mynodes/addeditnode?nodeID=' + newData.NodeId;
        if (newData.DisplayName != null) {
          url += '&name=' + newData.DisplayName;
        }
        this.http.post(url, { headers }).subscribe(data => {
          this.resetSource();
          event.confirm.resolve();
          this.source.refresh();
        }, err => {
          this.config = new NbToastrConfig({ duration: 8000 });
          this.config.status = "warning";
          this.config.icon = 'alert-triangle';
          this.toastrService.show(
            'A node was not found by searching for the NodeId ' + newData.NodeId + '. Please check you have entered the right NodeId.', 'Add Node', this.config);
        });
      }
      else {
        this.config = new NbToastrConfig({ duration: 8000 });
        this.config.status = "warning";
        this.config.icon = 'alert-triangle';
        this.toastrService.show(
          'A node was not found by searching for the NodeId ' + newData.NodeId + '. Please check you have entered the right NodeId.', 'Add Node', this.config);
      }
    });
  }

  onDelete(event) {
    var deleteData = event.data;

    var r = confirm("Are you sure you want to delete this node?");
    if (r == true) {
      // const model = new MyNodeModel();
      // model.Identity = deleteData.Identity;
      // model.DisplayName = '';
      //this.myNodeService.Remove(model);
      const headers = new HttpHeaders()
        .set('Content-Type', 'application/json')
        .set('Accept', 'application/json');
      const url = this.httpService.ApiUrl + '/api/mynodes/deletenode?nodeID=' + deleteData.NodeId;
      this.http.delete(url, { headers }).subscribe(data => {
        this.resetSource();
        event.confirm.resolve();
      }, err => {
        this.config = new NbToastrConfig({ duration: 8000 });
        this.config.status = "warning";
        this.config.icon = 'alert-triangle';
        this.toastrService.show(
          'A node was not found by searching for the NodeId ' + deleteData.NodeId + '. Please check you have entered the right NodeId.', 'Add Node', this.config);
      });
    }
  }

  onCreate(event) {
    var newData = event.newData;

    this.getNode(newData.NodeId).subscribe(data => {
      if (data) {
        // const model = new MyNodeModel();
        // model.Identity = newData.Identity;
        // model.DisplayName = newData.DisplayName;
        //this.myNodeService.Add(model);


        const headers = new HttpHeaders()
          .set('Content-Type', 'application/json')
          .set('Accept', 'application/json');
          let url = this.httpService.ApiUrl + '/api/mynodes/addeditnode?nodeID=' + newData.NodeId;
          if (newData.DisplayName != null) {
            url += '&name=' + newData.DisplayName;
          }
        this.http.post(url, { headers }).subscribe(data => {
          this.resetSource();
          event.confirm.resolve();
        }, err => {
          this.config = new NbToastrConfig({ duration: 8000 });
          this.config.status = "warning";
          this.config.icon = 'alert-triangle';
          this.toastrService.show(
            'A node was not found by searching for the NodeId ' + newData.NodeId + '. Please check you have entered the right NodeId.', 'Add Node', this.config);
        });

      } else {
        this.config = new NbToastrConfig({ duration: 8000 });
        this.config.status = "warning";
        this.config.icon = 'alert-triangle';
        this.toastrService.show(
          'A node was not found by searching for the NodeId ' + newData.NodeId + '. Please check you have entered the right NodeId.', 'Add Node', this.config);
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
        edit: this.showOnlyMyNodes === 'true',
        delete: this.showOnlyMyNodes === 'true'
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
        NodeId: {
          title: 'Node Id',
          type: 'custom',
          renderComponent: DataHolderIdentityColumnComponent,
          show: true,
          filter: true,
          sort: true,
          editable: false,
          addable: true,
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
        TotalWonOffers: {
          sort: true,
          title: 'Jobs',
          type: 'number',
          filter: false,
          editable: false,
          addable: false,
          //width: '1%'
          // valuePrepareFunction: (value) => {
          //   return '<a class="navigateJqueryToAngular" href="/offers/' + value + '" onclick="return false;" title="' + value + '" >' + value.substring(0, 40) + '...</a>';
          // }
        },
        WonOffersLast7Days: {
          sort: true,
          sortDirection: 'desc',
          title: 'Jobs (7 Days)',
          type: 'number',
          filter: false,
          editable: false,
          addable: false,
          //width: '7%'
          // valuePrepareFunction: (value) => {
          //   const stillUtc = moment.utc(value).toDate();
          //   const local = moment(stillUtc).local().format('DD/MM/YYYY HH:mm');
          //   return local;
          // }
        },
        ActiveOffers: {
          sort: true,
          title: 'Active Jobs',
          type: 'number',
          filter: false,
          editable: false,
          addable: false,
          //width: '1%'
          // valuePrepareFunction: (value) => { return (value / 1000).toFixed(2).replace(/[.,]00$/, '') + ' KB';}
        },
        PaidTokens: {
          sort: true,
          title: 'Paidout Tokens',
          type: 'number',
          filter: false,
          editable: false,
          addable: false,
          //width: '1%',
          valuePrepareFunction: (value) => {
            return this.formatAmount(value);
          }
        },
        StakeTokens: {
          sort: true,
          title: 'Staked Tokens',
          type: 'number',
          filter: false,
          editable: false,
          addable: false,
          //width: '1%',
          valuePrepareFunction: (value) => {
            return this.formatAmount(value);
          }
        },
        StakeReservedTokens: {
          sort: true,
          title: 'Locked Tokens',
          type: 'number',
          filter: false,
          editable: false,
          addable: false,
          //width: '1%',
          valuePrepareFunction: (value) => {
            return this.formatAmount(value);
          }
        }
      },
      pager: {
        display: true,
        perPage: 25
      }
    };

    if (this.showOnlyMyNodes !== 'true') {
      //delete this.settings.columns.DisplayName;
    } else {
      delete this.settings.columns.TotalWonOffers;
      delete this.settings.columns.WonOffersLast7Days;
      delete this.settings.columns.ActiveOffers;
      delete this.settings.columns.PaidTokens;
      delete this.settings.columns.StakeTokens;
      delete this.settings.columns.StakeReservedTokens;
    }

    this.resetSource();
  }

  getUrl() {
    let url = this.httpService.ApiUrl + '/api/nodes/dataholders?ercVersion=1';
    if (this.showOnlyMyNodes === 'true') {
      url += "&restrictToMyNodes=true";
    } else if (this.managementWallet) {
      url += '&managementWallet=' + this.managementWallet;
    }

    return url;
  }

  resetSource() {
    let url = this.getUrl();

    if (this.source == null) {
      this.source = new OTHubServerDataSource(this.http,
        { endPoint: url });
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

  constructor(http: HttpClient, conf?: ServerSourceConf | {}) {
    super(http, conf);
  }

  protected extractDataFromResponse(res: any): Array<any> {
    var data = super.extractDataFromResponse(res);

    data.forEach(element => {
      //element.DisplayName = this.myNodeService.GetName(element.Identity, true);
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
        element.NodeId = values.NodeId;
        element.DisplayName = values.DisplayName;
        //Don't call super because that will cause problems - instead copy what DataSource.ts does.
        ///super.update(found, values).then(resolve).catch(reject);
        this.emitOnUpdated(element);
        this.emitOnChanged('update');
        resolve(true);
      }).catch(reject);
    });
  }

  find(element) {
    const found = this.data.find(el => el.NodeId == element.NodeId);
    if (found) {
      return Promise.resolve(found);
    }
    return Promise.reject(new Error('Element was not found in the dataset'));
  }
}
