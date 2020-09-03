import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { DataHolderComponent } from './dataholder.component';

describe('NodeProfileComponent', () => {
  let component: DataHolderComponent;
  let fixture: ComponentFixture<DataHolderComponent>;

  beforeEach(async(() => {
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
