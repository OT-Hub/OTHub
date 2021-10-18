import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { AuthService } from '@auth0/auth0-angular';
import { NbComponentStatus, NbToastrConfig, NbToastrService } from '@nebular/theme';
import { HubHttpService } from 'app/pages/hub-http-service';
import { MomentModule } from 'ngx-moment';
@Component({
  selector: 'ngx-find-nodes-by-wallet',
  templateUrl: './find-nodes-by-wallet.component.html',
  styleUrls: ['./find-nodes-by-wallet.component.scss']
})
export class FindNodesByWalletComponent implements OnInit {
  jobs: Job[];
  blockchains: Blockchain[];
  selectedBlockchain: String;
  isLoggedIn: boolean;
  isLoading: boolean
  constructor(private toastrService: NbToastrService, private http: HttpClient, private httpService: HubHttpService, private auth: AuthService) {
    this.isSearching = false;
    this.isLoggedIn = false;
    this.isLoading = true;
  }

  address: string;
  isSearching: boolean;

  ngOnInit(): void {
    this.auth.user$.subscribe(usr => {
      if (usr != null && this.isLoading == true) {
        this.isLoggedIn = true;
        this.isLoading = false;
        this.getJobs().subscribe(data => {
          this.jobs = data;
        });
        this.getBlockchains().subscribe(data => {
          this.blockchains = data;
          this.selectedBlockchain = data[0].ID.toString();
        
        });
      }
    });
  }

  onselectedBlockchainChange($event) {

  }

  config: NbToastrConfig;

  refresh() {
    this.getJobs().subscribe(data => {
      this.jobs = data;
    });
  }

  getBlockchains() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    let url = this.httpService.ApiUrl + '/api/blockchain/GetBlockchains';

    return this.http.get<Blockchain[]>(url, { headers });
  }

  getJobs() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    let url = this.httpService.ApiUrl + '/api/tools/GetFindNodesByWalletJobs';

    return this.http.get<Job[]>(url, { headers });
  }


  search() {

    this.isSearching = false;

    if (this.address == null) return;
    if (this.address.length != 42) {
      this.config = new NbToastrConfig({ duration: 8000 });
      this.config.status = "warning";
      this.config.icon = 'alert-triangle';
      this.toastrService.show(
        'The address should be 42 characters long. Please check you have entered the correct information.', 'Search', this.config);
      return;
    }
    if (!this.address.startsWith('0x')) {
      this.config = new NbToastrConfig({ duration: 8000 });
      this.config.status = "warning";
      this.config.icon = 'alert-triangle';
      this.toastrService.show(
        'The address entered is not valid. Please check you have entered the correct information.', 'Search', this.config);
      return;
    }



    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');

    let url = this.httpService.ApiUrl + '/api/tools/FindNodesByWallet?blockchainID=' + this.selectedBlockchain + '&address=' + this.address;

    this.http.post<JobResult>(url, { headers }).subscribe(data => {

      if (data.IsError) {
        this.isSearching = false;

        this.config = new NbToastrConfig({ duration: 8000 });
        this.config.status = "warning";
        this.config.icon = 'alert-triangle';
        this.toastrService.show(
          data.Message, 'Search', this.config);

      } else {
        this.isSearching = true;
        this.getJobs().subscribe(data => {
          this.jobs = data;
        });
      }
    }, err => {
      this.config = new NbToastrConfig({ duration: 8000 });
      this.config.status = "warning";
      this.config.icon = 'alert-triangle';
      this.toastrService.show(
        'There was an error starting the search.', 'Search', this.config);
    });
  }

  showResults(job: Job) {
    job.ShowResults = true;
  }

}

export class JobResult {
  IsError: boolean;
  Message: string;
}

export class Job {
  BlockchainName: string;
  Address: string;
  StartDate: Date;
  EndDate: Date;
  Progress: Number;
  ShowResults: Boolean;
  Identities: MatchResult[];
  Failed: Boolean;
}

export class MatchResult {
  Identity: string;
  NodeID: string;
  DisplayName: string;
  Tokens: string;
}

export class Blockchain {
  ID: number;
  BlockchainName: string;
}