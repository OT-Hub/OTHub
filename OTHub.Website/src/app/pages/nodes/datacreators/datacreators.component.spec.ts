import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { DatacreatorsComponent } from './datacreators.component';

describe('DatacreatorsComponent', () => {
  let component: DatacreatorsComponent;
  let fixture: ComponentFixture<DatacreatorsComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ DatacreatorsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(DatacreatorsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
