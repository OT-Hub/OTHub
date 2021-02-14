import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { SmartcontractsComponent } from './smartcontracts.component';

describe('SmartcontractsComponent', () => {
  let component: SmartcontractsComponent;
  let fixture: ComponentFixture<SmartcontractsComponent>;

  beforeEach(async(() => {
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
