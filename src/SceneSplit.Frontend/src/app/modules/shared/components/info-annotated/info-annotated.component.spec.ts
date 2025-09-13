import { CommonModule } from '@angular/common';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatButtonModule } from '@angular/material/button';
import { MAT_SNACK_BAR_DATA, MatSnackBarModule, MatSnackBarRef } from '@angular/material/snack-bar';
import { By } from '@angular/platform-browser';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { InfoAnnotatedComponent } from './info-annotated.component';

describe('InfoAnnotatedComponent', () => {
  let fixture: ComponentFixture<InfoAnnotatedComponent>;
  let mockSnackBarRef: MatSnackBarRef<InfoAnnotatedComponent>;

  beforeEach(async () => {
    mockSnackBarRef = jasmine.createSpyObj('MatSnackBarRef', ['dismissWithAction']);

    await TestBed.configureTestingModule({
      imports: [
        MatSnackBarModule,
        NoopAnimationsModule,
        CommonModule,
        MatButtonModule,
        InfoAnnotatedComponent
      ],
      providers: [
        { provide: MAT_SNACK_BAR_DATA, useValue: { message: 'Info 1' } },
        { provide: MatSnackBarRef, useValue: mockSnackBarRef }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(InfoAnnotatedComponent);
    fixture.detectChanges();
  });

  it('should display info message', () => {
    const messages = fixture.debugElement.queryAll(By.css('.text-base'));
    expect(messages.length).toBe(1);
    expect(messages[0].nativeElement.textContent).toContain('Info 1');
  });

  it('should call dismissWithAction on dismiss button click', () => {
    const button = fixture.debugElement.query(By.css('button'));
    button.nativeElement.click();
    expect(mockSnackBarRef.dismissWithAction).toHaveBeenCalled();
  });
});
