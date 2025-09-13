import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SceneSplitViewComponent } from './scene-split-view.component';

describe('SceneSplitViewComponent', () => {
  let component: SceneSplitViewComponent;
  let fixture: ComponentFixture<SceneSplitViewComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [SceneSplitViewComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SceneSplitViewComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
