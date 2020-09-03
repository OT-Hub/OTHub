import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { DataHoldersComponent } from './dataholders.component';

describe('DataHoldersComponent', () => {
  let component: DataHoldersComponent;
  let fixture: ComponentFixture<DataHoldersComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [DataHoldersComponent]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(DataHoldersComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
