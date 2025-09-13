import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Store } from '@ngrx/store';
import { of } from 'rxjs';
import { SceneSplitImageDropDownComponent } from './scene-split-image-drop-down.component';

describe('BookTableComponent', () => {
  let component: SceneSplitImageDropDownComponent;
  let fixture: ComponentFixture<SceneSplitImageDropDownComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [SceneSplitImageDropDownComponent],
      providers: [
        { provide: Store, useValue: { select: () => of([]), dispatch: jasmine.createSpy('dispatch') } },
        {

        },
      ]
    })
      .compileComponents();

    fixture = TestBed.createComponent(SceneSplitImageDropDownComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
