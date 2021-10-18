import { HttpClient, HttpHeaders } from '@angular/common/http';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { AuthService } from '@auth0/auth0-angular';
import { HubHttpService } from 'app/pages/hub-http-service';
import { Blockchain } from 'app/pages/tools/find-nodes-by-wallet/find-nodes-by-wallet.component';
// declare const $: any;
// declare const ethereum: any;

 import Web3 from 'web3';
import { BeforePayoutResult, ContractAddress } from '../manual-payout-page/manual-payout-models';
declare let window: any;
declare const web3: any;
@Component({
  selector: 'ngx-my-nodes-payouts',
  templateUrl: './my-nodes-payouts.component.html',
  styleUrls: ['./my-nodes-payouts.component.scss']
})
export class MyNodesPayoutsComponent implements OnInit {
  payouts: PossiblePayoutVM[];
  blockchains: Blockchain[];
  selectedBlockchain: string;
  selectedPayouts: PossiblePayoutVM[];
  selectedChainID: number;
  selectedAddress: string;
  isMetaMaskUnlocked: boolean;
  isMetaMaskInstalled: boolean;
  gasPrice: number;
  isBusySending: boolean;
  sendError: string;
  hasSentTransaction: boolean;
  requiredChainID: any;
  holdingAddress: ContractAddress;
  holdingStorageAddress: ContractAddress;
  litigationStorageAddress: ContractAddress;
  isValidating: boolean;
  isValidated: boolean;
  validationText: string;

  constructor(private httpService: HubHttpService,
    private auth: AuthService, private http: HttpClient, private dect: ChangeDetectorRef) {
    this.isLoggedIn = false;
    this.isLoading = true;
    this.includeActiveJobs = true;
    this.includeCompletedJobs = true;
    this.blockchainID = null;
    this.dateFrom = null;
    this.dateTo = null;
    this.hasSentTransaction = false;
    this.selectedBlockchain = 'xDai';
    this.selectedPayouts = [];
    this.isBusySending = false;
    this.isValidating = false;
    this.isValidated = false;
  }

  canTryPayout(nodeID: string, offerId: string, identity: string) {

    let blockchainID = null;

    if (this.selectedBlockchain != 'All Blockchains') {
      blockchainID = this.blockchains.find(b => b.BlockchainName == this.selectedBlockchain).ID;
    }

    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    // tslint:disable-next-line:max-line-length
    let url = this.httpService.ApiUrl + '/api/nodes/dataholder/cantrypayout?nodeID=' + nodeID + '&offerId=' + 
    offerId + '&holdingAddress=' + this.holdingAddress.Address + '&holdingStorageAddress=' + 
    this.holdingStorageAddress.Address + '&litigationStorageAddress=' + this.litigationStorageAddress.Address + 
    '&identity=' + identity + '&blockchainID=' + blockchainID + '&selectedAddress=' + this.selectedAddress;
    url += '&' + (new Date()).getTime();
    return this.http.get<BeforePayoutResult>(url, { headers });
  }

  validateSelection() {
    this.isValidating = true;
    this.isValidated = false;

    let counter = 0;
    let pass = 0;
    let fail = 0;

    for (let index = 0; index < this.selectedPayouts.length; index++) {
      const payout = this.selectedPayouts[index];
      counter++;
      
      this.canTryPayout(payout.NodeID, payout.OfferID, payout.Identity).subscribe(data => {
        payout.ValidationResult = data.CanTryPayout;
        counter--;
        pass++;
      }, err => {
        payout.ValidationResult = false;
        counter--;
        fail++;
      });
    }

    let timer = setInterval(() => {
      if (counter == 0) {
      this.isValidating = false;
      this.isValidated = true;
      this.validationText = pass + ' jobs passed validation. ' + fail + ' jobs failed validation.';
      clearInterval(timer);
      }
    }, 200)
  }

  async sendTransaction() {


    this.selectedChainID = window.ethereum.networkVersion;

    if (this.selectedChainID != this.requiredChainID) {
      this.sendError = 'You have the wrong blockchain network selected in MetaMask. You need to have ' + this.requiredChainID + ' selected, you currently have ' + this.selectedChainID + ' selected. Look on this website to see what network the numbers belong to https://chainid.network/.';
      return;
    }

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


    if (this.holdingAddress == null || this.holdingAddress.Address == null) {
      this.sendError = 'The transaction was not sent. The smart contract address for the payout was not loaded properly.';
      return;
    }

    if (this.gasPrice == null || this.gasPrice <= 0 || isNaN(this.gasPrice)) {
      this.sendError = 'The transaction was not sent. The gas price has not been set properly.';
      return;
    }

    this.isBusySending = true;

    this.dect.detectChanges();

    //const web3Impl = new Web3(provider);
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

      const web3Impl = new Web3(provider);
      const contractInfo = JSON.parse(abi);
    const contractInstance = new web3Impl.eth.Contract(contractInfo, this.holdingAddress.Address);
    const self = this;


    for (let index = 0; index < this.selectedPayouts.length; index++) {
      const payout = this.selectedPayouts[index];

      if (payout.ValidationResult == true) {
        let builder = contractInstance.methods.payOut(payout.Identity, payout.OfferID);
        let response = builder.send({
          from: providerAddress, value: 0, gas: 300000,
          to: this.holdingAddress.Address,
          gasPrice: web3Impl.utils.toWei(this.gasPrice.toString(), 'gwei')
        },
          (err, res) => {
            this.isBusySending = false;
            if (err) {
              self.sendError = err.message;
            } else if (res) {
              //self.sentTransactionHash = res;
              self.hasSentTransaction = true;
            }
            self.dect.detectChanges();
          });
      }
      }
  }

  getHoldingAddresses() {
    let blockchainID = null;

    if (this.selectedBlockchain != 'All Blockchains') {
      blockchainID = this.blockchains.find(b => b.BlockchainName == this.selectedBlockchain).ID;
    }
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    let url = this.httpService.ApiUrl + '/api/contracts/getholdingaddresses?blockchainID=' + blockchainID;
    url += '&' + (new Date()).getTime();
    return this.http.get<ContractAddress[]>(url, { headers });
  }

  getHoldingStorageAddresses() {

    let blockchainID = null;

    if (this.selectedBlockchain != 'All Blockchains') {
      blockchainID = this.blockchains.find(b => b.BlockchainName == this.selectedBlockchain).ID;
    }

    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    let url = this.httpService.ApiUrl + '/api/contracts/getholdingstorageaddresses?blockchainID=' + blockchainID;
    url += '&' + (new Date()).getTime();
    return this.http.get<ContractAddress[]>(url, { headers });
  }

  getLitigationStorageAddresses() {

    let blockchainID = null;

    if (this.selectedBlockchain != 'All Blockchains') {
      blockchainID = this.blockchains.find(b => b.BlockchainName == this.selectedBlockchain).ID;
    }

    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    let url = this.httpService.ApiUrl + '/api/contracts/getlitigationstorageaddresses?blockchainID=' + blockchainID;
    url += '&' + (new Date()).getTime();
    return this.http.get<ContractAddress[]>(url, { headers });
  }


  toggleCheckbox(payout: PossiblePayoutVM, checked: boolean) {

    const index = this.selectedPayouts.indexOf(payout, 0);
    if (index > -1) {
      this.selectedPayouts.splice(index, 1);
    }

    if (checked) {
      this.selectedPayouts.push(payout);
    }
  }

  tickAll(checked: boolean) {
    while (this.selectedPayouts.length > 0) {
      this.selectedPayouts.pop();
    }

    this.payouts.forEach(p => {
      p.Checked = checked;

      if (checked) {
        this.selectedPayouts.push(p);
      }
    });
  }

  toggleActiveJobs(checked: boolean) {
    this.includeActiveJobs = checked;

    this.loadData();
  }

  toggleCompletedJobs(checked: boolean) {
    this.includeCompletedJobs = checked;

    this.loadData();
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
    
    this.selectedChainID = window.ethereum.networkVersion;

    window.ethereum
    .request({
      method: 'eth_requestAccounts',
    })
    .then((result) => {
      this.loadAccount(result);
      this.dect.detectChanges();

      // this.canTryPayout().subscribe(data => {
      //   this.canPayoutResult = data;
      // });
    })
    .catch((error) => {
      alert(error);
    });
  }

  getBlockchains() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    let url = this.httpService.ApiUrl + '/api/blockchain/GetBlockchains';

    return this.http.get<Blockchain[]>(url, { headers });
  }

  isLoading: boolean
  isLoggedIn: boolean;

  includeActiveJobs: boolean;
  includeCompletedJobs: boolean;
  blockchainID: number;
  dateFrom: Date;
  dateTo: Date;

  ngOnInit(): void {
    this.auth.user$.subscribe(usr => {
      if (usr != null) {
        if (!this.isLoading) {
          return;
        }

        this.isLoading = false;
        this.isLoggedIn = true;


          this.getBlockchains().subscribe(data => {
            this.blockchains = data;

            this.getHoldingAddresses().subscribe(data => {
              this.holdingAddress = data.find(d => d.IsLatest == true);

              this.getLitigationStorageAddresses().subscribe(data => {
                this.litigationStorageAddress = data.find(d => d.IsLatest == true);
              });

              this.getHoldingStorageAddresses().subscribe(data => {
                this.holdingStorageAddress = data.find(d => d.IsLatest == true);
              });
  
            this.getNetworkID().subscribe(data => {
              this.requiredChainID = data.toString();
            });
          });
  
            this.loadData();
          });
      }
    });

    
    const self = this;

    try {
      if (window.ethereum == null) {
        this.isMetaMaskInstalled = false;
      } else {
        this.isMetaMaskInstalled = true;

        //tslint:disable-next-line:only-arrow-functions
        window.ethereum.on('accountsChanged', function (accounts) {
          self.loadAccount(accounts);
        });

        window.ethereum.on('chainChanged', function (chainID: string) {
          setTimeout(function(){ 
            self.selectedChainID = window.ethereum.networkVersion;
          }, -1);
      
        });

        this.isMetaMaskUnlocked = false;
      }
    } catch (error) {
      this.isMetaMaskInstalled = false;
    }
  }

  getNetworkID() {
    let blockchainID = null;

    if (this.selectedBlockchain != 'All Blockchains') {
      blockchainID = this.blockchains.find(b => b.BlockchainName == this.selectedBlockchain).ID;
    }
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    let url = this.httpService.ApiUrl + '/api/blockchain/networkid?blockchainID=' + blockchainID;
    url += '&' + (new Date()).getTime();
    return this.http.get<number>(url, { headers });
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

  changeBlockchain(blockchain: string) {
    this.selectedBlockchain = blockchain;
    this.getHoldingAddresses().subscribe(data => {
      this.holdingAddress = data.find(d => d.IsLatest == true);
    });
    this.getLitigationStorageAddresses().subscribe(data => {
      this.litigationStorageAddress = data.find(d => d.IsLatest == true);
    });

    this.getHoldingStorageAddresses().subscribe(data => {
      this.holdingStorageAddress = data.find(d => d.IsLatest == true);
    });
    this.getNetworkID().subscribe(data => {
      this.requiredChainID = data.toString();
    });
    this.loadData();
  }

  loadData() {

    if (this.selectedBlockchain == '')
    return;

    let blockchainID = null;

    if (this.selectedBlockchain != 'All Blockchains') {
      blockchainID = this.blockchains.find(b => b.BlockchainName == this.selectedBlockchain).ID;
    }

    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    let url = this.httpService.ApiUrl + '/api/mynodes/PossibleJobPayouts?includeActiveJobs=' + this.includeActiveJobs + '&includeCompletedJobs=' + this.includeCompletedJobs
      + '&blockchainID=' + blockchainID + '&dateFrom=' + this.dateFrom + '&dateTo=' + this.dateTo;
    this.http.get<PossiblePayoutModel[]>(url, { headers }).subscribe(data => {

      let vms: PossiblePayoutVM[] = [];

      for (let index = 0; index < data.length; index++) {
        const element = data[index];

        let vm = new PossiblePayoutVM();
        vm.BlockchainID = element.BlockchainID;
        vm.BlockchainName = element.BlockchainName;
        vm.OfferID = element.OfferID;
        vm.NodeID = element.NodeID;
        vm.Identity = element.Identity;
        vm.TokenAmount = element.TokenAmount;
        vm.PaidAmount = element.PaidAmount;
        vm.LastPayout = element.LastPayout;
        vm.JobEndTimestamp = element.JobEndTimestamp;
        vm.Checked = false;
        vm.EstimatedPayout = element.EstimatedPayout;
        vms.push(vm);
      }

      this.payouts = data;
    });
  }
}

export interface PossiblePayoutModel {
  BlockchainID: number;
  BlockchainName: string;
  OfferID: string;
  NodeID: string;
  Identity: string;
  TokenAmount: number;
  PaidAmount: number;
  LastPayout: Date;
  JobEndTimestamp: Date
  Checked: boolean;
  EstimatedPayout: number;
  ValidationResult: boolean;
}

export class PossiblePayoutVM {
  BlockchainID: number;
  BlockchainName: string;
  OfferID: string;
  NodeID: string;
  Identity: string;
  TokenAmount: number;
  PaidAmount: number;
  LastPayout: Date;
  JobEndTimestamp: Date;
  Checked: boolean;
  EstimatedPayout: number;
  ValidationResult: boolean;
}
