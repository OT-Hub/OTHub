import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MynodesoverviewComponent } from './mynodesoverview.component';

describe('MynodesoverviewComponent', () => {
  let component: MynodesoverviewComponent;
  let fixture: ComponentFixture<MynodesoverviewComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ MynodesoverviewComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(MynodesoverviewComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
