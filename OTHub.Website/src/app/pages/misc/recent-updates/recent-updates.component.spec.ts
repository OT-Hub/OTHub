import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RecentUpdatesComponent } from './recent-updates.component';

describe('RecentUpdatesComponent', () => {
  let component: RecentUpdatesComponent;
  let fixture: ComponentFixture<RecentUpdatesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ RecentUpdatesComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(RecentUpdatesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
