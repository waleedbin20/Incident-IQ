import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SwarmMapComponent } from './swarm-map.component';

describe('SwarmMapComponent', () => {
  let component: SwarmMapComponent;
  let fixture: ComponentFixture<SwarmMapComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SwarmMapComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SwarmMapComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
