import { Component, OnInit, InjectionToken, Inject, ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
declare const $: any;
declare const ethereum: any;
declare const web3: any;
import Web3 from 'web3';
import { HubHttpService } from '../../hub-http-service';
@Component({
  selector: 'ngx-unstakeboarding',
  templateUrl: './unstakeboarding.component.html',
  styleUrls: ['./unstakeboarding.component.scss']
})
export class UnstakeboardingComponent implements OnInit {
  gasPrice: number;
  selectedAddress: string;
  isMetaMaskUnlocked: boolean;

  constructor(private http: HttpClient, private route: ActivatedRoute, private router: Router, private dect: ChangeDetectorRef,
    private httpService: HubHttpService) { }

  ngOnInit(): void {
    this.isMetaMaskUnlocked = false;

    const self = this;
    ethereum.on('accountsChanged', function (accounts) {
      self.loadAccount(accounts);
    });
  }

  async enableMetaMask() {

    //this.selectedChainID = ethereum.networkVersion;


    ethereum
    .request({
      method: 'eth_requestAccounts',
    })
    .then((result) => {
      this.loadAccount(result);
      this.dect.detectChanges();
    })
    .catch((error) => {
      alert(error);
    });
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

  sendTransaction() {
    const provider = web3.currentProvider;
    if (provider == null) {
      return;
    }

    const providerAddress = provider.selectedAddress;
    if (providerAddress == null) {
      return;
    }

    const web33 = new Web3(provider);
    const abi = `[
      {
        "inputs": [],
        "name": "withdrawTokens",
        "outputs": [],
        "stateMutability": "nonpayable",
        "type": "function"
      }
    ]`;

    const contractInfo = JSON.parse(abi);
    
    
    
    
    const contractInstance = new web33.eth.Contract(contractInfo, '0x0585a5e5aa9bf79e1788749d6b7ed08f87148122');

    const self = this;

    // Call a function of the contract:
    let builder = contractInstance.methods.withdrawTokens();
    let response = builder.send({
      from: providerAddress, value: 0, gas: 300000,
      to: '0x0585a5e5aa9bf79e1788749d6b7ed08f87148122',
      gasPrice: web33.utils.toWei(this.gasPrice.toString(), 'gwei')
    },
      (err, res) => {
       
        if (err) {
          alert(err.message);
        } else if (res) {
          // self.sentTransactionHash = res;
          // self.hasSentTransaction = true;
        }
        self.dect.detectChanges();
      });
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

}
