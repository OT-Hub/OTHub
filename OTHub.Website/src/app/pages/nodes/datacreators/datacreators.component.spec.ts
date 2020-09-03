import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { DatacreatorsComponent } from './datacreators.component';

describe('DatacreatorsComponent', () => {
  let component: DatacreatorsComponent;
  let fixture: ComponentFixture<DatacreatorsComponent>;

  beforeEach(async(() => {
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
