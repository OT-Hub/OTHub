import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { FindbymanagementwalletComponent } from './findbymanagementwallet.component';

describe('FindbymanagementwalletComponent', () => {
  let component: FindbymanagementwalletComponent;
  let fixture: ComponentFixture<FindbymanagementwalletComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ FindbymanagementwalletComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(FindbymanagementwalletComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
