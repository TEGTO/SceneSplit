import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SceneSplitViewComponent } from './scene-split-view.component';

@Component({ selector: 'app-book-table', template: '' })
class MockBookTableComponent { }

@Component({ selector: 'app-scene-split-object-image-table', template: '' })
class MockSceneSplitObjectImageTableComponent { }

describe('SceneSplitViewComponent', () => {
  let component: SceneSplitViewComponent;
  let fixture: ComponentFixture<SceneSplitViewComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [
        SceneSplitViewComponent,
        MockBookTableComponent,
        MockSceneSplitObjectImageTableComponent
      ]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(SceneSplitViewComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render the wrapper div', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.wrapper')).toBeTruthy();
  });

  it('should contain app-book-table component', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('app-book-table')).toBeTruthy();
  });

  it('should contain app-scene-split-object-image-table component', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('app-scene-split-object-image-table')).toBeTruthy();
  });
});
