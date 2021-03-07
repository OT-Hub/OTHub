import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { SmartcontractsComponent } from './smartcontracts.component';

describe('SmartcontractsComponent', () => {
  let component: SmartcontractsComponent;
  let fixture: ComponentFixture<SmartcontractsComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ SmartcontractsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(SmartcontractsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
