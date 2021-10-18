import { Component, OnInit, InjectionToken, Inject, ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
declare const $: any;
declare const ethereum: any;
declare const web3: any;
import { MomentModule } from 'ngx-moment';
import {DatePipe, isPlatformBrowser} from '@angular/common';
import Web3 from 'web3';
import { HubHttpService } from '../../hub-http-service';
import { ContractAddress, RecentPayoutGasPrice, BeforePayoutResult } from './manual-payout-models';
import { OTOfferDetailModel } from 'app/pages/jobs/offersdetail/offersdetail-models';
@Component({
  selector: 'app-manual-payout-page',
  templateUrl: './manual-payout-page.component.html',
  styleUrls: ['./manual-payout-page.component.scss']
})
export class ManualPayoutPageComponent implements OnInit {
  RouteObservable: any;
  identity: string;
  nodeID: string;
  offerId: string;
  blockchainID: number;
  selectedChainID: string;
  requiredChainID: string;
  isMetaMaskInstalled: boolean;
  isMetaMaskUnlocked: boolean;
  selectedAddress: string;
  sendError: string;
  holdingAddress: ContractAddress;
  holdingStorageAddress: ContractAddress;
  litigationStorageAddress: ContractAddress;
  allHoldingAddresses: ContractAddress[];
  allHoldingStorageAddresses: ContractAddress[];
  allLitigationStorageAddresses: ContractAddress[];
  failedLoading: boolean;
  isLoading: boolean;
  gasPrice: number;
  hasSentTransaction: boolean;
  recentGasPrices: RecentPayoutGasPrice[];
  sentTransactionHash: string;
  canPayoutResult: BeforePayoutResult;
  isBusySending: boolean;
  offer: OTOfferDetailModel;
  paidoutSoFar: Number;
  showLitigationWarning: boolean;

  constructor(private http: HttpClient, private route: ActivatedRoute, private router: Router, private dect: ChangeDetectorRef,
    private httpService: HubHttpService) {
    this.isLoading = true;
    this.failedLoading = false;
    this.isBusySending = false;
    this.hasSentTransaction = false;
  }

  getTotalPaidoutForJob() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    const url = this.httpService.ApiUrl + '/api/nodes/dataholder/GetTotalPaidoutForJob?offerID=' + this.offerId + '&identity=' + this.identity;
    const promise = this.http.get<Number>(url, { headers: headers });
    return promise;
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

    this.RouteObservable = this.route.params.subscribe(params => {
      this.nodeID = params.nodeID;
      this.offerId = params.offerId;
      this.blockchainID = params.blockchainID;
      this.identity = params.identity;

      const self = this;

      this.getOffer().subscribe(data => {
        this.offer = data;

        if (this.offer) {
          var holder = this.offer.Holders.find(h => h.NodeId == this.nodeID);
          if (holder) {
            if (holder.LitigationStatus == 4 || holder.LitigationStatus == 3 || holder.LitigationStatus == 0) {
              this.showLitigationWarning = true;
            }
          }
        }
      });

      this.getTotalPaidoutForJob().subscribe(data => {
        this.paidoutSoFar = data;
      });

      this.getHoldingStorageAddresses().subscribe(holdingStorageAddresses => {

        this.allHoldingStorageAddresses = holdingStorageAddresses;

        // tslint:disable-next-line:prefer-for-of
        for (let index = 0; index < holdingStorageAddresses.length; index++) {
          const element = holdingStorageAddresses[index];
          if (element.IsLatest) {
            this.holdingStorageAddress = element;
            break;
          }
        }

        this.getLitigationStorageAddresses().subscribe(litigationStorageAddresses => {

          this.allLitigationStorageAddresses = litigationStorageAddresses;

          // tslint:disable-next-line:prefer-for-of
          for (let index = 0; index < litigationStorageAddresses.length; index++) {
            const element = litigationStorageAddresses[index];
            if (element.IsLatest) {
              this.litigationStorageAddress = element;
              break;
            }
          }

          this.getHoldingAddresses().subscribe(data => {
            this.allHoldingAddresses = data;

            // tslint:disable-next-line:prefer-for-of
            for (let index = 0; index < data.length; index++) {
              const element = data[index];
              if (element.IsLatest) {
                this.holdingAddress = element;
                break;
              }
            }
            this.checkFinishedLoading();

            this.getNetworkID().subscribe(data => {
              this.requiredChainID = data.toString();
              this.checkFinishedLoading();
            });

          }, err => {
            this.failedLoading = true;
            this.isLoading = false;
          });

        }, err => {
          this.failedLoading = true;
          this.isLoading = false;
        });
      }, err => {
        this.failedLoading = true;
        this.isLoading = false;
      });

      this.getRecentPayoutGasPrices().subscribe(data => {
        this.recentGasPrices = data;
        this.checkFinishedLoading();
      }, err => {
        this.failedLoading = true;
        this.isLoading = false;
      });

      try {
        if (ethereum == null) {
          this.isMetaMaskInstalled = false;
        } else {
          this.isMetaMaskInstalled = true;

          // tslint:disable-next-line:only-arrow-functions
          ethereum.on('accountsChanged', function (accounts) {
            self.loadAccount(accounts);
          });

          ethereum.on('chainChanged', function (chainID: string) {
            setTimeout(function(){ 
              self.selectedChainID = ethereum.networkVersion;
            }, -1);
        
          });

          this.isMetaMaskUnlocked = false;
        }
      } catch (error) {
        this.isMetaMaskInstalled = false;
      }
    });
  }

  formatTime(value) {
    if (value > 1440) {
      const days = (value / 1440);
      if ((days / 365) % 1 == 0) {
        return (days / 365).toString() + ' years';
      }
      return +days.toFixed(1).replace(/[.,]00$/, '') + (days === 1 ? ' day' : ' days');
    }
    return value + ' minute' + (value == 1 ? '' : 's');
  }

  holdingSmartContractChanged(address: string) {
    // tslint:disable-next-line:prefer-for-of
    for (let index = 0; index < this.allHoldingAddresses.length; index++) {
      const element = this.allHoldingAddresses[index];
      if (element.Address === address) {
        this.holdingAddress = element;
        return;
      }
    }
    alert('Error finding smart contract. Please reload the page to avoid issues paying out.');
  }

  holdingStorageSmartContractChanged(address: string) {
    // tslint:disable-next-line:prefer-for-of
    for (let index = 0; index < this.allHoldingStorageAddresses.length; index++) {
      const element = this.allHoldingStorageAddresses[index];
      if (element.Address === address) {
        this.holdingStorageAddress = element;
        return;
      }
    }
    alert('Error finding smart contract. Please reload the page to avoid issues paying out.');
  }

  litigationStorageSmartContractChanged(address: string) {
    // tslint:disable-next-line:prefer-for-of
    for (let index = 0; index < this.allLitigationStorageAddresses.length; index++) {
      const element = this.allLitigationStorageAddresses[index];
      if (element.Address === address) {
        this.litigationStorageAddress = element;
        return;
      }
    }
    alert('Error finding smart contract. Please reload the page to avoid issues paying out.');
  }

  checkFinishedLoading() {
    // tslint:disable-next-line:max-line-length
    if (this.holdingAddress && this.recentGasPrices && this.litigationStorageAddress && this.holdingStorageAddress && this.requiredChainID) {
      this.isLoading = false;
    }
  }

  onGasPriceKeyUp(event: KeyboardEvent) {
    this.gasPrice = parseFloat((event.target as HTMLInputElement).value);
  }

  onGasPriceKeyDown(event): boolean {
    const charCode = (event.which) ? event.which : event.keyCode;

    if (charCode == 46)
    return true;

    if (charCode > 31 && (charCode < 48 || charCode > 57)) {
      return false;
    }
    return true;

  }

  getHoldingAddresses() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    let url = this.httpService.ApiUrl + '/api/contracts/getholdingaddresses?blockchainID=' + this.blockchainID;
    url += '&' + (new Date()).getTime();
    return this.http.get<ContractAddress[]>(url, { headers });
  }

  getHoldingStorageAddresses() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    let url = this.httpService.ApiUrl + '/api/contracts/getholdingstorageaddresses?blockchainID=' + this.blockchainID;
    url += '&' + (new Date()).getTime();
    return this.http.get<ContractAddress[]>(url, { headers });
  }

  getLitigationStorageAddresses() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    let url = this.httpService.ApiUrl + '/api/contracts/getlitigationstorageaddresses?blockchainID=' + this.blockchainID;
    url += '&' + (new Date()).getTime();
    return this.http.get<ContractAddress[]>(url, { headers });
  }

  getRecentPayoutGasPrices() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    let url = this.httpService.ApiUrl + '/api/nodes/dataholders/getrecentpayoutgasprices';
    url += '?' + (new Date()).getTime();
    return this.http.get<RecentPayoutGasPrice[]>(url, { headers });
  }

  getNetworkID() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    let url = this.httpService.ApiUrl + '/api/blockchain/networkid?blockchainID=' + this.blockchainID;
    url += '&' + (new Date()).getTime();
    return this.http.get<number>(url, { headers });
  }

  getContractAbi(type: number) {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    let url = this.httpService.ApiUrl + '/api/contracts/GetLatestABIForContract?blockchainID=' + this.blockchainID + '&contractType=' + type;
    url += '&' + (new Date()).getTime();
    return this.http.get<string>(url, { headers });
  }

  canTryPayout() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    // tslint:disable-next-line:max-line-length
    let url = this.httpService.ApiUrl + '/api/nodes/dataholder/cantrypayout?nodeID=' + this.nodeID + '&offerId=' + this.offerId + '&holdingAddress=' + this.holdingAddress.Address + '&holdingStorageAddress=' + this.holdingStorageAddress.Address + '&litigationStorageAddress=' + this.litigationStorageAddress.Address + '&identity=' + this.identity + '&blockchainID=' + this.blockchainID + '&selectedAddress=' + this.selectedAddress;
    url += '&' + (new Date()).getTime();
    return this.http.get<BeforePayoutResult>(url, { headers });
  }

  loadAccount(accounts) {
    if (accounts == null || accounts.length === 0) {
      this.selectedAddress = null;
      this.isMetaMaskUnlocked = false;
      this.dect.detectChanges();
      return;
    }
    this.selectedAddress = accounts[0];
    if (this.selectedAddress) {
      this.isMetaMaskUnlocked = true;
    }
    this.dect.detectChanges();
  }

  async enableMetaMask() {

    this.selectedChainID = ethereum.networkVersion;


    ethereum
    .request({
      method: 'eth_requestAccounts',
    })
    .then((result) => {
      this.loadAccount(result);
      this.dect.detectChanges();

      this.canTryPayout().subscribe(data => {
        this.canPayoutResult = data;
      });
    })
    .catch((error) => {
      alert(error);
    });
  }

//   checkManagementWallet() {
//     debugger;

//     const provider = web3.currentProvider;

//     {
//       const web3 = new Web3(provider);
//       const erc = new web3.eth.Contract(JSON.parse(this.erc725Abi),this.selectedAddress);
//       const keccakContract = new web3.eth.Contract(JSON.parse(this.keccakAbi),'0x7e1bbcd25507a6fcb6503a5be75795848dca32b7');
//       let builder = keccakContract.methods.keccakAddress(this.selectedAddress);
//       builder.call((err, result) => {
// debugger;
//       });
      
//       // .then((response) => {
//       //   const responseArray = Object.values(response);
//       //   const wallet = responseArray[0];
//       //   erc.methods.getKeysByPurpose(1).then((result) => {
//       //     const keys = result.keys;
//       //     const isValid = keys.includes(wallet);
//       //     if (!isValid) {
//       //       // this.$message.error({
//       //       //   message: 'This wallet is not registered as a management wallet, please change it in MetaMask!',
//       //       //   duration: 7000,
//       //       // });
//       //     }
//       //   });
//       // });
//     }


//   }

  getTransactionUrl() {
    return this.canPayoutResult.BlockchainExplorerUrlFormat.replace('{0}', this.sentTransactionHash);
  }

  sendTransaction() {
    this.sendError = null;

    this.selectedChainID = ethereum.networkVersion;

    if (this.selectedChainID != this.requiredChainID) {
      this.sendError = 'You have the wrong blockchain network selected in MetaMask. You need to have ' + this.requiredChainID + ' selected, you currently have ' + this.selectedChainID + ' selected. Look on this website to see what network the numbers belong to https://chainid.network/.';
      return;
    }

    if (this.nodeID == null) {
      this.sendError = 'The transaction was not sent. There appears to be a problem loading the node ID for this node.';
      return;
    }
    // if (this.managementWallet == null) {
    //   this.sendError = 'The transaction was not sent. There appears to be a problem loading the management wallet for this node.';
    //   return;
    // }

    const provider = web3.currentProvider;
    if (provider == null) {
      this.sendError = 'The transaction was not sent. There appears to be a problem interacting with MetaMask.';
      return;
    }

    const providerAddress = provider.selectedAddress;
    if (providerAddress == null) {
      this.sendError = 'The transaction was not sent. There appears to be a problem interacting with MetaMask (address related).';
      return;
    }

    // if (providerAddress !== this.managementWallet) {
    //   this.sendError = 'The transaction was not sent. The address loaded in MetaMask is not the correct management wallet.';
    //   return;
    // }

    if (this.holdingAddress == null || this.holdingAddress.Address == null) {
      this.sendError = 'The transaction was not sent. The smart contract address for the payout was not loaded properly.';
      return;
    }

    if (this.gasPrice == null || this.gasPrice <= 0 || isNaN(this.gasPrice)) {
      this.sendError = 'The transaction was not sent. The gas price has not been set properly.';
      return;
    }

    // if (this.gasPrice > 20) {
    //   this.sendError = 'The transaction was not sent. Please lower your gas price!';
    //   return;
    // }

    this.isBusySending = true;

    this.dect.detectChanges();

    this.canTryPayout().subscribe(data => {
      this.canPayoutResult = data;
    
      if (this.canPayoutResult.CanTryPayout) {
    
        try {
        
           const web3 = new Web3(provider);
          const abi = `[
            {
                "constant": false,
                "inputs": [
                    {
                        "name": "identity",
                        "type": "address"
                    },
                    {
                        "name": "offerId",
                        "type": "uint256"
                    }
                ],
                "name": "payOut",
                "outputs": [],
                "payable": false,
                "stateMutability": "nonpayable",
                "type": "function"
            }
            ]`;
    
           const contractInfo = JSON.parse(abi);
    
    
    
    
          const contractInstance = new web3.eth.Contract(contractInfo, this.holdingAddress.Address);
    
          const self = this;
    
          // Call a function of the contract:
          let builder = contractInstance.methods.payOut(this.identity, this.offerId);
          let response = builder.send({
            from: providerAddress, value: 0, gas: 300000,
            to: this.holdingAddress.Address,
            gasPrice: web3.utils.toWei(this.gasPrice.toString(), 'gwei')
          },
            (err, res) => {
              this.isBusySending = false;
              if (err) {
                self.sendError = err.message;
              } else if (res) {
                self.sentTransactionHash = res;
                self.hasSentTransaction = true;
              }
              self.dect.detectChanges();
            });
        } catch (error) {
          this.isBusySending = false;
          this.sendError = error.message;
        }
      } else {
        this.isBusySending = false;
      }
    }, err => {
      this.isBusySending = false;
      this.sendError = err.message;
    });
  }
}
