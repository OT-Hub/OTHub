import { DataCreatorSummaryModel } from './datacreators-models';
import { Component, OnInit, ChangeDetectorRef, Input, OnDestroy, Output, EventEmitter } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { MomentModule } from 'ngx-moment';
import { HubHttpService } from '../../hub-http-service';
declare const $: any;
import * as moment from 'moment';
import { ServerDataSource } from 'ng2-smart-table';
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
  constructor(private http: HttpClient,
              private httpService: HubHttpService, private toastrService: NbToastrService) {
    this.isLoading = true;
    this.failedLoading = false;
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
    const url = this.getUrl() + '&export=true&exporttype=0';
    window.location.href = url;
  }

  ExportToCsv() {
    const url = this.getUrl() + '&export=true&exporttype=1';
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



  getNode(identity: string) {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    const url = this.httpService.ApiUrl + '/api/nodes/datacreator/' + identity + '?' + (new Date()).getTime();
    return this.http.get<DataHolderDetailedModel>(url, { headers });
  }


  config: NbToastrConfig;
  toastStatus: NbComponentStatus;

  onEdit(event) {
    const oldData = event.data;
    const newData = event.newData;

    this.getNode(newData.Identity).subscribe(data => {
      if (data) {

        // const oldModel = new MyNodeModel();
        // oldModel.Identity = oldData.Identity;
        // oldModel.DisplayName = '';
        // //this.myNodeService.Remove(oldModel);

        // const model = new MyNodeModel();
        // model.Identity = newData.Identity;
        // model.DisplayName = newData.DisplayName;
        //this.myNodeService.Add(model);
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
    const deleteData = event.data;

    // const model = new MyNodeModel();
    // model.Identity = deleteData.Identity;
    // model.DisplayName = '';
    //this.myNodeService.Remove(model);
    this.resetSource();
    event.confirm.resolve();
  }

  onCreate(event) {
  const newData = event.newData;

  this.getNode(newData.Identity).subscribe(data => {
    if (data) {
      // const model = new MyNodeModel();
      // model.Identity = newData.Identity;
      // model.DisplayName = newData.DisplayName;
      //this.myNodeService.Add(model);
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
        NodeId: {
          sort: false,
          title: 'Node Id',
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
        // BlockchainName: {
        //   type: 'string',
        //   sort: false,
        //   filter: false,
        //   title: 'Blockchain',
        //   editable: false,
        // },
        // NetworkName: {
        //   type: 'string',
        //   sort: false,
        //   filter: false,
        //   title: 'Network',
        //   editable: false,
        // },
        OffersTotal: {
          sort: true,
          title: 'Jobs',
          type: 'number',
          filter: false,
          editable: false,
          addable: false,
          //width: '1%'
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
          //width: '1%'
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
          //width: '1%'
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
          //width: '1%'
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
          //width: '1%'
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
          //width: '1%'
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
          //width: '1%'
        },
        AvgTokenAmountPerHolder: {
          title: 'Offer Token Amount (Avg)',
          filter: false,
          sort: true,
          editable: false,
          addable: false,
          //width: '1%'
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
      
      delete this.settings.columns.OffersTotal;
      delete this.settings.columns.OffersLast7Days;
      delete this.settings.columns.LastJob;
      delete this.settings.columns.StakeTokens;
      delete this.settings.columns.StakeReservedTokens;
      delete this.settings.columns.AvgDataSetSizeKB;
      delete this.settings.columns.AvgHoldingTimeInMinutes;
      delete this.settings.columns.AvgTokenAmountPerHolder;
    }

    this.resetSource();
  }

  getUrl() {
    let url = this.httpService.ApiUrl + '/api/nodes/datacreators?ercVersion=1';
    if (this.showOnlyMyNodes === 'true') {
      url += "&restrictToMyNodes=true";
    }

    return url;
  }

  resetSource() {
    let url = this.getUrl();

    if (this.source == null) {
    this.source = new OTHubServerDataSource(this.http,
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
            element.Identity = values.Identity;
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
    const found = this.data.find(el => el.Identity == element.Identity);
    if (found) {
      return Promise.resolve(found);
    }
    return Promise.reject(new Error('Element was not found in the dataset'));
  }
}
