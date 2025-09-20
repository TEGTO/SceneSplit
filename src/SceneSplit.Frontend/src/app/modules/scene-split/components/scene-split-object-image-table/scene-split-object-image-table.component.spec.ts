import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { Store } from '@ngrx/store';
import { MockStore, provideMockStore } from '@ngrx/store/testing';
import { of } from 'rxjs';
import { getObjectImages, selectObjectImages } from '../..';
import { SceneSplitObjectImageTableComponent } from './scene-split-object-image-table.component';

describe('SceneSplitObjectImageTableComponent', () => {
  let component: SceneSplitObjectImageTableComponent;
  let fixture: ComponentFixture<SceneSplitObjectImageTableComponent>;
  let store: MockStore;

  const initialState = {
    objectImages: []
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [SceneSplitObjectImageTableComponent],
      providers: [provideMockStore({ initialState })]
    }).compileComponents();
  });

  beforeEach(() => {
    store = TestBed.inject(Store) as MockStore;
    spyOn(store, 'select').and.returnValue(of([]));
    spyOn(store, 'dispatch');

    fixture = TestBed.createComponent(SceneSplitObjectImageTableComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should call store.select and store.dispatch on ngOnInit', () => {
    component.ngOnInit();
    expect(store.select).toHaveBeenCalledWith(selectObjectImages);
    expect(store.dispatch).toHaveBeenCalledWith(getObjectImages());
  });

  it('should render "no images" message if images$ is empty', () => {
    component.images$ = of([]);
    fixture.detectChanges();

    const noImagesText = fixture.nativeElement.querySelector('p.text-lg');
    expect(noImagesText).toBeTruthy();
    expect(noImagesText.textContent).toContain(
      'Object images from the scene will appear here'
    );
  });

  it('should render images if images$ has data', () => {
    const mockImages = [
      { imageUrl: 'http://example.com/img1.png', description: 'Image 1' },
      { imageUrl: 'http://example.com/img2.png', description: 'Image 2' }
    ];

    component.images$ = of(mockImages);
    fixture.detectChanges();

    const imageElements = fixture.debugElement.queryAll(By.css('.row__image img'));
    expect(imageElements.length).toBe(2);
    expect(imageElements[0].nativeElement.src).toBe('http://example.com/img1.png');
    expect(imageElements[0].nativeElement.alt).toBe('Image 1');
    expect(imageElements[1].nativeElement.src).toBe('http://example.com/img2.png');
    expect(imageElements[1].nativeElement.alt).toBe('Image 2');

    const descriptionElements = fixture.debugElement.queryAll(By.css('.row__image + p'));
    expect(descriptionElements[0].nativeElement.textContent).toContain('Image 1');
    expect(descriptionElements[1].nativeElement.textContent).toContain('Image 2');
  });
});