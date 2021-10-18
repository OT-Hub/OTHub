import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BlockchainBreakdownComponent } from './blockchain-breakdown.component';

describe('BlockchainBreakdownComponent', () => {
  let component: BlockchainBreakdownComponent;
  let fixture: ComponentFixture<BlockchainBreakdownComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BlockchainBreakdownComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(BlockchainBreakdownComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
