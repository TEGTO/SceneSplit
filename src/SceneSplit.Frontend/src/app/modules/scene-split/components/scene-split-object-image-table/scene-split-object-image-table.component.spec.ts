import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SceneSplitObjectImageTableComponent } from './scene-split-object-image-table.component';

describe('SceneSplitObjectImageTableComponent', () => {
  let component: SceneSplitObjectImageTableComponent;
  let fixture: ComponentFixture<SceneSplitObjectImageTableComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [SceneSplitObjectImageTableComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SceneSplitObjectImageTableComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
