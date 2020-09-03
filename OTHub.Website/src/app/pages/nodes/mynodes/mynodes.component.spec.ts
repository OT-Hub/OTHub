import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { MynodesComponent } from './mynodes.component';

describe('MynodesComponent', () => {
  let component: MynodesComponent;
  let fixture: ComponentFixture<MynodesComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ MynodesComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(MynodesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
