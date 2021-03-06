import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { DataHolderComponent } from './dataholder.component';

describe('NodeProfileComponent', () => {
  let component: DataHolderComponent;
  let fixture: ComponentFixture<DataHolderComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [DataHolderComponent]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(DataHolderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
