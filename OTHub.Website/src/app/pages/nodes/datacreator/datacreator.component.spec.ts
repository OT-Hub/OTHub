import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { DatacreatorComponent } from './datacreator.component';

describe('DatacreatorComponent', () => {
  let component: DatacreatorComponent;
  let fixture: ComponentFixture<DatacreatorComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ DatacreatorComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(DatacreatorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
