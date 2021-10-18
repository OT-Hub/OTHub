import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MyNodesPayoutsComponent } from './my-nodes-payouts.component';

describe('MyNodesPayoutsComponent', () => {
  let component: MyNodesPayoutsComponent;
  let fixture: ComponentFixture<MyNodesPayoutsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ MyNodesPayoutsComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(MyNodesPayoutsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
