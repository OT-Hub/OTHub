import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { HubHttpService } from 'app/pages/hub-http-service';
import { MyNodeService } from 'app/pages/nodes/mynodeservice';
import { SystemStatusModel } from '../status/system-models';
import { SmartContractGroupModel } from './smartcontracts-model';

@Component({
  selector: 'ngx-smartcontracts',
  templateUrl: './smartcontracts.component.html',
  styleUrls: ['./smartcontracts.component.scss']
})
export class SmartcontractsComponent implements OnInit {

  constructor(private http: HttpClient, private route: ActivatedRoute, private router: Router,
    public myNodeService: MyNodeService, private httpService: HubHttpService) {
    this.isLoading = true;
    this.failedLoading = false;
    this.IsTestNet = httpService.IsTestNet;
    this.map[0] = 'Approval';
    this.map[1] = 'Profile';
    this.map[2] = 'ReadingStorage';
    this.map[3] = 'Reading';
    this.map[4] = 'Token';
    this.map[5] = 'HoldingStorage';
    this.map[6] = 'Holding';
    this.map[7] = 'ProfileStorage';
    this.map[8] = 'Litigation';
    this.map[9] = 'LitigationStorage';
    this.map[10] = 'Replacement';
    this.map[11] = 'ERC725';
    this.map[12] = 'Hub';
  }

  map: { [type: number]: string; } = {};
  failedLoading: boolean;
  IsTestNet: boolean;
  isLoading: boolean;
  isDarkTheme: boolean;
  getDataObservable: any;
  Data: SmartContractGroupModel[];

  ngOnInit() {
    this.getDataObservable = this.getData().subscribe(data => {
      const endTime = new Date();
      this.Data = data;
      this.failedLoading = false;
      this.isLoading = false;
    }, err => {
      this.failedLoading = true;
      this.isLoading = false;
    });
  }

  ngOnDestroy() {
    this.getDataObservable.unsubscribe();
  }

  getTypeName(type: number) {
    return this.map[type];
  }

  getData() {
    const headers = new HttpHeaders()
      .set('Content-Type', 'application/json')
      .set('Accept', 'application/json');
    const url = this.httpService.ApiUrl + '/api/contracts/GetAllContracts';
    return this.http.get<SmartContractGroupModel[]>(url, { headers });
  }

}
