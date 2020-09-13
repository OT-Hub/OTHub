import { MyNodeModel } from './../mynodemodel';
import { DatacreatorsComponent } from './../datacreators/datacreators.component';
import { DataHoldersComponent } from './../dataholders/dataholders.component';
import { Component, OnInit, ViewChildren, OnDestroy, AfterViewInit, ViewChild, ElementRef, ChangeDetectorRef, AfterViewChecked } from '@angular/core';
import { MyNodeService } from '../mynodeservice';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { DataHolderDetailedModel } from '../dataholder/dataholder-models';
import { FormsModule } from '@angular/forms';
import { HubHttpService } from '../../hub-http-service';
import * as moment from 'moment';
import { RecentActivityJobModel } from './mynodes-model';
declare const $: any;
declare const swal: any;
import Web3 from 'web3';
@Component({
  selector: 'app-mynodes',
  templateUrl: './mynodes.component.html',
  styleUrls: ['./mynodes.component.scss']
})
export class MynodesComponent implements OnInit, OnDestroy, AfterViewInit, AfterViewChecked {
  EditNode: MyNodeModel;
  web3: any;

  constructor(private http: HttpClient, private myNodeService: MyNodeService, private httpService: HubHttpService,
              private cdr: ChangeDetectorRef) {
    this.web3 = new Web3();
    this.recentActivityHeight = '0px';
  }
  recentActivity: RecentActivityJobModel[];
  recentActivityObserver: any;
  recentActivityHeight: string;
  showDataHolders: boolean;
  showDataCreators: boolean;
  showRecentActivity: boolean;
  //isNewIdentityValid: boolean;
  canImportOldNodes = false;
  // showAddNodeWizard = false;
  // showEditNodeWizard = false;
  newIdentity: DataHolderDetailedModel;
  MyNodes: { [key: string]: MyNodeModel; };
  MyNodesKeys: string[];
  MyNodesValues: MyNodeModel[];
  isDarkTheme: boolean;
  @ViewChildren(DataHoldersComponent) dataHolders;
  @ViewChildren(DatacreatorsComponent) dataCreators;
  @ViewChild('recentActivityView') public recentActivityView: ElementRef;
  @ViewChild('dataHoldersView') public dataHoldersView: ElementRef;
  getNode(identity: string) {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    const url = this.httpService.ApiUrl + '/api/nodes/dataholder/' + identity + '?' + (new Date()).getTime();
    return this.http.get<DataHolderDetailedModel>(url, { headers });
  }

  ngAfterViewInit() {

  }

  ngAfterViewChecked() {
    // if (this.recentActivityView != null && this.dataHoldersView != null) {
    //   this.recentActivityView.nativeElement.style.height = this.dataHoldersView.nativeElement.offsetHeight - 30 + 'px';
    // }
    // if (this.cdr != null) {
    //   this.cdr.detectChanges();
    // }
  }


  AddNodeClicked() {
    // $('#wizard_with_validation li:not(.first)').addClass('disabled');
    // $('#nodeInformationIdentity').text('');
    // $('#nodeInformationNodeId').text('');
    // $('#nodeInformationStakedTokens').text('');
    // $('#nodeInformationLockedTokens').text('');
    // $('#customiseNodeIdentity').val('');
    // $('#customiseNodeDisplayName').val('');
    // $('#searchIdentity').val('');
    // $('#wizard_with_validation-t-0').get(0).click();
    // this.showAddNodeWizard = true;
  }

  CancelNodeClicked() {
    // this.showAddNodeWizard = false;
  }

  EditMyNode(myNode: MyNodeModel) {
    // this.EditNode = myNode;
    // this.showEditNodeWizard = true;
  }

  SaveEditNodeClicked() {
    // this.myNodeService.Save();
    // this.showEditNodeWizard = false;
    // this.EditNode = null;
  }

  ngOnDestroy() {
    // this.recentActivityObserver.unsubscribe();
    // this.cdr.detach();
  }

  DeleteMyNode(myNode: MyNodeModel) {

    // const self = this;

    // swal({
    //   title: 'Are you sure?',
    //   text: myNode.Identity + ' will be removed from My Nodes.',
    //   type: 'warning',
    //   showCancelButton: true,
    //   confirmButtonColor: '#fb483a',
    //   confirmButtonText: 'Yes, delete it!',
    //   closeOnConfirm: true
    //   // tslint:disable-next-line:only-arrow-functions
    // }, function () {
    //   self.myNodeService.Remove(myNode);

    //   $.notify({
    //     message: 'Removed node from My Nodes.'
    //   },
    //     {
    //       type: 'bg-blue',
    //       allow_dismiss: true,
    //       newest_on_top: true,
    //       timer: 1000,
    //       placement: {
    //         from: 'top',
    //         align: 'left'
    //       },
    //       animate: {
    //         enter: 'animated fadeInLeft',
    //         exit: 'animated fadeOutLeft'
    //       },
    //       template: '<div data-notify="container" class="bootstrap-notify-container alert alert-dismissible {0} ' + (true ? 'p-r-35' : '') + '" role="alert">' +
    //         '<button type="button" aria-hidden="true" class="close" data-notify="dismiss">×</button>' +
    //         '<span data-notify="icon"></span> ' +
    //         '<span data-notify="title">{1}</span> ' +
    //         '<span data-notify="message">{2}</span>' +
    //         '<div class="progress" data-notify="progressbar">' +
    //         '<div class="progress-bar progress-bar-{0}" role="progressbar" aria-valuenow="0" aria-valuemin="0" aria-valuemax="100" style="width: 0%;"></div>' +
    //         '</div>' +
    //         '<a href="{3}" target="{4}" data-notify="url"></a>' +
    //         '</div>'
    //     });
    //   self.loadMyNodes();
    //   if (self.dataHolders.first != null) {
    //     self.dataHolders.first.Reload();
    //   }
    //   if (self.dataCreators.first != null) {
    //     self.dataCreators.first.Reload();
    //   }

    //   self.checkForOldFavouriteNodes();
    // });
  }

  loadMyNodes() {
    this.MyNodes = this.myNodeService.GetAll();
    this.MyNodesKeys = Object.keys(this.MyNodes);
    this.MyNodesValues = Object.values(this.MyNodes);

    // if (this.MyNodesKeys.length > 0) {
    //   this.showDataCreators = true;
    //   this.showDataHolders = true;
    // } else {
    //   this.showDataCreators = false;
    //   this.showDataHolders = false;
    // }
  }

  // addMyNode(model: MyNodeModel, showAlerts: boolean) {
  //   if (this.myNodeService.Add(model)) {
  //     this.loadMyNodes();
  //     this.showDataCreators = true;
  //     this.showDataHolders = true;
  //     if (this.dataHolders.first != null) {
  //       this.dataHolders.first.Reload();
  //     }
  //     if (this.dataCreators.first != null) {
  //       this.dataCreators.first.Reload();
  //     }
  //     this.showAddNodeWizard = false;
  //     if (showAlerts) {
  //     swal('You have added a node to My Nodes.', 'Success!', 'success');
  //     }
  //   } else if (showAlerts) {
  //     swal('You have already added this node.', 'Warning!', 'warning');
  //   }
  // }

  afterDataHoldersLoad(count: number) {
    //this.showDataHolders = count > 0;
  }

  afterDataCreatorsLoad(count: number) {
    //this.showDataCreators = count > 0;
  }

  showDataHoldersChanged(value: boolean) {
    this.showDataHolders = value;
    localStorage.setItem('MyNodes_ShowDataHolders', value.toString().toLowerCase());
  }

  showDataCreatorsChanged(value: boolean) {
    this.showDataCreators = value;
    localStorage.setItem('MyNodes_ShowDataCreators', value.toString().toLowerCase());
  }

  showRecentActivityChanged(value: boolean) {
    this.showRecentActivity = value;
    localStorage.setItem('MyNodes_ShowRecentActivity', value.toString().toLowerCase());
  }

  // ImportPrevious() {
  //   const text = localStorage.getItem('OTHub_FavouriteNodes');
  //   if (!text) {
  //     return;
  //   }

  //   const split = text.split(';');
  //   if (split.length > 0) {
  //     // tslint:disable-next-line:prefer-for-of
  //     for (let i = 0; i < split.length; i++) {
  //       const entry = split[i];
  //       const identityNameSplit = entry.split('~');
  //       const model = new MyNodeModel();
  //       if (identityNameSplit.length === 1) {
  //         model.Identity = identityNameSplit[0];
  //         this.addMyNode(model, false);
  //       } else if (identityNameSplit.length === 2) {
  //         model.Identity = identityNameSplit[0];
  //         model.DisplayName = identityNameSplit[1];
  //         this.addMyNode(model, false);
  //       }
  //     }
  //     this.canImportOldNodes = false;
  //   }
  // }

  // checkForOldFavouriteNodes() {
  //   if (this.MyNodesKeys.length > 0) {
  //     this.canImportOldNodes = false;
  //     return;
  //   }
  //   const text = localStorage.getItem('OTHub_FavouriteNodes');
  //   if (!text) {
  //     this.canImportOldNodes = false;
  //     return;
  //   }

  //   const split = text.split(';');
  //   if (split.length > 0) {
  //     this.canImportOldNodes = true;
  //   } else {
  //     this.canImportOldNodes = false;
  //   }
  // }

  getIdentityIcon(identity: string, size: number) {
    return this.httpService.ApiUrl + '/api/icon/node/' + identity + '/' + (this.isDarkTheme ? 'dark' : 'light') + '/' + size;
  }


  getRecentActivity() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    let url = this.httpService.ApiUrl + '/api/recentactivity?1=1';

    const myNodes = this.myNodeService.GetAll();
    // tslint:disable-next-line:prefer-for-of
    for (let index = 0; index < Object.keys(myNodes).length; index++) {
      const element = Object.values(myNodes)[index];
      url += '&identity=' + element.Identity;
    }

    url += '&' + (new Date()).getTime();
    return this.http.get<RecentActivityJobModel[]>(url, { headers });
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

  ngOnInit() {
    const self = this;

    self.loadMyNodes();

    self.recentActivityObserver = this.getRecentActivity().subscribe(data => {
      self.recentActivity = data;
    });

    let rawValue = localStorage.getItem('MyNodes_ShowDataHolders');
    this.showDataHolders = rawValue ? rawValue == 'true' : true;

    rawValue = localStorage.getItem('MyNodes_ShowDataCreators');
    this.showDataCreators = rawValue ? rawValue == 'true' : false;

    rawValue = localStorage.getItem('MyNodes_ShowRecentActivity');
    this.showRecentActivity = rawValue ? rawValue == 'true' : true;

    //self.checkForOldFavouriteNodes();

    this.isDarkTheme = $('body').hasClass('dark');

    // // tslint:disable-next-line:no-unused-expression
    // 'use strict';
    // // tslint:disable-next-line:only-arrow-functions
    // $(function () {

    //   // Advanced form with validation
    //   const form = $('#wizard_with_validation').show();
    //   form.steps({
    //     headerTag: 'h3',
    //     bodyTag: 'fieldset',
    //     transitionEffect: 'slideLeft',
    //     onInit(event, currentIndex) {

    //       // set tab width
    //       const $tab = $(event.currentTarget).find('ul[role="tablist"] li');
    //       const tabCount = $tab.length;
    //       $tab.css('width', (100 / tabCount) + '%');

    //       // set button waves effect
    //       setButtonWavesEffect(event);
    //     },
    //     onStepChanging(event, currentIndex, newIndex) {
    //       if (currentIndex > newIndex) { return true; }

    //       if (currentIndex < newIndex) {
    //         form.find('.body:eq(' + newIndex + ') label.error').remove();
    //         form.find('.body:eq(' + newIndex + ') .error').removeClass('error');
    //       }

    //       form.validate().settings.ignore = ':disabled,:hidden';
    //       const isValid = form.valid();
    //       const identity = form[0][1].value;
    //       if (isValid && currentIndex === 0 && newIndex === 1) {
    //         self.isNewIdentityValid = false;
    //         self.newIdentity = null;
    //         $('#nodeInformationFailed').hide();
    //         $('#nodeInformationLoaded').hide();
    //         $('#nodeInformationLoading').show();
    //         self.getNode(identity).subscribe(data => {
    //           self.newIdentity = data;
    //           if (data) {
    //             self.isNewIdentityValid = true;
    //             $('#nodeInformationFailed').hide();
    //             $('#nodeInformationLoading').hide();
    //             $('#nodeInformationLoaded').show();
    //             $('#nodeInformationIdentity').text(data.Identity);
    //             $('#nodeInformationNodeId').text(data.NodeId);
    //             $('#nodeInformationStakedTokens').text(data.StakeTokens);
    //             $('#nodeInformationLockedTokens').text(data.StakeReservedTokens);
    //           } else {
    //             $('#nodeInformationLoaded').hide();
    //             $('#nodeInformationLoading').hide();
    //             $('#nodeInformationFailed').show();
    //             self.isNewIdentityValid = false;
    //           }
    //         }, err => {
    //           $('#nodeInformationIdentity').text('');
    //           $('#nodeInformationNodeId').text('');
    //           $('#nodeInformationStakedTokens').text('');
    //           $('#nodeInformationLockedTokens').text('');
    //           $('#nodeInformationLoaded').hide();
    //           $('#nodeInformationLoading').hide();
    //           $('#nodeInformationFailed').show();
    //         });
    //       } else if (newIndex === 2) {
    //         if (self.newIdentity === null || !self.isNewIdentityValid ||
    //            self.newIdentity.Identity.toLowerCase() !== identity.toLowerCase()) {
    //           return false;
    //         } else {
    //           $('#customiseNodeIdentity').val(self.newIdentity.Identity);
    //         }
    //       }
    //       return isValid;
    //     },
    //     onStepChanged(event, currentIndex, priorIndex) {
    //       setButtonWavesEffect(event);
    //     },
    //     onFinishing(event, currentIndex) {
    //       form.validate().settings.ignore = ':disabled';

    //       return form.valid();
    //     },
    //     onFinished(event, currentIndex) {
    //       const model = new MyNodeModel();
    //       model.Identity = $('#customiseNodeIdentity').val();
    //       model.DisplayName = $('#customiseNodeDisplayName').val();
    //       self.addMyNode(model, true);
    //     }
    //   });

    //   $('#wizard_with_validation a').addClass('btn-primary');

    //   form.validate({
    //     highlight(input) {
    //       $(input).parents('.form-line').addClass('error');
    //     },
    //     unhighlight(input) {
    //       $(input).parents('.form-line').removeClass('error');
    //     },
    //     errorPlacement(error, element) {
    //       $(element).parents('.form-group').append(error);
    //     }
    //   });
    // });

    // function setButtonWavesEffect(event) {
    //   $(event.currentTarget).find('[role="menu"] li a').removeClass('waves-effect');
    //   $(event.currentTarget).find('[role="menu"] li:not(.disabled) a').addClass('waves-effect');
    // }
  }

}
