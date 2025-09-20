import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { Store } from '@ngrx/store';
import { sendSceneImageFile } from '../..';
import { ConfigService } from '../../../shared';
import { SceneSplitImageDropDownComponent } from './scene-split-image-drop-down.component';

describe('SceneSplitImageDropDownComponent', () => {
  let component: SceneSplitImageDropDownComponent;
  let fixture: ComponentFixture<SceneSplitImageDropDownComponent>;
  let storeSpy: jasmine.SpyObj<Store>;
  let configSpy: jasmine.SpyObj<ConfigService>;

  beforeEach(async () => {
    const storeMock = jasmine.createSpyObj('Store', ['dispatch']);
    const configMock = jasmine.createSpyObj('ConfigService', [], {
      maxSizeFile: 1024 * 1024 * 5,
      allowedImageTypes: ['image/png', 'image/jpeg']
    });

    await TestBed.configureTestingModule({
      imports: [ReactiveFormsModule],
      declarations: [SceneSplitImageDropDownComponent],
      providers: [
        { provide: Store, useValue: storeMock },
        { provide: ConfigService, useValue: configMock }
      ]
    }).compileComponents();

    storeSpy = TestBed.inject(Store) as jasmine.SpyObj<Store>;
    configSpy = TestBed.inject(ConfigService) as jasmine.SpyObj<ConfigService>;

    fixture = TestBed.createComponent(SceneSplitImageDropDownComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize formGroup on ngOnInit', () => {
    expect(component.formGroup).toBeTruthy();
    expect(component.fileInput).toBeTruthy();
    expect(component.fileInput.value).toBeNull();
  });

  it('should set fileInput and dispatch action for valid file', () => {
    const mockFile = new File(['dummy'], 'file.png', { type: 'image/png', lastModified: Date.now() });
    const event = { target: { files: [mockFile] } } as unknown as Event;

    spyOn(component, 'sendSceneImageFile');

    component.onFileSelected(event);

    expect(component.fileError).toBeNull();
    expect(component.fileInput.value).toBe(mockFile);
    expect(component.fileInput.touched).toBeTrue();
    expect(component.sendSceneImageFile).toHaveBeenCalled();
  });

  it('should show error if file exceeds max size', () => {
    const largeFile = new File(['dummy'], 'large.png', { type: 'image/png' });
    Object.defineProperty(largeFile, 'size', { value: configSpy.maxSizeFile + 1 });

    const event = { target: { files: [largeFile] } } as unknown as Event;

    spyOn(component, 'sendSceneImageFile');

    component.onFileSelected(event);

    expect(component.fileError).toContain('File size must be');
    expect(component.fileInput.value).toBeNull();
  });

  it('should show error if file type is not allowed', () => {
    const invalidFile = new File(['dummy'], 'file.gif', { type: 'image/gif' });
    const event = { target: { files: [invalidFile] } } as unknown as Event;

    spyOn(component, 'sendSceneImageFile');

    component.onFileSelected(event);

    expect(component.fileError).toBe('Only PNG and JPG files are allowed.');
    expect(component.fileInput.value).toBeNull();
  });

  it('should dispatch sendSceneImageFile action for valid file', () => {
    const validFile = new File(['dummy'], 'file.jpg', { type: 'image/jpeg' });
    const event = { target: { files: [validFile] } } as unknown as Event;

    component.onFileSelected(event);

    expect(storeSpy.dispatch).toHaveBeenCalledWith(sendSceneImageFile({ file: validFile }));
  });

  it('fileSizeStr should return formatted string', () => {
    expect(component.fileSizeStr).toBe(`${configSpy.maxSizeFile / (1024 * 1024)}MB`);
  });
});