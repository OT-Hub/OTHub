import { ComponentFixture, TestBed } from '@angular/core/testing';

import { JobHeatmapComponent } from './job-heatmap.component';

describe('JobHeatmapComponent', () => {
  let component: JobHeatmapComponent;
  let fixture: ComponentFixture<JobHeatmapComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ JobHeatmapComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(JobHeatmapComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
